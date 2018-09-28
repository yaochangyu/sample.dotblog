namespace WindowsFormsApp1
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
            this.OK_Button = new System.Windows.Forms.Button();
            this.Input_TextBox = new System.Windows.Forms.TextBox();
            this.Status_Label = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // OK_Button
            // 
            this.OK_Button.Location = new System.Drawing.Point(308, 88);
            this.OK_Button.Name = "OK_Button";
            this.OK_Button.Size = new System.Drawing.Size(75, 23);
            this.OK_Button.TabIndex = 0;
            this.OK_Button.Text = "button1";
            this.OK_Button.UseVisualStyleBackColor = true;
            // 
            // Input_TextBox
            // 
            this.Input_TextBox.Location = new System.Drawing.Point(148, 88);
            this.Input_TextBox.Name = "Input_TextBox";
            this.Input_TextBox.Size = new System.Drawing.Size(100, 20);
            this.Input_TextBox.TabIndex = 1;
            // 
            // Status_Label
            // 
            this.Status_Label.AutoSize = true;
            this.Status_Label.Location = new System.Drawing.Point(148, 141);
            this.Status_Label.Name = "Status_Label";
            this.Status_Label.Size = new System.Drawing.Size(35, 13);
            this.Status_Label.TabIndex = 2;
            this.Status_Label.Text = "label1";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.Status_Label);
            this.Controls.Add(this.Input_TextBox);
            this.Controls.Add(this.OK_Button);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button OK_Button;
        private System.Windows.Forms.TextBox Input_TextBox;
        private System.Windows.Forms.Label Status_Label;
    }
}

