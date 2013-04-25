using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace CMSApp.CMSWebParts.CardLab.Buxx.Account
{
    public partial class TeenTransactionsWebPart : PayjrBaseWebPart
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            //As a sample, need change input value
            _listRecentTransactions.BindControl("cardIdentifier", DateTime.Now, DateTime.Now);
        }
    }
}