using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using CERTCLILib;
using CERTENROLLLib;

namespace Lab.CertFromCA
{
    public class Certification
    {
        public string CreateRequest(string cn, string ou, string o, string l, string s, string c,
                                    string oid,
                                    int keyLength)
        {
            var csp = new CCspInformations();
            csp.AddAvailableCsps();

            var privateKey = new CX509PrivateKey();
            privateKey.Length = keyLength;
            privateKey.KeySpec = X509KeySpec.XCN_AT_SIGNATURE;
            privateKey.KeyUsage = X509PrivateKeyUsageFlags.XCN_NCRYPT_ALLOW_ALL_USAGES;
            privateKey.MachineContext = false;
            privateKey.ExportPolicy = X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_EXPORT_FLAG;
            privateKey.CspInformations = csp;
            privateKey.Create();

            var pkcs10 = new CX509CertificateRequestPkcs10();
            pkcs10.InitializeFromPrivateKey(X509CertificateEnrollmentContext.ContextUser,
                                            privateKey,
                                            string.Empty);

            var extensionKeyUsage = new CX509ExtensionKeyUsage();
            extensionKeyUsage.InitializeEncode(X509KeyUsageFlags.XCN_CERT_DIGITAL_SIGNATURE_KEY_USAGE |
                                               X509KeyUsageFlags.XCN_CERT_NON_REPUDIATION_KEY_USAGE |
                                               X509KeyUsageFlags.XCN_CERT_KEY_ENCIPHERMENT_KEY_USAGE |
                                               X509KeyUsageFlags.XCN_CERT_DATA_ENCIPHERMENT_KEY_USAGE);
            pkcs10.X509Extensions.Add((CX509Extension)extensionKeyUsage);

            var objectId = new CObjectId();
            var objectIds = new CObjectIds();
            var extensionEnhancedKeyUsage = new CX509ExtensionEnhancedKeyUsage();

            objectId.InitializeFromValue(oid);
            objectIds.Add(objectId);
            extensionEnhancedKeyUsage.InitializeEncode(objectIds);
            pkcs10.X509Extensions.Add((CX509Extension)extensionEnhancedKeyUsage);

            // TODO: Create CERTS with SAN: http://msdn.microsoft.com/en-us/library/windows/desktop/aa378081(v=vs.85).aspx

            var san = new CX509ExtensionAlternativeNames();
            var alternativeName = new CAlternativeName();
            var alternativeNames = new CAlternativeNames();

            alternativeName.InitializeFromString(AlternativeNameType.XCN_CERT_ALT_NAME_DNS_NAME, cn);
            alternativeNames.Add(alternativeName);
            san.InitializeEncode(alternativeNames);

            pkcs10.X509Extensions.Add((CX509Extension)san);

            var distinguishedName = new CX500DistinguishedName();
            var subjectName =
                "CN = " + cn + ",OU = " + ou + ",O = " + o + ",L = " + l + ",S = " + s + ",C = " + c;

            distinguishedName.Encode(subjectName);
            pkcs10.Subject = distinguishedName;

            var enroll = new CX509Enrollment();
            enroll.InitializeFromRequest(pkcs10);
            var request = enroll.CreateRequest();

            return request;
        }

        public string CreateRequest(SubjectBody subjectBody,
                                    string oid,
                                    int keyLength)
        {
            return this.CreateRequest(subjectBody.CommonName, subjectBody.OrganizationUnit, subjectBody.Organization,
                                      subjectBody.Locality, subjectBody.State, subjectBody.Country, oid,
                                      keyLength);
        }

        public string CreateTemplateRequest(string cn, string ou, string o, string l, string s, string c,
                                            int keyLength,
                                            string templateName)
        {
            var csp = new CCspInformations();
            csp.AddAvailableCsps();
            var privateKey = new CX509PrivateKey();
            privateKey.Length = keyLength;
            privateKey.KeySpec = X509KeySpec.XCN_AT_SIGNATURE;
            privateKey.KeyUsage = X509PrivateKeyUsageFlags.XCN_NCRYPT_ALLOW_ALL_USAGES;
            privateKey.MachineContext = false;
            privateKey.ExportPolicy = X509PrivateKeyExportFlags.XCN_NCRYPT_ALLOW_EXPORT_FLAG;
            privateKey.CspInformations = csp;
            privateKey.Create();

            var pkcs10 = new CX509CertificateRequestPkcs10();
            pkcs10.InitializeFromPrivateKey(X509CertificateEnrollmentContext.ContextUser,
                                            privateKey,
                                            templateName);

            var dn = new CX500DistinguishedName();

            var subjectName = "CN = " + cn + ",OU = " + ou + ",O = " + o + ",L = " + l + ",S = " + s + ",C = " + c;
            dn.Encode(subjectName);
            pkcs10.Subject = dn;

            var enroll = new CX509Enrollment();
            enroll.InitializeFromRequest(pkcs10);
            var strRequest = enroll.CreateRequest();

            return strRequest;
        }

        public string CreateTemplateRequest(SubjectBody subjectBody,
                                            int keyLength,
                                            string templateName)
        {
            return this.CreateTemplateRequest(subjectBody.CommonName, subjectBody.OrganizationUnit,
                                              subjectBody.Organization, subjectBody.Locality,
                                              subjectBody.State, subjectBody.Country,
                                              keyLength, templateName);
        }

