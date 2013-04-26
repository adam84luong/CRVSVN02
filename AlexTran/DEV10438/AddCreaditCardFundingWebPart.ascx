<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="AddCreaditCardFundingWebPart.ascx.cs" Inherits="CMSApp.CMSWebParts.CardLab.Buxx.Account.AddCreaditCardFundingWebPart" %>
<%@ Register Src="~/Controls/Buxx/Account/CreaditCardFundingInformation.ascx" TagPrefix="uc" TagName="CreaditCardFundingInformation" %>
<%@ Register Src="~/Controls/MessageBox.ascx" TagName="MessageBox" TagPrefix="uc" %>

<div class="sign-up">    
    <uc:MessageBox ID="_ucMsgBox" runat="server" CssClass="system_messages" />
    <asp:ValidationSummary ID="_validationSummary" runat="server" CssClass="validationmessage" ValidationGroup="SignUp" HeaderText="There are errors on this form. Please correct them." />        
    <div class="distance">
        <div class="buxx-box">
            <div class="buxx-box-head">
                <h3>Your Card</h3>
            </div>
            <div class="box_body" style="text-align: center;">                
                <asp:Image ID="_productImage" runat="server" ImageUrl="~/Themes/Buxx/PublicPage/Images/cc_images/placeholder.png" AlternateText="your card" CssClass="creditcard" />
                <asp:Label ID="_skuLabel" runat="server"></asp:Label>                
                <div style="padding-top: 20px; font-weight: bold">
                    <a href="#">Change My Design</a>
                </div>            
            </div>
        </div>
    </div>    
   
     <div class="distance">
        <uc:CreaditCardFundingInformation ID="_creaditCardFundingInformation" runat="server" ValidationGroup="SignUp"/>
    </div>   
    <div class="term">
        <div class="form_check">
            <asp:CheckBox ID="_agreeTermCheckBox" runat="server" />
            <label> I agree with the</label>
            <asp:HyperLink ID="_agreementLink" runat="server" CssClass="term-link">Cardholder agreement</asp:HyperLink>
        </div>
        <div class="right">
            <span id="_orderCardSpan">
                <telerik:RadButton ID="_orderCardButton" runat="server" Text="Add Funding Source" 
                    EnableEmbeddedSkins="False" Skin="OrangeBuxx" OnClick="orderCardButtonClick" ValidationGroup="SignUp">
                </telerik:RadButton>
            </span>
           
        </div>
    </div>
    <div style="clear: both"></div>    
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
