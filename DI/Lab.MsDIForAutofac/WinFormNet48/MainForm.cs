using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WinFormNet48
{
    public partial class MainForm : Form
    {
        private readonly Dictionary<string, Form> _subFormLook = new Dictionary<string, Form>();
        private          Form                     _previousForm;

        public MainForm()
        {
            this.InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Show("Form2");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Show("Form3");
        }

        private void Show(string name)
        {
            Form subForm;
            if (this._subFormLook.ContainsKey(name) == false)
            {
                subForm             = new Form2();
                subForm.TopLevel    = true;
                subForm.Visible     = true;
                subForm.WindowState = FormWindowState.Maximized;
                subForm.Dock        = DockStyle.Fill;
                //subForm.ControlBox  = false;
                this._subFormLook.Add(name, subForm);
                //this.splitContainer1.Panel2.Controls.Add(subForm);
            }

            subForm = this._subFormLook[name];

            subForm.Show();

            //if (this._previousForm != null)
            //{
            //    if (this._previousForm.Name != subForm.Name)
            //    {
            //        this._previousForm.Hide();
            //    }
            //}

            //this._previousForm = subForm;
        }
    }
}