using System;
using System.Collections.Generic;
using System.Text;

namespace WindowsFormsApp1
{

    // NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:schemas-microsoft-com:asm.v1")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "urn:schemas-microsoft-com:asm.v1", IsNullable = false)]
    public partial class assembly
    {

        private assemblyAssemblyIdentity assemblyIdentityField;

        private assemblyDescription descriptionField;

        private deployment deploymentField;

        private compatibleFrameworks compatibleFrameworksField;

        private dependency dependencyField;

        private decimal manifestVersionField;

        /// <remarks/>
        public assemblyAssemblyIdentity assemblyIdentity
        {
            get
            {
                return this.assemblyIdentityField;
            }
            set
            {
                this.assemblyIdentityField = value;
            }
        }

        /// <remarks/>
        public assemblyDescription description
        {
            get
            {
                return this.descriptionField;
            }
            set
            {
                this.descriptionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "urn:schemas-microsoft-com:asm.v2")]
        public deployment deployment
        {
            get
            {
                return this.deploymentField;
            }
            set
            {
                this.deploymentField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "urn:schemas-microsoft-com:clickonce.v2")]
        public compatibleFrameworks compatibleFrameworks
        {
            get
            {
                return this.compatibleFrameworksField;
            }
            set
            {
                this.compatibleFrameworksField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "urn:schemas-microsoft-com:asm.v2")]
        public dependency dependency
        {
            get
            {
                return this.dependencyField;
            }
            set
            {
                this.dependencyField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public decimal manifestVersion
        {
            get
            {
                return this.manifestVersionField;
            }
            set
            {
                this.manifestVersionField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:schemas-microsoft-com:asm.v1")]
    public partial class assemblyAssemblyIdentity
    {

        private string nameField;

        private string versionField;

        private byte publicKeyTokenField;

        private string languageField;

        private string processorArchitectureField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte publicKeyToken
        {
            get
            {
                return this.publicKeyTokenField;
            }
            set
            {
                this.publicKeyTokenField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string language
        {
            get
            {
                return this.languageField;
            }
            set
            {
                this.languageField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string processorArchitecture
        {
            get
            {
                return this.processorArchitectureField;
            }
            set
            {
                this.processorArchitectureField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:schemas-microsoft-com:asm.v1")]
    public partial class assemblyDescription
    {

        private string publisherField;

        private string productField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "urn:schemas-microsoft-com:asm.v2")]
        public string publisher
        {
            get
            {
                return this.publisherField;
            }
            set
            {
                this.publisherField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "urn:schemas-microsoft-com:asm.v2")]
        public string product
        {
            get
            {
                return this.productField;
            }
            set
            {
                this.productField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:schemas-microsoft-com:asm.v2")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "urn:schemas-microsoft-com:asm.v2", IsNullable = false)]
    public partial class deployment
    {

        private bool installField;

        private bool mapFileExtensionsField;

        private bool trustURLParametersField;

        private bool createDesktopShortcutField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool install
        {
            get
            {
                return this.installField;
            }
            set
            {
                this.installField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool mapFileExtensions
        {
            get
            {
                return this.mapFileExtensionsField;
            }
            set
            {
                this.mapFileExtensionsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public bool trustURLParameters
        {
            get
            {
                return this.trustURLParametersField;
            }
            set
            {
                this.trustURLParametersField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified, Namespace = "urn:schemas-microsoft-com:clickonce.v1")]
        public bool createDesktopShortcut
        {
            get
            {
                return this.createDesktopShortcutField;
            }
            set
            {
                this.createDesktopShortcutField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:schemas-microsoft-com:clickonce.v2")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "urn:schemas-microsoft-com:clickonce.v2", IsNullable = false)]
    public partial class compatibleFrameworks
    {

        private compatibleFrameworksFramework frameworkField;

        /// <remarks/>
        public compatibleFrameworksFramework framework
        {
            get
            {
                return this.frameworkField;
            }
            set
            {
                this.frameworkField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:schemas-microsoft-com:clickonce.v2")]
    public partial class compatibleFrameworksFramework
    {

        private string targetVersionField;

        private string profileField;

        private string supportedRuntimeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string targetVersion
        {
            get
            {
                return this.targetVersionField;
            }
            set
            {
                this.targetVersionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string profile
        {
            get
            {
                return this.profileField;
            }
            set
            {
                this.profileField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string supportedRuntime
        {
            get
            {
                return this.supportedRuntimeField;
            }
            set
            {
                this.supportedRuntimeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:schemas-microsoft-com:asm.v2")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "urn:schemas-microsoft-com:asm.v2", IsNullable = false)]
    public partial class dependency
    {

        private dependencyDependentAssembly dependentAssemblyField;

        /// <remarks/>
        public dependencyDependentAssembly dependentAssembly
        {
            get
            {
                return this.dependentAssemblyField;
            }
            set
            {
                this.dependentAssemblyField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:schemas-microsoft-com:asm.v2")]
    public partial class dependencyDependentAssembly
    {

        private dependencyDependentAssemblyAssemblyIdentity assemblyIdentityField;

        private dependencyDependentAssemblyHash hashField;

        private string dependencyTypeField;

        private string codebaseField;

        private ushort sizeField;

        /// <remarks/>
        public dependencyDependentAssemblyAssemblyIdentity assemblyIdentity
        {
            get
            {
                return this.assemblyIdentityField;
            }
            set
            {
                this.assemblyIdentityField = value;
            }
        }

        /// <remarks/>
        public dependencyDependentAssemblyHash hash
        {
            get
            {
                return this.hashField;
            }
            set
            {
                this.hashField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string dependencyType
        {
            get
            {
                return this.dependencyTypeField;
            }
            set
            {
                this.dependencyTypeField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string codebase
        {
            get
            {
                return this.codebaseField;
            }
            set
            {
                this.codebaseField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public ushort size
        {
            get
            {
                return this.sizeField;
            }
            set
            {
                this.sizeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:schemas-microsoft-com:asm.v2")]
    public partial class dependencyDependentAssemblyAssemblyIdentity
    {

        private string nameField;

        private string versionField;

        private byte publicKeyTokenField;

        private string languageField;

        private string processorArchitectureField;

        private string typeField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name
        {
            get
            {
                return this.nameField;
            }
            set
            {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string version
        {
            get
            {
                return this.versionField;
            }
            set
            {
                this.versionField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public byte publicKeyToken
        {
            get
            {
                return this.publicKeyTokenField;
            }
            set
            {
                this.publicKeyTokenField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string language
        {
            get
            {
                return this.languageField;
            }
            set
            {
                this.languageField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string processorArchitecture
        {
            get
            {
                return this.processorArchitectureField;
            }
            set
            {
                this.processorArchitectureField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string type
        {
            get
            {
                return this.typeField;
            }
            set
            {
                this.typeField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "urn:schemas-microsoft-com:asm.v2")]
    public partial class dependencyDependentAssemblyHash
    {

        private Transforms transformsField;

        private DigestMethod digestMethodField;

        private string digestValueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
        public Transforms Transforms
        {
            get
            {
                return this.transformsField;
            }
            set
            {
                this.transformsField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
        public DigestMethod DigestMethod
        {
            get
            {
                return this.digestMethodField;
            }
            set
            {
                this.digestMethodField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute(Namespace = "http://www.w3.org/2000/09/xmldsig#")]
        public string DigestValue
        {
            get
            {
                return this.digestValueField;
            }
            set
            {
                this.digestValueField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.w3.org/2000/09/xmldsig#", IsNullable = false)]
    public partial class Transforms
    {

        private TransformsTransform transformField;

        /// <remarks/>
        public TransformsTransform Transform
        {
            get
            {
                return this.transformField;
            }
            set
            {
                this.transformField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    public partial class TransformsTransform
    {

        private string algorithmField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Algorithm
        {
            get
            {
                return this.algorithmField;
            }
            set
            {
                this.algorithmField = value;
            }
        }
    }

    /// <remarks/>
    [System.SerializableAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, Namespace = "http://www.w3.org/2000/09/xmldsig#")]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "http://www.w3.org/2000/09/xmldsig#", IsNullable = false)]
    public partial class DigestMethod
    {

        private string algorithmField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Algorithm
        {
            get
            {
                return this.algorithmField;
            }
            set
            {
                this.algorithmField = value;
            }
        }
    }

}
