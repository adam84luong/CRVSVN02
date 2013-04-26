using CardLab.CMS.PayjrSites.Business;
using CardLab.CMS.PayjrSites.User;
using CMS.GlobalHelper;
using CMSApp.Controls;
using Common.Contracts.CreditCard.Records;
using Common.Contracts.Shared.Records;
using Common.Types;
using Payjr.GCL.Kentico.Web.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace CMSApp.CMSWebParts.CardLab.Buxx.Account
{
    public partial class AddCreaditCardFundingWebPart : PayjrBaseWebPart
    {
        #region CMS Properties

        public string FailedIdentityCheckUrl
        {
            get { return GetWebPartProperty("FailedIdentityCheckUrl", "/account/failed-identity-check"); }
            set { SetValue("FailedIdentityCheckUrl", string.IsNullOrWhiteSpace(value) ? null : value); }
        }

        public string FailedCreditCardTransactionUrl
        {
            get { return GetWebPartProperty("FailedCreditCardTransactionUrl", "/account/failed-credit-card-transaction"); }
            set { SetValue("FailedCreditCardTransactionUrl", string.IsNullOrWhiteSpace(value) ? null : value); }
        }

        public string ParentOverviewUrl
        {
            get { return GetWebPartProperty("ParentOverviewUrl", "/account/home"); }
            set { SetValue("ParentOverviewUrl", string.IsNullOrWhiteSpace(value) ? null : value); }
        }

        #endregion


        #region Page Event

        protected void Page_Load(object sender, EventArgs e)
        {

        }

        #endregion


        #region Button Event

        protected void orderCardButtonClick(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            if (_agreeTermCheckBox.Checked.Equals(false))
            {
                _ucMsgBox.ShowMessage("You must agree to the cardholder agreement to continue", MessageBox.MsgType.Error);
                return;
            }

            var parent = CurrentUser as ParentUser;
            //Create credit card
            string cardNumber = _creaditCardFundingInformation.CardNumber;
            string cvv = _creaditCardFundingInformation.CVVSecurityCode;
            int month = _creaditCardFundingInformation.ExpirationDate.Month;
            int year = _creaditCardFundingInformation.ExpirationDate.Year;
            CreditCardType cardType = _creaditCardFundingInformation.Type;
            AddressRecord address = parent.Address;
            var ccBusiness = CreditCardBusiness.Instance(PayjrSystemInfo);
            CreditCardDetailedRecord cc = ccBusiness.CreateCreditCard(cardNumber, cvv, month, year, cardType, CurrentUser.UserIdentifier, address);
            if (cc == null)
            {
                URLHelper.Redirect(FailedCreditCardTransactionUrl);
                return;
            }
            URLHelper.Redirect(ParentOverviewUrl);        

        }

        #endregion        
     

       
    }
}