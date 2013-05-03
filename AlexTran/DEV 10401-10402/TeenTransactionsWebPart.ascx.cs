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
                
        public string TeenOverviewUrl
        {
            get { return GetWebPartProperty("TeenOverviewUrl", "/account/teen/overview"); }
            set { SetValue("TeenOverviewUrl", string.IsNullOrWhiteSpace(value) ? null : value); }
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

        protected DateTime StartDate
        {
            get
            {
                if (ViewState["StartDate"] == null)
                {
                    ViewState["StartDate"] = EndDate.AddYears(-10);
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

        #region Page Event
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                RadButton button = (RadButton)_timePeriodCombo.Footer.FindControl("_Go");
                button.Click += new EventHandler(this._GoClick);
                AddDataItems();
                inItDatepicker();
            }
            if (CurrentUser is ParentUser)
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
                    _teenName.Text = selectedTeen.FirstName + "' Transactions";
                   _listRecentTransactions.BindControl(selectedTeen.PrepaidCard.PrepaidCardIdentifier, StartDate, EndDate);
           
                }
              }
        }
        #endregion

        #region Control Event

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
   
        #endregion
        
        
        #region Help Method

        private class TimePeriodItem
        {
            public string Statement { get; set; }
            public string StartDatetoEndDate { get; set; }
            public string ComboText { get; set; }
            public string ComboValue { get; set; }
        }
        protected void AddDataItems()
        {
            var listItem = new List<TimePeriodItem>();
            DateTime firstDayofYear = new DateTime(DateTime.Now.Year, 1, 1);
            DateTime firstDayofMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            DateTime firstLastYear = new DateTime(DateTime.Now.Year - 1, 1, 1);
            DateTime endLastYear = new DateTime(DateTime.Now.Year - 1, 12, 31);

            listItem.Add(new TimePeriodItem
            {
                Statement = "Recent Activity",
                StartDatetoEndDate = firstDayofMonth.ToString("MMM d, yyyy") + " to " + "Present",
                ComboText = string.Format("Recent Activity   {0} to {1}", firstDayofMonth.ToString("MMM d, yyyy"), "Present"),
                ComboValue = firstDayofMonth.ToShortDateString() + " - " + DateTime.Now.ToShortDateString()
            });
            listItem.Add(new TimePeriodItem
            {
                Statement = "Current Statement",
                StartDatetoEndDate = firstDayofMonth.AddMonths(-1).ToString("MMM d, yyyy") + " to " + firstDayofMonth.AddDays(-1).ToString("MMM d, yyyy"),
                ComboText = string.Format("Current Statement   {0} to {1}", firstDayofMonth.AddMonths(-1).ToString("MMM d, yyyy"), firstDayofMonth.AddDays(-1).ToString("MMM d, yyyy")),
                ComboValue = firstDayofMonth.AddMonths(-1).ToShortDateString() + " - " + firstDayofMonth.AddDays(-1).ToShortDateString()
            });

            if (DateTime.Now.Month > 2)
            {
                for (int i = DateTime.Now.Month - 2; i > 0; i--)
                {
                    if (i == DateTime.Now.Month - 2)
                    {
                        listItem.Add(new TimePeriodItem
                        {
                            Statement = "Previous Statement",
                            StartDatetoEndDate = firstDayofMonth.AddMonths(-2).ToString("MMM d, yyyy") + " to " + firstDayofMonth.AddMonths(-1).AddDays(-1).ToString("MMM d, yyyy"),
                            ComboText = string.Format("Previous Statement   {0} to {1}", firstDayofMonth.AddMonths(-i).ToString("MMM d, yyyy"), firstDayofMonth.AddMonths(-2).AddDays(-1).ToString("MMM d, yyyy")),
                            ComboValue = firstDayofMonth.AddMonths(-2).ToShortDateString() + " - " + firstDayofMonth.AddMonths(-1).AddDays(-1).ToShortDateString()
                        });
                    }
                    else
                    {
                        int j = 0;
                        listItem.Add(new TimePeriodItem
                        {
                            Statement = "",
                            StartDatetoEndDate = firstDayofMonth.AddMonths(-3 - j).ToString("MMM d, yyyy") + " to " + firstDayofMonth.AddMonths(-2 - j).AddDays(-1).ToString("MMM d, yyyy"),
                            ComboText = string.Format("Previous Statement   {0} to {1}", firstDayofMonth.AddMonths(-j - 3).ToString("MMM d, yyyy"), firstDayofMonth.AddMonths(-j - 2).AddDays(-1).ToString("MMM d, yyyy")),
                            ComboValue = firstDayofMonth.AddMonths(-3 - j).ToShortDateString() + " - " + firstDayofMonth.AddMonths(-2 - j).AddDays(-1).ToShortDateString()
                        });
                        j++;
                    }
                }
            }
            listItem.Add(new TimePeriodItem
            {
                Statement = "Year to Date",
                StartDatetoEndDate = firstDayofYear.ToString("MMM d, yyyy") + " to " + "Present",
                ComboText = string.Format("Year to Date   {0} to {1}", firstDayofYear.ToString("MMM d, yyyy"), "Present"),
                ComboValue = firstDayofYear.ToShortDateString() + " - " + DateTime.Now.ToShortDateString(),
            });
            listItem.Add(new TimePeriodItem
            {
                Statement = "Year End Summary",
                StartDatetoEndDate = firstLastYear.ToString("MMM d, yyyy") + " to " + endLastYear.ToString("MMM d, yyyy"),
                ComboText = string.Format("Year End Summary   {0} to {1}", firstLastYear.ToString("MMM d, yyyy"), endLastYear.ToString("MMM d, yyyy")),
                ComboValue = firstLastYear.ToShortDateString() + " - " + endLastYear.ToShortDateString()
            });

            _timePeriodCombo.DataSource = listItem;
            _timePeriodCombo.DataTextField = "ComboText";
            _timePeriodCombo.DataValueField = "ComboValue";
            _timePeriodCombo.DataBind();

        }

        protected void inItDatepicker()
        {
            RadDatePicker radDatepicker1 = (RadDatePicker)_timePeriodCombo.Footer.FindControl("RadDatePicker1");
            RadDatePicker radDatepicker2 = (RadDatePicker)_timePeriodCombo.Footer.FindControl("RadDatePicker2");
            radDatepicker1.MinDate = DateTime.Now.AddYears(-2);
            radDatepicker1.SelectedDate = DateTime.Now.AddYears(-1);
            radDatepicker2.SelectedDate = DateTime.Now;
        }


      
        #endregion


    }
}