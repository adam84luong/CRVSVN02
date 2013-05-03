<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="FundingSourceWebPart.ascx.cs" Inherits="CMSApp.CMSWebParts.CardLab.Buxx.Account.FundingSourceWebPart" %>
<%@ Register Src="~/Controls/Buxx/Account/CreditCardFundingSourceList.ascx" TagName="CreditCardFundingSourceList" TagPrefix="uc" %>
<%@ Register Src="~/Controls/Buxx/Account/AddCreditCard.ascx" TagName="AddCreditCard" TagPrefix="uc" %>
<%@ Register Src="~/Controls/MessageBox.ascx" TagName="MessageBox" TagPrefix="uc" %>
<div class="content_main">
    <uc:MessageBox ID="_ucMsgBox" runat="server" CssClass="system_messages" />
    <asp:ValidationSummary ID="_validationSummary" runat="server" CssClass="validationmessage" ValidationGroup="SignUp" HeaderText="There are errors on this form. Please correct them." />
    <asp:Panel ID="_formFundingSourceList" runat="server">
           <h2 class="table-head head">
            <table border="0" style="width: 100%">
                <tr>
                    <td style="text-align: left">
                        <span aria-hidden="true" class="icon" data-icon="&#x37;"></span>Funding Sources
                    </td>
                    <td style="text-align: right">
                        <telerik:RadButton ID="_showAddButton" runat="server" Text="Add a Funding Source"
                            EnableEmbeddedSkins="False" Skin="OrangeBuxx" OnClick="ShowAddButtonClick">
                        </telerik:RadButton>
                    </td>
                </tr>
            </table>
                </h2>
            <uc:CreditCardFundingSourceList ID="_creditCardFundingSourceList" runat="server" />      
    </asp:Panel>
    <asp:Panel ID="_formAddCard" runat="server">
            <uc:AddCreditCard ID="_addCreditCard" runat="server" />
            <div class="term">
                <div class="form_check">
                    <asp:CheckBox ID="_agreeTermCheckBox" runat="server" />
                    <label>I agree with the</label>
                    <asp:HyperLink ID="_agreementLink" runat="server" CssClass="term-link">Cardholder agreement</asp:HyperLink>
                </div>
                <div class="right">
                    <span id="_orderCardSpan">
                        <telerik:RadButton ID="_addCreditCardButton" runat="server" Text="Add a Funding Source"
                            EnableEmbeddedSkins="False" Skin="OrangeBuxx" OnClick="AddCreditCardButtonClick" ValidationGroup="SignUp">
                        </telerik:RadButton>
                    </span>
                    <span id="_orderCardDisibleSpan">
                        <asp:Label ID="_orderCardDisibleLabel" runat="server" Text="Add a Funding Source" CssClass="orange-disable-button" />
                    </span>
                </div>
            </div>
      
    </asp:Panel>
</div>

<telerik:RadScriptBlock ID="_radScriptBlock" runat="server">
    <script type="text/javascript">
        jQuery(".term").ready(function () {
            if ($("[id$='_agreeTermCheckBox']").is(':checked')) {
                $("[id$='_orderCardDisibleSpan']").hide();
                $("[id$='_orderCardSpan']").show();
            } else {
                $("[id$='_orderCardDisibleSpan']").show();
                $("[id$='_orderCardSpan']").hide();
            }
            $("[id$='_agreeTermCheckBox']").click(function () {
                if ($("[id$='_agreeTermCheckBox']").is(':checked')) {
                    $("[id$='_orderCardDisibleSpan']").hide();
                    $("[id$='_orderCardSpan']").show();
                } else {
                    $("[id$='_orderCardDisibleSpan']").show();
                    $("[id$='_orderCardSpan']").hide();
                }
            });
        });
    </script>
</telerik:RadScriptBlock>
