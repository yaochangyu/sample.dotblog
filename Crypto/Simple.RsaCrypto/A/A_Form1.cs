using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using RasCryptor;

namespace A
{
    public partial class A_Form1 : Form
    {
        public A_Form1()
        {
            InitializeComponent();
        }

        private RasCryptorService _rsa = new RasCryptorService("A_RsaCspParameters_Key");

        private void button1_Click(object sender, EventArgs e)
        {
            this._rsa.GenerateKey();
            this.richTextBox_PrivateKey.Text = this._rsa.PrivateKey;
            this.richTextBox_PublicKey.Text = this._rsa.PublicKey;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.richTextBox_PrivateKey.Text = this._rsa.PrivateKey;
            this.richTextBox_PublicKey.Text = this._rsa.PublicKey;

            using (FileStream file = new FileStream("A_privateKey.xml", FileMode.Create, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(file))
            {
                writer.Write(this._rsa.PrivateKey);
            }

            using (FileStream file = new FileStream("A_publicKey.xml", FileMode.Create, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(file))
            {
                writer.Write(this._rsa.PublicKey);
            }
        }

        private void button_ImportKey_Click(object sender, EventArgs e)
        {
            using (FileStream file = new FileStream("A_privateKey.xml", FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(file))
            {
                this.richTextBox_PrivateKey.Text = reader.ReadToEnd();
            }

            using (FileStream file = new FileStream("A_publicKey.xml", FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(file))
            {
                this.richTextBox_PublicKey.Text = reader.ReadToEnd();
            }

            this._rsa.PrivateKey = this.richTextBox_PrivateKey.Text;
            this._rsa.PublicKey = this.richTextBox_PublicKey.Text;
        }

        private void button_Decrypt_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.richTextBox_PrivateKey.Text) || string.IsNullOrEmpty(this.richTextBox_EncryptContent.Text))
            {
                return;
            }

            this._rsa.PrivateKey = this.richTextBox_PrivateKey.Text;
            textBox_DecryptContent.Text = this._rsa.DecryptString(this.richTextBox_EncryptContent.Text);
        }

        private void button_GetKey_Click(object sender, EventArgs e)
        {
            this.richTextBox_PrivateKey.Text = this._rsa.PrivateKey;
            this.richTextBox_PublicKey.Text = this._rsa.PublicKey;
        }

        private void button_VerifySignature_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.richTextBox_B_PublicKey.Text) ||
                string.IsNullOrEmpty(this.textBox_DecryptContent.Text) ||
                string.IsNullOrEmpty(this.richTextBox_SignatureContent.Text))
            {
                return;
            }

            this._rsa.PublicKey = this.richTextBox_B_PublicKey.Text;
            var isVerify = this._rsa.VerifySignature(this.textBox_DecryptContent.Text, this.richTextBox_SignatureContent.Text);
            if (isVerify)
            {
                MessageBox.Show("驗証成功");
            }
            else
            {
                MessageBox.Show("驗証失敗");
            }
        }
    }
}