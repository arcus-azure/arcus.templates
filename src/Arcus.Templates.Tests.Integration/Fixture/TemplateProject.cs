using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private const string ProjectName = "Arcus.Demo.Project";

        private readonly Process _process;
        private readonly DirectoryInfo _templateDirectory;
        private readonly ITestOutputHelper _outputWriter;

        private bool _created, _started, _disposed;

        protected TemplateProject(DirectoryInfo templateDirectory, ITestOutputHelper outputWriter)
        {
            _process = new Process();
            _templateDirectory = templateDirectory;
            _outputWriter = outputWriter;

            WorkingDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"{ProjectName}-{Guid.NewGuid()}"));;
        }

        /// <summary>
        /// Gets the directory where the project from this template will be created.
        /// </summary>
        public DirectoryInfo WorkingDirectory { get; }

        /// <summary>
        /// Creates a new project from this template at the <see cref="WorkingDirectory"/> with a given set of <paramref name="projectOptions"/>.
        /// </summary>
        /// <param name="projectOptions">The console arguments that controls the creation process of the to-be-created project.</param>
        public void CreateNewProject(ProjectOptions projectOptions)
        {
            if (_created)
            {
                return;
            }

            _created = true;

            string shortName = GetTemplateShortNameAtTemplateFolder(_templateDirectory);
            _outputWriter.WriteLine($"Creates new project from template {shortName} at {WorkingDirectory.FullName}");
            
            RunDotNet(
                $"new -i {_templateDirectory.FullName}", 
                $"Cannot install custom project template at: {_templateDirectory.FullName}");

            string commandArguments = projectOptions.ToCommandLineArguments();
            RunDotNet(
                $"new {shortName} {commandArguments ?? String.Empty} -n {ProjectName} -o {WorkingDirectory.FullName}", 
                $"Cannot create an project from the custom {shortName} project template");

            projectOptions.UpdateProjectToCorrectlyUseOptions(WorkingDirectory);
        }

        private static string GetTemplateShortNameAtTemplateFolder(DirectoryInfo templateDir)
        {
            var templateFile = new FileInfo(Path.Combine(templateDir.FullName, ".template.config", "template.json"));
            if (!templateFile.Exists)
            {
                throw new FileNotFoundException(
                    $"Cannot find a correct template project at: {templateDir.FullName} because no './.template.config/template.json' was found");
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
        /// <param name="commandArguments">The command line arguments that control the startup of the project.</param>
        protected void Run(string buildConfiguration, string commandArguments)
        {
            if (_started)
            {
                throw new InvalidOperationException("Test demo project from template is already started");
            }

            RunDotNet($"build -c {buildConfiguration} {WorkingDirectory.FullName}", "Cannot build created project from template");
            
            string runCommand = $"exec {Path.Combine(WorkingDirectory.FullName, $"bin/{buildConfiguration}/netcoreapp2.2/{ProjectName}.dll")} {commandArguments ?? String.Empty}";
            _outputWriter.WriteLine("> dotnet {0}", runCommand);
            var processInfo = new ProcessStartInfo("dotnet", runCommand)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = WorkingDirectory.FullName,
            };

            _process.StartInfo = processInfo;

            _started = true;
            _process.Start();
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

            PolicyResult[] results = 
            {
                Policy.NoOp().ExecuteAndCapture(() => Disposing(true)),
                RetryAction(StopProject),
                RetryAction(UninstallTemplate),
                RetryAction(DeleteProjectDirectory), 
            };

            IEnumerable<Exception> exceptions = 
                results.Where(result => result.Outcome == OutcomeType.Failure)
                       .Select(result => result.FinalException);

            if (exceptions.Any())
            {
                throw new AggregateException(exceptions);
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

            foreach (Process process in Process.GetProcessesByName(".NET Core Host"))
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
        }

        private void UninstallTemplate()
        {
            RunDotNet(
                $"new -u {_templateDirectory.FullName}", 
                $"Cannot uninstall custom project template at: {_templateDirectory.FullName}");
        }

        private void RunDotNet(string command, string exceptionMessage)
        {
            try
            {
                _outputWriter.WriteLine("> dotnet {0}", command);
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
            Directory.Delete(WorkingDirectory.FullName, recursive: true);
        }

        private static PolicyResult RetryAction(Action action)
        {
            return Policy.Timeout(TimeSpan.FromSeconds(30))
                         .Wrap(Policy.Handle<IOException>()
                                     .WaitAndRetryForever(_ => TimeSpan.FromSeconds(1)))
                         .ExecuteAndCapture(action);
        }
    }
}
