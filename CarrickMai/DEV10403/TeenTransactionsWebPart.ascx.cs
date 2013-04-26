using CardLab.CMS.PayjrSites.User;
using CardLab.CMS.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telerik.Web.UI;

namespace CMSApp.CMSWebParts.CardLab.Buxx.Account
{
    public partial class TeenTransactionsWebPart : PayjrBaseWebPart
    {
        #region CMS Properties

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
                    ViewState["StartDate"] = EndDate.AddDays(-1);
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
        #endregion

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack && CurrentUser is ParentUser)
            {
                RadButton button = (RadButton)_timePeriodCombo.Footer.FindControl("_Go");
                button.Click += new EventHandler(this._GoClick);
                AddDataItems();
                inItDatepicker();
            }
            _listRecentTransactions.BindControl(CardIdentifier, StartDate, EndDate);

        }


        protected void _GoClick(object sender, EventArgs e)
        {
            RadDatePicker radDatepicker1 = (RadDatePicker)_timePeriodCombo.Footer.FindControl("RadDatePicker1");
            RadDatePicker radDatepicker2 = (RadDatePicker)_timePeriodCombo.Footer.FindControl("RadDatePicker2");
            StartDate = radDatepicker1.SelectedDate.Value;
            EndDate = radDatepicker2.SelectedDate.Value;
        }
        protected void _timePeriodComboSelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
        {
            string stringdate = _timePeriodCombo.SelectedValue;
            int pos = stringdate.IndexOf("-");
            StartDate = DateTime.Parse(stringdate.Substring(0, pos));
            EndDate = DateTime.Parse(stringdate.Substring(pos + 1, stringdate.Length - pos - 1));
        }

        protected void AddDataItems()
        {
            var listItem = new List<TestItem>();
            DateTime firstDay = new DateTime(DateTime.Now.Year, 1, 1);
            DateTime firstLastYear = new DateTime(DateTime.Now.Year - 1, 1, 1);
            DateTime endLastYear = new DateTime(DateTime.Now.Year - 1, 12, 31);

            listItem.Add(new TestItem
            {
                Column1 = "Recent Activity",
                Column2 = DateTime.Now.AddMonths(-1).AddDays(-1).ToShortDateString() + " to " + DateTime.Now.ToShortDateString(),
                Column3 = string.Format("Recent Activity   {0} to {1}", DateTime.Now.AddMonths(-1).AddDays(-1).ToShortDateString(), DateTime.Now.ToShortDateString()),
                Column4 = DateTime.Now.AddMonths(-1).AddDays(-1).ToShortDateString() + " - " + DateTime.Now.ToShortDateString()
            });
            listItem.Add(new TestItem
            {
                Column1 = "Current Statement",
                Column2 = DateTime.Now.AddMonths(-2).ToShortDateString() + " to " + DateTime.Now.AddMonths(-1).AddDays(-1).ToShortDateString(),
                Column3 = string.Format("Current Statement   {0} to {1}", DateTime.Now.AddMonths(-2).ToShortDateString(), DateTime.Now.AddMonths(-1).AddDays(-1).ToShortDateString()),
                Column4 = DateTime.Now.AddMonths(-2).ToShortDateString() + " - " + DateTime.Now.AddMonths(-1).AddDays(-1).ToShortDateString()
            });

            for (int i = 0; i < DateTime.Now.Month; i++)
            {
                if (i == 0)
                {
                    listItem.Add(new TestItem
                    {
                        Column1 = "Previous Statement",
                        Column2 = DateTime.Now.AddMonths(-i - 3).AddDays(-1).ToShortDateString() + " to " + DateTime.Now.AddMonths(-i - 2).AddDays(-i).ToShortDateString(),
                        Column3 = string.Format("Previous Statement   {0} to {1}", DateTime.Now.AddMonths(-i - 3).AddDays(-1).ToShortDateString(), DateTime.Now.AddMonths(-1).AddDays(-i).ToShortDateString()),
                        Column4 = DateTime.Now.AddMonths(-i - 3).AddDays(-1).ToShortDateString() + " - " + DateTime.Now.AddMonths(-i - 2).AddDays(-i).ToShortDateString()
                    });
                }
                else
                {
                    listItem.Add(new TestItem
                    {
                        Column1 = "",
                        Column2 = DateTime.Now.AddMonths(-i - 3).AddDays(-1).ToShortDateString() + " to " + DateTime.Now.AddMonths(-2 - i).AddDays(-i).ToShortDateString(),
                        Column3 = string.Format("Previous Statement   {0} to {1}", DateTime.Now.AddMonths(-i).AddDays(-1).ToShortDateString(), DateTime.Now.AddMonths(-1).AddDays(-i).ToShortDateString()),
                        Column4 = DateTime.Now.AddMonths(-i - 3).AddDays(-1).ToShortDateString() + " - " + DateTime.Now.AddMonths(-2 - i).AddDays(-i).ToShortDateString()
                    });
                }
            }
            listItem.Add(new TestItem
            {
                Column1 = "Year to Date",
                Column2 = firstDay.ToShortDateString() + " to " + DateTime.Now.ToShortDateString(),
                Column3 = string.Format("Year to Date   {0} to {1}", firstDay.ToShortDateString(), DateTime.Now.ToShortDateString()),
                Column4 = firstDay.ToShortDateString() + " - " + DateTime.Now.ToShortDateString(),
            });
            listItem.Add(new TestItem
            {
                Column1 = "Year End Summary",
                Column2 = firstLastYear.ToShortDateString() + " to " + endLastYear.ToShortDateString(),
                Column3 = string.Format("Year End Summary   {0} to {1}", firstLastYear.ToShortDateString(), endLastYear.ToShortDateString()),
                Column4 = firstLastYear.ToShortDateString() + " - " + endLastYear.ToShortDateString()
            });

            RadComboBox1.DataSource = listItem;
            RadComboBox1.DataTextField = "Column3";
            RadComboBox1.DataValueField = "Column4";
            RadComboBox1.DataBind();
        }

        protected void inItDatepicker()
        {
            RadDatePicker radDatepicker1 = (RadDatePicker)_timePeriodCombo.Footer.FindControl("RadDatePicker1");
            RadDatePicker radDatepicker2 = (RadDatePicker)_timePeriodCombo.Footer.FindControl("RadDatePicker2");
            radDatepicker1.MinDate = DateTime.Now.AddYears(-2);
            radDatepicker1.SelectedDate = DateTime.Now.AddYears(-1);
            radDatepicker2.SelectedDate = DateTime.Now;
        }
    }
}