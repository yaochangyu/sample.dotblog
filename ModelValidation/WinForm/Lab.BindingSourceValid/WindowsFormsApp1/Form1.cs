using System;
using System.ComponentModel;
using System.Windows.Forms;
using WindowsFormsApp1.Member;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private bool          _cancelEdit;
        private int           _previousBindingPosition;
        private InsertRequest _previousInsertRequest;

        public Form1()
        {
            this.InitializeComponent();
            var insertRequestBinding = this.InsertRequest_BindingSource;
            var insertRequestGrid    = this.InsertRequest_DataGridView;

            insertRequestBinding.DataSource = new BindingList<InsertRequest>
            {
                new InsertRequest
                {
                    Id = Guid.NewGuid(), Age = 20, Birthday = DateTime.Now, Name = "yao"
                }
            };
            this._previousBindingPosition = insertRequestBinding.Position;
            this._previousInsertRequest   = (InsertRequest) insertRequestBinding.Current;

            insertRequestBinding.PositionChanged += this.InsertRequest_BindingSource_PositionChanged;

            insertRequestGrid.MultiSelect   =  false;
            insertRequestGrid.SelectionMode =  DataGridViewSelectionMode.FullRowSelect;
            this.Id_TextBox.Click           += this.Id_TextBox_Click;
        }

        private void Add_Button_Click(object sender, EventArgs e)
        {
            this.InsertRequest_BindingSource.AddNew();
        }

        private void Delete_Button_Click(object sender, EventArgs e)
        {
            this._cancelEdit = true;
            this.InsertRequest_BindingSource.RemoveCurrent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            var insertRequestGrid = this.InsertRequest_DataGridView;
            insertRequestGrid.SelectionChanged += this.InsertRequest_DataGridView_SelectionChanged;
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

            if (this._cancelEdit)
            {
                this._cancelEdit = true;
                this.SetPreviousBindState();
                this.SetGridCurrentCell();

                return;
            }

            insertRequestBinding.EndEdit();
            this.RegisterPositionEvent(false);
            var request = this._previousInsertRequest;
            var isValid = this.ValidateControl(request, this.ErrorProvider, out var errorValidationResults);
            if (!isValid)
            {
                this.SetGridCurrentCell();
                this.RegisterPositionEvent(true);
                return;
            }

            this.ErrorProvider.Clear();
            this.RegisterPositionEvent(true);
            this.SetPreviousBindState();
        }

        private void InsertRequest_DataGridView_SelectionChanged(object sender, EventArgs e)
        {
            if (this._cancelEdit)
            {
                this._cancelEdit = true;
                return;
            }

            var insertRequestGrid = (DataGridView) sender;
            this.RegisterPositionEvent(false);
            var previousCell = this.InsertRequest_DataGridView[0, this._previousBindingPosition];
            insertRequestGrid.CurrentCell = previousCell;
            this.RegisterPositionEvent(true);
        }

        private void RegisterPositionEvent(bool isRegister)
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

        private void SetGridCurrentCell()
        {
            this.InsertRequest_DataGridView.ClearSelection();
            this.InsertRequest_DataGridView
                .Rows[this._previousBindingPosition]
                .Selected = true;
            var previousCell = this.InsertRequest_DataGridView[0, this._previousBindingPosition];
            this.InsertRequest_DataGridView.CurrentCell = previousCell;
        }

        private void SetPreviousBindState()
        {
            var insertRequestBinding = this.InsertRequest_BindingSource;
            this._previousBindingPosition = insertRequestBinding.Position;
            this._previousInsertRequest   = insertRequestBinding.Current as InsertRequest;
        }
    }
}