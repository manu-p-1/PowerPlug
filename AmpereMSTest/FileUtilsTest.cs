using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Ampere.FileUtils.FileUtils;
//USE THIS NAMESPACE FOR TESTING METHODS
namespace PowerPlugMSTest
{
    [TestClass]
    public class FileUtilsTest
    {
        [TestMethod]
        public void GetRootPathTest()
        {
            Assert.AreEqual(System.IO.Path.GetPathRoot(System.Environment.SystemDirectory), GetRootPath());
        }
    }
}
