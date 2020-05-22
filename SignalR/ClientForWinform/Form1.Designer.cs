﻿namespace ClientForWinform
{
    partial class Form1
    {
        /// <summary>
        /// 設計工具所需的變數。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清除任何使用中的資源。
        /// </summary>
        /// <param name="disposing">如果應該處置 Managed 資源則為 true，否則為 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form 設計工具產生的程式碼

        /// <summary>
        /// 此為設計工具支援所需的方法 - 請勿使用程式碼編輯器修改
        /// 這個方法的內容。
        /// </summary>
        private void InitializeComponent()
        {
            this.Name_TextBox = new System.Windows.Forms.TextBox();
            this.Conutry_TextBox = new System.Windows.Forms.TextBox();
            this.SendMessage_Button = new System.Windows.Forms.Button();
            this.Messages_TextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // Name_TextBox
            // 
            this.Name_TextBox.Location = new System.Drawing.Point(87, 13);
            this.Name_TextBox.Name = "Name_TextBox";
            this.Name_TextBox.Size = new System.Drawing.Size(152, 22);
            this.Name_TextBox.TabIndex = 0;
            // 
            // Conutry_TextBox
            // 
            this.Conutry_TextBox.Location = new System.Drawing.Point(87, 51);
            this.Conutry_TextBox.Name = "Conutry_TextBox";
            this.Conutry_TextBox.Size = new System.Drawing.Size(152, 22);
            this.Conutry_TextBox.TabIndex = 1;
            // 
            // SendMessage_Button
            // 
            this.SendMessage_Button.Location = new System.Drawing.Point(254, 37);
            this.SendMessage_Button.Name = "SendMessage_Button";
            this.SendMessage_Button.Size = new System.Drawing.Size(98, 36);
            this.SendMessage_Button.TabIndex = 2;
            this.SendMessage_Button.Text = "Say Hello";
            this.SendMessage_Button.UseVisualStyleBackColor = true;
            this.SendMessage_Button.Click += new System.EventHandler(this.SendMessageButton_Click);
            // 
            // Messages_TextBox
            // 
            this.Messages_TextBox.Location = new System.Drawing.Point(15, 120);
            this.Messages_TextBox.Multiline = true;
            this.Messages_TextBox.Name = "Messages_TextBox";
            this.Messages_TextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.Messages_TextBox.Size = new System.Drawing.Size(547, 402);
            this.Messages_TextBox.TabIndex = 3;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(27, 18);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(49, 17);
            this.label1.TabIndex = 4;
            this.label1.Text = "Name:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 54);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(61, 17);
            this.label2.TabIndex = 5;
            this.label2.Text = "Country:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 90);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(147, 17);
            this.label3.TabIndex = 6;
            this.label3.Text = "Message from Server:";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(571, 534);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.Messages_TextBox);
            this.Controls.Add(this.SendMessage_Button);
            this.Controls.Add(this.Conutry_TextBox);
            this.Controls.Add(this.Name_TextBox);
            this.Name = "Form1";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox Name_TextBox;
        private System.Windows.Forms.TextBox Conutry_TextBox;
        private System.Windows.Forms.Button SendMessage_Button;
        private System.Windows.Forms.TextBox Messages_TextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
    }
}

