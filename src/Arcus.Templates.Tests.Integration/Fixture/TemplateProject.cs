using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        /// <summary>
        /// Gets the example project name passed to the project template.
        /// </summary>
        public const string ProjectName = "Arcus.Demo.Project";

        private readonly Process _process;
        private readonly DirectoryInfo _templateDirectory;

        private ProjectOptions _options;
        private bool _created, _started, _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateProject"/> class.
        /// </summary>
        /// <param name="templateDirectory">The file directory where the .NET project template is located.</param>
        /// <param name="fixtureDirectory">The file directory where the test fixtures for the project template are located..</param>
        /// <param name="outputWriter">The logger instance to write diagnostic trace messages during the lifetime of the test project.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="templateDirectory"/>, <paramref name="fixtureDirectory"/>, or <paramref name="outputWriter"/> is <c>null</c>.</exception>
        protected TemplateProject(DirectoryInfo templateDirectory, DirectoryInfo fixtureDirectory, ITestOutputHelper outputWriter)
        {
            Guard.NotNull(templateDirectory, nameof(templateDirectory), "Requires a file template directory where the .NET project template is located");
            Guard.NotNull(fixtureDirectory, nameof(fixtureDirectory), "Requires a file fixture directory where the test fixtures are located");
            Guard.NotNull(outputWriter, nameof(outputWriter), "Requires an logger instance to write diagnostic trace messages during the lifetime of the project.");

            _process = new Process();
            _templateDirectory = templateDirectory;

            string tempDirectoryPath = Path.Combine(Path.GetTempPath(), $"{ProjectName}-{Guid.NewGuid()}");
            ProjectDirectory = new DirectoryInfo(tempDirectoryPath);
            FixtureDirectory = fixtureDirectory;
            Logger = outputWriter;
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
            _options = projectOptions;
            TearDownOptions = projectOptions.TearDownOptions;

            string shortName = GetTemplateShortNameAtTemplateFolder();
            Logger.WriteLine($"Creates new project from template {shortName} at {ProjectDirectory.FullName}");

            RunDotNet($"new install {_templateDirectory.FullName} --force");

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
        /// Adds a file in the target project folder, using the given file <paramref name="contents"/>.
        /// </summary>
        /// <param name="fileName">The file name (no file path) of the new project file.</param>
        /// <param name="contents">The file contents to write to the project file.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="fileName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="contents"/> is <c>null</c>.</exception>
        public void AddFileInProject(string fileName, string contents)
        {
            Guard.NotNullOrWhitespace(fileName, nameof(fileName), "Requires a non-blank file name (no file path) to add the file");
            Guard.NotNull(contents, nameof(contents), "Requires contents to add to the project file");

            string destPath = Path.Combine(ProjectDirectory.FullName, fileName);
            File.WriteAllText(destPath, contents);
        }

        /// <summary>
        /// Updates a file in the target project folder with a 'using' statement for a given <paramref name="type"/>.
        /// </summary>
        /// <param name="fileName">The target file name to change its contents.</param>
        /// <param name="type">The type for which the namespace should be added as a 'using' statement.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="fileName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="type"/> is <c>null</c>.</exception>
        public void UpdateFileWithUsingStatement(string fileName, Type type)
        {
            Guard.NotNullOrWhitespace(fileName, nameof(fileName), "Requires a non-blank file name (no file path) to update the contents with a 'using' statement");
            Guard.NotNull(type, nameof(type), "Requires a type definition to retrieve the namespace for the 'using' statement");

            UpdateFileInProject(fileName, contents => $"using {type.Namespace};{Environment.NewLine}" + contents);
        }

        /// <summary>
        /// Updates a file in the target project folder, using the given <paramref name="updateContents"/> function.
        /// </summary>
        /// <param name="fileName">The target file name to change its contents.</param>
        /// <param name="updateContents">The function that changes the contents of the file.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="fileName"/> is blank.</exception>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="updateContents"/> is <c>null</c>.</exception>
        public void UpdateFileInProject(string fileName, Func<string, string> updateContents)
        {
            Guard.NotNullOrWhitespace(fileName, nameof(fileName), "Requires a non-blank file name (no file path) to update the contents");
            Guard.NotNull(updateContents, nameof(updateContents), "Requires a function to update the project file contents");

            string destPath = Path.Combine(ProjectDirectory.FullName, fileName);
            if (!File.Exists(destPath))
            {
                throw new FileNotFoundException($"No project file with the file name: '{fileName}' was found in the target project folder");
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
            namespaces = namespaces ?? Array.Empty<string>();

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

            CommandArgument[] runCommandArguments = DetermineRunCommandArguments(commandArguments);

            try
            {
                ProcessStartInfo processInfo = PrepareProjectRun(buildConfiguration, targetFramework, runCommandArguments);
                _process.StartInfo = processInfo;

                _started = true;
                _process.Start();
            }
            catch (Exception exception)
            {
                throw CreateProjectStartupFailure(exception);
            }
        }

        private CommandArgument[] DetermineRunCommandArguments(CommandArgument[] commandArguments)
        {
            CommandArgument[] defaultCommandArguments = commandArguments ?? Array.Empty<CommandArgument>();
            IEnumerable<CommandArgument> optionsCommandArguments =
                _options?.AdditionalRunArguments ?? Array.Empty<CommandArgument>();
            
            return defaultCommandArguments.Concat(optionsCommandArguments).ToArray();
        }

        /// <summary>
        /// Customized project process preparation that results in an <see cref="ProcessStartInfo"/> instance.
        /// </summary>
        /// <param name="buildConfiguration">The configuration to which the project should built.</param>
        /// <param name="targetFramework">The code framework to which this project targets to.</param>
        /// <param name="commandArguments">The CLI parameters which should be sent to the starting project.</param>
        /// <returns>
        ///     An run-ready <see cref="ProcessStartInfo"/> instance that will be used to start the project.
        /// </returns>
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

        /// <summary>
        /// Creates an user-friendly exception based on an occurred <paramref name="exception"/> to show and help the tester pinpoint the problem.
        /// </summary>
        /// <param name="exception">The occurred exception during the startup process of the test project based on the project template.</param>
        protected virtual CannotStartTemplateProjectException CreateProjectStartupFailure(Exception exception)
        {
            return new CannotStartTemplateProjectException(
                "Could start test project based on project template due to an exception occurred during the build/run process, " 
                + "please check for any compile errors or runtime failures (via the 'TearDownOptions') in the created test project based on the project template", exception);
        }

        /// <summary>
        /// Gets the identifier for the given <paramref name="targetFramework"/> (ex: 'netcoreapp3.1').
        /// </summary>
        /// <param name="targetFramework">The target framework for which the end result project from the project template is build.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when the <paramref name="targetFramework"/> is outside the bounds of the enumeration.</exception>
        protected static string GetTargetFrameworkIdentifier(TargetFramework targetFramework)
        {
            switch (targetFramework)
            {
                case TargetFramework.Net8_0: return "net8.0";
                default:
                    throw new ArgumentOutOfRangeException(nameof(targetFramework), targetFramework, "Unknown target framework specified for template project");
            }
        }

        /// <summary>
        /// Gets the file contents of the '.csproj' project file in the end-result project created from the project template.
        /// </summary>
        public string GetFileContentsOfProjectFile()
        {
            string fileContents = GetFileContentsInProject(ProjectName + ".csproj");
            return fileContents;
        }

        /// <summary>
        /// Gets the file contents of a <paramref name="fileName"/> located at the end-result project created from the project template.
        /// </summary>
        /// <param name="fileName">The file name to retrieve the file contents in the end-result project from the project template.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="fileName"/> is blank.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the <paramref name="fileName"/> doesn't point to a existing file in the end-result project from the project template.</exception>
        public string GetFileContentsInProject(string fileName)
        {
            Guard.NotNullOrWhitespace(fileName, nameof(fileName), "Requires a non-blank file name to retrieve the file contents in the end-result project from the project template");

            string filePath = Path.Combine(ProjectDirectory.FullName, fileName);
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(
                    $"Cannot find file within end-result project from project template at file path: '{filePath}', " 
                    +  "please make sure that the project template includes this file or that the test suite adds this file afterwards");
            }

            string fileContents = File.ReadAllText(filePath);
            return fileContents;
        }

        /// <summary>
        /// Determines whether or not a file is present in the resulting project directory.
        /// </summary>
        /// <param name="fileName">The file name (without path) that should be present in the resulting project directory.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="fileName"/> is blank.</exception>
        public bool ContainsFile(string fileName)
        {
            Guard.NotNullOrWhitespace(fileName, nameof(fileName), "Requires a non-blank file name to determine whether the file is present in the resulting project directory");

            string filePath = Path.Combine(ProjectDirectory.FullName, fileName);
            return File.Exists(filePath);
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
                RetryActionExceptWhen(_started && !TearDownOptions.HasFlag(TearDownOptions.KeepProjectRunning), StopProject),
                RetryActionExceptWhen(_created && !TearDownOptions.HasFlag(TearDownOptions.KeepProjectDirectory), DeleteProjectDirectory),
                RetryActionExceptWhen(!TearDownOptions.HasFlag(TearDownOptions.KeepProjectTemplateInstalled), UninstallTemplate),
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
            if (TearDownOptions.HasFlag(TearDownOptions.KeepProjectDirectory))
            {
                Logger.WriteLine("Keep project directory at: {0}", ProjectDirectory.FullName);
            }

            if (TearDownOptions.HasFlag(TearDownOptions.KeepProjectRunning))
            {
                Logger.WriteLine("Keep project running");
            }

            if (TearDownOptions.HasFlag(TearDownOptions.KeepProjectTemplateInstalled))
            {
                Logger.WriteLine("Keep project template template installed");
            }
        }

        /// <summary>
        /// Performs additional application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="disposing">The flag indicating whether or not the additional tasks should be disposed.</param>
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
            RunDotNet($"new uninstall {_templateDirectory.FullName}");
        }

        protected void RunDotNet(string command)
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

        private PolicyResult RetryActionExceptWhen(bool shouldRetry, Action action)
        {
            if (shouldRetry)
            {
                return Policy.Timeout(TimeSpan.FromSeconds(30))
                             .Wrap(Policy.Handle<Exception>()
                                         .WaitAndRetryForever(_ => TimeSpan.FromSeconds(1)))
                             .ExecuteAndCapture(action);
            }

            return Policy.NoOp().ExecuteAndCapture(() => { });
        }
    }
}
