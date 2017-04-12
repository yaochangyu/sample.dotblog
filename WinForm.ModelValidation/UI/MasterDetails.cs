using System.Collections.Generic;
using System.Windows.Forms;
using DevExpress.XtraGrid;
using DevExpress.XtraGrid.Views.Grid;

namespace UI
{
    public partial class MasterDetails : Form
    {
        private GridView _detailView;
        private GridControl _gridControl;
        private GridView _masterView;
        private GridLevelNode _detailNode;

        public MasterDetails()
        {
            this.InitializeComponent();
            this.InitializeGrid();
            this.InitializeAndAddColumnsToViews();
            this.InitializeAndBindDataSource();
        }

        private void InitializeGrid()
        {
            this._gridControl = new GridControl();
            this._masterView = new GridView(this._gridControl);
            this._detailView = new GridView(this._gridControl);
            //this._detailNode = this._gridControl.LevelTree.Nodes.Add("SuppliersProducts", this._detailView);
            this._gridControl.Dock = DockStyle.Fill;
            this.Controls.Add(this._gridControl);
        }

        private void InitializeAndAddColumnsToViews()
        {
            if (this._masterView != null && this._detailView != null)
            {
                //this._masterView.Columns.AddField("ID").VisibleIndex = 0;

                //this._detailView.Columns.AddField("ID").VisibleIndex = 0;
                //this._detailView.Columns.AddField("Name").VisibleIndex = 1;
                //this._detailView.Columns.AddField("Category").VisibleIndex = 2;
            }
        }
        private void InitializeAndBindDataSource()
        {
            var categories = new List<Category>();

            Category category = null;
            for (var j = 0; j < 5; j++)
            {
                category = new Category
                {
                    ID = j + 1
                };
                for (var i = 0; i < 5; i++)
                {
                    category.Books.Add(new Book
                    {
                        ID = 1,
                        Name = "Book - " + (i + 1),
                        Category = j + 1
                    });
                }

                categories.Add(category);
            }

            this._gridControl.DataSource = categories;
        }

        //private void InitializeAndBindDataSource()
        //{
        //    var bookDetails = new List<BookDetail>();

        //    BookDetail bookDetail = null;
        //    for (var j = 0; j < 5; j++)
        //    {
        //        bookDetail = new BookDetail {CategoryID = j + 1};
        //        for (var i = 0; i < 5; i++)
        //        {
        //            bookDetail.Books.Add(new Book
        //            {
        //                ID = 1,
        //                Name = "Book - " + (i + 1),
        //                Category = j + 1
        //            });
        //        }

        //        bookDetails.Add(bookDetail);
        //    }

        //    this._gridControl.DataSource = bookDetails;
        //}
    }
}