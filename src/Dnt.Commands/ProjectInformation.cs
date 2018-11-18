using System;
using Microsoft.Build.Evaluation;

namespace Dnt.Commands
{
    public class ProjectInformation : IDisposable
    {
        private readonly ProjectCollection _projectCollection;

        public ProjectInformation(ProjectCollection projectCollection, Project project, bool isLegacyProject)
        {
            _projectCollection = projectCollection;
            Project = project;
            IsLegacyProject = isLegacyProject;
        }

        public Project Project { get; }

        public bool IsLegacyProject { get; }

        public bool IsSdkProject => !IsLegacyProject;

        public void Dispose()
        {
            _projectCollection.Dispose();
        }
    }
}
