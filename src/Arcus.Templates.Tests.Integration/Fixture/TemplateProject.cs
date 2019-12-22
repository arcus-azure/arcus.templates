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
        public void CreateNewProject(ProjectOptions projectOptions)
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
        /// Run the created project from the template with a given set of <paramref name="commandArguments"/>.
        /// </summary>
        /// <param name="buildConfiguration">The build configuration on which the project should be build.</param>
        /// <param name="targetFramework">The target framework in which the project should be build and run.</param>
        /// <param name="commandArguments">The command line arguments that control the startup of the project.</param>
        protected void Run(BuildConfiguration buildConfiguration, TargetFramework targetFramework, string commandArguments)
        {
            if (_started)
            {
                throw new InvalidOperationException("Test demo project from template is already started");
            }

            RunDotNet($"build -c {buildConfiguration} {ProjectDirectory.FullName}");

            string targetFrameworkIdentifier = GetTargetFrameworkIdentifier(targetFramework);
            string targetAssembly = Path.Combine(ProjectDirectory.FullName, $"bin/{buildConfiguration}/{targetFrameworkIdentifier}/{ProjectName}.dll");
            string runCommand = $"exec {targetAssembly} {commandArguments ?? String.Empty}";

            Logger.WriteLine("> dotnet {0}", runCommand);
            var processInfo = new ProcessStartInfo("dotnet", runCommand)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = ProjectDirectory.FullName,
            };

            _process.StartInfo = processInfo;

            _started = true;
            _process.Start();
        }

        private static string GetTargetFrameworkIdentifier(TargetFramework targetFramework)
        {
            switch (targetFramework)
            {
                case TargetFramework.NetCoreApp22: return "netcoreapp2.2";
                case TargetFramework.NetCoreApp30: return "netcoreapp3.0";
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
                _process.Kill();
            }

            _process.Dispose();
        }

        private void UninstallTemplate()
        {
            RunDotNet($"new -u {_templateDirectory.FullName}");
        }

        private void RunDotNet(string command)
        {
            try
            {
                Logger.WriteLine("> dotnet {0}", command);
            }
            catch
            {
                Console.WriteLine("> dotnet {0}", command);
            }

            var startInfo = new ProcessStartInfo("dotnet", command)
            {
                CreateNoWindow = true,
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();

                if (!process.HasExited)
                {
                    process.Kill();
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
