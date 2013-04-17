<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ListRetrieveTransactions.ascx.cs" Inherits="CMSApp.Controls.Buxx.Account.ListRetrieveTransactions" %>
<div class="list-recent-transactions">
    <h2><span aria-hidden="true" class="icon" data-icon="&#x37;"></span>
        <asp:Label ID="_teenName" runat="server"></asp:Label></h2>
    <div>
           <telerik:RadScriptManager ID="RadScriptManager1" runat="server" />   
        <telerik:RadAjaxLoadingPanel ID="RadAjaxLoadingPanel1" runat="server" Skin="Vista" />
        <telerik:RadAjaxPanel ID="RadAjaxPanel1" runat="server" LoadingPanelID="RadAjaxLoadingPanel1">     
        <telerik:RadGrid ID="_transactionGrid" runat="server" AutoGenerateColumns="False"
            OnItemDataBound="TransactionItemDataBound" OnNeedDataSource="TransactionNeedDataSource"
            EnableEmbeddedSkins="False" Skin="GrayBuxx">
            <MasterTableView DataKeyNames="TransactionNumber" ClientDataKeyNames="TransactionNumber">
                <Columns>
                    <telerik:GridBoundColumn HeaderText="Date" DataField="Date" DataFormatString="{0:MM/dd/yy}">
                        <ItemStyle CssClass="item" />
                    </telerik:GridBoundColumn>
                    <telerik:GridBoundColumn HeaderText="Description" DataField="Description">
                        <ItemStyle CssClass="item" />
                    </telerik:GridBoundColumn>
                    <telerik:GridBoundColumn HeaderText="Category" DataField="MerchantCategoryGroup">
                        <ItemStyle CssClass="item" />
                    </telerik:GridBoundColumn>
                    <telerik:GridBoundColumn HeaderText="Amount" DataField="Amount" DataType="System.Decimal" DataFormatString="{0:$###,###.##}">
                        <ItemStyle CssClass="item" />
                    </telerik:GridBoundColumn>
                </Columns>
            </MasterTableView>
        </telerik:RadGrid>
<br/>        
            <telerik:RadDataPager ID="_radDataPager" runat="server" PageSize="100" OnPageIndexChanged="SearchResultPageIndexChanged" Skin="Metro"
    OnCommand="SearchResultPagerCommand" CssClass="gallerypager" PagedControlID="_transactionGrid">
        <Fields>
            <telerik:RadDataPagerTemplatePageField>
                <PagerTemplate>
                    <div style="float: right">
                        <b>
                            <asp:Label runat="server" ID="MinItemLabel" Text="<%# Container.Owner.StartRowIndex+1%>" />
                            -<asp:Label runat="server" ID="MaxItemLabel" Text="<%# Container.Owner.TotalRowCount > (Container.Owner.StartRowIndex+Container.Owner.PageSize) ? Container.Owner.StartRowIndex+Container.Owner.PageSize : Container.Owner.TotalRowCount %>" />
                            of
                            <asp:Label runat="server" ID="TotalItemsLabel" Text="<%# Container.Owner.TotalRowCount%>" />
                            Transactions
                                                            <br />
                        </b>
                    </div>
                </PagerTemplate>
            </telerik:RadDataPagerTemplatePageField>
            <telerik:RadDataPagerButtonField FieldType="FirstPrev" />
            <telerik:RadDataPagerTemplatePageField>
                <PagerTemplate>
                    <div style="float: right">
                        <b>Page 
                        <asp:Label runat="server" ID="CurrentPageLabel" Text="<%# Container.Owner.CurrentPageIndex%>" />
                            of 
                            <asp:Label runat="server" ID="TotalPagesLabel" Text="<%# Container.Owner.PageCount%>" />
                            <br />
                        </b>
                    </div>
                </PagerTemplate>
            </telerik:RadDataPagerTemplatePageField>
            <telerik:RadDataPagerButtonField FieldType="NextLast" />
            <telerik:RadDataPagerTemplatePageField>
                <PagerTemplate>
                    <div style="float: right">
                        <b><telerik:RadComboBox ID="PerPageCombo" CommandName="PageSize"  runat="server">
                   <Items>
                       <telerik:RadComboBoxItem runat="server" CommandName="PageSize" CommandArgument="100" Text="100 Per Page" Value="100" Selected="True" />
                       <telerik:RadComboBoxItem runat="server" CommandName="PageSize" CommandArgument="200" Text="200 Per Page" Value="200" />
                       <telerik:RadComboBoxItem runat="server" CommandName="PageSize" CommandArgument="300" Text="300 Per Page" Value="300" />
                       <telerik:RadComboBoxItem runat="server" CommandName="PageSize" CommandArgument="400" Text="400 Per Page" Value="400" />
                       <telerik:RadComboBoxItem runat="server" CommandName="PageSize" CommandArgument="500" Text="500 Per Page" Value="500" />
                   </Items>
               </telerik:RadComboBox> <br />
                        </b>
                    </div>
                </PagerTemplate>
            </telerik:RadDataPagerTemplatePageField>

        </Fields>
    </telerik:RadDataPager>
                 </telerik:RadAjaxPanel>
             </div>
  
   
</div>
