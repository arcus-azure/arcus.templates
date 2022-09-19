using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.Fixture 
{
    /// <summary>
    /// Represents all the available project options on the a template project.
    /// </summary>
    public class ProjectOptions
    {
        private readonly ICollection<string> _arguments = new Collection<string>();
        private readonly ICollection<Action<DirectoryInfo, DirectoryInfo>> _updateProject = new Collection<Action<DirectoryInfo, DirectoryInfo>>();
        private readonly List<CommandArgument> _additionalRunArguments = new List<CommandArgument>();

        /// <summary>
        /// Gets the additional console arguments to pass along the 'dotnet run' command.
        /// These arguments are related to the project options that were configured for the project.
        /// </summary>
        public IEnumerable<CommandArgument> AdditionalRunArguments => _additionalRunArguments.ToArray();

        /// <summary>
        /// Adds an option to the project that should be added for a project template.
        /// </summary>
        /// <param name="optionArgument">The console argument to pass along the 'dotnet new' command.</param>
        /// <param name="additionalRunArguments">The additional console arguments to pass along the 'dotnet run' command.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="optionArgument"/> is blank.</exception>
        protected ProjectOptions AddOption(string optionArgument, params CommandArgument[] additionalRunArguments)
        {
            Guard.NotNullOrWhitespace(optionArgument, nameof(optionArgument), "Requires a console argument to pass along to the 'dotnet new' command");

            _arguments.Add(optionArgument);
            _additionalRunArguments.AddRange(additionalRunArguments);

            return this;
        }

        /// <summary>
        /// Adds an option to the project that should be added for a project template.
        /// </summary>
        /// <param name="optionArgument">The console argument to pass along the 'dotnet new' command.</param>
        /// <param name="updateProject">The custom action to be executed in order that the created project uses the project option correctly.</param>
        /// <param name="additionalRunArguments">The additional console arguments to pass along the 'dotnet run' command.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="optionArgument"/> is blank.</exception>
        protected ProjectOptions AddOption(string optionArgument, Action<DirectoryInfo, DirectoryInfo> updateProject, params CommandArgument[] additionalRunArguments)
        {
            Guard.NotNullOrWhitespace(optionArgument, nameof(optionArgument), "Requires a console argument to pass along to the 'dotnet new' command");
            Guard.NotNull(updateProject, nameof(updateProject), "Requires an update action to alter the project's content so that the created project uses the project option correctly");

            _arguments.Add(optionArgument);
            _updateProject.Add(updateProject);
            _additionalRunArguments.AddRange(additionalRunArguments);
            
            return this;
        }

        /// <summary>
        /// Adds an additional run argument to pass along the 'dotnet run' command.
        /// </summary>
        /// <param name="runArgument">The open/secret command argument to pass along.</param>
        /// <exception cref="ArgumentNullException">Thrown when the <paramref name="runArgument"/> is <c>null</c>.</exception>
        protected void AddRunArgument(CommandArgument runArgument)
        {
            Guard.NotNull(runArgument, nameof(runArgument), "Requires a command run argument to pass along the project 'dotnet run' command");
            _additionalRunArguments.Add(runArgument);
        }

        /// <summary>
        /// Removes a previously registered run argument(s) that should not be passed along the 'dotnet run' command.
        /// </summary>
        /// <param name="argumentName">The argument name of the open/secret command argument that should not be passed along.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="argumentName"/> is blank.</exception>
        protected void RemoveRunArgument(string argumentName)
        {
            Guard.NotNullOrWhitespace(argumentName, nameof(argumentName), "Requires a name of the command run argument that should not be passed along to the project 'dotnet run' command");
            _additionalRunArguments.RemoveAll(cmd => cmd.Name == argumentName);
        }

        /// <summary>
        /// Converts all the project options to a command line representation that can be passed along a 'dotnet new' command.
        /// </summary>
        internal string ToCommandLineArguments()
        {
            return string.Join(" ", _arguments);
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