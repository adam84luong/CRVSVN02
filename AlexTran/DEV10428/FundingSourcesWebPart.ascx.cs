using CardLab.CMS.PayjrSites.User;
using Common.Contracts.CreditCard.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace CMSApp.CMSWebParts.CardLab.Buxx.Account
{
    public partial class FundingSourcesWebPart : PayjrBaseWebPart
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && CurrentUser is ParentUser)
            {
                var parent = CurrentUser as ParentUser;

                BindGrid(parent, true);
            }
        }
        #region Class Item FundingSources

        protected class FundingSourcesItem
        {
            public string AccountIdentifier { get; set; }
            public string CardType { get; set; }
            public string CardNumber { get; set; }
            public string Expiration { get; set; }
            public string Status { get; set; }
        }

        #endregion
        
        #region Helper Method
              
        public void BindGrid(ParentUser parent, bool isDataBind = true)
        {
            List<CreditCardDetailedRecord> creditCardAccounts = parent.ProviderFactory.CreditCardProcessing.RetrieveAccounts(parent.UserIdentifier);
            List<FundingSourcesItem> fundingSourcesItems = new List<FundingSourcesItem>();
            foreach (CreditCardDetailedRecord creditCardAccount in creditCardAccounts)
            {
                FundingSourcesItem fundingSourcesItem = new FundingSourcesItem();
                fundingSourcesItem.AccountIdentifier = creditCardAccount.AccountIdentifier;
                fundingSourcesItem.CardType = creditCardAccount.CardType.ToString();
                fundingSourcesItem.CardNumber = "XXXX-XXXX-XXXX-" + creditCardAccount.CardNumberLastFour;
                string status;
                fundingSourcesItem.Expiration = GetExpiration(creditCardAccount.ExpirationMonth, creditCardAccount.ExpirationYear, out status);
                fundingSourcesItem.Status = status;
                fundingSourcesItems.Add(fundingSourcesItem);
            }
            _fundingSourcesGrid.DataSource = fundingSourcesItems;
            if (isDataBind)
            {
                _fundingSourcesGrid.DataBind();
            }
        }
        public string GetExpiration(string month, string year, out string status)
        {
            string expireDay = month + "/" + year.Substring(2, 2);
            string expirestatus = "";
            if (DateTime.Now.Year < Int32.Parse(year))
            {
                status = "Good";
            }
            else if (DateTime.Now.Year == Int32.Parse(year))
            {
                if (DateTime.Now.Month < Int32.Parse(month))
                {
                    status = "Good";
                }
                else
                {
                    status = "Bad";
                    expirestatus = " (expired)";
                }
            }
            else
            {
                status = "Bad";
                expirestatus = " (expired)";
            }
            return expireDay + expirestatus;
        }

        #endregion
    }

}