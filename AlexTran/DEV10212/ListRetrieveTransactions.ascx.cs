using CardLab.CMS.PayjrSites.User;
using Common.Contracts.Prepaid.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telerik.Web.UI;

namespace CMSApp.Controls.Buxx.Account
{
    public partial class ListRetrieveTransactions : BuxxBaseControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            _transactionGrid.PageSize = NumberPerPage;
        }   

         #region Properties

        public TeenUser Teen
        {
            get
            {
                if (ViewState["Teen"] == null)
                {
                    ViewState["Teen"] = (TeenUser)CurrentUser;
                }
                return (TeenUser)ViewState["Teen"];
            }
            set
            {
                ViewState["Teen"] = value;
            }
        }

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

        public int PageNumber
        {
            get
            {
                if (ViewState["PageNumber"] == null)
                {
                    ViewState["PageNumber"]= _radDataPager.CurrentPageIndex;
                }
                return (int)ViewState["PageNumber"];
            }
            set
            {
                ViewState["PageNumber"] = value;
            }
        }


        public int NumberPerPage
        {
            get
            {
                if (ViewState["NumberPerPage"] == null)
                {
                    ViewState["NumberPerPage"] = _radDataPager.PageSize;
                }
                return (int)ViewState["NumberPerPage"];
            }
            set
            {
                ViewState["NumberPerPage"] = value;
            }
        }
          #endregion

        #region Public Method
        public void BindControl(TeenUser teen)
           {
            Teen = teen;
            _teenName.Text = teen.FullName + "' Transactions";
            NumberPerPage = _radDataPager.PageSize;
            PageNumber= _radDataPager.CurrentPageIndex;
            CardIdentifier = teen.PrepaidCard.PrepaidCardIdentifier;
            StartDate = DateTime.Today.AddDays(-10);
            EndDate = DateTime.Today.AddDays(10);
            BinRadGrid(teen);
            _transactionGrid.DataBind();   
        }
    
        #endregion
        #region Grid Event
        protected void BinRadGrid(TeenUser teen)
        {
           var cardTransactionRecord = teen.ProviderFactory.Prepaid.RetrieveCardTransactions(ApplicationKey, CardIdentifier, StartDate, EndDate, PageNumber, NumberPerPage);
          _transactionGrid.PageSize = NumberPerPage;
          _transactionGrid.CurrentPageIndex = PageNumber;
          _transactionGrid.DataSource = cardTransactionRecord;           
        }
        protected void TransactionNeedDataSource(object source, GridNeedDataSourceEventArgs e)
        {
            BinRadGrid(Teen);
            _transactionGrid.Rebind(); 
        }

        protected void TransactionItemDataBound(object sender, GridItemEventArgs e)
        {
            if (e.Item.ItemType == GridItemType.Item || e.Item.ItemType == GridItemType.AlternatingItem)
            {

            }
        }

        #endregion
        
        #region Page event
      
        protected void SearchResultPageIndexChanged(object sender, RadDataPagerPageIndexChangeEventArgs e)
        {         
            PageNumber = e.NewPageIndex;
            BinRadGrid(Teen);
            _transactionGrid.Rebind(); 
        }
        protected void SearchResultPagerCommand(object sender, RadDataPagerCommandEventArgs e)
        {
            if (e.CommandName == "PageSize")
            {
                NumberPerPage = Convert.ToInt32(e.CommandArgument);
                PageNumber = 1;
                BinRadGrid(Teen);
                _transactionGrid.Rebind(); 
            }
        }
        #endregion
    }


}