using System;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace Lab.CertFromCA.Winform
{
    public partial class Form1 : Form
    {


        public Form1()
        {
            this.InitializeComponent();
            this.SubjectBodyBindingSource.DataSource = new SubjectBody
            {
                CommonName = "*.lab.local,*.lab1.local",
                OrganizationUnit = "MIS",
                Organization = "THS",
                Country = "TW",
                Locality = "Taipei"
            };
            this.CaConfigBindingSource.DataSource = new CaConfig
            {
                Server = @"ad.lab.local\CA",
                TemplateName = "WebServer"
            };
            this.templateNameComboBox.DisplayMember="Name";
            this.templateNameComboBox.ValueMember="Name";
        }
        

        private void Encroll_Button_Click(object sender, EventArgs e)
        {
            var templateName = this.templateNameComboBox.Text;
            var caServer = this.serverTextBox.Text;
            var keyLength = 2048;
            var certification = new Certification();
            var subjectBody = this.SubjectBodyBindingSource[0] as SubjectBody;

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
                var caConfig = this.CaConfigBindingSource[0] as CaConfig;
                var certification = new Certification();
                caConfig.Server = certification.SelectCA();
                this.templateNameComboBox.DataSource = certification.GetCaTemplates(caConfig.Server);

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + " " + ex.HResult);
            }
        }
    }
}