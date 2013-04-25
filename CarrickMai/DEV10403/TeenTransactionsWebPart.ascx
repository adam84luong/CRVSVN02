<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="TeenTransactionsWebPart.ascx.cs" Inherits="CMSApp.CMSWebParts.CardLab.Buxx.Account.TeenTransactionsWebPart" %>
<%@ Register Src="~/Controls/Buxx/Account/ListRecentTransactions.ascx" TagName="ListRecentTransactions" TagPrefix="uc" %>

<div class="teen-transactions-webpart">
    <h2><span aria-hidden="true" class="icon" data-icon="&#x37;"></span>Teen's Transactions</h2>
    <uc:ListRecentTransactions ID="_listRecentTransactions" runat="server" AllowPaging="True" />
</div>
