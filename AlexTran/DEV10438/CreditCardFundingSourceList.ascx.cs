using CardLab.CMS.PayjrSites.Business;
using CardLab.CMS.PayjrSites.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace CMSApp.Controls.Buxx.Account
{
    public partial class CreditCardFundingSourceList : BuxxBaseControl
    {
        protected void Page_Load(object sender, EventArgs e)
        {
     
        }

        #region Helper Method

        public void BindGrid(ParentUser parent)
        {
            var ccBusiness = CreditCardBusiness.Instance(PayjrSystemInfo);
            _fundingSourcesGrid.DataSource = ccBusiness.GetCreditCardFunding(parent.UserIdentifier);
            _fundingSourcesGrid.DataBind();
        }

        #endregion
    }
}