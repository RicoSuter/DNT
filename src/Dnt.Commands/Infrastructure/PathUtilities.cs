using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Dnt.Commands.Infrastructure
{
    public class PathUtilities
    {
        /// <summary>Converts a relative path to an absolute path.</summary>
        /// <param name="relativePath">The relative path.</param>
        /// <param name="relativeTo">The current directory.</param>
        /// <returns>The absolute path.</returns>
        public static string ToAbsolutePath(string relativePath, string relativeTo)
        {
            if (Path.IsPathRooted(relativePath))
                return relativePath;

            var absolutePath = Path.Combine(relativeTo, relativePath);
            return Path.GetFullPath(absolutePath);
        }

        /// <summary>Converts an absolute path to a relative path if possible.</summary>
        /// <param name="absolutePath">The absolute path.</param>
        /// <param name="relativeTo">The current directory.</param>
        /// <returns>The relative path.</returns>
        /// <exception cref="ArgumentException">The path of the two files doesn't have any common base.</exception>
        public static string ToRelativePath(string absolutePath, string relativeTo)
        {
            var absoluteSegments = absolutePath.Split(Path.DirectorySeparatorChar);
            var relativeSegments = relativeTo.Split(Path.DirectorySeparatorChar);

            var lastCommonRootIndex = FindLastCommonRootIndex(absoluteSegments, relativeSegments);
            if (lastCommonRootIndex == -1)
                return absolutePath;

            var relativePath = new StringBuilder();

            AddDirectoryBacks(lastCommonRootIndex, relativeSegments, relativePath);
            AddDirectories(lastCommonRootIndex, absoluteSegments, relativePath);

            relativePath.Append(absoluteSegments[absoluteSegments.Length - 1]);
            return relativePath.ToString();
        }

        private static void AddDirectories(int lastCommonRootIndex, string[] absoluteSegments, StringBuilder relativePath)
        {
            for (var index = lastCommonRootIndex + 1; index < absoluteSegments.Length - 1; index++)
            {
                relativePath.Append(absoluteSegments[index]);
                relativePath.Append(Path.DirectorySeparatorChar);
            }
        }

        private static void AddDirectoryBacks(int lastCommonRootIndex, string[] relativeSegments, StringBuilder relativePath)
        {
            for (var index = lastCommonRootIndex + 1; index < relativeSegments.Length; index++)
            {
                relativePath.Append("..");
                relativePath.Append(Path.DirectorySeparatorChar);
            }
        }

        private static int FindLastCommonRootIndex(IReadOnlyList<string> absoluteSegments, IReadOnlyList<string> relativeSegments)
        {
            int index;
            var lastCommonRoot = -1;
            var length = absoluteSegments.Count < relativeSegments.Count ? absoluteSegments.Count : relativeSegments.Count;
            for (index = 0; index < length; index++)
            {
                if (absoluteSegments[index].Equals(relativeSegments[index], StringComparison.OrdinalIgnoreCase))
                    lastCommonRoot = index;
                else
                    break;
            }
            return lastCommonRoot;
        }
    }
}