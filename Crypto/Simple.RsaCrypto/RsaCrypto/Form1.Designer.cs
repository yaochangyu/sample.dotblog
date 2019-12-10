namespace RsaCrypto
{
    partial class Form1
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
            this.textBox_Original = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.richTextBox_Encrypt = new System.Windows.Forms.RichTextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.textBox_Decrypt = new System.Windows.Forms.TextBox();
            this.button3 = new System.Windows.Forms.Button();
            this.textBox_OriginalFile = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_EncryptFile = new System.Windows.Forms.TextBox();
            this.button4 = new System.Windows.Forms.Button();
            this.textBox_DecryptFile = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.richTextBox_SignHash = new System.Windows.Forms.RichTextBox();
            this.button5 = new System.Windows.Forms.Button();
            this.textBox_SignOriginal = new System.Windows.Forms.TextBox();
            this.button6 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textBox_Original
            // 
            this.textBox_Original.Location = new System.Drawing.Point(86, 12);
            this.textBox_Original.Name = "textBox_Original";
            this.textBox_Original.Size = new System.Drawing.Size(161, 20);
            this.textBox_Original.TabIndex = 0;
            this.textBox_Original.Text = "Hello,我是余小章。";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Original Data";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 38);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "Encrypt Data";
            // 
            // richTextBox_Encrypt
            // 
            this.richTextBox_Encrypt.Enabled = false;
            this.richTextBox_Encrypt.Location = new System.Drawing.Point(86, 38);
            this.richTextBox_Encrypt.Name = "richTextBox_Encrypt";
            this.richTextBox_Encrypt.Size = new System.Drawing.Size(161, 96);
            this.richTextBox_Encrypt.TabIndex = 3;
            this.richTextBox_Encrypt.Text = "";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(253, 10);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 4;
            this.button1.Text = "加密";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(253, 39);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 5;
            this.button2.Text = "解密";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // textBox_Decrypt
            // 
            this.textBox_Decrypt.Enabled = false;
            this.textBox_Decrypt.Location = new System.Drawing.Point(86, 142);
            this.textBox_Decrypt.Name = "textBox_Decrypt";
            this.textBox_Decrypt.Size = new System.Drawing.Size(161, 20);
            this.textBox_Decrypt.TabIndex = 6;
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(253, 194);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(75, 23);
            this.button3.TabIndex = 7;
            this.button3.Text = "加密";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // textBox_OriginalFile
            // 
            this.textBox_OriginalFile.Location = new System.Drawing.Point(86, 196);
            this.textBox_OriginalFile.Name = "textBox_OriginalFile";
            this.textBox_OriginalFile.Size = new System.Drawing.Size(161, 20);
            this.textBox_OriginalFile.TabIndex = 8;
            this.textBox_OriginalFile.Text = "Original.txt";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(20, 199);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 13);
            this.label3.TabIndex = 9;
            this.label3.Text = "Original File";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(19, 224);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(62, 13);
            this.label4.TabIndex = 10;
            this.label4.Text = "Encrypt File";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(11, 142);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(70, 13);
            this.label5.TabIndex = 11;
            this.label5.Text = "Decrypt Data";
            // 
            // textBox_EncryptFile
            // 
            this.textBox_EncryptFile.Location = new System.Drawing.Point(86, 221);
            this.textBox_EncryptFile.Name = "textBox_EncryptFile";
            this.textBox_EncryptFile.Size = new System.Drawing.Size(161, 20);
            this.textBox_EncryptFile.TabIndex = 12;
            this.textBox_EncryptFile.Text = "Encrypt.txt";
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(253, 219);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(75, 23);
            this.button4.TabIndex = 13;
            this.button4.Text = "解密";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // textBox_DecryptFile
            // 
            this.textBox_DecryptFile.Location = new System.Drawing.Point(86, 247);
            this.textBox_DecryptFile.Name = "textBox_DecryptFile";
            this.textBox_DecryptFile.Size = new System.Drawing.Size(161, 20);
            this.textBox_DecryptFile.TabIndex = 15;
            this.textBox_DecryptFile.Text = "Decrypt.txt";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(18, 250);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(63, 13);
            this.label6.TabIndex = 14;
            this.label6.Text = "Decrypt File";
            // 
            // richTextBox_SignHash
            // 
            this.richTextBox_SignHash.Enabled = false;
            this.richTextBox_SignHash.Location = new System.Drawing.Point(369, 41);
            this.richTextBox_SignHash.Name = "richTextBox_SignHash";
            this.richTextBox_SignHash.Size = new System.Drawing.Size(161, 96);
            this.richTextBox_SignHash.TabIndex = 16;
            this.richTextBox_SignHash.Text = "";
            // 
            // button5
            // 
            this.button5.Location = new System.Drawing.Point(536, 12);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(75, 23);
            this.button5.TabIndex = 17;
            this.button5.Text = "簽章";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // textBox_SignOriginal
            // 
            this.textBox_SignOriginal.Location = new System.Drawing.Point(369, 15);
            this.textBox_SignOriginal.Name = "textBox_SignOriginal";
            this.textBox_SignOriginal.Size = new System.Drawing.Size(161, 20);
            this.textBox_SignOriginal.TabIndex = 18;
            this.textBox_SignOriginal.Text = "Hello,我是余小章。";
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(536, 41);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(75, 23);
            this.button6.TabIndex = 19;
            this.button6.Text = "驗証";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(651, 337);
            this.Controls.Add(this.button6);
            this.Controls.Add(this.textBox_SignOriginal);
            this.Controls.Add(this.button5);
            this.Controls.Add(this.richTextBox_SignHash);
            this.Controls.Add(this.textBox_DecryptFile);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.textBox_EncryptFile);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBox_OriginalFile);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.textBox_Decrypt);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.richTextBox_Encrypt);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox_Original);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_Original;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RichTextBox richTextBox_Encrypt;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox textBox_Decrypt;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.TextBox textBox_OriginalFile;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_EncryptFile;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.TextBox textBox_DecryptFile;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.RichTextBox richTextBox_SignHash;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.TextBox textBox_SignOriginal;
        private System.Windows.Forms.Button button6;
    }
}

