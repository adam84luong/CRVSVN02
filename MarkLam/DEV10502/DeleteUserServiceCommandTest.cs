using Authentication.Contracts.Authentication;
using Common.Contracts.Shared.Records;
using Common.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Payjr.Core.Identifiers;
using Payjr.Core.Modules;
using Payjr.Core.ServiceCommands;
using Payjr.Core.ServiceCommands.Authentication;
using Payjr.Core.Users;
using Payjr.Util.Test;
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
        private string _prepaidCardIdentifier1;
        private PrepaidModule _prepaidModule;

        [TestInitialize]
        public void InitializeTest()
        {
            MyTestInitialize();
            TestEntityFactory.CreatePrepaidAccount(_teen, true, PrepaidCardStatus.Good, MockCreator.CreateFakeCreditCardNumber());
            TestEntityFactory.CreateCreditCardAccount(_parent, true, Entity.AccountStatus.AllowMoneyMovement, MockCreator.CreateFakeCreditCardNumber());

            var ppAcctId1 = _teen.FinancialAccounts.PrepaidCardAccounts[0].AccountID;
            _prepaidCardIdentifier1 = new PrepaidCardAccountIdentifier(ppAcctId1).DisplayableIdentifier;

            _prepaidModule = TestEntityFactory.CreatePrepaidModule(_branding.BrandingId);
        }
        [TestMethod]
        public void ExecuteTestSuccessful_WithRoleTeen()
        {
            var request = CreateLocalRequest(false);
            //newly created teen have actived status
            var teen = User.RetrieveUser(_teen.UserID) as Teen;
            Assert.IsFalse(teen.IsCancelled);
            var target = new DeleteUserServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.IsTrue(string.IsNullOrWhiteSpace(result.Status.ErrorMessage));
            Assert.AreEqual(Result.Success, result.Result);
            //re-check teen status after deleted
            teen = User.RetrieveUser(_teen.UserID) as Teen;
            Assert.IsTrue(teen.IsCancelled);

        }

        [TestMethod]
        public void ExecuteTestSuccessful_WithRoleParent()
        {
            var request = CreateLocalRequest(true);
            //newly created parent have actived status
            var parent = User.RetrieveUser(_parent.UserID) as Parent;
            Assert.IsTrue(parent.IsActive);
            Assert.IsFalse(parent.MarkedForDeletion);
            Assert.IsFalse(parent.IsLockedOut);
            Assert.AreEqual(1, parent.Teens.Count);

            var target = new DeleteUserServiceCommand(ProviderFactory);
            //init CardProvider
            ProviderFactory.SetupPrepaidCardProvider(_parent.Site, true);
            var result = target.Execute(request);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.IsTrue(string.IsNullOrWhiteSpace(result.Status.ErrorMessage));
            Assert.AreEqual(Result.Success, result.Result);
            //re-check teen status after deleted
            parent = User.RetrieveUser(_parent.UserID) as Parent;
            Assert.IsFalse(parent.IsActive);
            Assert.IsTrue(parent.MarkedForDeletion);
            Assert.IsTrue(parent.IsLockedOut);
            Assert.AreEqual(0, parent.Teens.Count);
        }
        [TestMethod]
        public void ExecuteTestFail_RequestIsNull()
        {
            LocalRequest request = null;
            var target = new DeleteUserServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.IsTrue(result.Status.ErrorMessage.Contains("Delete User can not process with request is null"));
        }
        [TestMethod]
        public void ExecuteTestFail_UserNameEmtpy()
        {
            var request = CreateLocalRequest(true);
            request.UserName = string.Empty;
            var target = new DeleteUserServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.IsTrue(result.Status.ErrorMessage.Contains("Delete User can not process with request.UserName has not value"));
        }
        [TestMethod]
        public void ExecuteTestFail_UserNotExists()
        {
            var request = CreateLocalRequest(true);
            request.UserName = "User Not Exists";
            var target = new DeleteUserServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.IsTrue(result.Status.ErrorMessage.Contains(string.Format("Could not determine exactly an User who has user name:{0}", request.UserName)));
        }

        #region helper
        private LocalRequest CreateLocalRequest(bool initParentRole)
        {
            var request = new LocalRequest
            {
                Configuration = new RetrievalConfigurationRecord { ApplicationKey = Guid.NewGuid(), BrandingKey = Guid.NewGuid() },
                Header = new RequestHeaderRecord { CallerName = "IAuthentication.DeleteUser" },
                UserName = (initParentRole == true) ? _parent.UserName : _teen.UserName
            };
            return request;
        }
       
        #endregion

    }
}
