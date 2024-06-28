using System.IO;
using NUnit.Framework;

namespace Arcus.Templates.Tests.Integration.Testing.Fixture
{
    public class NUnitTestWriteToDisk
    {
        [Test]
        public void Test_WriteToDisk()
        {
            var dir = Directory.GetCurrentDirectory();
            string path = Path.Combine(dir, "nunit.txt");
            File.WriteAllText(path, "Hello, from NUnit!");
        }
    }
}
