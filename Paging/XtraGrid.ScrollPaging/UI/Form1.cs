using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using BLL;
using DevExpress.XtraGrid.Views.Base;
using DevExpress.XtraGrid.Views.Grid;
using Infrastructure;

namespace UI
{
    public partial class Form1 : Form
    {
        private MemberBLL _bll;

        private Paging _paging;
        private BindingSource _queryResultBindingSource;

        //private ObservableCollection<MemberViewModel> _queryResults;
        //private BindingList<MemberViewModel> _queryResults;
        private List<MemberViewModel> _queryResults;

        public Form1()
        {
            this.InitializeComponent();
            this.InitializeInstance();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //this._queryResults = new ObservableCollection<MemberViewModel>(this._bll.GetMasters(this._paging).ToList());
            //this._queryResults = new BindingList<MemberViewModel>(this._bll.GetMasters(this._paging).ToList());
            this._queryResults = new List<MemberViewModel>(this._bll.GetMasters(this._paging).ToList());

            this._queryResultBindingSource.DataSource = this._queryResults;
        }

        private void InitializeInstance()
        {
            if (this._paging == null)
            {
                this._paging = new Paging
                {
                    PageIndex = 0,
                    RowSize = 10,
                    SortExpression = "SequentialId Asc"
                };
            }

            if (this._bll == null)
            {
                this._bll = new MemberBLL();
            }
            if (this._queryResultBindingSource == null)
            {
                this._queryResultBindingSource = new BindingSource();
            }

            this.Master_GridView.HorzScrollVisibility = ScrollVisibility.Always;
            this.Master_GridView.VertScrollVisibility = ScrollVisibility.Always;
            this.Master_GridView.OptionsDetail.AllowExpandEmptyDetails = true;

            this.Master_GridView.OptionsNavigation.EnterMoveNextColumn = true;
            this.Master_GridView.OptionsNavigation.AutoFocusNewRow = true;

            this.Master_GridView.MasterRowExpanding += this.Master_GridView_MasterRowExpanding;
            this.Master_GridView.TopRowChanged += this.Master_GridView_TopRowChanged;

            this.QueryResult_GridControl.UseEmbeddedNavigator = true;
            this._queryResultBindingSource.PositionChanged += this.QueryResult_BindingSource_PositionChanged;

            this.QueryResult_GridControl.DataSource = this._queryResultBindingSource;
        }

        private void Master_GridView_MasterRowExpanding(object sender, MasterRowCanExpandEventArgs e)
        {
            var master = this._queryResultBindingSource.Current as MemberViewModel;
            var resultLogs = this._bll.GetDetails(master.Id).ToList();
            master.MemberLogs = resultLogs;
        }

        private void Master_GridView_TopRowChanged(object sender, EventArgs e)
        {
            var sourceGridView = (GridView) sender;
            if (sourceGridView.IsRowVisible(sourceGridView.DataRowCount - 1) != RowVisibleState.Visible)
            {
                return;
            }

            if (this._paging.TotalCount <= this._queryResults.Count)
            {
                return;
            }

            this._paging.PageIndex++;
            var queryResult = this._bll.GetMasters(this._paging);

            //this.Master_GridView.BeginDataUpdate();
            this.Master_GridView.BeginUpdate();
            this._queryResults.AddRange(queryResult);

            //this.Master_GridView.EndDataUpdate();
            this.Master_GridView.EndUpdate();

            //sourceGridView.RefreshData();
        }

        private void QueryResult_BindingSource_PositionChanged(object sender, EventArgs e)
        {
            var source = (BindingSource) sender;
            if (source.Position != this._queryResults.Count - 1)
            {
                return;
            }

            if (this._paging.TotalCount == this._queryResults.Count)
            {
                return;
            }

            this._paging.PageIndex++;
            var queryResult = this._bll.GetMasters(this._paging);
            foreach (var item in queryResult)
            {
                this._queryResults.Add(item);
            }

            this.Master_GridView.RefreshData();

        }
    }
}