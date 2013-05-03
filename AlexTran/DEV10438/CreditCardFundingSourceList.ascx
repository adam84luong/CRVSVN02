<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="CreditCardFundingSourceList.ascx.cs" Inherits="CMSApp.Controls.Buxx.Account.CreditCardFundingSourceList" %>
 <telerik:RadGrid ID="_fundingSourcesGrid" runat="server" GridLines="None" AutoGenerateColumns="False"
        EnableEmbeddedBaseStylesheet="False" EnableEmbeddedSkins="False" Skin="GrayBuxx">
        <MasterTableView ClientDataKeyNames="AccountIdentifier" DataKeyNames="AccountIdentifier">
            <Columns>
                <telerik:GridBoundColumn UniqueName="CardType" HeaderText="Card Type" DataField="CardType">
                </telerik:GridBoundColumn>
                <telerik:GridBoundColumn UniqueName="CardNumber" HeaderText="Card Number"
                    DataField="CardNumber">
                </telerik:GridBoundColumn>
                <telerik:GridBoundColumn UniqueName="Expiration" HeaderText="Expiration" DataField="Expiration">
                </telerik:GridBoundColumn>
                <telerik:GridBoundColumn UniqueName="Status" HeaderText="Status" DataField="Status">
                </telerik:GridBoundColumn>
                <telerik:GridTemplateColumn HeaderText="Actions">
                    <ItemTemplate>
                        <asp:LinkButton ID="_editButton" runat="server" CommandName="Edit" ToolTip="Edit"
                            CssClass="editbuton">Edit </asp:LinkButton>
                    </ItemTemplate>
                </telerik:GridTemplateColumn>
            </Columns>
        </MasterTableView>
    </telerik:RadGrid>