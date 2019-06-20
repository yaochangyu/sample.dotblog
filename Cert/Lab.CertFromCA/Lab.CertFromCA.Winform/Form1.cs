using System;
using System.Windows.Forms;

namespace Lab.CertFromCA.Winform
{
    public partial class Form1 : Form
    {
        private const int CC_DEFAULTCONFIG = 0;

        private const int CC_UIPICKCONFIG = 0x1;

        private const int CR_IN_BASE64 = 0x1;

        private const int CR_IN_FORMATANY = 0;

        private const int CR_IN_PKCS10 = 0x100;

        private const int CR_DISP_ISSUED = 0x3;

        private const int CR_DISP_UNDER_SUBMISSION = 0x5;

        private const int CR_OUT_BASE64 = 0x1;

        private const int CR_OUT_CHAIN = 0x100;

        private readonly string sOK = "";

        public Form1()
        {
            this.InitializeComponent();
            this.SubjectBodyBindingSource.DataSource = new SubjectBody
            {
                CommonName       = "*.lab.local,*.lab1.local",
                OrganizationUnit = "MIS",
                Organization     = "THS",
                Country          = "TW",
                Locality         = "Taipei"
            };
            this.CaConfigBindingSource.DataSource = new CaConfig
            {
                Server       = @"FQDN\CA",
                TemplateName = "WebServer"
            };
        }

        private void Encroll_Button_Click(object sender, EventArgs e)
        {
            var templateName = this.templateNameComboBox.Text;
            var caServer      = this.serverTextBox.Text;
            var keyLength     = 2048;
            var certification = new Certification();
            var subjectBody   = this.SubjectBodyBindingSource[0] as SubjectBody;

            var create = certification.CreateRequest(subjectBody,
                                                     OID.ServerAuthentication.Oid,
                                                     keyLength);
            var send = certification.SendRequest(create, caServer, templateName);
            certification.Enroll(send, null);
            MessageBox.Show("Done!!");
        }

        private void SelectCA_Button_Click(object sender, EventArgs e)
        {
            try
            {
                var certification = new Certification();
                certification.SelectCA();
            }
            catch (Exception ex)
            {
                //Check if the user closed the dialog. Do nothing.
                if (ex.HResult.ToString() == "-2147023673")
                {
                    //MessageBox.Show("Closed By user");
                }

                //Check if there is no available CA Servers.
                else if (ex.HResult.ToString() == "-2147024637")
                {
                    MessageBox.Show("Can't find available Servers");
                }

                // If unknown error occurs.
                else
                {
                    MessageBox.Show(ex.Message + " " + ex.HResult);
                }
            }
        }
    }
}