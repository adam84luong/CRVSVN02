<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="TeenTransactionsWebPart.ascx.cs" Inherits="CMSApp.CMSWebParts.CardLab.Buxx.Account.TeenTransactionsWebPart" %>
<%@ Register Src="~/Controls/Buxx/Account/ListRecentTransactions.ascx" TagName="ListRecentTransactions" TagPrefix="uc" %>
<%@ Register Src="~/Controls/Buxx/Account/TeenListTab.ascx" TagName="TeenListTab" TagPrefix="uc" %>
<div class="teen-transactions-webpart">
    <uc:TeenListTab ID="_teenListTab" runat="server" />
    <h2><span aria-hidden="true" class="icon" data-icon="&#x37;"></span><asp:Label ID="_teenName" runat="server"></asp:Label></h2>
    <uc:ListRecentTransactions ID="_listRecentTransactions" runat="server" AllowPaging="True" />
</div>
