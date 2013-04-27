using Authentication.Contracts.Authentication;
using Common.Contracts.Shared.Records;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Payjr.Core.Identifiers;
using Payjr.Core.ServiceCommands;
using Payjr.Core.ServiceCommands.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payjr.Core.Test.ServiceCommands.Authentication
{
     [TestClass]
    public class DeleteUserServiceCommandTest : TestBase2
    {
         [TestInitialize]
         public void InitializeTest()
         {
             MyTestInitialize();
         }
         [TestMethod]
         public void ExecuteTestSuccessful_WithRoleTeen()
         {
             var request = CreateLocalRequest(false);
             var target = new DeleteUserServiceCommand(ProviderFactory);
             var result = target.Execute(request);
             Assert.IsTrue(result.Status.IsSuccessful);
             Assert.IsTrue(string.IsNullOrWhiteSpace(result.Status.ErrorMessage));
             Assert.AreEqual(Result.Success, result.Result);
             Assert.AreEqual(new UserIdentifier(_teen.UserName).Identifier, result.Data);
         }

         [TestMethod]
         public void ExecuteTestSuccessful_WithRoleParent()
         {
             var request = CreateLocalRequest(true);
             var target = new DeleteUserServiceCommand(ProviderFactory);
             var result = target.Execute(request);
             Assert.IsTrue(result.Status.IsSuccessful);
             Assert.IsTrue(string.IsNullOrWhiteSpace(result.Status.ErrorMessage));
             Assert.AreEqual(Result.Success, result.Result);
             Assert.AreEqual(new UserIdentifier(_parent.UserName).Identifier, result.Data);
         }

         [TestMethod]
         public void ExecuteTestFail_UserNameEmtpy()
         {
             var request = CreateLocalRequest(true);
             request.UserName = string.Empty;
             var target = new DeleteUserServiceCommand(ProviderFactory);
             var result = target.Execute(request);
             Assert.IsFalse(result.Status.IsSuccessful);
             Assert.IsTrue(result.Status.ErrorMessage.Contains("request.UserName must be set"));
         }
         [TestMethod]
         public void ExecuteTestFail_UserNotExists()
         {
             var request = CreateLocalRequest(true);
             request.UserName = "User Not Exists";
             var target = new DeleteUserServiceCommand(ProviderFactory);
             var result = target.Execute(request);
             Assert.IsFalse(result.Status.IsSuccessful);
             Assert.IsTrue(result.Status.ErrorMessage.Contains(string.Format("Could not determine exactly an User who has user name:{0}",request.UserName)));
         }
        
        #region helper
         private LocalRequest CreateLocalRequest(bool initParentRole)
         {
             var request = new LocalRequest
             {
                 Configuration = new RetrievalConfigurationRecord { ApplicationKey = Guid.NewGuid(),BrandingKey = Guid.NewGuid() },
                 Header =  new RequestHeaderRecord { CallerName = "IAuthentication.DeleteUser"},
                 
                 UserName = (initParentRole == true)? _parent.UserName: _teen.UserName
                 //this is old password user inputed
             };
             return request;
         }
         private LocalRequest CreateLocalRequestWithRoleIsNotParentOrTeen()
         {
             _user.RoleType = Entity.RoleType.Admin;
             var request = new LocalRequest
             {
                 Configuration = new RetrievalConfigurationRecord { ApplicationKey = Guid.NewGuid(), BrandingKey = Guid.NewGuid() },
                 Header = new RequestHeaderRecord { CallerName = "IAuthentication.DeleteUser" },

                 UserName = _user.UserName
             };
             return request;
         }
        #endregion

    }
}
