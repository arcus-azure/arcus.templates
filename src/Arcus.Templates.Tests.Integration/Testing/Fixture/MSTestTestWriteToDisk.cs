using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Arcus.Templates.Tests.Integration.Testing.Fixture
{
    [TestClass]
    public class MSTestTestWriteToDisk
    {
        [TestMethod]
        public void Test_WriteToDisk()
        {
            var dir = Directory.GetCurrentDirectory();
            string path = Path.Combine(dir, "mstest.txt");
            File.WriteAllText(path, "Hello, from MSTest!");
        }
    }
}
