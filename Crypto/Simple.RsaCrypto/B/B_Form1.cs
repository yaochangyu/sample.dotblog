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

namespace B
{
    public partial class B_Form1 : Form
    {
        public B_Form1()
        {
            InitializeComponent();
        }

        private RasCryptorService _rsa = new RasCryptorService("B_RsaCspParameters_Key");

        private void button_Encrypt_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.richTextBox_A_PublicKey.Text) || string.IsNullOrEmpty(this.textBox_Original.Text))
            {
                return;
            }

            this._rsa.PublicKey = this.richTextBox_A_PublicKey.Text;
            this.richTextBox_EncryptContent.Text = this._rsa.EncryptString(this.textBox_Original.Text);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

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

            using (FileStream file = new FileStream("B_privateKey.xml", FileMode.Create, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(file))
            {
                writer.Write(this._rsa.PrivateKey);
            }

            using (FileStream file = new FileStream("B_publicKey.xml", FileMode.Create, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(file))
            {
                writer.Write(this._rsa.PublicKey);
            }
        }

        private void button_ImportKey_Click(object sender, EventArgs e)
        {
            using (FileStream file = new FileStream("B_privateKey.xml", FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(file))
            {
                this.richTextBox_PrivateKey.Text = reader.ReadToEnd();
            }

            using (FileStream file = new FileStream("B_publicKey.xml", FileMode.Open, FileAccess.Read))
            using (StreamReader reader = new StreamReader(file))
            {
                this.richTextBox_PublicKey.Text = reader.ReadToEnd();
            }
        }

        private void button_GetKey_Click(object sender, EventArgs e)
        {
            this.richTextBox_PrivateKey.Text = this._rsa.PrivateKey;
            this.richTextBox_PublicKey.Text = this._rsa.PublicKey;
        }

        private void button_GenerateSignature_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.textBox_Original.Text))
            {
                return;
            }

            this._rsa.PrivateKey = this.richTextBox_PrivateKey.Text;
            this.richTextBox_SignatureContent.Text = this._rsa.GetSignature(this.textBox_Original.Text);
        }
    }
}