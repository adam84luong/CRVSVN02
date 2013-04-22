using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Telerik.Web.UI;

namespace WebApplication2
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
             BindcomboDate(DateTime.Now.AddYears(-1),DateTime.Now.AddYears(0));
        }
        
        public void BindcomboDate(DateTime startDate, DateTime endDate)
        {

            DateTime startdatetemp = startDate;
            DateTime enddatetemp = endDate;
            int i = 0;
            while (DateTime.Compare(startDate, enddatetemp) <= 0)
            {
                if (DateTime.Compare(startDate.AddMonths(1), enddatetemp) >= 0)
                {
                    if (i == 0)
                    {
                        RadComboBox1.Items.Add(new RadComboBoxItem("Current Statement " + startDate.ToShortDateString() + " to " + enddatetemp.ToShortDateString(), startDate.ToShortDateString() + "-" + enddatetemp.ToShortDateString()));
                    }
                    else
                    {
                        RadComboBox1.Items.Add(new RadComboBoxItem(startDate.ToShortDateString() + " to " + enddatetemp.ToShortDateString(), startDate.ToShortDateString() + "-" + enddatetemp.ToShortDateString()));
                    }
                    break;
                }
                else
                {
                    startdatetemp = enddatetemp.AddMonths(-1);
                    if (i == 0)
                    {
                        RadComboBox1.Items.Add(new RadComboBoxItem("Current Statement " + startdatetemp.ToShortDateString() + " to " + enddatetemp.ToShortDateString(), startdatetemp.ToShortDateString() + "-" + enddatetemp.ToShortDateString()));
                    }
                    else
                    {
                        RadComboBox1.Items.Add(new RadComboBoxItem(startdatetemp.ToShortDateString() + " to " + enddatetemp.ToShortDateString(), startdatetemp.ToShortDateString() + "-" + enddatetemp.ToShortDateString()));
                    }
                    enddatetemp = startdatetemp.AddDays(-1);
                }

                i++;
            }

        }
        protected void RadComboBox1_SelectedIndexChanged(object sender, Telerik.Web.UI.RadComboBoxSelectedIndexChangedEventArgs e)
        {
           
        }
    }
}