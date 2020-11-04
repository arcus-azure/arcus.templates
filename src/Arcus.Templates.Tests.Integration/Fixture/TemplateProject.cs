using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using GuardNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Xunit.Abstractions;

namespace Arcus.Templates.Tests.Integration.Fixture
{
    /// <summary>
    /// Skeleton implementation of a project template from which new projects can be created.
    /// </summary>
    public abstract class TemplateProject : IDisposable
    {
        protected const string ProjectName = "Arcus.Demo.Project";

        private readonly Process _process;
        private readonly DirectoryInfo _templateDirectory;

        private bool _created, _started, _disposed;

        protected TemplateProject(DirectoryInfo templateDirectory, DirectoryInfo fixtureDirectory, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(templateDirectory, nameof(templateDirectory));
            Guard.NotNull(fixtureDirectory, nameof(fixtureDirectory));
            Guard.NotNull(outputWriter, nameof(outputWriter));

            _process = new Process();
            _templateDirectory = templateDirectory;
            Logger = outputWriter;

            string tempDirectoryPath = Path.Combine(Path.GetTempPath(), $"{ProjectName}-{Guid.NewGuid()}");
            ProjectDirectory = new DirectoryInfo(tempDirectoryPath);
            FixtureDirectory = fixtureDirectory;
        }

        /// <summary>
        /// Gets the directory where the fixtures are located that are used when a project requires additional functionality.
        /// </summary>
        protected DirectoryInfo FixtureDirectory { get; }

        /// <summary>
        /// Gets the directory where a new project is created from the template.
        /// </summary>
        protected DirectoryInfo ProjectDirectory { get; }

        /// <summary>
        /// Gets the output logger to add telemetry information during the creation, startup and breakdown process.
        /// </summary>
        protected ITestOutputHelper Logger { get; }

        /// <summary>
        /// Sets the options to control how the template project should be tear down during the <see cref="Dispose"/>.
        /// </summary>
        public TearDownOptions TearDownOptions { private get; set; } = TearDownOptions.None;

        /// <summary>
        /// Creates a new project from this template at the project directory with a given set of <paramref name="projectOptions"/>.
        /// </summary>
        /// <param name="projectOptions">The console arguments that controls the creation process of the to-be-created project.</param>
        protected void CreateNewProject(ProjectOptions projectOptions)
        {
            if (_created)
            {
                return;
            }

            _created = true;

            string shortName = GetTemplateShortNameAtTemplateFolder();
            Logger.WriteLine($"Creates new project from template {shortName} at {ProjectDirectory.FullName}");

            RunDotNet($"new -i {_templateDirectory.FullName}");

            string commandArguments = projectOptions.ToCommandLineArguments();
            RunDotNet($"new {shortName} {commandArguments ?? String.Empty} -n {ProjectName} -o {ProjectDirectory.FullName}");

            projectOptions.UpdateProjectToCorrectlyUseOptions(FixtureDirectory, ProjectDirectory);
        }

        private string GetTemplateShortNameAtTemplateFolder()
        {
            var templateFile = new FileInfo(Path.Combine(_templateDirectory.FullName, ".template.config", "template.json"));
            if (!templateFile.Exists)
            {
                throw new FileNotFoundException(
                    $"Cannot find a correct template project at: {_templateDirectory.FullName} because no './.template.config/template.json' was found");
            }

            string json = File.ReadAllText(templateFile.FullName);
            JObject templateJson = JObject.Parse(json);
            if (!templateJson.TryGetValue("shortName", out JToken shortNameJson))
            {
                throw new JsonException(
                    "Cannot create a custom project from the template project because no 'shortName' element found in the template.json file");
            }

            return shortNameJson.Value<string>();
        }

        /// <summary>
        /// Removes the custom error lines from the file <paramref name="content"/> in the project.
        /// </summary>
        /// <param name="content">The contents to remove the error lines from.</param>
        /// <returns>
        ///     The content without any error lines.
        /// </returns>
        protected string RemovesUserErrorsFromContents(string content)
        {
            return content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
                          .Where(line => !line.Contains("#error"))
                          .Aggregate((line1, line2) => line1 + Environment.NewLine + line2);
        }

