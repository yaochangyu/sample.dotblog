namespace A
{
    partial class A_Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.button_ImportKey = new System.Windows.Forms.Button();
            this.button_GetKey = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.richTextBox_PublicKey = new System.Windows.Forms.RichTextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.richTextBox_PrivateKey = new System.Windows.Forms.RichTextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.button_VerifySignature = new System.Windows.Forms.Button();
            this.label6 = new System.Windows.Forms.Label();
            this.richTextBox_SignatureContent = new System.Windows.Forms.RichTextBox();
            this.richTextBox_EncryptContent = new System.Windows.Forms.RichTextBox();
            this.textBox_DecryptContent = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.button_Decrypt = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.richTextBox_B_PublicKey = new System.Windows.Forms.RichTextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(508, 385);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.button_ImportKey);
            this.tabPage1.Controls.Add(this.button_GetKey);
            this.tabPage1.Controls.Add(this.button2);
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Controls.Add(this.button1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(500, 359);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Key";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // button_ImportKey
            // 
            this.button_ImportKey.Location = new System.Drawing.Point(331, 6);
            this.button_ImportKey.Name = "button_ImportKey";
            this.button_ImportKey.Size = new System.Drawing.Size(75, 23);
            this.button_ImportKey.TabIndex = 8;
            this.button_ImportKey.Text = "匯入金鑰";
            this.button_ImportKey.UseVisualStyleBackColor = true;
            this.button_ImportKey.Click += new System.EventHandler(this.button_ImportKey_Click);
            // 
            // button_GetKey
            // 
            this.button_GetKey.Location = new System.Drawing.Point(14, 6);
            this.button_GetKey.Name = "button_GetKey";
            this.button_GetKey.Size = new System.Drawing.Size(104, 23);
            this.button_GetKey.TabIndex = 7;
            this.button_GetKey.Text = "從容器取得金鑰";
            this.button_GetKey.UseVisualStyleBackColor = true;
            this.button_GetKey.Click += new System.EventHandler(this.button_GetKey_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(250, 6);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 6;
            this.button2.Text = "保存金鑰";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.richTextBox_PublicKey);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.richTextBox_PrivateKey);
            this.groupBox1.Location = new System.Drawing.Point(8, 35);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(484, 310);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Key";
            // 
            // richTextBox_PublicKey
            // 
            this.richTextBox_PublicKey.Location = new System.Drawing.Point(6, 211);
            this.richTextBox_PublicKey.Name = "richTextBox_PublicKey";
            this.richTextBox_PublicKey.Size = new System.Drawing.Size(472, 93);
            this.richTextBox_PublicKey.TabIndex = 6;
            this.richTextBox_PublicKey.Text = "";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 195);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Public Key";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(61, 13);
            this.label1.TabIndex = 4;
            this.label1.Text = "Private Key";
            // 
            // richTextBox_PrivateKey
            // 
            this.richTextBox_PrivateKey.Location = new System.Drawing.Point(6, 32);
            this.richTextBox_PrivateKey.Name = "richTextBox_PrivateKey";
            this.richTextBox_PrivateKey.Size = new System.Drawing.Size(472, 160);
            this.richTextBox_PrivateKey.TabIndex = 3;
            this.richTextBox_PrivateKey.Text = "";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(169, 6);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "產生新金鑰";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.label4);
            this.tabPage2.Controls.Add(this.richTextBox_B_PublicKey);
            this.tabPage2.Controls.Add(this.button_VerifySignature);
            this.tabPage2.Controls.Add(this.label6);
            this.tabPage2.Controls.Add(this.richTextBox_SignatureContent);
            this.tabPage2.Controls.Add(this.richTextBox_EncryptContent);
            this.tabPage2.Controls.Add(this.textBox_DecryptContent);
            this.tabPage2.Controls.Add(this.label5);
            this.tabPage2.Controls.Add(this.button_Decrypt);
            this.tabPage2.Controls.Add(this.label3);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(500, 359);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "解密";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // button_VerifySignature
            // 
            this.button_VerifySignature.Location = new System.Drawing.Point(413, 247);
            this.button_VerifySignature.Name = "button_VerifySignature";
            this.button_VerifySignature.Size = new System.Drawing.Size(75, 23);
            this.button_VerifySignature.TabIndex = 39;
            this.button_VerifySignature.Text = "驗証簽章";
            this.button_VerifySignature.UseVisualStyleBackColor = true;
            this.button_VerifySignature.Click += new System.EventHandler(this.button_VerifySignature_Click);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 247);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(78, 13);
            this.label6.TabIndex = 38;
            this.label6.Text = "Signature Data";
            // 
            // richTextBox_SignatureContent
            // 
            this.richTextBox_SignatureContent.Location = new System.Drawing.Point(95, 247);
            this.richTextBox_SignatureContent.Name = "richTextBox_SignatureContent";
            this.richTextBox_SignatureContent.Size = new System.Drawing.Size(317, 99);
            this.richTextBox_SignatureContent.TabIndex = 37;
            this.richTextBox_SignatureContent.Text = "";
            // 
            // richTextBox_EncryptContent
            // 
            this.richTextBox_EncryptContent.Location = new System.Drawing.Point(95, 12);
            this.richTextBox_EncryptContent.Name = "richTextBox_EncryptContent";
            this.richTextBox_EncryptContent.Size = new System.Drawing.Size(317, 99);
            this.richTextBox_EncryptContent.TabIndex = 36;
            this.richTextBox_EncryptContent.Text = "";
            // 
            // textBox_DecryptContent
            // 
            this.textBox_DecryptContent.Enabled = false;
            this.textBox_DecryptContent.Location = new System.Drawing.Point(95, 116);
            this.textBox_DecryptContent.Name = "textBox_DecryptContent";
            this.textBox_DecryptContent.Size = new System.Drawing.Size(317, 20);
            this.textBox_DecryptContent.TabIndex = 13;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(10, 119);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(70, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Decrypt Data";
            // 
            // button_Decrypt
            // 
            this.button_Decrypt.Location = new System.Drawing.Point(413, 116);
            this.button_Decrypt.Name = "button_Decrypt";
            this.button_Decrypt.Size = new System.Drawing.Size(75, 23);
            this.button_Decrypt.TabIndex = 6;
            this.button_Decrypt.Text = "解密";
            this.button_Decrypt.UseVisualStyleBackColor = true;
            this.button_Decrypt.Click += new System.EventHandler(this.button_Decrypt_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 12);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(69, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Encrypt Data";
            // 
            // richTextBox_B_PublicKey
            // 
            this.richTextBox_B_PublicKey.Location = new System.Drawing.Point(95, 142);
            this.richTextBox_B_PublicKey.Name = "richTextBox_B_PublicKey";
            this.richTextBox_B_PublicKey.Size = new System.Drawing.Size(317, 99);
            this.richTextBox_B_PublicKey.TabIndex = 40;
            this.richTextBox_B_PublicKey.Text = "";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 142);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(88, 13);
            this.label4.TabIndex = 41;
            this.label4.Text = "B廠商Public Key";
            // 
            // A_Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(508, 385);
            this.Controls.Add(this.tabControl1);
            this.Name = "A_Form1";
            this.Text = "A廠商";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox richTextBox_PrivateKey;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.RichTextBox richTextBox_PublicKey;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button_GetKey;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button_Decrypt;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_DecryptContent;
        private System.Windows.Forms.Button button_ImportKey;
        private System.Windows.Forms.RichTextBox richTextBox_EncryptContent;
        private System.Windows.Forms.RichTextBox richTextBox_SignatureContent;
        private System.Windows.Forms.Button button_VerifySignature;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.RichTextBox richTextBox_B_PublicKey;

    }
}

