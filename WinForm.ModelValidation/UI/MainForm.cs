using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BLL;
using DevExpress.Utils;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using Infrastructure;

namespace UI
{
    public partial class MainForm : Form
    {
        private readonly MemberBLL _bll;
        private readonly BindingSource _queryResultBindingSource;

        public MainForm()
        {
            this.InitializeComponent();
            this.Master_GridView.OptionsDetail.AllowExpandEmptyDetails = true;
            this.Master_GridView.MasterRowExpanding += this.Master_GridView_MasterRowExpanding;
            this.Master_GridView.CustomDrawCell += this.gridView1_CustomDrawCell;

            if (this._bll == null)
            {
                this._bll = new MemberBLL();
            }
            if (this._queryResultBindingSource == null)
            {
                this._queryResultBindingSource = new BindingSource();
            }
            this.QueryResult_GridControl.DataSource = this._queryResultBindingSource;

            this.QueryResult_GridControl.ViewRegistered += this.QueryResult_GridControl_ViewRegistered;
            this.QueryResult_GridControl.ViewRemoved += this.QueryResult_GridControl_ViewRemoved;
        }

        private void QueryResult_GridControl_ViewRegistered(object sender, ViewOperationEventArgs e)
        {
            var view = e.View as GridView;
            if (view.DataRowCount != 0)
            {
                return;
            }

            view.OptionsView.ShowGroupPanel = false;
            view.OptionsView.ShowColumnHeaders = false;
            view.OptionsView.ShowFooter = true;
            view.CustomDrawFooter += this.View_CustomDrawFooter;
        }

        private void View_CustomDrawFooter(object sender, RowObjectCustomDrawEventArgs e)
        {
            e.DefaultDraw();
            e.Handled = true;
            e.Graphics.DrawString("No Data", AppearanceObject.DefaultFont, Brushes.Black,
                                  new Point(e.Bounds.X + e.Bounds.Width / 2, e.Bounds.Y + e.Bounds.Height / 4));
        }

        private void QueryResult_GridControl_ViewRemoved(object sender, ViewOperationEventArgs e)
        {
            var view = e.View as GridView;
            view.CustomDrawFooter -= this.View_CustomDrawFooter;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var queryResult = this._bll.GetMasters(new Paging
            {
                RowSize = 100,
                SortExpression = "SequentialId Asc"
            });
            this._queryResultBindingSource.DataSource = queryResult;
        }

        private void Master_GridView_MasterRowExpanding(object sender, MasterRowCanExpandEventArgs e)
        {
            var master = this._queryResultBindingSource.Current as MemberViewModel;
            var resultLogs = this._bll.GetDetails(master.Id);

            master.MemberLogs = e.RowHandle % 2 == 0 ? resultLogs.ToList() : new List<MemberLogViewModel>();

            //(e.Cell as GridCellInfo).CellButtonRect = Rectangle.Empty;
        }

        private void gridView1_CustomDrawCell(object sender, RowCellCustomDrawEventArgs e)
        {
            var view = sender as GridView;
            if (e.Column.VisibleIndex == 0 &&
                view.OptionsDetail.SmartDetailExpandButtonMode != DetailExpandButtonMode.AlwaysEnabled)
            {
                bool isMasterRowEmpty;
                if (view.OptionsDetail.SmartDetailExpandButtonMode == DetailExpandButtonMode.CheckAllDetails)
                {
                    isMasterRowEmpty = true;
                    for (var i = 0; i < view.GetRelationCount(e.RowHandle); i++)
                    {
                        if (!view.IsMasterRowEmptyEx(e.RowHandle, i))
                        {
                            isMasterRowEmpty = false;
                            break;
                        }
                    }
                }
                else
                {
                    isMasterRowEmpty = view.IsMasterRowEmpty(e.RowHandle);
                }

                if (isMasterRowEmpty)
                {
                    //(e.Cell as GridCellInfo).CellButtonRect = Rectangle.Empty;
                }
            }
        }

        private void Add_ToolStripButton_Click(object sender, EventArgs e)
        {
            MemberChangeForm form=new MemberChangeForm();
            form.ShowDialog();
        }
    }
}