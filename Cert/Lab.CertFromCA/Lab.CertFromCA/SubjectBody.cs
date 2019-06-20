namespace Lab.CertFromCA
{
    public class SubjectBody
    {
        public string CommonName { get; set; }

        public string Country { get; set; } = "";

        public string Locality { get; set; } = "";

        public string Organization { get; set; } = "";

        public string OrganizationUnit { get; set; } = "";

        public string State { get; set; } = "";
    }
}