namespace Lab.CertFromCA
{
    public partial class OID
    {
        public static OID ServerAuthentication { get; set; } = new OID { Name = "Server Authentication", Oid = "1.3.6.1.5.5.7.3.1" };

        public static OID ClientAuthentication { get; set; } = new OID { Name = "Client Authentication", Oid = "1.3.6.1.5.5.7.3.2" };

        public static OID CodeSigning { get; set; } = new OID { Name = "CodeSigning", Oid = "1.3.6.1.5.5.7.3.3" };

        public static OID EmailProtection { get; set; } = new OID { Name = "Email Protection", Oid = "1.3.6.1.5.5.7.3.4" };

        public static OID IPSECEndSystem { get; set; } = new OID { Name = "IPSEC EndSystem", Oid = "1.3.6.1.5.5.7.3.5" };

        public static OID IPSECTunnel { get; set; } = new OID { Name = "IPSEC Tunnel", Oid = "1.3.6.1.5.5.7.3.6" };

        public static OID IPSECUser { get; set; } = new OID { Name = "IPSEC User", Oid = "1.3.6.1.5.5.7.3.7" };

        public static OID TimeStamping { get; set; } = new OID { Name = "TimeStamping", Oid = "1.3.6.1.5.5.7.3.8" };

        public static OID OCSPSigning { get; set; } = new OID { Name = "OCSPSigning", Oid = "1.3.6.1.5.5.7.3.9" };

        public string Oid { get; set; }

        public string Name { get; set; }
    }
}