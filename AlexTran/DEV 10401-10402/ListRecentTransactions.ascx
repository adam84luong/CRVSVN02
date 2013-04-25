<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ListRecentTransactions.ascx.cs" Inherits="CMSApp.Controls.Buxx.Account.ListRecentTransactions" %>
<div class="list-transactions">

    <telerik:RadGrid ID="_transactionGrid" runat="server" AutoGenerateColumns="False"
        OnItemDataBound="TransactionItemDataBound" 
        OnItemCreated="TransactionItemCreated" OnPageIndexChanged="TransactionGridPageIndexChanged"
        OnPageSizeChanged="TransactionGridPageSizeChanged"
        EnableEmbeddedBaseStylesheet="False" EnableEmbeddedSkins="False" Skin="GrayBuxx"
        AllowCustomPaging="True">
        <MasterTableView DataKeyNames="TransactionNumber" ClientDataKeyNames="TransactionNumber">
            <Columns>
                <telerik:GridBoundColumn UniqueName="Date" HeaderText="Date" DataField="Date" DataFormatString="{0:MM/dd/yy}">
                </telerik:GridBoundColumn>
                <telerik:GridBoundColumn UniqueName="Description" HeaderText="Description"  DataField="Description">
                </telerik:GridBoundColumn>
                <telerik:GridBoundColumn UniqueName="MerchantCategoryGroup" HeaderText="Category" DataField="MerchantCategoryGroup">
                </telerik:GridBoundColumn>
                <telerik:GridBoundColumn UniqueName="Amount" HeaderText="Amount" DataField="Amount" DataFormatString="{0:$###,###.##}">
                </telerik:GridBoundColumn>
            </Columns>
            <PagerStyle Mode="NumericPages" AlwaysVisible="true"></PagerStyle>
        </MasterTableView>
    </telerik:RadGrid>

</div>