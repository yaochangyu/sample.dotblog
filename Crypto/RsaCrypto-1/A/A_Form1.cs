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

using Cryptor;

namespace A
{
    public partial class A_Form1 : Form
    {
        public A_Form1()
        {
            InitializeComponent();
        }

        private BindingList<string> _keyList = new BindingList<string>();
        private BindingSource _source = new BindingSource();
        private CryptorService _service = new CryptorService();
        private AesCryptorService _aes;

        private void A_Form1_Load(object sender, EventArgs e)
        {
            this._source.PositionChanged += new EventHandler(_source_PositionChanged);

            this._source.DataSource = this._keyList;
            this.listBox1.DataSource = this._source;
        }

        private void _source_PositionChanged(object sender, EventArgs e)
        {
            BindingSource source = (BindingSource)sender;
            if (source.Position == -1)
            {
                return;
            }

            var current = (string)source.Current;
            this.richTextBox_A_Key.Text = current;
        }

        private void button_GenerateKey_Click(object sender, EventArgs e)
        {
            var key = this._service.GenerateNewKey("yaochang");
            this._keyList.Add(key);

            this._aes = this._service.CreateCryptService(key);
        }

        private void button_Encrypt_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(this.richTextBox_A_Key.Text) ||
                string.IsNullOrEmpty(this.textBox_Original.Text))
            {
                //TODO:處理錯誤
                return;
            }

            this._aes = this._service.CreateCryptService(this.richTextBox_A_Key.Text);
            this.richTextBox_EncryptContent.Text = this._aes.EncryptString(this.textBox_Original.Text);
        }
    }
}