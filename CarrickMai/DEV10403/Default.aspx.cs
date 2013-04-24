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
            RadComboBoxItem selectedText = new RadComboBoxItem();
            selectedText.Text = radDatepicker1.SelectedDate.Value.ToShortDateString() + " to " + radDatepicker2.SelectedDate.Value.ToShortDateString();
            selectedText.Value ="0";
            RadComboBox1.Items.Add(selectedText);
        }
        public void AddDataItems()
        {
            RadComboBoxItem recentAcivity = new RadComboBoxItem();
            recentAcivity.Text = "Recent Activity " + DateTime.Now.AddDays(-1).ToShortDateString() + " to " + DateTime.Now.ToShortDateString() ;
            recentAcivity.Value = "0";            
            RadComboBox1.Items.Add(recentAcivity);

            if (RadComboBox1.SelectedItem.Text == recentAcivity.Text)
            {
                _startDate = DateTime.Now.AddDays(-1);
                _endDate = DateTime.Now;
            }

            RadComboBoxItem currentStatement = new RadComboBoxItem();
            currentStatement.Text = "Current Statement " + DateTime.Now.AddMonths(-1).ToShortDateString() + " to " + DateTime.Now.AddMonths(0).AddDays(-1).ToShortDateString();
            currentStatement.Value = "1";
            RadComboBox1.Items.Add(currentStatement);
                       
            for (int i = 2; i < 7; i++)
            {
                if (i == 2)
                {
                    RadComboBox1.Items.Add(new RadComboBoxItem("Previous Statement " + DateTime.Now.AddMonths(-i).AddDays(-1).ToShortDateString() + " to " + DateTime.Now.AddMonths(-1).AddDays(-i).ToShortDateString(),"2"));
                }
                else
                {
                    RadComboBox1.Items.Add(new RadComboBoxItem("Previous Statement " + DateTime.Now.AddMonths(-i).ToShortDateString() + " to " + DateTime.Now.AddMonths(-i - 1).ToShortDateString(), i.ToString()));
                }
            }

            RadComboBoxItem yearToDate = new RadComboBoxItem();
            DateTime firstDay = new DateTime(DateTime.Now.Year , 1, 1);
            yearToDate.Text = "Year to Date " + firstDay.ToShortDateString() + " to " + DateTime.Now.ToShortDateString();
            yearToDate.Value = "7";
            RadComboBox1.Items.Add(yearToDate);

            RadComboBoxItem yearEndSummary = new RadComboBoxItem();
            DateTime firstLastYear = new DateTime(DateTime.Now.Year - 1, 1, 1);
            DateTime endLastYear = new DateTime(DateTime.Now.Year - 1, 12, 31);
            yearEndSummary.Text = "Year End Summary " + firstLastYear.ToShortDateString() + " to " + endLastYear.ToShortDateString();
            yearEndSummary.Value = "8";
            RadComboBox1.Items.Add(yearEndSummary);
        
        }
    }
}