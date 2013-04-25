<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="TeenTransactionsWebPart.ascx.cs" Inherits="CMSApp.CMSWebParts.CardLab.Buxx.Account.TeenTransactionsWebPart" %>
<%@ Register Src="~/Controls/Buxx/Account/ListRecentTransactions.ascx" TagName="ListRecentTransactions" TagPrefix="uc" %>

<div class="teen-transactions-webpart">
    <h2><span aria-hidden="true" class="icon" data-icon="&#x37;"></span>Teen's Transactions</h2>
    <uc:ListRecentTransactions ID="_listRecentTransactions" runat="server" AllowPaging="True" />

    <div class="form_col_timeperiod">
			    <label for="transactions_timeperiod" style="position: absolute;margin-left: -100px;margin-top: 5px;">Time period: </label>
				        <form id="form1" runat="server">
                            <telerik:RadScriptManager ID="RadScriptManager1" runat="server" >
                                <Scripts>
                                    <asp:ScriptReference Assembly="Telerik.Web.UI" Name="Telerik.Web.UI.Common.Core.js">
                                    </asp:ScriptReference>
                                    <asp:ScriptReference Assembly="Telerik.Web.UI" Name="Telerik.Web.UI.Common.jQuery.js">
                                    </asp:ScriptReference>
                                    <asp:ScriptReference Assembly="Telerik.Web.UI" Name="Telerik.Web.UI.Common.jQueryInclude.js">
                                    </asp:ScriptReference>
                                </Scripts>
                            </telerik:RadScriptManager>
                            <telerik:RadAjaxManager ID="RadAjaxManager1" runat="server">
                            </telerik:RadAjaxManager>
                        <div>
                            <telerik:RadComboBox ID="RadComboBox1" runat="server" AutoPostBack="true" CssClass ="CustomCssClass" class="RadComboBox RadComboBox_Default"  style="white-space:normal;width:440px;" OnSelectedIndexChanged="RadComboBox1_SelectedIndexChanged">        
                                <FooterTemplate>
                                  <asp:Label ID="Label1" runat="server" Text="DateRange"></asp:Label>
                                    <br />
                                    <asp:Label ID="Label2" runat="server" Text="From"></asp:Label>
                                    <telerik:RadDatePicker ID="RadDatePicker1" Runat="server" ReadOnly="true" >
                                    </telerik:RadDatePicker>
                                    <asp:Label ID="Label3" runat="server" Text="To"></asp:Label>
                                    <telerik:RadDatePicker ID="RadDatePicker2" Runat="server" ReadOnly="true" >
                                    </telerik:RadDatePicker>
                                     <telerik:RadButton ID="RadButton2" OnClick="RadButton2_Click" runat="server" Text="GO">
                                     </telerik:RadButton>
                                </FooterTemplate>
                            </telerik:RadComboBox>
                        </div>
                        </form>
			    </div>
</div>
