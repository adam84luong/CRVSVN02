<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="CreaditCardFundingInformation.ascx.cs" Inherits="CMSApp.Controls.Buxx.Account.CreaditCardFundingInformation" %>
 
<div class="buxx-box">
    <div class="buxx-box-head">
        <h3>Load Information</h3>
    </div>
    <div class="box_body">
        <ul class="form-input">         
            <li class="inline">
                <div class="elementlabel">
                    Credit/Debit Card Number
                    <asp:RequiredFieldValidator ID="_cardNumberRequestValidator" runat="server" ControlToValidate="_cardNumberTextBox"
                        Display="dynamic" SetFocusOnError="True"><br/> Please enter card number
                    </asp:RequiredFieldValidator>
                    <asp:RegularExpressionValidator ID="_cardNumberExpressionValidator" runat="server" Display="Dynamic" ControlToValidate="_cardNumberTextBox"
                        ForeColor="Red" SetFocusOnError="True"> 
                         <br/>Credit card number is invalid                    
                    </asp:RegularExpressionValidator>
                </div>
                <div class="elementinput">
                    <asp:RequiredFieldValidator ID="_cardNumberIconRequestValidator" runat="server" ControlToValidate="_cardNumberTextBox"
                        Display="dynamic" SetFocusOnError="True" CssClass="invalid-style"><span class="validator-arrow-icon">&#xe001;</span>
                    </asp:RequiredFieldValidator>
                    <asp:RegularExpressionValidator ID="_cardNumberIconExpressionValidator" runat="server" CssClass="invalid-style" ControlToValidate="_cardNumberTextBox"
                        Display="dynamic" SetFocusOnError="True"><span class="validator-arrow-icon">&#xe001;</span>
                    </asp:RegularExpressionValidator>
                    <telerik:RadTextBox runat="server" ID="_cardNumberTextBox" Width="262" MaxLength="16" Skin="GrayBuxx" EnableEmbeddedSkins="false" AutoCompleteType="DisplayName">
                    </telerik:RadTextBox>
                </div>
            </li>
            <li class="inline">
                <div class="elementlabel">
                    We Accept Visa, Mastercard, and Discover.
                </div>
                <div class="elementinput">
                    <img id="_visaLogo" src="/Themes/Buxx/PublicPage/Images/logo-visa-cardload.png" alt="VisaCard" class="disablelogo" width="45" height="33" />
                    <img id="_masterLogo" src="/Themes/Buxx/PublicPage/Images/logo-master-card.png" alt="MasterCard" class="disablelogo" width="49" height="33"/>
                    <img id="_dicoverLogo" src="/Themes/Buxx/PublicPage/Images/logo-discover.png" alt="dicoverCard" class="disablelogo" width="46" height="33" />                                  
                </div>
            </li>
            <li>
                <div class="elementlabel">
                    Name on the Card
                    <asp:RequiredFieldValidator ID="_nameOnCardRequestValidator" runat="server" ControlToValidate="_nameOnCardTextBox"
                        Display="dynamic" SetFocusOnError="True" ><br/> Please enter name on the card
                    </asp:RequiredFieldValidator>
                    <asp:RegularExpressionValidator ID="_nameOnCardExpressionValidator" runat="server" Display="Dynamic" ControlToValidate="_nameOnCardTextBox"
                        ForeColor="Red" SetFocusOnError="True">  
                         <br/>Name on the Card is invalid
                    </asp:RegularExpressionValidator>
                </div>
                <div class="elementinput">
                    <asp:RequiredFieldValidator ID="_nameOnCardIconRequestValidator" runat="server" CssClass="invalid-style" ControlToValidate="_nameOnCardTextBox"
                        Display="dynamic" SetFocusOnError="True"><span class="validator-arrow-icon">&#xe001;</span>
                    </asp:RequiredFieldValidator>
                    <asp:RegularExpressionValidator ID="_nameOnCardIconExpressionValidator" runat="server" CssClass="invalid-style" ControlToValidate="_nameOnCardTextBox"
                        Display="dynamic" SetFocusOnError="True"><span class="validator-arrow-icon">&#xe001;</span>
                    </asp:RegularExpressionValidator>
                    <telerik:RadTextBox runat="server" ID="_nameOnCardTextBox" Width="262" Skin="GrayBuxx"
                        EnableEmbeddedSkins="false" AutoCompleteType="DisplayName" MaxLength="128">
                    </telerik:RadTextBox>
                </div>
            </li>
            <li class="inline">
                <div class="elementlabel">
                    Expiration Date
                    <asp:RequiredFieldValidator ID="_expirationDateRequestValidator" runat="server" ControlToValidate="_expirationDatePicker"
                        Display="dynamic" SetFocusOnError="True"><br/> Expiration date is required or invalid format
                    </asp:RequiredFieldValidator>
                </div>
                <div class="elementinput">
                    <asp:RequiredFieldValidator ID="_expirationDateIconRequestValidator" runat="server" CssClass="invalid-style" ControlToValidate="_expirationDatePicker"
                        Display="dynamic" SetFocusOnError="True"><span class="validator-arrow-icon">&#xe001;</span>
                    </asp:RequiredFieldValidator>
                    <telerik:RadDatePicker ID="_expirationDatePicker" runat="server" Skin="GrayBuxx" EnableEmbeddedSkins="False" Width="262" ShowPopupOnFocus="True"
                        DatePopupButton-Visible="false" MinDate="1/1/1990">                     
                        <DateInput ID="DateInput1" EmptyMessage="__/__/____" runat="server"></DateInput>                        
                    </telerik:RadDatePicker>
                </div>
            </li>
            <li class="inline">
                <div class="elementlabel">
                    CVV Security Code
                    <asp:RequiredFieldValidator ID="_CVVRequestValidator" runat="server" ControlToValidate="_CVVTextBox"
                        Display="dynamic" SetFocusOnError="True"><br/>CVV security code is required
                    </asp:RequiredFieldValidator>
                    <asp:RegularExpressionValidator ID="_CVVExpressionValidator" runat="server" Display="Dynamic" ControlToValidate="_CVVTextBox"
                        ForeColor="Red" SetFocusOnError="True"> 
                         <br/>Enter 3 digit CVV Code                       
                    </asp:RegularExpressionValidator>
                </div>
                <div class="elementinput">
                    <asp:RequiredFieldValidator ID="_CVVIconRequestValidator" runat="server" CssClass="invalid-style" ControlToValidate="_CVVTextBox"
                        Display="dynamic" SetFocusOnError="True"><span class="validator-arrow-icon">&#xe001;</span>
                    </asp:RequiredFieldValidator>
                    <asp:RegularExpressionValidator ID="_CVVIconExpressionValidator" runat="server" CssClass="invalid-style" ControlToValidate="_CVVTextBox"
                        Display="dynamic" SetFocusOnError="True"><span class="validator-arrow-icon">&#xe001;</span>
                    </asp:RegularExpressionValidator>
                    <telerik:RadTextBox runat="server" ID="_CVVTextBox" Width="116" MaxLength="3" Skin="GrayBuxx" EnableEmbeddedSkins="false"></telerik:RadTextBox>
                    <div class="elementlabel" style="font-weight: bold">
                        <asp:HyperLink runat="server" ID="_cvvHelpLink" Text="What's this?" CssClass="tooltip-help"></asp:HyperLink>
                    </div>                    
                </div>
                <telerik:RadToolTip ID="_cvvTooltip" runat="server" Position="TopRight" TargetControlID="_cvvHelpLink" Width="300"
                    Animation="None" RelativeTo="Element" EnableShadow="true" ShowEvent="OnClick">
                    <div class="cvv2Code-title">
                        Look on the back side of the card for the three digit number by the signature panel.
                    </div>
                    <div class="cvv2Code-image">
                        <img id="_imageSecurityCode" src="~/App_Themes/GCL/Images/3digitcode.gif" alt="Security Code Location" />
                    </div>
                </telerik:RadToolTip>
            </li>
        </ul>
    </div>
