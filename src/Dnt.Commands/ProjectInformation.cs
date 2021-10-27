using System;
using Microsoft.Build.Evaluation;

namespace Dnt.Commands
{
    public class ProjectInformation : IDisposable
    {
        private readonly ProjectCollection _projectCollection;

        public ProjectInformation(ProjectCollection projectCollection, Project project, bool isLegacyProject, string lineEndings)
        {
            _projectCollection = projectCollection;
            Project = project;
            IsLegacyProject = isLegacyProject;
            LineEndings = lineEndings;
        }

        public Project Project { get; }

        public bool IsLegacyProject { get; }

        public string LineEndings { get; }

        public bool IsSdkProject => !IsLegacyProject;

        public void Dispose()
        {
            _projectCollection.Dispose();
        }
    }
}
