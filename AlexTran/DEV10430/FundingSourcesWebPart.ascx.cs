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

                BindGrid(parent);
            }
        }
      
        #region Helper Method
              
        public void BindGrid(ParentUser parent)
        {
            var ccBussiness = CreditCardBussiness.Instance(PayjrSystemInfo);
            _fundingSourcesGrid.DataSource = ccBussiness.GetCreditCardFunding(parent.UserIdentifier);
            _fundingSourcesGrid.DataBind();       
        }
       
        #endregion
    }

}