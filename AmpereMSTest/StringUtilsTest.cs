using System.Collections.Generic;
using System.Text;
using Ampere.StringUtils;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PowerPlugMSTest
{
    [TestClass]
    public class StringUtilsTest
    {
        /// <summary>
        /// Gets or sets the test context which provides
        /// information about and functionality for the current test run.
        /// </summary>
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void IsWellFormedTest()
        {
            var dt = new Dictionary<char, char>
            {
                ['<'] = '>',
                ['('] = ')'
            };

            Assert.AreEqual(true, "<<((Manu))>>".IsWellFormed(dt));
            Assert.AreEqual(false, "<<((Manu)>>".IsWellFormed(dt));
        }

        [TestMethod]
        public void AppendFromEnumerableTest()
        {
            var ss = new List<string>(5) { "How", "Hello", "Are", "You","Doing" };
            var sb = new StringBuilder(5);
            sb.AppendLineFromEnumerable(ss);
            TestContext.WriteLine(sb.ToString());
        }
    }
}
