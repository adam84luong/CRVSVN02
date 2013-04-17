<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="CardTransactionsWebPart.ascx.cs" Inherits="CMSApp.CMSWebParts.CardLab.Buxx.Account.CardTransactionsWebPart" %>
<%@ Register Src="~/Controls/Buxx/Account/TeenListTab.ascx" TagName="TeenListTab" TagPrefix="uc" %>
<%@ Register Src="~/Controls/Buxx/Account/ListRetrieveTransactions.ascx" TagName="ListRetrieveTransactions" TagPrefix="uc" %>
<div class="teen-overview-webpart">
    <uc:TeenListTab ID="_teenListTab" runat="server" />
    <uc:ListRetrieveTransactions ID="_listRetrieveTransactions" runat="server" />
     </div>