        public void Enroll(string certText, string password)
        {
            var enroll = new CX509EnrollmentClass();
            enroll.Initialize(X509CertificateEnrollmentContext.ContextUser);
            enroll.InstallResponse(InstallResponseRestrictionFlags.AllowUntrustedRoot,
                                   certText,
                                   EncodingType.XCN_CRYPT_STRING_BASE64REQUESTHEADER,
                                   password
                                  );
            var dir = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString();
            var pfx = enroll.CreatePFX(password, PFXExportOptions.PFXExportChainWithRoot);
            File.WriteAllText(dir + @"\" + "cert.pfx", pfx);
        }

        public IEnumerable<Template> GetCaTemplates(string caServer)
        {
            CCertRequest certRequest = new CCertRequestClass();
            var templates = new List<Template>();

            var regex = new Regex(@"([A-Za-z]+)");
            var value = certRequest.GetCAProperty(caServer, 29, 0, 4, 0).ToString();
            var lines = Regex.Split(value, @"\n");

            foreach (var line in lines)
            {
                var match = regex.Match(line);
                if (match.Success)
                {
                    templates.Add(new Template { Name = line });
                }
            }

            return templates;
        }

        public List<OID> ListOids()
        {
            var items = new List<OID>();

            items.Add(new OID { Name = "Server Authentication", Oid = "1.3.6.1.5.5.7.3.1" });
            items.Add(new OID { Name = "Client Authentication", Oid = "1.3.6.1.5.5.7.3.2" });
            items.Add(new OID { Name = "CodeSigning", Oid = "1.3.6.1.5.5.7.3.3" });
            items.Add(new OID { Name = "Email Protection", Oid = "1.3.6.1.5.5.7.3.4" });
            items.Add(new OID { Name = "IPSEC EndSystem", Oid = "1.3.6.1.5.5.7.3.5" });
            items.Add(new OID { Name = "IPSEC Tunnel", Oid = "1.3.6.1.5.5.7.3.6" });
            items.Add(new OID { Name = "IPSEC User", Oid = "1.3.6.1.5.5.7.3.7" });
            items.Add(new OID { Name = "TimeStamping", Oid = "1.3.6.1.5.5.7.3.8" });
            items.Add(new OID { Name = "OCSPSigning", Oid = "1.3.6.1.5.5.7.3.9" });

            return items;
        }

        public string RetrieveCertStatus(int id, string caServer)
        {
            int strDisposition;
            var msg = "";

            CCertRequest objCertRequest = new CCertRequestClass();
            strDisposition = objCertRequest.RetrievePending(id, caServer);

            switch (strDisposition)
            {
                case (int)RequestDisposition.CR_DISP_INCOMPLETE:
                    msg = "incomplete certificate";
                    break;
                case (int)RequestDisposition.CR_DISP_DENIED:
                    msg = "request denied";
                    break;
                case (int)RequestDisposition.CR_DISP_ISSUED:
                    msg = "certificate issued";
                    break;
                case (int)RequestDisposition.CR_DISP_UNDER_SUBMISSION:
                    msg = "request pending";
                    break;
                case (int)RequestDisposition.CR_DISP_REVOKED:
                    msg = "certificate revoked";
                    break;
            }

            return msg;
        }

        public string SendRequest(string createRequest, string caServer,
                                  string templateName,
                                  string additionalAttributes = "")
        {
            var attributes = string.Format("CertificateTemplate: {0}", templateName);

            if (!string.IsNullOrEmpty(additionalAttributes))
            {
                attributes += "\n" + additionalAttributes;
            }

            var certRequest = new CCertRequest();
            var requestResult = (RequestDisposition)certRequest.Submit((int)EncodingType.XCN_CRYPT_STRING_BASE64,
                                                                        createRequest,
                                                                        attributes,
                                                                        caServer);
            string cert = null;
            if (requestResult == RequestDisposition.CR_DISP_ISSUED)
            {
                cert = certRequest.GetCertificate((int)EncodingType.XCN_CRYPT_STRING_BASE64REQUESTHEADER);
            }

            return cert;
        }

        public void FindCA()
        {
            CCertConfig objCertConfig = new CCertConfigClass();
            CCertRequest objCertRequest = new CCertRequestClass();
            const int CC_UIPICKCONFIG = 0x1;

            // Get CA config from UI
            var strCAConfig = objCertConfig.GetConfig(CC_UIPICKCONFIG);

            if (string.IsNullOrWhiteSpace(strCAConfig))
            {
                return;
            }

            // Get CA Connection string
            var CACon = objCertConfig.GetField("Config");

            // Get CA Type
            var caType = objCertRequest.GetCAProperty(strCAConfig, 10, 0, 1, 0).ToString();
            var caTypeTXT = "";
            switch (caType)
            {
                case "0":
                    caTypeTXT = "ENTERPRISE ROOT CA";
                    break;
                case "1":
                    caTypeTXT = "ENTERPRISE SUB CA";
                    break;
                case "3":
                    caTypeTXT = "STANDALONE ROOT CA";
                    break;
                case "4":
                    caTypeTXT = "STANDALONE SUB CA";
                    break;
            }

        }

    }
}