        /// <summary>
        /// Add a package to the created project from the template.
        /// </summary>
        /// <param name="packageName">The name of the package.</param>
        /// <param name="packageVersion">The version of the package.</param>
        protected void AddPackage(string packageName, string packageVersion)
        {
            Guard.NotNullOrWhitespace(packageName, nameof(packageName), "Cannot add a package with a blank name");
            Guard.NotNullOrWhitespace(packageVersion, nameof(packageVersion), "Cannot add a package with a blank version");

            RunDotNet($"add {Path.Combine(ProjectDirectory.FullName, ProjectName)}.csproj package {packageName} -v {packageVersion}");
        }

        /// <summary>
        /// Updates a file in the target project folder, using the given <paramref name="updateContents"/> function.
        /// </summary>
        /// <param name="fileName">The target file name to change it's contents.</param>
        /// <param name="updateContents">The function that changes the contents of the file.</param>
        public void UpdateFileInProject(string fileName, Func<string, string> updateContents)
        {
            Guard.NotNull(fileName, nameof(fileName), "Requires a file name (no file path) to update the contents");
            Guard.NotNull(updateContents, nameof(updateContents), "Requires a function to update the project file contents");

            string destPath = Path.Combine(ProjectDirectory.FullName, fileName);
            if (!File.Exists(destPath))
            {
                string files = String.Join(", ", ProjectDirectory.GetFiles().Select(f => f.FullName));
                throw new FileNotFoundException($"No project file with the file name: '{fileName}' was found in the target project folder '{ProjectDirectory.FullName}' ({files})");
            }

            string content = File.ReadAllText(destPath);
            content = updateContents(content);
            File.WriteAllText(destPath, content);
        }

        /// <summary>
        /// Adds a fixture file to the web API project by its type: <typeparamref name="TFixture"/>,
        /// and replace tokens with values via the given <paramref name="replacements"/> dictionary.
        /// </summary>
        /// <typeparam name="TFixture">The fixture type to include in the template project.</typeparam>
        /// <param name="replacements">The tokens and their corresponding values to replace in the fixture file.</param>
        /// <param name="namespaces">The additional namespace the fixture file should be placed in.</param>
        public void AddTypeAsFile<TFixture>(IDictionary<string, string> replacements = null, params string[] namespaces)
        {
            replacements = replacements ?? new Dictionary<string, string>();
            namespaces = namespaces ?? new string[0];

            string srcPath = FindFixtureTypeInDirectory(FixtureDirectory, typeof(TFixture));
            string destPath = Path.Combine(ProjectDirectory.FullName, Path.Combine(namespaces), typeof(TFixture).Name + ".cs");
            File.Copy(srcPath, destPath);

            string key = typeof(TFixture).Namespace ?? throw new InvalidOperationException("Generic fixture requires a namespace");
            string value = namespaces.Length == 0 ? ProjectName : $"{ProjectName}.{String.Join(".", namespaces)}";
            replacements[key] = value;

            string content = File.ReadAllText(destPath);
            content = replacements.Aggregate(content, (txt, kv) => txt.Replace(kv.Key, kv.Value));
            
            File.WriteAllText(destPath, content);
        }

        private static string FindFixtureTypeInDirectory(DirectoryInfo fixtureDirectory, Type fixtureType)
        {
            string fixtureFileName = fixtureType.Name + ".cs";
            IEnumerable<FileInfo> files = 
                fixtureDirectory.EnumerateFiles(fixtureFileName, SearchOption.AllDirectories);

            if (!files.Any())
            {
                throw new FileNotFoundException(
                    $"Cannot find fixture with file name: {fixtureFileName} in directory: {fixtureDirectory.FullName}", 
                    fixtureFileName);
            }

            if (files.Count() > 1)
            {
                throw new IOException(
                    $"More than a single fixture matches the file name: {fixtureFileName} in directory: {fixtureDirectory.FullName}");
            }

            return files.First().FullName;
        }

        /// <summary>
        /// Run the created project from the template with a given set of <paramref name="commandArguments"/>.
        /// </summary>
        /// <param name="buildConfiguration">The build configuration on which the project should be build.</param>
        /// <param name="targetFramework">The target framework in which the project should be build and run.</param>
        /// <param name="commandArguments">The command line arguments that control the startup of the project.</param>
        protected void Run(BuildConfiguration buildConfiguration, TargetFramework targetFramework, params CommandArgument[] commandArguments)
        {
            if (_started)
            {
                throw new InvalidOperationException("Test demo project from template is already started");
            }

            commandArguments = commandArguments ?? new CommandArgument[0];

            ProcessStartInfo processInfo = PrepareProjectRun(buildConfiguration, targetFramework, commandArguments);
            _process.StartInfo = processInfo;

            _started = true;
            _process.Start();
        }

