using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Lab.CertFromCA.UnitTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        //[Ignore]
        public void CreateRequest()
        {
            var templateName = "WebServer";
            var keyLength = 2048;

            //var caServer = @"ad.lab.local\lab-ca";
            var caServer = @"NTTP3VS22.nttp3.ths.com.tw\CA";
            var certification = new Certification();
            var subjectBody = new SubjectBody()
            {
                CommonName = "*.lab.local"
            };
            var create = certification.CreateRequest(subjectBody,
                                           OID.ServerAuthentication.Oid,
                                           keyLength);
            var send = certification.SendRequest(create, caServer, templateName);
            certification.Enroll(send, null);
        }
        [TestMethod]
        //[Ignore]
        public void GetCaTemplates()
        {
            var templateName = "WebServer";
            var keyLength = 2048;

            //var caServer = @"ad.lab.local\lab-ca";
            var caServer = @"NTTP3VS22.nttp3.ths.com.tw\CA";
            var certification = new Certification();
            var templates = certification.GetCaTemplates(caServer);
        }
        [TestMethod]
        //[Ignore]
        public void FindCA()
        {
            var templateName = "WebServer";
            var keyLength    = 2048;

            //var caServer = @"ad.lab.local\lab-ca";
            var caServer      = @"NTTP3VS22.nttp3.ths.com.tw\CA";
            var certification = new Certification();
            certification.FindCA();
        }
    }
}