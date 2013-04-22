<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="WebApplication2._Default" %>

<%@ Register assembly="Telerik.Web.UI" namespace="Telerik.Web.UI" tagprefix="telerik" %>

<asp:Content runat="server" ID="FeaturedContent" ContentPlaceHolderID="FeaturedContent">
  
</asp:Content>
<asp:Content runat="server" ID="BodyContent" ContentPlaceHolderID="MainContent">
    
    <p class = "combobox">
        <div>
            <p>Time period </p>
        </div>
        <div>
            &nbsp;<telerik:RadComboBox ID="RadComboBox1" Runat="server" OnSelectedIndexChanged="RadComboBox1_SelectedIndexChanged" Width ="250px">
                   </telerik:RadComboBox>
         </div>
     </p>
 
</asp:Content>
