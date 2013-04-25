using System;
using System.Collections.Generic;
using CMSApp.Controls.RadCustomControl;
using Telerik.Web.UI;
using CardLab.CMS.PayjrSites.Business;

namespace CMSApp.Controls.Buxx.Account
{
    public partial class ListRecentTransactions : BuxxBaseControl
    {
        #region Properties

        protected string CardIdentifier
        {
            get
            {
                if (ViewState["CardIdentifier"] == null)
                {
                    ViewState["CardIdentifier"] = String.Empty;
                }
                return (string)ViewState["CardIdentifier"];
            }
            set
            {
                ViewState["CardIdentifier"] = value;
            }
        }
        protected DateTime StartDate
        {
            get
            {
                if (ViewState["StartDate"] == null)
                {
                    ViewState["StartDate"] = EndDate.AddDays(-10);
                }
                return (DateTime)ViewState["StartDate"];
            }
            set
            {
                ViewState["StartDate"] = value;
            }
        }

        protected DateTime EndDate
        {
            get
            {
                if (ViewState["EndDate"] == null)
                {
                    ViewState["EndDate"] = DateTime.Now.Date;
                }
                return (DateTime)ViewState["EndDate"];
            }
            set
            {
                ViewState["EndDate"] = value;
            }
        }

        public int PageSize
        {
            get
            {
                if (ViewState["PageSize"] == null)
                {
                    ViewState["PageSize"] = _transactionGrid.PageSize;
                }
                return (int)ViewState["PageSize"];
            }
            set
            {
                ViewState["PageSize"] = value;
            }
        }

        public int CurrentPageIndex
        {
            get
            {
                if (ViewState["CurrentPageIndex"] == null)
                {
                    ViewState["CurrentPageIndex"] = _transactionGrid.CurrentPageIndex;
                }
                return (int)ViewState["CurrentPageIndex"];
            }
            set
            {
                ViewState["CurrentPageIndex"] = value;
            }
        }
        
        public bool AllowPaging
        {
            get { return _transactionGrid.AllowPaging; }
            set { _transactionGrid.AllowPaging = value; }
        }

        #endregion


        #region Public Method

        public void BindControl(string cardIdentifier, DateTime startDate, DateTime endDate)
        {
            CardIdentifier = cardIdentifier;
            StartDate = startDate;
            EndDate = endDate;

            BindDataToTransactionGrid(CardIdentifier, CurrentPageIndex, PageSize);
        }

        #endregion


        #region Grid Event

        protected void TransactionItemDataBound(object sender, GridItemEventArgs e)
        {
            if (e.Item.ItemType == GridItemType.Item || e.Item.ItemType == GridItemType.AlternatingItem)
            {
                //Todo: custom displaying
            }
        }

        protected void TransactionGridPageIndexChanged(object sender, GridPageChangedEventArgs e)
        {
            CurrentPageIndex = e.NewPageIndex;
            BindDataToTransactionGrid(CardIdentifier, CurrentPageIndex, PageSize);
        }

        protected void TransactionGridPageSizeChanged(object sender, GridPageSizeChangedEventArgs e)
        {
            PageSize = e.NewPageSize;
        }   

        protected void TransactionItemCreated(object sender, GridItemEventArgs e)
        {
            if (e.Item is GridPagerItem)
            {
                var item = (GridPagerItem)e.Item;
                item.PagerContentCell.Controls.Clear();
                var pager = LoadControl("~/Controls/RadCustomControl/NonNumericCustomizeGridPager.ascx") as NonNumericCustomizeGridPager;
                pager.BindControl(item);
                item.PagerContentCell.Controls.Add(pager);
            }
        }

        #endregion 


        #region Page event

        protected void Page_Load(object sender, EventArgs e)
        {
            
        }


        #endregion


        #region Helper Method

        private void BindDataToTransactionGrid(string cardIdentifier, int currentPageIndex, int pageSize)
        {
            _transactionGrid.PageSize = pageSize;
            _transactionGrid.CurrentPageIndex = currentPageIndex;
            int totalRecord;
            var ccBusiness = PrepaidCardBusiness.Instance(PayjrSystemInfo);
            _transactionGrid.DataSource = ccBusiness.GetCardTransaction(cardIdentifier, StartDate, EndDate, currentPageIndex + 1, pageSize, out totalRecord);
            _transactionGrid.VirtualItemCount = totalRecord;
            _transactionGrid.DataBind();     
        }

        #endregion

    
        
    }
}