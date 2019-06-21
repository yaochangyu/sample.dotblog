namespace Lab.CertFromCA.Winform
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.Label commonNameLabel;
            System.Windows.Forms.Label countryLabel;
            System.Windows.Forms.Label localityLabel;
            System.Windows.Forms.Label organizationLabel;
            System.Windows.Forms.Label organizationUnitLabel;
            System.Windows.Forms.Label stateLabel;
            System.Windows.Forms.Label serverLabel;
            System.Windows.Forms.Label templateNameLabel;
            System.Windows.Forms.Label passwordLabel;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.SelectCA_Button = new System.Windows.Forms.Button();
            this.commonNameTextBox = new System.Windows.Forms.TextBox();
            this.SubjectBodyBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.countryTextBox = new System.Windows.Forms.TextBox();
            this.localityTextBox = new System.Windows.Forms.TextBox();
            this.organizationTextBox = new System.Windows.Forms.TextBox();
            this.organizationUnitTextBox = new System.Windows.Forms.TextBox();
            this.stateTextBox = new System.Windows.Forms.TextBox();
            this.Encroll_Button = new System.Windows.Forms.Button();
            this.serverTextBox = new System.Windows.Forms.TextBox();
            this.CaConfigBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.templateNameComboBox = new System.Windows.Forms.ComboBox();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.passwordTextBox = new System.Windows.Forms.TextBox();
            this.richTextBox2 = new System.Windows.Forms.RichTextBox();
            commonNameLabel = new System.Windows.Forms.Label();
            countryLabel = new System.Windows.Forms.Label();
            localityLabel = new System.Windows.Forms.Label();
            organizationLabel = new System.Windows.Forms.Label();
            organizationUnitLabel = new System.Windows.Forms.Label();
            stateLabel = new System.Windows.Forms.Label();
            serverLabel = new System.Windows.Forms.Label();
            templateNameLabel = new System.Windows.Forms.Label();
            passwordLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.SubjectBodyBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.CaConfigBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // commonNameLabel
            // 
            commonNameLabel.AutoSize = true;
            commonNameLabel.Location = new System.Drawing.Point(15, 105);
            commonNameLabel.Name = "commonNameLabel";
            commonNameLabel.Size = new System.Drawing.Size(82, 13);
            commonNameLabel.TabIndex = 4;
            commonNameLabel.Text = "Common Name:";
            // 
            // countryLabel
            // 
            countryLabel.AutoSize = true;
            countryLabel.Location = new System.Drawing.Point(51, 131);
            countryLabel.Name = "countryLabel";
            countryLabel.Size = new System.Drawing.Size(46, 13);
            countryLabel.TabIndex = 6;
            countryLabel.Text = "Country:";
            // 
            // localityLabel
            // 
            localityLabel.AutoSize = true;
            localityLabel.Location = new System.Drawing.Point(51, 157);
            localityLabel.Name = "localityLabel";
            localityLabel.Size = new System.Drawing.Size(46, 13);
            localityLabel.TabIndex = 8;
            localityLabel.Text = "Locality:";
            // 
            // organizationLabel
            // 
            organizationLabel.AutoSize = true;
            organizationLabel.Location = new System.Drawing.Point(28, 183);
            organizationLabel.Name = "organizationLabel";
            organizationLabel.Size = new System.Drawing.Size(69, 13);
            organizationLabel.TabIndex = 10;
            organizationLabel.Text = "Organization:";
            // 
            // organizationUnitLabel
            // 
            organizationUnitLabel.AutoSize = true;
            organizationUnitLabel.Location = new System.Drawing.Point(6, 209);
            organizationUnitLabel.Name = "organizationUnitLabel";
            organizationUnitLabel.Size = new System.Drawing.Size(91, 13);
            organizationUnitLabel.TabIndex = 12;
            organizationUnitLabel.Text = "Organization Unit:";
            // 
            // stateLabel
            // 
            stateLabel.AutoSize = true;
            stateLabel.Location = new System.Drawing.Point(62, 235);
            stateLabel.Name = "stateLabel";
            stateLabel.Size = new System.Drawing.Size(35, 13);
            stateLabel.TabIndex = 14;
            stateLabel.Text = "State:";
            // 
            // serverLabel
            // 
            serverLabel.AutoSize = true;
            serverLabel.Location = new System.Drawing.Point(56, 24);
            serverLabel.Name = "serverLabel";
            serverLabel.Size = new System.Drawing.Size(41, 13);
            serverLabel.TabIndex = 16;
            serverLabel.Text = "Server:";
            // 
            // templateNameLabel
            // 
            templateNameLabel.AutoSize = true;
            templateNameLabel.Location = new System.Drawing.Point(12, 52);
            templateNameLabel.Name = "templateNameLabel";
            templateNameLabel.Size = new System.Drawing.Size(85, 13);
            templateNameLabel.TabIndex = 18;
            templateNameLabel.Text = "Template Name:";
            // 
            // passwordLabel
            // 
            passwordLabel.AutoSize = true;
            passwordLabel.Location = new System.Drawing.Point(83, 278);
            passwordLabel.Name = "passwordLabel";
            passwordLabel.Size = new System.Drawing.Size(56, 13);
            passwordLabel.TabIndex = 20;
            passwordLabel.Text = "Password:";
            // 
            // SelectCA_Button
            // 
            this.SelectCA_Button.Location = new System.Drawing.Point(332, 23);
            this.SelectCA_Button.Name = "SelectCA_Button";
            this.SelectCA_Button.Size = new System.Drawing.Size(75, 23);
            this.SelectCA_Button.TabIndex = 0;
            this.SelectCA_Button.Text = "Select CA";
            this.SelectCA_Button.UseVisualStyleBackColor = true;
            this.SelectCA_Button.Click += new System.EventHandler(this.SelectCA_Button_Click);
            // 
            // commonNameTextBox
            // 
            this.commonNameTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.SubjectBodyBindingSource, "CommonName", true));
            this.commonNameTextBox.Location = new System.Drawing.Point(112, 102);
            this.commonNameTextBox.Name = "commonNameTextBox";
            this.commonNameTextBox.Size = new System.Drawing.Size(214, 20);
            this.commonNameTextBox.TabIndex = 5;
            this.commonNameTextBox.Text = "*.lab.local,*.lab1.local";
            // 
            // SubjectBodyBindingSource
            // 
            this.SubjectBodyBindingSource.DataSource = typeof(Lab.CertFromCA.SubjectBody);
            // 
            // countryTextBox
            // 
            this.countryTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.SubjectBodyBindingSource, "Country", true));
            this.countryTextBox.Location = new System.Drawing.Point(112, 128);
            this.countryTextBox.Name = "countryTextBox";
            this.countryTextBox.Size = new System.Drawing.Size(214, 20);
            this.countryTextBox.TabIndex = 7;
            // 
            // localityTextBox
            // 
            this.localityTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.SubjectBodyBindingSource, "Locality", true));
            this.localityTextBox.Location = new System.Drawing.Point(112, 154);
            this.localityTextBox.Name = "localityTextBox";
            this.localityTextBox.Size = new System.Drawing.Size(214, 20);
            this.localityTextBox.TabIndex = 9;
            // 
            // organizationTextBox
            // 
            this.organizationTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.SubjectBodyBindingSource, "Organization", true));
            this.organizationTextBox.Location = new System.Drawing.Point(112, 180);
            this.organizationTextBox.Name = "organizationTextBox";
            this.organizationTextBox.Size = new System.Drawing.Size(214, 20);
            this.organizationTextBox.TabIndex = 11;
            // 
            // organizationUnitTextBox
            // 
            this.organizationUnitTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.SubjectBodyBindingSource, "OrganizationUnit", true));
            this.organizationUnitTextBox.Location = new System.Drawing.Point(112, 206);
            this.organizationUnitTextBox.Name = "organizationUnitTextBox";
            this.organizationUnitTextBox.Size = new System.Drawing.Size(214, 20);
            this.organizationUnitTextBox.TabIndex = 13;
            // 
            // stateTextBox
            // 
            this.stateTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.SubjectBodyBindingSource, "State", true));
            this.stateTextBox.Location = new System.Drawing.Point(112, 232);
            this.stateTextBox.Name = "stateTextBox";
            this.stateTextBox.Size = new System.Drawing.Size(214, 20);
            this.stateTextBox.TabIndex = 15;
            // 
            // Encroll_Button
            // 
            this.Encroll_Button.Location = new System.Drawing.Point(251, 273);
            this.Encroll_Button.Name = "Encroll_Button";
            this.Encroll_Button.Size = new System.Drawing.Size(75, 23);
            this.Encroll_Button.TabIndex = 16;
            this.Encroll_Button.Text = "Encroll";
            this.Encroll_Button.UseVisualStyleBackColor = true;
            this.Encroll_Button.Click += new System.EventHandler(this.Encroll_Button_Click);
            // 
            // serverTextBox
            // 
            this.serverTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.CaConfigBindingSource, "Server", true));
            this.serverTextBox.Location = new System.Drawing.Point(112, 24);
            this.serverTextBox.Name = "serverTextBox";
            this.serverTextBox.Size = new System.Drawing.Size(214, 20);
            this.serverTextBox.TabIndex = 17;
            // 
            // CaConfigBindingSource
            // 
            this.CaConfigBindingSource.DataSource = typeof(Lab.CertFromCA.Winform.CaConfig);
            // 
            // templateNameComboBox
            // 
            this.templateNameComboBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.CaConfigBindingSource, "TemplateName", true));
            this.templateNameComboBox.FormattingEnabled = true;
            this.templateNameComboBox.Location = new System.Drawing.Point(112, 52);
            this.templateNameComboBox.Name = "templateNameComboBox";
            this.templateNameComboBox.Size = new System.Drawing.Size(214, 21);
            this.templateNameComboBox.TabIndex = 19;
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(342, 102);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(282, 72);
            this.richTextBox1.TabIndex = 20;
            this.richTextBox1.Text = "";
            // 
            // passwordTextBox
            // 
            this.passwordTextBox.DataBindings.Add(new System.Windows.Forms.Binding("Text", this.CaConfigBindingSource, "Password", true));
            this.passwordTextBox.Location = new System.Drawing.Point(145, 275);
            this.passwordTextBox.Name = "passwordTextBox";
            this.passwordTextBox.Size = new System.Drawing.Size(100, 20);
            this.passwordTextBox.TabIndex = 21;
            // 
            // richTextBox2
            // 
            this.richTextBox2.Location = new System.Drawing.Point(342, 180);
            this.richTextBox2.Name = "richTextBox2";
            this.richTextBox2.Size = new System.Drawing.Size(282, 72);
            this.richTextBox2.TabIndex = 22;
            this.richTextBox2.Text = "";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(645, 316);
            this.Controls.Add(this.richTextBox2);
            this.Controls.Add(passwordLabel);
            this.Controls.Add(this.passwordTextBox);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(templateNameLabel);
            this.Controls.Add(this.templateNameComboBox);
            this.Controls.Add(serverLabel);
            this.Controls.Add(this.serverTextBox);
            this.Controls.Add(this.Encroll_Button);
            this.Controls.Add(commonNameLabel);
            this.Controls.Add(this.commonNameTextBox);
            this.Controls.Add(countryLabel);
            this.Controls.Add(this.countryTextBox);
            this.Controls.Add(localityLabel);
            this.Controls.Add(this.localityTextBox);
            this.Controls.Add(organizationLabel);
            this.Controls.Add(this.organizationTextBox);
            this.Controls.Add(organizationUnitLabel);
            this.Controls.Add(this.organizationUnitTextBox);
            this.Controls.Add(stateLabel);
            this.Controls.Add(this.stateTextBox);
            this.Controls.Add(this.SelectCA_Button);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "憑證產生工具";
            ((System.ComponentModel.ISupportInitialize)(this.SubjectBodyBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.CaConfigBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button SelectCA_Button;
        private System.Windows.Forms.BindingSource SubjectBodyBindingSource;
        private System.Windows.Forms.TextBox commonNameTextBox;
        private System.Windows.Forms.TextBox countryTextBox;
        private System.Windows.Forms.TextBox localityTextBox;
        private System.Windows.Forms.TextBox organizationTextBox;
        private System.Windows.Forms.TextBox organizationUnitTextBox;
        private System.Windows.Forms.TextBox stateTextBox;
        private System.Windows.Forms.Button Encroll_Button;
        private System.Windows.Forms.BindingSource CaConfigBindingSource;
        private System.Windows.Forms.TextBox serverTextBox;
        private System.Windows.Forms.ComboBox templateNameComboBox;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.TextBox passwordTextBox;
        private System.Windows.Forms.RichTextBox richTextBox2;
    }
}

