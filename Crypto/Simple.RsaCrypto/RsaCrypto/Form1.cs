using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using RasCryptor;

namespace RsaCrypto
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private RasCryptorService _rsa = new RasCryptorService("RsaCspParameters_Key");

        private void Form1_Load(object sender, EventArgs e)
        {
            using (FileStream file = new FileStream("Original.txt", FileMode.Create, FileAccess.Write))
            using (StreamWriter writer = new StreamWriter(file, Encoding.GetEncoding(950)))
            {
                writer.WriteLine("名字:余小章");
                writer.WriteLine("我今年20歲");
                writer.WriteLine("我的部落格是http://www.dotblogs.com.tw/yc421206/");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.richTextBox_Encrypt.Text = this._rsa.EncryptString(this.textBox_Original.Text);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.textBox_Decrypt.Text = this._rsa.DecryptString(this.richTextBox_Encrypt.Text);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this._rsa.EncryptFile(this.textBox_OriginalFile.Text, this.textBox_EncryptFile.Text);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this._rsa.DecryptFile(this.textBox_EncryptFile.Text, this.textBox_DecryptFile.Text);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            this.richTextBox_SignHash.Text = this._rsa.GetSignature(this.textBox_SignOriginal.Text);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            var verify = this._rsa.VerifySignature(this.textBox_SignOriginal.Text, this.richTextBox_SignHash.Text);
            if (verify)
            {
                MessageBox.Show("驗證成功");
            }
            else
            {
                MessageBox.Show("簽章無效");
            }
        }
    }
}