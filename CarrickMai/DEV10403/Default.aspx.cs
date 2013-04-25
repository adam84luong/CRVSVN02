using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telerik.Web.UI;
namespace Demo
{
    public partial class WebForm1 : System.Web.UI.Page
    {
        DateTime _startDate = new DateTime();
        DateTime _endDate = new DateTime();
        protected void Page_Load(object sender, EventArgs e)
        {           
            if (!IsPostBack)
            {
                
                RadButton button = (RadButton)RadComboBox1.Footer.FindControl("RadButton2");
                button.Click += new EventHandler(this.RadButton2_Click);
                AddDataItems();
                RadDatePicker radDatepicker1 = (RadDatePicker)RadComboBox1.Footer.FindControl("RadDatePicker1");
                RadDatePicker radDatepicker2 = (RadDatePicker)RadComboBox1.Footer.FindControl("RadDatePicker2");
                radDatepicker1.MinDate = DateTime.Now.AddYears(-2);
                radDatepicker1.SelectedDate = DateTime.Now.AddYears(-1);
                radDatepicker2.SelectedDate = DateTime.Now;
            }
        }
        protected void RadButton2_Click(object sender, EventArgs e)
        {
            RadDatePicker radDatepicker1 = (RadDatePicker)RadComboBox1.Footer.FindControl("RadDatePicker1");
            RadDatePicker radDatepicker2 = (RadDatePicker)RadComboBox1.Footer.FindControl("RadDatePicker2");
            _startDate= radDatepicker1.SelectedDate.Value;
            _endDate = radDatepicker2.SelectedDate.Value;
            Label4.Text = "Start Date:" + _startDate.ToShortDateString();
            Label5.Text = "End Date:" + _endDate.ToShortDateString();
        }
        protected void RadComboBox1_SelectedIndexChanged(object sender, RadComboBoxSelectedIndexChangedEventArgs e)
        {
            string stringdate = RadComboBox1.SelectedValue;
            int pos = stringdate.IndexOf("-");
            Label4.Text = "Start Date:" + stringdate.Substring(0, pos);
            Label5.Text = "End Date:" + stringdate.Substring(pos + 1, stringdate.Length - pos - 1);
        }
        public void AddDataItems()
        {
            RadComboBoxItem recentAcivity = new RadComboBoxItem();
            recentAcivity.Text = "Recent Activity \t" + DateTime.Now.AddDays(-1).ToShortDateString() + " to " + DateTime.Now.ToShortDateString() ;
            recentAcivity.Value = DateTime.Now.AddDays(-1).ToShortDateString() + " - " + DateTime.Now.ToShortDateString();            
            RadComboBox1.Items.Add(recentAcivity);          

            RadComboBoxItem currentStatement = new RadComboBoxItem();
            currentStatement.Text = "Current Statement \t" + DateTime.Now.AddMonths(-1).ToShortDateString() + " to " + DateTime.Now.AddMonths(0).AddDays(-1).ToShortDateString();
            currentStatement.Value = DateTime.Now.AddMonths(-1).ToShortDateString() + " - " + DateTime.Now.AddMonths(0).AddDays(-1).ToShortDateString();
            RadComboBox1.Items.Add(currentStatement);

            for (int i = 0; i < DateTime.Now.Month; i++)
            {
                if (i == 0)
                {
                    RadComboBox1.Items.Add(new RadComboBoxItem("Previous Statement \t" + DateTime.Now.AddMonths(-i).AddDays(-1).ToShortDateString() + " to " + DateTime.Now.AddMonths(-1).AddDays(-i).ToShortDateString(), DateTime.Now.AddMonths(-i).AddDays(-1).ToShortDateString() + " - " + DateTime.Now.AddMonths(-1).AddDays(-i).ToShortDateString()));
                }
                else
                {
                    RadComboBox1.Items.Add(new RadComboBoxItem(DateTime.Now.AddMonths(-i).ToShortDateString() + " to " + DateTime.Now.AddMonths(-i - 1).ToShortDateString(), DateTime.Now.AddMonths(-i).ToShortDateString() + " - " + DateTime.Now.AddMonths(-i - 1).ToShortDateString()));
                }
            }

            RadComboBoxItem yearToDate = new RadComboBoxItem();
            DateTime firstDay = new DateTime(DateTime.Now.Year , 1, 1);
            yearToDate.Text = "Year to Date \t" + firstDay.ToShortDateString() + " to " + DateTime.Now.ToShortDateString();
            yearToDate.Value = firstDay.ToShortDateString() + " - " + DateTime.Now.ToShortDateString();
            RadComboBox1.Items.Add(yearToDate);

            RadComboBoxItem yearEndSummary = new RadComboBoxItem();
            DateTime firstLastYear = new DateTime(DateTime.Now.Year - 1, 1, 1);
            DateTime endLastYear = new DateTime(DateTime.Now.Year - 1, 12, 31);
            yearEndSummary.Text = "Year End Summary \t" + firstLastYear.ToShortDateString() + " to " + endLastYear.ToShortDateString();
            yearEndSummary.Value = firstLastYear.ToShortDateString() + " - " + endLastYear.ToShortDateString();
            RadComboBox1.Items.Add(yearEndSummary);
        
        }
    }
}