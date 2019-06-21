using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using CERTCLILib;
using CERTENROLLLib;
using X509KeyUsageFlags = CERTENROLLLib.X509KeyUsageFlags;
namespace Lab.CertFromCA
{
    public class Certification
    {
        private static string ConvertCn(string source)
        {
            var builder = new StringBuilder();
            if (source.IndexOf(",") < 0)
            {
                return $"CN = {source}";
            }
            var split = source.Split(',');

            for (var i = 0; i < split.Length; i++)
            {
                if (i == 0)
                {
                    builder.Append($"CN = {split[i]},");
                }
                else
                {
                    builder.Append($"CN = {split[i]}");
                }
            }

            return builder.ToString();
        }

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

            var san = GetSAN(cn);
            pkcs10.X509Extensions.Add((CX509Extension)san);

            var distinguishedName = new CX500DistinguishedName();
            cn = ConvertCn(cn);

            //var subjectName =
            //    "CN = " + cn + ",OU = " + ou + ",O = " + o + ",L = " + l + ",S = " + s + ",C = " + c;
            var subjectName = $"{cn},OU = {ou},O = {o} ,L = {l},S = {s},C = {c}";
            distinguishedName.Encode(subjectName);
            pkcs10.Subject = distinguishedName;

            var enroll = new CX509Enrollment();
            enroll.InitializeFromRequest(pkcs10);
            var request = enroll.CreateRequest(EncodingType.XCN_CRYPT_STRING_BASE64HEADER);

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

            var san = GetSAN(cn);
            pkcs10.X509Extensions.Add((CX509Extension)san);

            var distinguishedName = new CX500DistinguishedName();

            var subjectName = $"{cn},OU = {ou},O = {o} ,L = {l},S = {s},C = {c}";
            distinguishedName.Encode(subjectName);
            pkcs10.Subject = distinguishedName;

            var enroll = new CX509Enrollment();
            enroll.InitializeFromRequest(pkcs10);
            var strRequest = enroll.CreateRequest(EncodingType.XCN_CRYPT_STRING_BASE64HEADER);

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

        /// <summary>
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="content"></param>
        /// <remarks>不能直接寫檔File.WriteAllText(filePath, content);，要先由Base6轉成字串</remarks>
        private static void Download(string filePath, string content)
        {
            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                var base64String = Convert.FromBase64String(content);
                fs.Write(base64String, 0, base64String.Length);
            }
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

        private static CX509ExtensionAlternativeNames GetSAN(string cn)
        {
            var san = new CX509ExtensionAlternativeNames();
            var alternativeNames = new CAlternativeNames();

            foreach (var item in cn.Split(','))
            {
                var alternativeName = new CAlternativeName();
                alternativeName.InitializeFromString(AlternativeNameType.XCN_CERT_ALT_NAME_DNS_NAME, item);
                alternativeNames.Add(alternativeName);
            }

            san.InitializeEncode(alternativeNames);
            return san;
        }

        private static void Install(string filePath, string password)
        {
            var cert = new X509Certificate2(filePath, password, X509KeyStorageFlags.PersistKeySet);
            var store = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadWrite);
            store.Add(cert);
        }

        public void InstallAndDownload(string certText, string password, string friendlyName)
        {
            var enroll = new CX509EnrollmentClass();
            enroll.Initialize(X509CertificateEnrollmentContext.ContextUser);
            enroll.CertificateFriendlyName = friendlyName;
            enroll.InstallResponse(InstallResponseRestrictionFlags.AllowUntrustedRoot,
                                   certText,
                                   EncodingType.XCN_CRYPT_STRING_BASE64REQUESTHEADER,
                                   password
                                  );
            var dir = Directory.GetParent(Assembly.GetExecutingAssembly().Location).ToString();
            var pfx = enroll.CreatePFX(password, PFXExportOptions.PFXExportChainWithRoot);

            var fileName = "cert.pfx";
            var filePath = $@"{dir}\{fileName}";
            Download(filePath, pfx);
            Install(filePath, password);
        }

        public List<OID> ListOids()
        {
            //TODO:應該由常數轉換
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

        public string SelectCA()
        {
            CCertConfig certConfig = new CCertConfigClass();
            CCertRequest certRequest = new CCertRequestClass();

            try
            {
                // Get CA config from UI
                var caConfig = certConfig.GetConfig((int)CertificateConfiguration.CC_UIPICKCONFIG);

                if (string.IsNullOrWhiteSpace(caConfig))
                {
                    return null;
                }

                // Get CA Connection string
                var ca = certConfig.GetField("Config");

                // Get CA Type
                var caType = certRequest.GetCAProperty(caConfig, 10, 0, 1, 0).ToString();
                var caTypeText = "";
                switch (caType)
                {
                    case "0":
                        caTypeText = "ENTERPRISE ROOT CA";
                        break;
                    case "1":
                        caTypeText = "ENTERPRISE SUB CA";
                        break;
                    case "3":
                        caTypeText = "STANDALONE ROOT CA";
                        break;
                    case "4":
                        caTypeText = "STANDALONE SUB CA";
                        break;
                }

                return ca;
            }
            catch (Exception ex)
            {
                string error = null;

                if (ex.HResult.ToString() == "-2147023673")
                {
                    error = "Closed By user";
                }
                else if (ex.HResult.ToString() == "-2147024637")
                {
                    error = "Can't find available Servers";
                }
                else
                {
                    error = ex.Message + " " + ex.HResult;
                }

                throw new Exception(error, ex);
            }
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
            var requestResult =
                (RequestDisposition)certRequest.Submit((int)EncodingType.XCN_CRYPT_STRING_BASE64HEADER,
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
    }
}