using System.IO;
using Microsoft.Build.Evaluation;

namespace Dnt.Commands
{
    public static class ProjectExtensions
    {
        public static bool GeneratesPackage(this Project project)
        {
            return project.GetProperty("GeneratePackageOnBuild")?.EvaluatedValue.ToLowerInvariant() == "true";
        }

        public static bool HasVersion(this Project project)
        {
            var data = File.ReadAllText(project.FullPath);
            return data.Contains("<Version>");
            //return project.Properties.Any(i => i.Name == "Version" && !string.IsNullOrEmpty(i.UnevaluatedValue));
        }
    }
}
