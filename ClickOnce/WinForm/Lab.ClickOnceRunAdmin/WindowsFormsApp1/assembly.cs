using System;
using System.ComponentModel;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace WindowsFormsApp1
{
    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace                                = "urn:schemas-microsoft-com:asm.v1")]
    [XmlRoot(Namespace     = "urn:schemas-microsoft-com:asm.v1", IsNullable = false)]
    public class assembly
    {
        /// <remarks />
        public assemblyAssemblyIdentity assemblyIdentity
        {
            get => this.assemblyIdentityField;
            set => this.assemblyIdentityField = value;
        }

        /// <remarks />
        public assemblyDescription description
        {
            get => this.descriptionField;
            set => this.descriptionField = value;
        }

        /// <remarks />
        [XmlElement(Namespace = "urn:schemas-microsoft-com:asm.v2")]
        public deployment deployment
        {
            get => this.deploymentField;
            set => this.deploymentField = value;
        }

        /// <remarks />
        [XmlElement(Namespace = "urn:schemas-microsoft-com:clickonce.v2")]
        public compatibleFrameworks compatibleFrameworks
        {
            get => this.compatibleFrameworksField;
            set => this.compatibleFrameworksField = value;
        }

        /// <remarks />
        [XmlElement(Namespace = "urn:schemas-microsoft-com:asm.v2")]
        public dependency dependency
        {
            get => this.dependencyField;
            set => this.dependencyField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public decimal manifestVersion
        {
            get => this.manifestVersionField;
            set => this.manifestVersionField = value;
        }

        private assemblyAssemblyIdentity assemblyIdentityField;

        private compatibleFrameworks compatibleFrameworksField;

        private dependency dependencyField;

        private deployment deploymentField;

        private assemblyDescription descriptionField;

        private decimal manifestVersionField;
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace = "urn:schemas-microsoft-com:asm.v1")]
    public class assemblyAssemblyIdentity
    {
        /// <remarks />
        [XmlAttribute]
        public string name
        {
            get => this.nameField;
            set => this.nameField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string version
        {
            get => this.versionField;
            set => this.versionField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public byte publicKeyToken
        {
            get => this.publicKeyTokenField;
            set => this.publicKeyTokenField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string language
        {
            get => this.languageField;
            set => this.languageField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string processorArchitecture
        {
            get => this.processorArchitectureField;
            set => this.processorArchitectureField = value;
        }

        private string languageField;

        private string nameField;

        private string processorArchitectureField;

        private byte publicKeyTokenField;

        private string versionField;
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace = "urn:schemas-microsoft-com:asm.v1")]
    public class assemblyDescription
    {
        /// <remarks />
        [XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "urn:schemas-microsoft-com:asm.v2")]
        public string publisher
        {
            get => this.publisherField;
            set => this.publisherField = value;
        }

        /// <remarks />
        [XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "urn:schemas-microsoft-com:asm.v2")]
        public string product
        {
            get => this.productField;
            set => this.productField = value;
        }

        private string productField;

        private string publisherField;
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace                                = "urn:schemas-microsoft-com:asm.v2")]
    [XmlRoot(Namespace     = "urn:schemas-microsoft-com:asm.v2", IsNullable = false)]
    public class deployment
    {
        /// <remarks />
        [XmlAttribute]
        public bool install
        {
            get => this.installField;
            set => this.installField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public bool mapFileExtensions
        {
            get => this.mapFileExtensionsField;
            set => this.mapFileExtensionsField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public bool trustURLParameters
        {
            get => this.trustURLParametersField;
            set => this.trustURLParametersField = value;
        }

        /// <remarks />
        [XmlAttribute(Form = XmlSchemaForm.Qualified, Namespace = "urn:schemas-microsoft-com:clickonce.v1")]
        public bool createDesktopShortcut
        {
            get => this.createDesktopShortcutField;
            set => this.createDesktopShortcutField = value;
        }

        private bool createDesktopShortcutField;

        private bool installField;

        private bool mapFileExtensionsField;

        private bool trustURLParametersField;
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true,
        Namespace          = "urn:schemas-microsoft-com:clickonce.v2")]
    [XmlRoot(Namespace = "urn:schemas-microsoft-com:clickonce.v2", IsNullable = false)]
    public class compatibleFrameworks
    {
        /// <remarks />
        public compatibleFrameworksFramework framework
        {
            get => this.frameworkField;
            set => this.frameworkField = value;
        }

        private compatibleFrameworksFramework frameworkField;
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace = "urn:schemas-microsoft-com:clickonce.v2")]
    public class compatibleFrameworksFramework
    {
        /// <remarks />
        [XmlAttribute]
        public string targetVersion
        {
            get => this.targetVersionField;
            set => this.targetVersionField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string profile
        {
            get => this.profileField;
            set => this.profileField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string supportedRuntime
        {
            get => this.supportedRuntimeField;
            set => this.supportedRuntimeField = value;
        }

        private string profileField;

        private string supportedRuntimeField;

        private string targetVersionField;
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace                                = "urn:schemas-microsoft-com:asm.v2")]
    [XmlRoot(Namespace     = "urn:schemas-microsoft-com:asm.v2", IsNullable = false)]
    public class dependency
    {
        /// <remarks />
        public dependencyDependentAssembly dependentAssembly
        {
            get => this.dependentAssemblyField;
            set => this.dependentAssemblyField = value;
        }

        private dependencyDependentAssembly dependentAssemblyField;
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace = "urn:schemas-microsoft-com:asm.v2")]
    public class dependencyDependentAssembly
    {
        /// <remarks />
        public dependencyDependentAssemblyAssemblyIdentity assemblyIdentity
        {
            get => this.assemblyIdentityField;
            set => this.assemblyIdentityField = value;
        }

        /// <remarks />
        public dependencyDependentAssemblyHash hash
        {
            get => this.hashField;
            set => this.hashField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string dependencyType
        {
            get => this.dependencyTypeField;
            set => this.dependencyTypeField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string codebase
        {
            get => this.codebaseField;
            set => this.codebaseField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public ushort size
        {
            get => this.sizeField;
            set => this.sizeField = value;
        }

        private dependencyDependentAssemblyAssemblyIdentity assemblyIdentityField;

        private string codebaseField;

        private string dependencyTypeField;

        private dependencyDependentAssemblyHash hashField;

        private ushort sizeField;
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace = "urn:schemas-microsoft-com:asm.v2")]
    public class dependencyDependentAssemblyAssemblyIdentity
    {
        /// <remarks />
        [XmlAttribute]
        public string name
        {
            get => this.nameField;
            set => this.nameField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string version
        {
            get => this.versionField;
            set => this.versionField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public byte publicKeyToken
        {
            get => this.publicKeyTokenField;
            set => this.publicKeyTokenField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string language
        {
            get => this.languageField;
            set => this.languageField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string processorArchitecture
        {
            get => this.processorArchitectureField;
            set => this.processorArchitectureField = value;
        }

        /// <remarks />
        [XmlAttribute]
        public string type
        {
            get => this.typeField;
            set => this.typeField = value;
        }

        private string languageField;

        private string nameField;

        private string processorArchitectureField;

        private byte publicKeyTokenField;

        private string typeField;

        private string versionField;
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace = "urn:schemas-microsoft-com:asm.v2")]
    public class dependencyDependentAssemblyHash
    {
        /// <remarks />
        [XmlElement(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
        public Transforms Transforms
        {
            get => this.transformsField;
            set => this.transformsField = value;
        }

        /// <remarks />
        [XmlElement(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
        public DigestMethod DigestMethod
        {
            get => this.digestMethodField;
            set => this.digestMethodField = value;
        }

        /// <remarks />
        [XmlElement(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
        public string DigestValue
        {
            get => this.digestValueField;
            set => this.digestValueField = value;
        }

        private DigestMethod digestMethodField;

        private string digestValueField;

        private Transforms transformsField;
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace                                  = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot(Namespace     = "http://www.w3.org/2000/09/xmldsig#", IsNullable = false)]
    public class Transforms
    {
        /// <remarks />
        public TransformsTransform Transform
        {
            get => this.transformField;
            set => this.transformField = value;
        }

        private TransformsTransform transformField;
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public class TransformsTransform
    {
        /// <remarks />
        [XmlAttribute]
        public string Algorithm
        {
            get => this.algorithmField;
            set => this.algorithmField = value;
        }

        private string algorithmField;
    }

    /// <remarks />
    [Serializable]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true, Namespace                                  = "http://www.w3.org/2000/09/xmldsig#")]
    [XmlRoot(Namespace     = "http://www.w3.org/2000/09/xmldsig#", IsNullable = false)]
    public class DigestMethod
    {
        /// <remarks />
        [XmlAttribute]
        public string Algorithm
        {
            get => this.algorithmField;
            set => this.algorithmField = value;
        }

        private string algorithmField;
    }
}