using CardLab.CMS.PayjrSites.User;
using CardLab.CMS.Util;
using Common.Contracts.Prepaid.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace CMSApp.CMSWebParts.CardLab.Buxx.Account
{
    public partial class CardTransactionsWebPart : PayjrBaseWebPart
    {
        #region CMS Properties

        public string TeenOverviewUrl
        {
            get { return GetWebPartProperty("TeenOverviewUrl", "/account/teen/overview"); }
            set { SetValue("TeenOverviewUrl", string.IsNullOrWhiteSpace(value) ? null : value); }
        }

        public string AddaTeenUrl
        {
            get { return GetWebPartProperty("AddaTeenUrl", "/account/add-a-teen"); }
            set { SetValue("AddaTeenUrl", string.IsNullOrWhiteSpace(value) ? null : value); }
        }

        public string TeenCreateAccountUrl
        {
            get { return GetWebPartProperty("TeenCreateAccountUrl", "/account/teen/create-account"); }
            set { SetValue("TeenCreateAccountUrl", string.IsNullOrWhiteSpace(value) ? null : value); }
        }

        public string TeenEditSettingsUrl
        {
            get { return GetWebPartProperty("TeenEditSettingsUrl", "/account/teen/edit-settings"); }
            set { SetValue("TeenEditSettingsUrl", string.IsNullOrWhiteSpace(value) ? null : value); }
        }
        public string TeenAddMoneyButtonCssClass
        {
            get { return GetWebPartProperty("TeenAddMoneyButtonCssClass", "btn"); }
            set { SetValue("TeenAddMoneyButtonCssClass", string.IsNullOrWhiteSpace(value) ? null : value); }
        }
        public string TeenAddMoneyBigButtonCssClass
        {
            get { return GetWebPartProperty("TeenAddMoneyBigButtonCssClass", "btn btn_big"); }
            set { SetValue("TeenAddMoneyBigButtonCssClass", string.IsNullOrWhiteSpace(value) ? null : value); }
        }
        public string TeenSummaryCssClass
        {
            get { return GetWebPartProperty("TeenSummaryCssClass", "buxx-box"); }
            set { SetValue("TeenSummaryCssClass", string.IsNullOrWhiteSpace(value) ? null : value); }
        }
        public string TeenSummaryActiveCssClass
        {
            get { return GetWebPartProperty("TeenSummaryActiveCssClass", "buxx-box box_active"); }
            set { SetValue("TeenSummaryActiveCssClass", string.IsNullOrWhiteSpace(value) ? null : value); }
        }

        public string TeenAddMoneyUrl
        {
            get { return GetWebPartProperty("TeenAddMoneyUrl", "/account/teen/add-money"); }
            set { SetValue("TeenAddMoneyUrl", string.IsNullOrWhiteSpace(value) ? null : value); }
        }

        public string TeenActiveCardUrl
        {
            get { return GetWebPartProperty("TeenActiveCardUrl", "/account/teen/active-card"); }
            set { SetValue("TeenActiveCardUrl", string.IsNullOrWhiteSpace(value) ? null : value); }
        }

        public string TeenTabCssClass
        {
            get { return GetWebPartProperty("TeenTabCssClass", "btn"); }
            set { SetValue("TeenTabCssClass", string.IsNullOrWhiteSpace(value) ? null : value); }
        }

        public string TeenActiveTabCssClass
        {
            get { return GetWebPartProperty("TeenActiveTabCssClass", "btn btn_active"); }
            set { SetValue("TeenActiveTabCssClass", string.IsNullOrWhiteSpace(value) ? null : value); }
        }


        #endregion


        #region Page Event

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && CurrentUser is ParentUser)
            {
                var parent = CurrentUser as ParentUser;
                var teens = parent.GetTeenUsers();
                if (teens.Count >= 1)
                {
                    var passer = UrlCardLabPasser.Instance();
                    string teenUserIdentifier = passer.UserIdentifier;
                    var query = teens.Where(u => u.UserIdentifier == teenUserIdentifier);
                    TeenUser selectedTeen = null;
                    if (query.Count() > 0)
                    {
                        selectedTeen = query.First();
                    }
                    _teenListTab.BindControl(teens, TeenOverviewUrl, TeenTabCssClass, TeenActiveTabCssClass, selectedTeen);
                    _listRetrieveTransactions.BindControl(selectedTeen);
                }
            }
        }

        #endregion


        #region Helper Method

        
        #endregion
    }
}