</div>
<telerik:RadScriptBlock ID="_radScriptBlock" runat="server">
    <script type="text/javascript">
        $(document).ready(function () {
            var visaLogo = $("[id$='_visaLogo']");
            var masterLogo = $("[id$='_masterLogo']");
            var dicoverLogo = $("[id$='_dicoverLogo']");

            $("[id$='_cardNumberTextBox']").keyup(function () {
                apllySelect(this);
            });

            function apllySelect(e) {
                var value = $(e).val();
                if (value.startsWith("4")) {
                    selectedVisa();
                }
                else if (value.startsWith("5")) {
                    selectedMaster();
                }
                else if (value.startsWith("6")) {
                    selectedDiscover();
                }
                else {
                    selectedNone();
                }
            }
            function selectedVisa() {
                visaLogo.removeClass("disablelogo");
                masterLogo.addClass("disablelogo");
                dicoverLogo.addClass("disablelogo");
            }
            function selectedMaster() {

                visaLogo.addClass("disablelogo");
                masterLogo.removeClass("disablelogo");
                dicoverLogo.addClass("disablelogo");
            }
            function selectedDiscover() {
                visaLogo.addClass("disablelogo");
                masterLogo.addClass("disablelogo");
                dicoverLogo.removeClass("disablelogo");
            }
            function selectedNone() {
                visaLogo.addClass("disablelogo");
                masterLogo.addClass("disablelogo");
                dicoverLogo.addClass("disablelogo");
            }

            //for auto complete textboxes
            if (navigator.userAgent.search("Chrome") >= 0) {
                $("[id$='_cardNumberTextBox']").attr('x-autocomplete', 'cc-number');
                $("[id$='_nameOnCardTextBox']").attr('x-autocomplete', 'cc-name');
            }
        });
     </script>
</telerik:RadScriptBlock>
