using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Arcus.Templates.Tests.Integration.WebApi;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.Fixture 
{
    /// <summary>
    /// Represents all the available project options on the a template project.
    /// </summary>
    public class ProjectOptions
    {
        private readonly IEnumerable<string> _arguments;
        private readonly  IEnumerable<Action<DirectoryInfo, DirectoryInfo>> _updateProject;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiProjectOptions"/> class.
        /// </summary>
        public ProjectOptions() 
            : this(Enumerable.Empty<string>(),
                   Enumerable.Empty<Action<DirectoryInfo, DirectoryInfo>>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectOptions"/> class.
        /// </summary>
        public ProjectOptions(ProjectOptions options) 
            : this(options?._arguments, 
                  options?._updateProject)
        {
        }

        private ProjectOptions(
            IEnumerable<string> arguments,
            IEnumerable<Action<DirectoryInfo, DirectoryInfo>> updateProject)
        {
            Guard.NotNull(arguments, nameof(arguments), "Cannot create web API project without project options");
            Guard.NotNull(updateProject, nameof(updateProject), "Cannot create web API project without post-project-created actions");

            _arguments = arguments;
            _updateProject = updateProject;
        }

        /// <summary>
        /// Adds an option to the project that should be added for a project template.
        /// </summary>
        /// <param name="argument">The console argument to pass along the 'dotnet new' command.</param>
        protected ProjectOptions AddOption(string argument)
        {
            Guard.NotNullOrWhitespace(argument, nameof(argument));

            return new ProjectOptions(_arguments.Append(argument), _updateProject);
        }

        /// <summary>
        /// Adds an option to the project that should be added for a project template.
        /// </summary>
        /// <param name="argument">The console argument to pass along the 'dotnet new' command.</param>
        /// <param name="updateProject">The custom action to be executed in order that the created project uses the project option correctly.</param>
        protected ProjectOptions AddOption(string argument, Action<DirectoryInfo, DirectoryInfo> updateProject)
        {
            Guard.NotNullOrWhitespace(argument, nameof(argument));

            return new ProjectOptions(
                _arguments.Append(argument),
                _updateProject.Append(updateProject));
        }

        /// <summary>
        /// Converts all the project options to a command line representation that can be passed along a 'dotnet new' command.
        /// </summary>
        internal string ToCommandLineArguments()
        {
            return String.Join(" ", _arguments);
        }

        /// <summary>
        /// Update the created project at the given <paramref name="projectDirectory"/> with a set of custom action in order that the created project is using the options correctly.
        /// After this update the project is ready to start.
        /// </summary>
        /// <param name="fixtureDirectory">The project directory where the fixtures for the newly created project is located.</param>
        /// <param name="projectDirectory">The project directory where the newly project from a template is located.</param>
        internal void UpdateProjectToCorrectlyUseOptions(DirectoryInfo fixtureDirectory, DirectoryInfo projectDirectory)
        {
            Guard.NotNull(projectDirectory, nameof(projectDirectory), "Cannot execute any post-create custom actions without a project directory");
            Guard.For<ArgumentException>(() => !projectDirectory.Exists, "Cannot execute any post-create custom action on a project directory that doesn't exists on disk");

            foreach (Action<DirectoryInfo, DirectoryInfo> postBuildAction in _updateProject)
            {
                postBuildAction(fixtureDirectory, projectDirectory);
            }
        }
    }
}