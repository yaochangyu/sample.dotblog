using System;
using System.Windows.Forms;
using CERTCLILib;
using CERTENROLLLib;

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
        }

        private void CreateRequestButton_Click(object sender, EventArgs e)
        {
            var certification = new Certification();
            certification.FindCA();
        }



    }
}