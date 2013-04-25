<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="FundingSourcesWebPart.ascx.cs" Inherits="CMSApp.CMSWebParts.CardLab.Buxx.Account.FundingSourcesWebPart" %>
<div style="margin-top: 15px">
    <h3 class="table-head head">
        <table border="0" cellpadding="0" cellspacing="0" style="width: 100%">
            <tr>
                <td>
                    Funding Sources
                </td>
                <td style="text-align: right">
                    <telerik:RadButton ID="_addNewFunding" runat="server" Text="Add a Funding Source"
                        CssClass="btn btn_thin" ButtonType="LinkButton">
                    </telerik:RadButton>
                </td>
            </tr>
        </table>
    </h3>
    <telerik:RadGrid ID="_fundingSourcesGrid" runat="server" GridLines="None" AutoGenerateColumns="False"
         EnableEmbeddedBaseStylesheet="False" EnableEmbeddedSkins="False" Skin="GrayBuxx">
        <MasterTableView ClientDataKeyNames="AccountIdentifier" DataKeyNames="AccountIdentifier"     >
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
</div>