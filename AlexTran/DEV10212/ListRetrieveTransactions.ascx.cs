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
        List<DateTime> _startDates = new List<DateTime>();
        List<DateTime> _endDates = new List<DateTime>();
        protected void Page_Load(object sender, EventArgs e)
        {
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
                    ViewState["StartDate"] = DateTime.Now.AddMonths(-1).Date;
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
                    ViewState["PageNumber"]= 1;
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
                    ViewState["NumberPerPage"] = 5;
                }
                return (int)ViewState["NumberPerPage"];
            }
            set
            {
                ViewState["NumberPerPage"] = value;
            }
        }
        public int TotalRecords
        {
            get
            {
                if (ViewState["RecordTotal"] == null)
                {
                    ViewState["RecordTotal"] = 0;
                }
                return (int)ViewState["RecordTotal"];
            }
            private set
            {
                ViewState["RecordTotal"] = value;
            }
        }
          #endregion

        #region Public Method
        public void BindControl(TeenUser teen)
           {
            Teen = teen;
            _teenName.Text = teen.FullName + "' Transactions";          
            BinComboDate(DateTime.Now.AddYears(-1).Date, DateTime.Now.Date);
            StartDate = _startDates[0].Date;
            EndDate = _endDates[0].Date;
            CardIdentifier = teen.PrepaidCard.PrepaidCardIdentifier;
            PageNumber = _radDataPager.CurrentPageIndex;
            NumberPerPage = _radDataPager.PageSize;
            BinGrid(teen);         
            _transactionView.DataBind();   
        }

        public void BinGrid(TeenUser teen)
        {
            int totalRecord;
            var cardTransactionRecord = teen.ProviderFactory.Prepaid.RetrieveCardTransactions(ApplicationKey, CardIdentifier, StartDate, EndDate, PageNumber, NumberPerPage, out totalRecord);
            TotalRecords = totalRecord;
            _transactionView.PageSize = NumberPerPage;
            _transactionView.DataSource = cardTransactionRecord;
        }

        public void BinComboDate(DateTime startDate, DateTime endDate)
        {
            DateTime startdatetemp = startDate;
            DateTime enddatetemp = endDate;
            RadComboBoxItem item;
            int i = 0;
            while (DateTime.Compare(startDate, enddatetemp) <= 0)
            {
                startdatetemp = enddatetemp.AddMonths(-1);
                _endDates.Add(enddatetemp);
                _startDates.Add(startdatetemp);
                enddatetemp = startdatetemp.AddDays(-1);
                if (i == 0)
                {
                    item = new RadComboBoxItem("Current Statement " + _startDates[i].ToShortDateString() + " to " + _endDates[i].ToShortDateString(), i.ToString());
                }
                else
                {
                    item = new RadComboBoxItem(_startDates[i].ToShortDateString() + " to " + _endDates[i].ToShortDateString(), i.ToString());
                }
                _comboDate.Items.Add(item);
                i++;
            }
        }
        #endregion
                  
        #region Page event
        protected void _PageIndexChanged(object sender, RadDataPagerPageIndexChangeEventArgs e)
        {
            PageNumber = e.NewPageIndex;
            BinGrid(Teen);
            _transactionView.Rebind();
        }     
        
        protected void PerPage_SelectedIndexChanged(object sender, Telerik.Web.UI.RadComboBoxSelectedIndexChangedEventArgs e)
        {
            NumberPerPage = Int32.Parse(e.Value);
            _radDataPager.PageSize = NumberPerPage;
            PageNumber = 1;
            BinGrid(Teen);
            _transactionView.Rebind();
        }

        protected void _OnTotalRowCountRequest(object sender, RadDataPagerTotalRowCountRequestEventArgs e)
        {
            e.TotalRowCount = TotalRecords;
        }

        protected void _comboDate_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
        {
            StartDate = _startDates[Int32.Parse(_comboDate.SelectedValue)].Date;
            EndDate = _endDates[Int32.Parse(_comboDate.SelectedValue)].Date;
            PageNumber = 1;
            BinGrid(Teen);
            _transactionView.Rebind();
        }
        #endregion
        
    }


}