using CardLab.CMS.PayjrSites.Business;
using CardLab.CMS.PayjrSites.User;
using CMS.GlobalHelper;
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
    public partial class FundingSourceWebPart : PayjrBaseWebPart
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && CurrentUser is ParentUser)
            {
                var parent = CurrentUser as ParentUser;
                CreditCardFundingSourceList(parent);
                _formAddCard.Visible = false;
              
           }
        }
        #region Button Event
        protected void ShowAddButtonClick(object sender, EventArgs e)
        {
            _formAddCard.Visible = true;
            _formFundingSourceList.Visible = false;
        }
        protected void AddCreditCardButtonClick(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            if (_agreeTermCheckBox.Checked.Equals(false))
            {
                _ucMsgBox.ShowMessage("You must agree to the cardholder agreement to continue", MessageBox.MsgType.Error);
                return;
            }
            var parent = CurrentUser as ParentUser;
            string cardNumber = _addCreditCard.CardNumber;
            string cvv = _addCreditCard.CVVSecurityCode;
            int month = _addCreditCard.ExpirationDate.Month;
            int year = _addCreditCard.ExpirationDate.Year;
            CreditCardType cardType = _addCreditCard.Type;
            AddressRecord address = parent.Address;
            var ccBusiness = CreditCardBusiness.Instance(PayjrSystemInfo);
            CreditCardDetailedRecord cc = ccBusiness.CreateCreditCard(cardNumber, cvv, month, year, cardType, CurrentUser.UserIdentifier, address);
            if (cc == null)
            {
                _ucMsgBox.ShowMessage("An error occurred while creating credit card.", MessageBox.MsgType.Error);
                return;
            }
            _formFundingSourceList.Visible = true;
            _formAddCard.Visible = false;
            CreditCardFundingSourceList(parent);
        }
        #endregion        

        #region Helper Method
        private void CreditCardFundingSourceList(ParentUser parent)
        {
            _creditCardFundingSourceList.BindGrid(parent);
        }
        #endregion

    }
}