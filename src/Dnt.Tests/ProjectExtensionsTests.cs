using Dnt.Commands;
using Xunit;

namespace Dnt.Tests
{
    public class ProjectExtensionsTests
    {
        [Theory]
        [InlineData("MyProject.csproj", true)]
        [InlineData("MyProject.vbproj", true)]
        [InlineData("MyProject.CSPROJ", true)]
        [InlineData("MyProject.VbProj", true)]
        [InlineData("/path/to/MyProject.csproj", true)]
        [InlineData("MyProject.sqlproj", false)]
        [InlineData("MyProject.dcproj", false)]
        [InlineData("MyProject.fsproj", false)]
        [InlineData("MyProject.wixproj", false)]
        [InlineData("", false)]
        public void IsSupportedProject_ReturnsExpectedResult(string path, bool expected)
        {
            Assert.Equal(expected, ProjectExtensions.IsSupportedProject(path));
        }
    }
}
