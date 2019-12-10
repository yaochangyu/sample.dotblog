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
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.button_GenerateKey = new System.Windows.Forms.Button();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.label2 = new System.Windows.Forms.Label();
            this.richTextBox_A_Key = new System.Windows.Forms.RichTextBox();
            this.richTextBox_EncryptContent = new System.Windows.Forms.RichTextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox_Original = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.button_Encrypt = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage3);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(508, 385);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.groupBox1);
            this.tabPage1.Controls.Add(this.button_GenerateKey);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(500, 359);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Key";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.listBox1);
            this.groupBox1.Location = new System.Drawing.Point(8, 35);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(484, 282);
            this.groupBox1.TabIndex = 5;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Key";
            // 
            // listBox1
            // 
            this.listBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(3, 16);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(478, 263);
            this.listBox1.TabIndex = 0;
            // 
            // button_GenerateKey
            // 
            this.button_GenerateKey.Location = new System.Drawing.Point(11, 6);
            this.button_GenerateKey.Name = "button_GenerateKey";
            this.button_GenerateKey.Size = new System.Drawing.Size(75, 23);
            this.button_GenerateKey.TabIndex = 1;
            this.button_GenerateKey.Text = "產生新金鑰";
            this.button_GenerateKey.UseVisualStyleBackColor = true;
            this.button_GenerateKey.Click += new System.EventHandler(this.button_GenerateKey_Click);
            // 
            // tabPage3
            // 
            this.tabPage3.Controls.Add(this.label2);
            this.tabPage3.Controls.Add(this.richTextBox_A_Key);
            this.tabPage3.Controls.Add(this.richTextBox_EncryptContent);
            this.tabPage3.Controls.Add(this.label4);
            this.tabPage3.Controls.Add(this.textBox_Original);
            this.tabPage3.Controls.Add(this.label7);
            this.tabPage3.Controls.Add(this.button_Encrypt);
            this.tabPage3.Location = new System.Drawing.Point(4, 22);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage3.Size = new System.Drawing.Size(500, 359);
            this.tabPage3.TabIndex = 2;
            this.tabPage3.Text = "加密";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(48, 46);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(25, 13);
            this.label2.TabIndex = 35;
            this.label2.Text = "Key";
            // 
            // richTextBox_A_Key
            // 
            this.richTextBox_A_Key.Location = new System.Drawing.Point(89, 43);
            this.richTextBox_A_Key.Name = "richTextBox_A_Key";
            this.richTextBox_A_Key.Size = new System.Drawing.Size(319, 96);
            this.richTextBox_A_Key.TabIndex = 34;
            this.richTextBox_A_Key.Text = "";
            // 
            // richTextBox_EncryptContent
            // 
            this.richTextBox_EncryptContent.Location = new System.Drawing.Point(89, 145);
            this.richTextBox_EncryptContent.Name = "richTextBox_EncryptContent";
            this.richTextBox_EncryptContent.Size = new System.Drawing.Size(319, 96);
            this.richTextBox_EncryptContent.TabIndex = 33;
            this.richTextBox_EncryptContent.Text = "";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(4, 150);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(69, 13);
            this.label4.TabIndex = 32;
            this.label4.Text = "Encrypt Data";
            // 
            // textBox_Original
            // 
            this.textBox_Original.Location = new System.Drawing.Point(89, 17);
            this.textBox_Original.Name = "textBox_Original";
            this.textBox_Original.Size = new System.Drawing.Size(319, 20);
            this.textBox_Original.TabIndex = 31;
            this.textBox_Original.Text = "Hello,我是余小章，欢迎收看我的blog，请多多指教啊，";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(8, 19);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(68, 13);
            this.label7.TabIndex = 30;
            this.label7.Text = "Original Data";
            // 
            // button_Encrypt
            // 
            this.button_Encrypt.Location = new System.Drawing.Point(414, 14);
            this.button_Encrypt.Name = "button_Encrypt";
            this.button_Encrypt.Size = new System.Drawing.Size(75, 23);
            this.button_Encrypt.TabIndex = 29;
            this.button_Encrypt.Text = "加密";
            this.button_Encrypt.UseVisualStyleBackColor = true;
            this.button_Encrypt.Click += new System.EventHandler(this.button_Encrypt_Click);
            // 
            // A_Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(508, 385);
            this.Controls.Add(this.tabControl1);
            this.Name = "A_Form1";
            this.Text = "A廠商";
            this.Load += new System.EventHandler(this.A_Form1_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.tabPage3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button button_GenerateKey;
        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RichTextBox richTextBox_A_Key;
        private System.Windows.Forms.RichTextBox richTextBox_EncryptContent;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox_Original;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Button button_Encrypt;

    }
}

