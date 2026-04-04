using System.IO;
using Dnt.Commands.Infrastructure;
using Xunit;

namespace Dnt.Tests
{
    public class PathUtilitiesTests
    {
        [Fact]
        public void ToAbsolutePath_WithAbsolutePath_ReturnsSamePath()
        {
            var absolutePath = Path.Combine(Path.GetTempPath(), "test", "file.csproj");
            var result = PathUtilities.ToAbsolutePath(absolutePath, "/some/other/dir");
            Assert.Equal(absolutePath, result);
        }

        [Fact]
        public void ToAbsolutePath_WithRelativePath_CombinesWithBasePath()
        {
            var basePath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
            var result = PathUtilities.ToAbsolutePath("sub" + Path.DirectorySeparatorChar + "file.csproj", basePath);
            Assert.Equal(Path.Combine(basePath, "sub", "file.csproj"), result);
        }

        [Fact]
        public void ToRelativePath_WithCommonRoot_ReturnsRelative()
        {
            var sep = Path.DirectorySeparatorChar;
            var absolutePath = $"{sep}root{sep}projects{sep}lib{sep}file.cs";
            var relativeTo = $"{sep}root{sep}projects{sep}app";
            var result = PathUtilities.ToRelativePath(absolutePath, relativeTo);
            Assert.Equal($"..{sep}lib{sep}file.cs", result);
        }

        [Fact]
        public void ToRelativePath_SameDirectory_ReturnsFileName()
        {
            var sep = Path.DirectorySeparatorChar;
            var absolutePath = $"{sep}root{sep}projects{sep}file.cs";
            var relativeTo = $"{sep}root{sep}projects";
            var result = PathUtilities.ToRelativePath(absolutePath, relativeTo);
            Assert.Equal("file.cs", result);
        }
    }
}
