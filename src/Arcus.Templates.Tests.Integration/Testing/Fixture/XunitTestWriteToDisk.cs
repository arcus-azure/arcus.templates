using System.IO;
using Xunit;

namespace Arcus.Templates.Tests.Integration.Testing.Fixture
{
    public class XunitTestWriteToDisk
    {
        [Fact]
        public void Test_WriteToDisk()
        {
            var dir = Directory.GetCurrentDirectory();
            string path = Path.Combine(dir, "xunit.txt");
            File.WriteAllText(path, "Hello, from xUnit!");
        }
    }
}
