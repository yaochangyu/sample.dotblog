namespace TPL_IProgress
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
            this.button_SyncStart = new System.Windows.Forms.Button();
            this.button_AsyncStart = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.button_AsyncStop = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button_SyncStart
            // 
            this.button_SyncStart.Location = new System.Drawing.Point(12, 12);
            this.button_SyncStart.Name = "button_SyncStart";
            this.button_SyncStart.Size = new System.Drawing.Size(75, 23);
            this.button_SyncStart.TabIndex = 0;
            this.button_SyncStart.Text = "Sync Start";
            this.button_SyncStart.UseVisualStyleBackColor = true;
            this.button_SyncStart.Click += new System.EventHandler(this.button_SyncStart_Click);
            // 
            // button_AsyncStart
            // 
            this.button_AsyncStart.Location = new System.Drawing.Point(12, 41);
            this.button_AsyncStart.Name = "button_AsyncStart";
            this.button_AsyncStart.Size = new System.Drawing.Size(75, 23);
            this.button_AsyncStart.TabIndex = 1;
            this.button_AsyncStart.Text = "Async Start";
            this.button_AsyncStart.UseVisualStyleBackColor = true;
            this.button_AsyncStart.Click += new System.EventHandler(this.button_AsyncStart_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 67);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(35, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "label1";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 80);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(35, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "label2";
            // 
            // button_AsyncStop
            // 
            this.button_AsyncStop.Location = new System.Drawing.Point(93, 41);
            this.button_AsyncStop.Name = "button_AsyncStop";
            this.button_AsyncStop.Size = new System.Drawing.Size(75, 23);
            this.button_AsyncStop.TabIndex = 4;
            this.button_AsyncStop.Text = "Async Stop";
            this.button_AsyncStop.UseVisualStyleBackColor = true;
            this.button_AsyncStop.Click += new System.EventHandler(this.button_AsyncStop_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Controls.Add(this.button_AsyncStop);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button_AsyncStart);
            this.Controls.Add(this.button_SyncStart);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button_SyncStart;
        private System.Windows.Forms.Button button_AsyncStart;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button_AsyncStop;
    }
}

