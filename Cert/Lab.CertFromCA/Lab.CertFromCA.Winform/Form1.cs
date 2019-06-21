using System;
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
                CommonName       = "*.lab.local,*.lab1.local",
                OrganizationUnit = "MIS",
                Organization     = "THS",
                Country          = "TW",
                Locality         = "Taipei"
            };
            this.CaConfigBindingSource.DataSource = new CaConfig
            {
                //Server = @"ad.lab.local\lab-ca",
                Server       = @"nttp3vs22.nttp3.ths.com.tw\CA",
                TemplateName = "WebServer",
                Password     = "12345678",
                FriendlyName = "Demo"
            };
            this.templateNameComboBox.DisplayMember = "Name";
            this.templateNameComboBox.ValueMember   = "Name";
        }

        private void Encroll_Button_Click(object sender, EventArgs e)
        {
            var caConfig      = this.CaConfigBindingSource[0] as CaConfig;
            var keyLength     = 2048;
            var certification = new Certification();
            var subjectBody   = this.SubjectBodyBindingSource[0] as SubjectBody;
            try
            {
                var create = certification.CreateRequest(subjectBody,
                                                         OID.ServerAuthentication.Oid,
                                                         keyLength);
                this.richTextBox1.Text = create;

                var send = certification.SendRequest(create, caConfig.Server, caConfig.TemplateName);
                this.richTextBox2.Text = send;

                certification.InstallAndDownload(send, caConfig.Password, caConfig.FriendlyName);
                MessageBox.Show("Done!!");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex},{ex.HResult}");
            }
        }

        private void SelectCA_Button_Click(object sender, EventArgs e)
        {
            var caConfig      = this.CaConfigBindingSource[0] as CaConfig;
            var certification = new Certification();
            try
            {
                caConfig.Server                      = certification.SelectCA();
                this.templateNameComboBox.DataSource = certification.GetCaTemplates(caConfig.Server);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex},{ex.HResult}");
            }
        }
    }
}