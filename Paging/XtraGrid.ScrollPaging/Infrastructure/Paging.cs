using System;

namespace Infrastructure
{
    /// <summary>
    ///     Class Paging.
    /// </summary>
    public class Paging
    {
        /// <summary>
        ///     The _is first page
        /// </summary>
        private bool _isFirstPage;

        /// <summary>
        ///     The _is last page
        /// </summary>
        private bool _isLastPage;

        /// <summary>
        ///     The _page size
        /// </summary>
        private int _pageSize;

        /// <summary>
        ///     The _page size information
        /// </summary>
        private string _pageSizeInfo;

        /// <summary>
        ///     The _skip
        /// </summary>
        private int _skip;

        /// <summary>
        ///     The _sort expression
        /// </summary>
        private string _sortExpression;

        /// <summary>
        ///     Gets a value indicating whether this instance is last page.
        /// </summary>
        /// <value><c>true</c> if this instance is last page; otherwise, <c>false</c>.</value>
        public bool IsLastPage
        {
            get
            {
                this._isLastPage = this.PageIndex == this.PageSize - 1;
                return this._isLastPage;
            }
            internal set { this._isLastPage = value; }
        }

        /// <summary>
        ///     Gets a value indicating whether this instance is first page.
        /// </summary>
        /// <value><c>true</c> if this instance is first page; otherwise, <c>false</c>.</value>
        public bool IsFirstPage
        {
            get
            {
                this._isFirstPage = this.PageIndex == 0;
                return this._isFirstPage;
            }
            internal set { this._isFirstPage = value; }
        }

        /// <summary>
        ///     Gets or sets the index of the page.
        /// </summary>
        /// <value>The index of the page.</value>
        public int PageIndex { get; set; }

        /// <summary>
        ///     Gets or sets the page index information.
        /// </summary>
        /// <value>The page index information.</value>
        public string PageIndexInfo
        {
            get
            {
                this._pageSizeInfo = string.Format("{0}/{1}", this.PageIndex + 1, this.PageSize);
                return this._pageSizeInfo;
            }
            set { this._pageSizeInfo = value; }
        }

        /// <summary>
        ///     Gets the size of the page.
        /// </summary>
        /// <value>The size of the page.</value>
        public int PageSize
        {
            get
            {
                if (this.TotalCount.HasValue)
                {
                    if (this.RowSize != 0)
                    {
                        var pageSize = (double)this.TotalCount / this.RowSize;
                        this._pageSize = (int)Math.Ceiling(pageSize);
                    }
                }
                return this._pageSize;
            }
            internal set { this._pageSize = value; }
        }

        /// <summary>
        ///     Gets or sets the size of the row.
        /// </summary>
        /// <value>The size of the row.</value>
        public int RowSize { get; set; } = 10;

        public int Skip
        {
            get
            {
                this._skip = this.PageIndex * this.RowSize;
                return this._skip;
            }
            internal set { this._skip = value; }
        }

        public int? TotalCount { get; set; }

        public string SortExpression { get; set; }

        public string FilterExpression { get; set; }

        public void FirstPageIndex()
        {
            this.PageIndex = 0;
        }

        public void LastPageIndex()
        {
            this.PageIndex = this.PageSize - 1;
        }

        public void NextPageIndex()
        {
            if (this.PageIndex >= this.PageSize - 1)
            {
                return;
            }

            this.PageIndex++;
        }

        public void PreviousPageIndex()
        {
            if (this.PageIndex <= 0)
            {
                return;
            }

            this.PageIndex--;
        }
    }
}