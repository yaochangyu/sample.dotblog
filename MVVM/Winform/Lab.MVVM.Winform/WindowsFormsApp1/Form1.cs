using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ReactiveUI;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form, IViewFor<HomeViewModel>
    {
        public Form1()
        {
            this.InitializeComponent();
            if (this.ViewModel == null)
            {
                this.ViewModel = new HomeViewModel();
            }

            this.Bind(this.ViewModel, x => x.Input, x => x.Input_TextBox.Text);
            this.Bind(this.ViewModel, x => x.Status, x => x.Status_Label.Text);

            this.BindCommand(this.ViewModel, x => x.OK, x => x.OK_Button);//ok
            //this.BindCommand(this.ViewModel, x => x.OK, x => x.OK_Button, nameof(MouseDown));//ok

            //this.BindCommand(this.ViewModel, x => x.OK, x => x.OK_Button, "MouseDown");//ok

            //this.OK_Button
            //    .Events()
            //    .MouseDown
            //    .Where(p => p.Button == MouseButtons.Right)
            //    .InvokeCommand(this.ViewModel, p => p.OK);//fail

            this.OK_Button
                .Events()
                .MouseDown
                .Where(p => p.Button == MouseButtons.Right)
                .Subscribe(p => this.ViewModel.Run());//ok

        }

        object IViewFor.ViewModel
        {
            get { return this.ViewModel; }
            set { this.ViewModel = (HomeViewModel) value; }
        }

        public HomeViewModel ViewModel { get; set; }

        private void OK_Button_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                //TODO:Command 
            }
        }
    }
}
