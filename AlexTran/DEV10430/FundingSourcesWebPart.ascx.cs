using CardLab.CMS.PayjrSites.Bussiness;
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
      
        #region Helper Method
              
        public void BindGrid(ParentUser parent, bool isDataBind = true)
        {  
           _fundingSourcesGrid.DataSource =CreditCardBussiness.GetFundingCreditCard(parent.UserIdentifier);
            if (isDataBind)
            {
                _fundingSourcesGrid.DataBind();
            }
        }
       
        #endregion
    }

}