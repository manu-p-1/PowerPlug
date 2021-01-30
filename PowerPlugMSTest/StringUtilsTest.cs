using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PowerPlug.StringUtils;

namespace PowerPlugMSTest
{
    [TestClass]
    public class StringUtilsTest
    {
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
        public void ContainsDuplicateInnerStringTest()
        {
           
        }
    }
}
