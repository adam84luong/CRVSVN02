<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="TeenTransactionsWebPart.ascx.cs" Inherits="CMSApp.CMSWebParts.CardLab.Buxx.Account.TeenTransactionsWebPart" %>
<%@ Register Src="~/Controls/Buxx/Account/ListRecentTransactions.ascx" TagName="ListRecentTransactions" TagPrefix="uc" %>
<%@ Register Src="~/Controls/Buxx/Account/TeenListTab.ascx" TagName="TeenListTab" TagPrefix="uc" %>
<div class="teen-transactions-webpart">
    <uc:TeenListTab ID="_teenListTab" runat="server" />
    <h2><span aria-hidden="true" class="icon" data-icon="&#x37;"></span><asp:Label ID="_teenName" runat="server"></asp:Label></h2>                          
                            
        <table border="0" style="width: 100%">
            <tr>
                <td  style="text-align: right; width:30%" >Time period: 
                </td>
                <td style="text-align: right; width:70%" >               
                <telerik:RadComboBox ID="_timePeriodCombo" runat="server" AutoPostBack="true" EnableEmbeddedSkins="False" Skin="GrayBuxx" Width="98%" OnSelectedIndexChanged="_timePeriodComboSelectedIndexChanged">        
                                <ItemTemplate>
                                    <table style="width: 430px;border-top:solid 1px;margin-left: -7px;" class="comboTable" cellspacing="0" cellpadding="0">
                                        <tr>
                                            
                                                <td style="width: 210px;padding-left: 30px;">
                                                    <div class='comboItem'>
                                                        <%#DataBinder.Eval(Container.DataItem, "Column1")%>
                                                    </div>
                                                </td>
                                                <td style="width: 230px;">
                                                    <div class='comboItem'>
                                                        <%#DataBinder.Eval(Container.DataItem, "Column2")%>
                                                    </div>
                                                </td>
                                            
                                        </tr>
                                    </table>
                                </ItemTemplate>
                                <FooterTemplate>
                                  <asp:Label ID="Label1" runat="server" Text="DateRange"></asp:Label>
                                    <br />
                                    <asp:Label ID="Label2" runat="server" Text="From"></asp:Label>
                                    <telerik:RadDatePicker ID="RadDatePicker1" Runat="server" ReadOnly="true" >
                                    </telerik:RadDatePicker>
                                    <asp:Label ID="Label3" runat="server" Text="To"></asp:Label>
                                    <telerik:RadDatePicker ID="RadDatePicker2" Runat="server" ReadOnly="true" >
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
