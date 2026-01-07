// // ICVR CONFIDENTIAL
// // __________________
// //
// // [2016] - [2024] ICVR LLC
// // All Rights Reserved.
// //
// // NOTICE:  All information contained herein is, and remains
// // the property of ICVR LLC and its suppliers,
// // if any.  The intellectual and technical concepts contained
// // herein are proprietary to ICVR LLC
// // and its suppliers and may be covered by U.S. and Foreign Patents,
// // patents in process, and are protected by trade secret or copyright law.
// // Dissemination of this information or reproduction of this material
// // is strictly forbidden unless prior written permission is obtained
// // from ICVR LLC.

using NUnit.Framework;

namespace ICVR.Tools.Tests
{
    [TestFixture]
    public class VersionTests
    {
        [Test]
        [TestCase("1.2b3")]
        [TestCase("1.2.3")]
        public void ParsingVersion(string versionText)
        {
            var version = AppVersion.ParseFromString(versionText, true);
            
            Assert.IsTrue(version.Major == 1);
            Assert.IsTrue(version.Minor == 2);
            Assert.IsTrue(version.Build == 3);
        }
    }
}