        protected virtual ProcessStartInfo PrepareProjectRun(BuildConfiguration buildConfiguration, TargetFramework targetFramework, CommandArgument[] commandArguments)
        {
            RunDotNet($"build -c {buildConfiguration} {ProjectDirectory.FullName}");

            string targetFrameworkIdentifier = GetTargetFrameworkIdentifier(targetFramework);
            string targetAssembly = Path.Combine(ProjectDirectory.FullName, $"bin/{buildConfiguration}/{targetFrameworkIdentifier}/{ProjectName}.dll");
            string exposedSecretsCommands = String.Join(" ", commandArguments.Select(arg => arg.ToExposedString()));
            string runCommand = $"exec {targetAssembly} {exposedSecretsCommands}";

            string hiddenSecretsCommands = String.Join(" ", commandArguments.Select(arg => arg.ToString()));
            Logger.WriteLine("> dotnet {0}", $"exec {targetAssembly} {hiddenSecretsCommands}");

            var processInfo = new ProcessStartInfo("dotnet", runCommand)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = ProjectDirectory.FullName,
            };

            return processInfo;
        }

        private static string GetTargetFrameworkIdentifier(TargetFramework targetFramework)
        {
            switch (targetFramework)
            {
                case TargetFramework.NetCoreApp31: return "netcoreapp3.1";
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetFramework), targetFramework, "Unknown target framework specified for template project");
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            LogTearDownAction();

            PolicyResult[] results =
            {
                Policy.NoOp().ExecuteAndCapture(() => Disposing(true)),
                RetryActionExceptWhen(TearDownOptions.KeepProjectRunning, StopProject),
                RetryActionExceptWhen(TearDownOptions.KeepProjectDirectory, DeleteProjectDirectory),
                RetryActionExceptWhen(TearDownOptions.KeepProjectTemplateInstalled, UninstallTemplate),
            };

            IEnumerable<Exception> exceptions =
                results.Where(result => result.Outcome == OutcomeType.Failure)
                       .Select(result => result.FinalException);

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
            }
        }

        private void LogTearDownAction()
        {
            if ((TearDownOptions & TearDownOptions.KeepProjectDirectory) == TearDownOptions.KeepProjectDirectory)
            {
                Logger.WriteLine("Keep project directory at: {0}", ProjectDirectory.FullName);
            }

            if ((TearDownOptions & TearDownOptions.KeepProjectRunning) == TearDownOptions.KeepProjectRunning)
            {
                Logger.WriteLine("Keep project running");
            }

            if ((TearDownOptions & TearDownOptions.KeepProjectTemplateInstalled) == TearDownOptions.KeepProjectTemplateInstalled)
            {
                Logger.WriteLine("Keep project template template installed");
            }
        }

        protected virtual void Disposing(bool disposing)
        {
        }

        private void StopProject()
        {
            if (!_process.HasExited)
            {
                _process.Kill(entireProcessTree: true);
            }

            _process.Dispose();
        }

        private void UninstallTemplate()
        {
            RunDotNet($"new -u {_templateDirectory.FullName}");
        }

        private void RunDotNet(string command)
        {
            RunCommand("dotnet", command);
        }

        protected void RunCommand(string fileName, string arguments)
        {
            try
            {
                Logger.WriteLine("> {0} {1}", fileName, arguments);
            }
            catch
            {
                Console.WriteLine("> {0} {1}", fileName, arguments);
            }

            var startInfo = new ProcessStartInfo(fileName, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();

                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
        }

        private void DeleteProjectDirectory()
        {
            ProjectDirectory.Delete(recursive: true);
        }

        private PolicyResult RetryActionExceptWhen(TearDownOptions tearDownOptions, Action action)
        {
            if ((TearDownOptions & tearDownOptions) == tearDownOptions)
            {
                return Policy.NoOp().ExecuteAndCapture(() => { });
            }

            return Policy.Timeout(TimeSpan.FromSeconds(30))
                         .Wrap(Policy.Handle<Exception>()
                                     .WaitAndRetryForever(_ => TimeSpan.FromSeconds(1)))
                         .ExecuteAndCapture(action);
        }
    }
}
