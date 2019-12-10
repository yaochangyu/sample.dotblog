using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Cryptor;

namespace B
{
    public partial class B_Form1 : Form
    {
        public B_Form1()
        {
            InitializeComponent();
        }

        private CryptorService _service = new CryptorService();
        private AesCryptorService _aes = new AesCryptorService();

        private void B_Form1_Load(object sender, EventArgs e)
        {
        }

        private void button_Decrypt_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.richTextBox_EncryptContent.Text) ||
                string.IsNullOrEmpty(this.richTextBox_A_Key.Text))
            {
                //todo:錯誤處理
                return;
            }
            this._aes = this._service.CreateCryptService(this.richTextBox_A_Key.Text);
            this.textBox_DecryptContent.Text = this._aes.DecryptString(this.richTextBox_EncryptContent.Text);
        }
    }
}