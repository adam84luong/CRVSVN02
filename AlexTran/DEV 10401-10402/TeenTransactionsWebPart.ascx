<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="TeenTransactionsWebPart.ascx.cs" Inherits="CMSApp.CMSWebParts.CardLab.Buxx.Account.TeenTransactionsWebPart" %>
<%@ Register Src="~/Controls/Buxx/Account/ListRecentTransactions.ascx" TagName="ListRecentTransactions" TagPrefix="uc" %>
<%@ Register Src="~/Controls/Buxx/Account/TeenListTab.ascx" TagName="TeenListTab" TagPrefix="uc" %> 
<div class="teen-transactions-webpart">
   <uc:TeenListTab ID="_teenListTab" runat="server" />  
    <div class="teen-transaction-recent">
          <h2><span aria-hidden="true" class="icon" data-icon="&#x37;"></span><asp:Label ID="_teenName" runat="server"></asp:Label></h2>
        <table border="0" style="width: 100%">
        <tr>
            <td style="text-align: right; width:50%">Time period: 
            </td>
            <td style="text-align: right; width: 50%">
                <telerik:RadComboBox ID="_timePeriodCombo" runat="server" AutoPostBack="true" EnableEmbeddedSkins="False" Skin="GrayBuxx" Width="100%" OnSelectedIndexChanged="_timePeriodComboSelectedIndexChanged" >
                    <ItemTemplate >                        
                        <table style="width: 100%">
                            <tr>
                                <td style="width: 45%; text-align:left;font-weight: bold;">                                 
                                        <%#DataBinder.Eval(Container.DataItem, "Statement")%>                            
                                </td>
                                <td style="width: 55%;text-align:left">                             
                                        <%#DataBinder.Eval(Container.DataItem, "StartDatetoEndDate")%>        
                                </td>

                            </tr>
                        </table>
                    </ItemTemplate>
                    <FooterTemplate>
                        <asp:Label ID="Label1" runat="server" Text="DateRange"></asp:Label>
                        <br />
                        <asp:Label ID="Label2" runat="server" Text="From"></asp:Label>
                        <telerik:RadDatePicker ID="RadDatePicker1" runat="server" ReadOnly="true" Width="100px">
                        </telerik:RadDatePicker>
                        <asp:Label ID="Label3" runat="server" Text="To"></asp:Label>
                        <telerik:RadDatePicker ID="RadDatePicker2" runat="server" ReadOnly="true"  Width="100px">
                        </telerik:RadDatePicker>
                        <telerik:RadButton ID="_Go" OnClick="_GoClick" runat="server" Text="GO">
                        </telerik:RadButton>
                    </FooterTemplate>
                </telerik:RadComboBox>
            </td>
        </tr>
    </table>
    <uc:ListRecentTransactions ID="_listRecentTransactions" runat="server" AllowPaging="True" />
        </div>
    <div style="padding-bottom: 60px;"></div>
    <div class="clear"></div>
</div>
