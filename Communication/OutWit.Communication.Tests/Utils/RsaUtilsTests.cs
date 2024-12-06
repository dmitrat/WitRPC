using System;
using System.Security.Cryptography;
using OutWit.Communication.Utils;

namespace OutWit.Communication.Tests.Utils
{
    [TestFixture]
    public class RsaUtilsTests
    {
        [Test]
        public void RsaParametersSerializationTest()
        {
            using var rsa = RSA.Create();

            rsa.KeySize = 2048;

            var key = rsa.ExportParameters(true);

            var bytes = key.ToBytes();
            Assert.That(bytes, Is.Not.Empty);

            RSAParameters key1 = bytes.ToRsaParameters();
            Assert.That(key.D, Is.EqualTo(key1.D));
            Assert.That(key.DP, Is.EqualTo(key1.DP));
            Assert.That(key.DQ, Is.EqualTo(key1.DQ));
            Assert.That(key.Exponent, Is.EqualTo(key1.Exponent));
            Assert.That(key.InverseQ, Is.EqualTo(key1.InverseQ));
            Assert.That(key.Modulus, Is.EqualTo(key1.Modulus));
            Assert.That(key.P, Is.EqualTo(key1.P));
            Assert.That(key.Q, Is.EqualTo(key1.Q));
        }
    }
}
