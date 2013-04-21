<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ListRetrieveTransactions.ascx.cs" Inherits="CMSApp.Controls.Buxx.Account.ListRetrieveTransactions" %>
<div class="list-recent-transactions">
    <h2><span aria-hidden="true" class="icon" data-icon="&#x37;"></span>
        <asp:Label ID="_teenName" runat="server"></asp:Label></h2>
    <div>
        <telerik:RadScriptManager ID="RadScriptManager1" runat="server" />
        <telerik:RadAjaxManager ID="RadAjaxManager1" runat="server">
            <AjaxSettings>
                <telerik:AjaxSetting AjaxControlID="_radDataPager">
                    <UpdatedControls>
                        <telerik:AjaxUpdatedControl ControlID="_transactionView" />
                    </UpdatedControls>
                    <UpdatedControls>
                        <telerik:AjaxUpdatedControl ControlID="_radDataPager" />
                    </UpdatedControls>
                </telerik:AjaxSetting>

                <telerik:AjaxSetting AjaxControlID="_comboDate">
                    <UpdatedControls>
                        <telerik:AjaxUpdatedControl ControlID="_transactionView" />
                    </UpdatedControls>
                    <UpdatedControls>
                        <telerik:AjaxUpdatedControl ControlID="_radDataPager" />
                    </UpdatedControls>
                </telerik:AjaxSetting>
            </AjaxSettings>
        </telerik:RadAjaxManager>
        <table style="width: 100%;">
            <tr align="right">
                <td>Time period:
                    <telerik:RadComboBox ID="_comboDate" runat="server" Width="300px" OnSelectedIndexChanged="_comboDate_SelectedIndexChanged"></telerik:RadComboBox>
                </td>
            </tr>
            <tr align="left">
                <td>
                    <telerik:RadListView ID="_transactionView" runat="server" DataKeyNames="TransactionNumber" ItemPlaceholderID="ItemPlaceHolder1" Skin="Metro"
                        AllowPaging="true">
                        <LayoutTemplate>
                            <div>
                                <table style="width: 100%">
                                    <thead>
                                        <tr>
                                            <td>Date
                                            </td>
                                            <td>Description
                                            </td>
                                            <td>Category
                                            </td>
                                            <td>Amount
                                            </td>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        <asp:PlaceHolder ID="ItemPlaceHolder1" runat="server"></asp:PlaceHolder>
                                    </tbody>
                                </table>
                            </div>
                        </LayoutTemplate>
                        <ItemTemplate>
                            <tr class="rlvI">
                                <td>
                                    <%# Eval("Date","{0:MM/dd/yy}")%>
                                </td>
                                <td>
                                    <%# Eval("Description")%>
                                </td>
                                <td>
                                    <%# Eval("MerchantCategoryGroup")%>
                                </td>
                                <td>
                                    <%# Eval("Amount","{0:C}")%>
                                </td>
                            </tr>
                        </ItemTemplate>
                    </telerik:RadListView>
                </td>
            </tr>
            <tr align="right">
                <td>
                    <telerik:RadDataPager ID="_radDataPager" runat="server" OnTotalRowCountRequest="_OnTotalRowCountRequest"
                        OnPageIndexChanged="_PageIndexChanged" Skin="Metro">
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
                        <asp:Label runat="server" ID="CurrentPageLabel" Text="<%# Container.Owner.CurrentPageIndex+1%>" />
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
                                        <b>
                                            <telerik:RadComboBox ID="_PerPage" runat="server" AutoPostBack="true" OnSelectedIndexChanged="_SelectedIndexChanged"
                                                SelectedValue="<%#Container.Owner.PageSize %>" Skin="Metro" Width="300px">
                                                <Items>
                                                    <telerik:RadComboBoxItem Text="5 Per Page" Value="5" />
                                                    <telerik:RadComboBoxItem Text="10 Per Page" Value="10" />
                                                    <telerik:RadComboBoxItem Text="15 Per Page" Value="15" />
                                                    <telerik:RadComboBoxItem Text="20 Per Page" Value="20" />
                                                    <telerik:RadComboBoxItem Text="25 Per Page" Value="25" />
                                                </Items>
                                            </telerik:RadComboBox>
                                            <br />
                                        </b>
                                    </div>
                                </PagerTemplate>
                            </telerik:RadDataPagerTemplatePageField>
                        </Fields>
                    </telerik:RadDataPager>
                </td>
            </tr>

        </table>

        <br />

    </div>


</div>
