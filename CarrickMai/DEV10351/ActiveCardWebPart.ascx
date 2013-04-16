<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="ActiveCardWebPart.ascx.cs" Inherits="CMSApp.CMSWebParts.CardLab.Buxx.Account.ActiveCardWebPart" %>
<%@ Register Src="~/Controls/MessageBox.ascx" TagName="MessageBox" TagPrefix="uc" %>

<div class="active-card">
    <uc:MessageBox ID="_ucMsgBox" runat="server" CssClass="system_messages" />
    <asp:ValidationSummary ID="_validationSummary" runat="server" CssClass="validationmessage" ValidationGroup="ActivateCard" HeaderText="There are errors on this form. Please correct them." />

    <div class="card-image">
        <img src="~/Themes/Buxx/AccountPage/images/card.png" alt="Visa" width="207" height="132" />
    </div>
    <div class="form-active">
        <span class="active-card-title">Activate card for <span id="_teenName" runat="server"></span>
        </span>
        <p class="active-card-desc">
            Before the card can used it must be activated.
        </p>
        <ul class="form-input">
            <li>
                <div class="elementlabel">
                    Full Card Number
                <asp:RequiredFieldValidator ID="_cardNumberRequestValidator" runat="server" ControlToValidate="_cardNumberTextBox" ValidationGroup="ActivateCard"
                    Display="dynamic" SetFocusOnError="True"><br/> Please enter card number
                </asp:RequiredFieldValidator>
                    <asp:RegularExpressionValidator ID="_cardNumberExpressionValidator" runat="server" Display="Dynamic" ControlToValidate="_cardNumberTextBox" ValidationGroup="ActivateCard"
                        ValidationExpression="^(\d{4}[- ]){3}\d{4}|\d{16}$" ForeColor="Red" SetFocusOnError="True"> 
                        <br/>Full card number is invalid                    
                    </asp:RegularExpressionValidator>
                </div>
                <div class="elementinput">
                    <asp:RequiredFieldValidator ID="_cardNumberIconRequestValidator" runat="server" ControlToValidate="_cardNumberTextBox" ValidationGroup="ActivateCard"
                        Display="dynamic" SetFocusOnError="True" CssClass="invalid-style"><span class="validator-arrow-icon">&#xe001;</span>
                    </asp:RequiredFieldValidator>
                    <asp:RegularExpressionValidator ID="_cardNumberIconExpressionValidator" runat="server" CssClass="invalid-style" ControlToValidate="_cardNumberTextBox" ValidationGroup="ActivateCard"
                        Display="dynamic" SetFocusOnError="True" ValidationExpression="^(\d{4}[- ]){3}\d{4}|\d{16}$"><span class="validator-arrow-icon">&#xe001;</span>
                    </asp:RegularExpressionValidator>
                    <telerik:RadTextBox runat="server" ID="_cardNumberTextBox" Width="228" MaxLength="16" Skin="GrayBuxx" EnableEmbeddedSkins="false">
                    </telerik:RadTextBox>
                </div>
            </li>
            <li>
                <div class="elementlabel">
                    Your Birth Date
                <asp:RequiredFieldValidator ID="_birthDateRequestValidator" runat="server" ControlToValidate="_birthDatePicker" ValidationGroup="ActivateCard"
                    Display="dynamic" SetFocusOnError="True"><br/>  Your birth date is required
                </asp:RequiredFieldValidator>
                </div>
                <div class="elementinput">
                    <asp:RequiredFieldValidator ID="_birthDateIconRequestValidator" runat="server" CssClass="invalid-style" ControlToValidate="_birthDatePicker" ValidationGroup="ActivateCard"
                        Display="dynamic" SetFocusOnError="True"><span class="validator-arrow-icon">&#xe001;</span>
                    </asp:RequiredFieldValidator>
                    <telerik:RadDatePicker ID="_birthDatePicker" runat="server" Skin="GrayBuxx" EnableEmbeddedSkins="False" Width="130px">
                        <DateInput EmptyMessage="__/__/____"></DateInput>
                    </telerik:RadDatePicker>
                </div>
            </li>
            <li>
                <div class="elementcommand">
                    <telerik:RadButton ID="_activateCardButton" runat="server" Text="Activate"
                        EnableEmbeddedSkins="False" Skin="OrangeBuxx" ValidationGroup="ActivateCard">
                    </telerik:RadButton>
                </div>
            </li>
        </ul>
    </div>
</div>






