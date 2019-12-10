namespace B
{
    partial class B_Form1
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
            this.button_GetKey = new System.Windows.Forms.Button();
            this.button_ImportKey = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.richTextBox_PublicKey = new System.Windows.Forms.RichTextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.richTextBox_PrivateKey = new System.Windows.Forms.RichTextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.richTextBox_SignatureContent = new System.Windows.Forms.RichTextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.button_GenerateSignature = new System.Windows.Forms.Button();
            this.richTextBox_EncryptContent = new System.Windows.Forms.RichTextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_Original = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.button_Encrypt = new System.Windows.Forms.Button();
            this.richTextBox_A_PublicKey = new System.Windows.Forms.RichTextBox();
            this.label2 = new System.Windows.Forms.Label();
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
            this.tabControl1.TabIndex = 21;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.button_GetKey);
            this.tabPage1.Controls.Add(this.button_ImportKey);
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
            // button_GetKey
            // 
            this.button_GetKey.Location = new System.Drawing.Point(8, 10);
            this.button_GetKey.Name = "button_GetKey";
            this.button_GetKey.Size = new System.Drawing.Size(104, 23);
            this.button_GetKey.TabIndex = 23;
            this.button_GetKey.Text = "從容器取得金鑰";
            this.button_GetKey.UseVisualStyleBackColor = true;
            this.button_GetKey.Click += new System.EventHandler(this.button_GetKey_Click);
            // 
            // button_ImportKey
            // 
            this.button_ImportKey.Location = new System.Drawing.Point(331, 10);
            this.button_ImportKey.Name = "button_ImportKey";
            this.button_ImportKey.Size = new System.Drawing.Size(75, 23);
            this.button_ImportKey.TabIndex = 22;
            this.button_ImportKey.Text = "匯入金鑰";
            this.button_ImportKey.UseVisualStyleBackColor = true;
            this.button_ImportKey.Click += new System.EventHandler(this.button_ImportKey_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(250, 10);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 12;
            this.button2.Text = "保存金鑰";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.richTextBox_PublicKey);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label4);
            this.groupBox1.Controls.Add(this.richTextBox_PrivateKey);
            this.groupBox1.Location = new System.Drawing.Point(8, 39);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(484, 310);
            this.groupBox1.TabIndex = 9;
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
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 195);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(57, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Public Key";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 16);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(61, 13);
            this.label4.TabIndex = 4;
            this.label4.Text = "Private Key";
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
            this.button1.Location = new System.Drawing.Point(169, 10);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 8;
            this.button1.Text = "產生新金鑰";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.label2);
            this.tabPage2.Controls.Add(this.richTextBox_A_PublicKey);
            this.tabPage2.Controls.Add(this.richTextBox_SignatureContent);
            this.tabPage2.Controls.Add(this.label6);
            this.tabPage2.Controls.Add(this.button_GenerateSignature);
            this.tabPage2.Controls.Add(this.richTextBox_EncryptContent);
            this.tabPage2.Controls.Add(this.label5);
            this.tabPage2.Controls.Add(this.textBox_Original);
            this.tabPage2.Controls.Add(this.label1);
            this.tabPage2.Controls.Add(this.button_Encrypt);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(500, 359);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "加密";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // richTextBox_SignatureContent
            // 
            this.richTextBox_SignatureContent.Location = new System.Drawing.Point(97, 239);
            this.richTextBox_SignatureContent.Name = "richTextBox_SignatureContent";
            this.richTextBox_SignatureContent.Size = new System.Drawing.Size(319, 96);
            this.richTextBox_SignatureContent.TabIndex = 26;
            this.richTextBox_SignatureContent.Text = "";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(6, 242);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(78, 13);
            this.label6.TabIndex = 25;
            this.label6.Text = "Signature Data";
            // 
            // button_GenerateSignature
            // 
            this.button_GenerateSignature.Location = new System.Drawing.Point(422, 239);
            this.button_GenerateSignature.Name = "button_GenerateSignature";
            this.button_GenerateSignature.Size = new System.Drawing.Size(75, 23);
            this.button_GenerateSignature.TabIndex = 20;
            this.button_GenerateSignature.Text = "產生簽章";
            this.button_GenerateSignature.UseVisualStyleBackColor = true;
            this.button_GenerateSignature.Click += new System.EventHandler(this.button_GenerateSignature_Click);
            // 
            // richTextBox_EncryptContent
            // 
            this.richTextBox_EncryptContent.Location = new System.Drawing.Point(97, 138);
            this.richTextBox_EncryptContent.Name = "richTextBox_EncryptContent";
            this.richTextBox_EncryptContent.Size = new System.Drawing.Size(319, 96);
            this.richTextBox_EncryptContent.TabIndex = 17;
            this.richTextBox_EncryptContent.Text = "";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 143);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(69, 13);
            this.label5.TabIndex = 16;
            this.label5.Text = "Encrypt Data";
            // 
            // textBox_Original
            // 
            this.textBox_Original.Location = new System.Drawing.Point(97, 10);
            this.textBox_Original.Name = "textBox_Original";
            this.textBox_Original.Size = new System.Drawing.Size(319, 20);
            this.textBox_Original.TabIndex = 15;
            this.textBox_Original.Text = "Hello,我是余小章。";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 12);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 13);
            this.label1.TabIndex = 14;
            this.label1.Text = "Original Data";
            // 
            // button_Encrypt
            // 
            this.button_Encrypt.Location = new System.Drawing.Point(422, 7);
            this.button_Encrypt.Name = "button_Encrypt";
            this.button_Encrypt.Size = new System.Drawing.Size(75, 23);
            this.button_Encrypt.TabIndex = 6;
            this.button_Encrypt.Text = "加密";
            this.button_Encrypt.UseVisualStyleBackColor = true;
            this.button_Encrypt.Click += new System.EventHandler(this.button_Encrypt_Click);
            // 
            // richTextBox_A_PublicKey
            // 
            this.richTextBox_A_PublicKey.Location = new System.Drawing.Point(97, 36);
            this.richTextBox_A_PublicKey.Name = "richTextBox_A_PublicKey";
            this.richTextBox_A_PublicKey.Size = new System.Drawing.Size(319, 96);
            this.richTextBox_A_PublicKey.TabIndex = 27;
            this.richTextBox_A_PublicKey.Text = "";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(85, 13);
            this.label2.TabIndex = 28;
            this.label2.Text = "A廠商PublicKey";
            // 
            // B_Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(508, 385);
            this.Controls.Add(this.tabControl1);
            this.Name = "B_Form1";
            this.Text = "B廠商";
            this.Load += new System.EventHandler(this.Form1_Load);
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
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.RichTextBox richTextBox_EncryptContent;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_Original;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button_Encrypt;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RichTextBox richTextBox_PublicKey;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.RichTextBox richTextBox_PrivateKey;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Button button_ImportKey;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button button_GenerateSignature;
        private System.Windows.Forms.RichTextBox richTextBox_SignatureContent;
        private System.Windows.Forms.Button button_GetKey;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RichTextBox richTextBox_A_PublicKey;
    }
}

