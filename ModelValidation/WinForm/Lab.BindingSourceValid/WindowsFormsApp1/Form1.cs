using System;
using System.ComponentModel;
using System.Windows.Forms;
using WindowsFormsApp1.Member;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private bool          _isDelete;
        private int           _previousBindingPosition;
        private InsertRequest _previousInsertRequest;
        private static string DateTimeFormat = "yyyy/MM/dd hh:mm:ss";

        public Form1()
        {
            this.InitializeComponent();
            var insertRequestBinding = this.InsertRequest_BindingSource;
            var insertRequestGrid    = this.InsertRequest_DataGridView;

            insertRequestGrid.MultiSelect   = false;
            insertRequestGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            insertRequestBinding.DataSource = new BindingList<InsertRequest>
            {
                new InsertRequest
                {
                    Id = Guid.NewGuid(), Age = 20, Birthday = DateTime.Now, Name = "yao"
                }
            };
            this.SetPreviousRow();
            this.Id_TextBox.Click                     += this.Id_TextBox_Click;
            this.Birthday_DateTimePicker.Format       =  DateTimePickerFormat.Custom;
            this.Birthday_DateTimePicker.CustomFormat =  DateTimeFormat;
            this.Birthday_DateTimePicker.ValueChanged += this.Birthday_DateTimePicker_ValueChanged;
        }

        private void Add_Button_Click(object sender, EventArgs e)
        {
            this.InsertRequest_BindingSource.AddNew();
            this.Birthday_DateTimePicker.CustomFormat = " ";
        }

        private void Birthday_DateTimePicker_ValueChanged(object sender, EventArgs e)
        {
            this.Birthday_DateTimePicker.CustomFormat = DateTimeFormat;
        }

        private void Delete_Button_Click(object sender, EventArgs e)
        {
            this._isDelete = true;
            this.InsertRequest_BindingSource.RemoveCurrent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            this.RegisterChangeRowEvent(true);
        }

        private void Id_TextBox_Click(object sender, EventArgs e)
        {
            var textBox = (TextBox) sender;
            if (string.IsNullOrWhiteSpace(textBox.Text))
            {
                textBox.Text = Guid.NewGuid().ToString();
            }
        }

        private void InsertRequest_BindingSource_PositionChanged(object sender, EventArgs e)
        {
            var insertRequestBinding = (BindingSource) sender;
            var insertRequest        = this._previousInsertRequest;
            var errorProvider        = this.ErrorProvider;

            if (this._isDelete)
            {
                this._isDelete = false;
                this.SetPreviousRow();
                this.StayCurrentRow();
                errorProvider.Clear();
                return;
            }

            insertRequestBinding.EndEdit();
            this.RegisterChangeRowEvent(false);
            var isValid = this.ValidateControl(insertRequest, errorProvider, out var errorValidationResults);
            if (!isValid)
            {
                this.StayCurrentRow();
                this.RegisterChangeRowEvent(true);
                return;
            }

            this.RegisterChangeRowEvent(true);
            this.SetPreviousRow();
        }

        private void InsertRequest_DataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (this._isDelete)
            {
                this._isDelete = false;
                return;
            }

            this.RegisterChangeRowEvent(false);
            this.StayCurrentRow();
            this.RegisterChangeRowEvent(true);
        }

        private void RegisterChangeRowEvent(bool isRegister)
        {
            var requestBinding = this.InsertRequest_BindingSource;
            var dataGridView   = this.InsertRequest_DataGridView;
            if (isRegister)
            {
                requestBinding.PositionChanged += this.InsertRequest_BindingSource_PositionChanged;
                dataGridView.SelectionChanged  += this.InsertRequest_DataGridView_SelectionChanged;
            }
            else
            {
                requestBinding.PositionChanged -= this.InsertRequest_BindingSource_PositionChanged;
                dataGridView.SelectionChanged  -= this.InsertRequest_DataGridView_SelectionChanged;
            }
        }

        private void SetPreviousRow()
        {
            var insertRequestBinding = this.InsertRequest_BindingSource;
            this._previousBindingPosition = insertRequestBinding.Position;
            this._previousInsertRequest   = insertRequestBinding.Current as InsertRequest;
            if (this._previousInsertRequest.Birthday.HasValue)
            {
                this.Birthday_DateTimePicker.CustomFormat = DateTimeFormat;
            }
        }

        private void StayCurrentRow()
        {
            var insertRequestGrid = this.InsertRequest_DataGridView;
            var insertRequestBind = this.InsertRequest_BindingSource;

            insertRequestGrid.ClearSelection();
            insertRequestGrid
                .Rows[this._previousBindingPosition]
                .Selected = true;
            var previousCell = insertRequestGrid[0, this._previousBindingPosition];
            insertRequestGrid.CurrentCell = previousCell;
            insertRequestBind.Position    = this._previousBindingPosition;
        }
    }
}