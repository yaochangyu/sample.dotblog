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
            this.richTextBox_A_Key = new System.Windows.Forms.RichTextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.richTextBox_EncryptContent = new System.Windows.Forms.RichTextBox();
            this.textBox_DecryptContent = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.button_Decrypt = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(508, 385);
            this.tabControl1.TabIndex = 21;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.richTextBox_A_Key);
            this.tabPage1.Controls.Add(this.label3);
            this.tabPage1.Controls.Add(this.richTextBox_EncryptContent);
            this.tabPage1.Controls.Add(this.textBox_DecryptContent);
            this.tabPage1.Controls.Add(this.label4);
            this.tabPage1.Controls.Add(this.button_Decrypt);
            this.tabPage1.Controls.Add(this.label7);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(500, 359);
            this.tabPage1.TabIndex = 2;
            this.tabPage1.Text = "解密";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // richTextBox_A_Key
            // 
            this.richTextBox_A_Key.Location = new System.Drawing.Point(91, 15);
            this.richTextBox_A_Key.Name = "richTextBox_A_Key";
            this.richTextBox_A_Key.Size = new System.Drawing.Size(317, 99);
            this.richTextBox_A_Key.TabIndex = 45;
            this.richTextBox_A_Key.Text = "";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(57, 18);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(22, 13);
            this.label3.TabIndex = 44;
            this.label3.Text = "Kry";
            // 
            // richTextBox_EncryptContent
            // 
            this.richTextBox_EncryptContent.Location = new System.Drawing.Point(91, 118);
            this.richTextBox_EncryptContent.Name = "richTextBox_EncryptContent";
            this.richTextBox_EncryptContent.Size = new System.Drawing.Size(317, 99);
            this.richTextBox_EncryptContent.TabIndex = 43;
            this.richTextBox_EncryptContent.Text = "";
            // 
            // textBox_DecryptContent
            // 
            this.textBox_DecryptContent.Enabled = false;
            this.textBox_DecryptContent.Location = new System.Drawing.Point(91, 222);
            this.textBox_DecryptContent.Name = "textBox_DecryptContent";
            this.textBox_DecryptContent.Size = new System.Drawing.Size(317, 20);
            this.textBox_DecryptContent.TabIndex = 42;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(14, 225);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(70, 13);
            this.label4.TabIndex = 41;
            this.label4.Text = "Decrypt Data";
            // 
            // button_Decrypt
            // 
            this.button_Decrypt.Location = new System.Drawing.Point(417, 222);
            this.button_Decrypt.Name = "button_Decrypt";
            this.button_Decrypt.Size = new System.Drawing.Size(75, 23);
            this.button_Decrypt.TabIndex = 40;
            this.button_Decrypt.Text = "解密";
            this.button_Decrypt.UseVisualStyleBackColor = true;
            this.button_Decrypt.Click += new System.EventHandler(this.button_Decrypt_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(10, 118);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(69, 13);
            this.label7.TabIndex = 39;
            this.label7.Text = "Encrypt Data";
            // 
            // B_Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(508, 385);
            this.Controls.Add(this.tabControl1);
            this.Name = "B_Form1";
            this.Text = "B廠商";
            this.Load += new System.EventHandler(this.B_Form1_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.RichTextBox richTextBox_A_Key;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RichTextBox richTextBox_EncryptContent;
        private System.Windows.Forms.TextBox textBox_DecryptContent;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button_Decrypt;
        private System.Windows.Forms.Label label7;
    }
}

