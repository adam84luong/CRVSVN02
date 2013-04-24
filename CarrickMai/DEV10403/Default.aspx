<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WebForm1.aspx.cs" Inherits="Demo.WebForm1" %>

<%@ Register Assembly="Telerik.Web.UI" Namespace="Telerik.Web.UI" TagPrefix="telerik" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <telerik:RadScriptManager ID="RadScriptManager1" runat="server">
            <Scripts>
                <asp:ScriptReference Assembly="Telerik.Web.UI" Name="Telerik.Web.UI.Common.Core.js">
                </asp:ScriptReference>
                <asp:ScriptReference Assembly="Telerik.Web.UI" Name="Telerik.Web.UI.Common.jQuery.js">
                </asp:ScriptReference>
                <asp:ScriptReference Assembly="Telerik.Web.UI" Name="Telerik.Web.UI.Common.jQueryInclude.js">
                </asp:ScriptReference>
            </Scripts>
        </telerik:RadScriptManager>
        <telerik:RadAjaxManager ID="RadAjaxManager1" runat="server">
        </telerik:RadAjaxManager>
    <div>
        <telerik:RadComboBox ID="RadComboBox1" runat="server"  style="white-space:normal;width:460px;">
             <Items>
                <telerik:RadComboBoxItem Text="" Value="EmptyItem" />
             </Items>          
            <FooterTemplate>
              <asp:Label ID="Label1" runat="server" Text="DateRange"></asp:Label>
                <br />
                <asp:Label ID="Label2" runat="server" Text="From"></asp:Label>
                <telerik:RadDatePicker ID="RadDatePicker1" Runat="server">
                </telerik:RadDatePicker>
                <asp:Label ID="Label3" runat="server" Text="To"></asp:Label>
                <telerik:RadDatePicker ID="RadDatePicker2" Runat="server">
                </telerik:RadDatePicker>
                 <telerik:RadButton ID="RadButton2" OnClick="RadButton2_Click" runat="server" Text="GO">
                 </telerik:RadButton>
            </FooterTemplate>
        </telerik:RadComboBox>
    </div>
        <asp:Label ID="Label4" runat="server" Text="Startdate"></asp:Label>
        <br />
        <asp:Label ID="Label5" runat="server" Text="Enddate"></asp:Label>
    </form>
</body>
</html>
