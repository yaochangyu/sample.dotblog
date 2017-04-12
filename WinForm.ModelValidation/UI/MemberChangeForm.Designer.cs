namespace UI
{
    partial class MemberChangeForm
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.Label ageLabel;
            System.Windows.Forms.Label birthdayLabel;
            System.Windows.Forms.Label nameLabel;
            System.Windows.Forms.Label userIdLabel;
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.memberViewModelBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.Age_TextBox = new System.Windows.Forms.TextBox();
            this.Birthday_DateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.Name_TextBox = new System.Windows.Forms.TextBox();
            this.UserId_TextBox = new System.Windows.Forms.TextBox();
            ageLabel = new System.Windows.Forms.Label();
            birthdayLabel = new System.Windows.Forms.Label();
            nameLabel = new System.Windows.Forms.Label();
            userIdLabel = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.memberViewModelBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(ageLabel);
            this.groupBox1.Controls.Add(this.Age_TextBox);
            this.groupBox1.Controls.Add(birthdayLabel);
            this.groupBox1.Controls.Add(this.Birthday_DateTimePicker);
            this.groupBox1.Controls.Add(nameLabel);
            this.groupBox1.Controls.Add(this.Name_TextBox);
            this.groupBox1.Controls.Add(userIdLabel);
            this.groupBox1.Controls.Add(this.UserId_TextBox);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(400, 301);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "groupBox1";
            // 
            // memberViewModelBindingSource
            // 
            this.memberViewModelBindingSource.DataSource = typeof(Infrastructure.MemberViewModel);
            // 
            // ageLabel
            // 
            ageLabel.AutoSize = true;
            ageLabel.Location = new System.Drawing.Point(94, 83);
            ageLabel.Name = "ageLabel";
            ageLabel.Size = new System.Drawing.Size(29, 13);
            ageLabel.TabIndex = 0;
            ageLabel.Text = "Age:";
            // 
            // Age_TextBox
            // 
            this.Age_TextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.memberViewModelBindingSource, "Age", true));
            this.Age_TextBox.Location = new System.Drawing.Point(148, 80);
            this.Age_TextBox.Name = "Age_TextBox";
            this.Age_TextBox.Size = new System.Drawing.Size(200, 20);
            this.Age_TextBox.TabIndex = 1;
            // 
            // birthdayLabel
            // 
            birthdayLabel.AutoSize = true;
            birthdayLabel.Location = new System.Drawing.Point(94, 110);
            birthdayLabel.Name = "birthdayLabel";
            birthdayLabel.Size = new System.Drawing.Size(48, 13);
            birthdayLabel.TabIndex = 2;
            birthdayLabel.Text = "Birthday:";
            // 
            // Birthday_DateTimePicker
            // 
            this.Birthday_DateTimePicker.DataBindings.Add(new System.Windows.Forms.Binding("Value", this.memberViewModelBindingSource, "Birthday", true));
            this.Birthday_DateTimePicker.Location = new System.Drawing.Point(148, 106);
            this.Birthday_DateTimePicker.Name = "Birthday_DateTimePicker";
            this.Birthday_DateTimePicker.Size = new System.Drawing.Size(200, 20);
            this.Birthday_DateTimePicker.TabIndex = 3;
            // 
            // nameLabel
            // 
            nameLabel.AutoSize = true;
            nameLabel.Location = new System.Drawing.Point(94, 135);
            nameLabel.Name = "nameLabel";
            nameLabel.Size = new System.Drawing.Size(38, 13);
            nameLabel.TabIndex = 4;
            nameLabel.Text = "Name:";
            // 
            // Name_TextBox
            // 
            this.Name_TextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.memberViewModelBindingSource, "Name", true));
            this.Name_TextBox.Location = new System.Drawing.Point(148, 132);
            this.Name_TextBox.Name = "Name_TextBox";
            this.Name_TextBox.Size = new System.Drawing.Size(200, 20);
            this.Name_TextBox.TabIndex = 5;
            // 
            // userIdLabel
            // 
            userIdLabel.AutoSize = true;
            userIdLabel.Location = new System.Drawing.Point(94, 161);
            userIdLabel.Name = "userIdLabel";
            userIdLabel.Size = new System.Drawing.Size(44, 13);
            userIdLabel.TabIndex = 6;
            userIdLabel.Text = "User Id:";
            // 
            // UserId_TextBox
            // 
            this.UserId_TextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.memberViewModelBindingSource, "UserId", true));
            this.UserId_TextBox.Location = new System.Drawing.Point(148, 158);
            this.UserId_TextBox.Name = "UserId_TextBox";
            this.UserId_TextBox.Size = new System.Drawing.Size(200, 20);
            this.UserId_TextBox.TabIndex = 7;
            // 
            // MemberChangeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(400, 301);
            this.Controls.Add(this.groupBox1);
            this.Name = "MemberChangeForm";
            this.Text = "MemberChangeForm";
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.memberViewModelBindingSource)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TextBox Age_TextBox;
        private System.Windows.Forms.BindingSource memberViewModelBindingSource;
        private System.Windows.Forms.DateTimePicker Birthday_DateTimePicker;
        private System.Windows.Forms.TextBox Name_TextBox;
        private System.Windows.Forms.TextBox UserId_TextBox;
    }
}