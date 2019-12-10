using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace RasCryptor
{
    [Serializable, XmlRoot("RSAKeyValue")]
    public class PrivateKeyEntity : PublicKeyEntity
    {
        [XmlElement("P")]
        public string P { get; set; }

        [XmlElement("Q")]
        public string Q { get; set; }

        [XmlElement("DP")]
        public string DP { get; set; }

        [XmlElement("DQ")]
        public string DQ { get; set; }

        [XmlElement("InverseQ")]
        public string InverseQ { get; set; }

        [XmlElement("D")]
        public string D { get; set; }
    }

    [Serializable, XmlRoot("RSAKeyValue")]
    public class PublicKeyEntity
    {
        [XmlElement("Modulus")]
        public string Modulus { get; set; }

        [XmlElement("Exponent")]
        public string Exponent { get; set; }
    }
}