using System;
using System.Web.Security;
using Authentication.Contracts.Authentication;
using Authentication.Contracts.Authentication.Requests;
using CardLab.CMS.Providers;
using Common.Contracts.Authentication.Requests;
using Common.Contracts.Shared.Records;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using CardLab.CMS.Providers.Authentication;
using System.Collections.Generic;

namespace CardLab.CMS.Test.Providers.Authentication
{
   
    [TestClass]
    public class AuthenticationProviderTest : TestBase
    {
        #region Additional test attributes

        [TestInitialize]
        public override void MyTestInitialize()
        {
            base.MyTestInitialize();
        }

        #endregion


        #region ChangePassword 

        [TestMethod]
        public void ChangePasswordSuccess()
        {
            var response = new AuthServiceResponse()
            {
                Status = new ResponseStatusRecord()
                {
                    IsSuccessful = true
                }
            };

            AuthenticationMock.Setup(
              mock =>
              mock.ChangePassword(It.IsAny<Guid>(), "username", "oldpassword",
                                  "newpassword")).Returns(response);

            var target = new AuthenticationProvider(ProviderFactory, AuthenticationMock.Object);

            bool result = target.ChangePassword(It.IsAny<Guid>(), "username", "oldpassword",
                                  "newpassword");
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void ChangePasswordFailed()
        {
            var response = new AuthServiceResponse()
            {
                Status = new ResponseStatusRecord()
                {
                    IsSuccessful = false
                }
            };

            AuthenticationMock.Setup(
              mock =>
              mock.ChangePassword(It.IsAny<Guid>(), "username", "oldpassword",
                                  "newpassword")).Returns(response);

            var target = new AuthenticationProvider(ProviderFactory, AuthenticationMock.Object);

            bool result = target.ChangePassword(It.IsAny<Guid>(), "username", "oldpassword",
                                  "newpassword");
            Assert.IsFalse(result);
        }

        #endregion


        #region ResetPasswordByEmail

        [TestMethod]
        public void ResetPasswordByEmailSuccess()
        {
            var response = new AuthServiceResponse()
	  	    {
	  	        Status = new ResponseStatusRecord()
	  	        {
	  	            IsSuccessful = true
	  	        },
                Data = "newpassword"
	  	    }; 
            Guid applicationKey = Guid.NewGuid();
	  	    string newpassword;
            AuthenticationMock.Setup(mock =>
	  	                mock.ResetPasswordByEmail(applicationKey, "email@email.com")).Returns(response);
	  	
	  	    var target = new AuthenticationProvider(ProviderFactory, AuthenticationMock.Object);
	  	
	  	    bool result = target.ResetPasswordByEmail(applicationKey, "email@email.com", out newpassword);
	  	
	  	    Assert.IsTrue(result);
            Assert.IsNotNull(newpassword);
        }

        [TestMethod]
        public void ResetPasswordByEmailFailed()
        {
            var response = new AuthServiceResponse()
	  	    {
	  	        Status = new ResponseStatusRecord()
	  	        {
	  	            IsSuccessful = false
	  	        }
	  	    };
            Guid applicationKey = Guid.NewGuid();
	        string newpassword;
	
	        AuthenticationMock.Setup(mock =>
	            mock.ResetPasswordByEmail(applicationKey, "email@email.com")).Returns(response);

            var target = new AuthenticationProvider(ProviderFactory, AuthenticationMock.Object);
	
	        bool result = target.ResetPasswordByEmail(applicationKey, "email@email.com", out newpassword);
	
	        Assert.IsFalse(result);    
            Assert.IsNull(newpassword);
	    }

        #endregion


        #region ValidateUser Testing
	
	    [TestMethod()]
	    public void ValidateUserSuccessful()
	    {
	        var response = new AuthServiceResponse()
	        {
	            Status = new ResponseStatusRecord()
	                            {
	                                IsSuccessful = true
	                            },
                Data = "TestUser"
	        };
	
	        AuthenticationMock.Setup(
	            mock => mock.ValidateUser(It.IsAny<Guid>(),
	                It.IsAny<ValidateUserRequest>())).Returns(response);
	
	        var target = new AuthenticationProvider(ProviderFactory, AuthenticationMock.Object);
	        string userIdentifier;
	        bool result = target.ValidateUser(new Guid(), "username", "password", "1.1.1.1", "userAgent", out userIdentifier);
	        Assert.IsTrue(result);
            Assert.IsTrue(userIdentifier.Equals("TestUser"));
	    }

        [TestMethod()]
        public void ValidateUserNotSuccessful()
        {
            var response = new AuthServiceResponse()
            {
                Status = new ResponseStatusRecord()
                {
                    IsSuccessful = false
                }
            };

            AuthenticationMock.Setup(
                mock => mock.ValidateUser(It.IsAny<Guid>(),
                    It.IsAny<ValidateUserRequest>())).Returns(response);

            var target = new AuthenticationProvider(ProviderFactory, AuthenticationMock.Object);
            string userIdentifier;
            bool result = target.ValidateUser(new Guid(), "username", "password", "1.1.1.1", "userAgent", out userIdentifier);
            Assert.IsFalse(result);
            Assert.IsTrue(string.IsNullOrEmpty(userIdentifier));
        }

	    #endregion


        #region CreateUser
        [TestMethod]
        public void CreateUserSuccess()
        {
            var membershipUser = new MembershipUserRecord
                             {
                                 UserIdentifier = "UserIdentifier",
                                 CreationDate = DateTime.Now,
                                 EmailAddress = "test@cardlabcorp.com",
                                 FirstName = "First",
                                 LastName = "Last",
                                 ApplicationKeys = new Guid[] {}
                             };            

            var response = new CreateUserResponse()
            {
                Status = new ResponseStatusRecord()
                {
                    IsSuccessful = true
                },
                CreateStatus = MembershipCreateStatus.Success,
                User = membershipUser
            };

            Guid applicationKey = Guid.NewGuid();
            MembershipCreateStatus status;
            AuthenticationMock.Setup(mock =>
                        mock.CreateUser2(applicationKey, Guid.Empty, "first name", "last name", "test@test.com", "pass", "test@test.com", string.Empty,
                        string.Empty, true, string.Empty)).Returns(response);

            var target = new AuthenticationProvider(ProviderFactory, AuthenticationMock.Object);

            UserRecord result = target.CreateUser(applicationKey, "first name", "last name", "pass", "test@test.com", out status);

            Assert.IsNotNull(result);
            Assert.AreEqual(MembershipCreateStatus.Success, status);
        }

        [TestMethod]
        public void CreateUserFailed()
        {            
            var response = new CreateUserResponse()
            {
                Status = new ResponseStatusRecord()
                {
                    IsSuccessful = false
                },
                CreateStatus = MembershipCreateStatus.DuplicateUserName,
                User = null
            };

            Guid applicationKey = Guid.NewGuid();
            MembershipCreateStatus status;
            AuthenticationMock.Setup(mock =>
                        mock.CreateUser2(applicationKey, Guid.Empty, "first name", "last name", "test@test.com", "pass", "test@test.com", string.Empty,
                        string.Empty, true, string.Empty)).Returns(response);

            var target = new AuthenticationProvider(ProviderFactory, AuthenticationMock.Object);

            UserRecord result = target.CreateUser(applicationKey, "first name", "last name", "pass", "test@test.com", out status);

            Assert.IsNull(result);
            Assert.IsFalse(response.Status.IsSuccessful);
        }
        #endregion


        #region Create User With Roles

        [TestMethod]
        public void CreateUserWithRolesSuccess()
        {
            var membershipUser = new MembershipUserRecord
            {
                UserIdentifier = "UserIdentifier",
                CreationDate = DateTime.Now,
                EmailAddress = "test@cardlabcorp.com",
                FirstName = "First",
                LastName = "Last",
                ApplicationKeys = new Guid[] { }
            };

            var response = new CreateUserResponse()
            {
                Status = new ResponseStatusRecord()
                {
                    IsSuccessful = true
                },
                CreateStatus = MembershipCreateStatus.Success,
                User = membershipUser
            };

            Guid applicationKey = Guid.NewGuid();
            MembershipCreateStatus status;

            AuthenticationMock.Setup(mock =>
                        mock.CreateUserWithRoles(It.IsAny<CreateUserWithRolesRequest>())).Returns(response);

            var target = new AuthenticationProvider(ProviderFactory, AuthenticationMock.Object);

            UserRecord result = target.CreateUser(applicationKey, "accountGroupIdentifier", "username", "firstname", "lastname", "password", "test@test.com", DateTime.UtcNow, "ssn",  
               new List<string>(), new AddressRecord(), new AddressRecord(), true, out status);

            Assert.IsNotNull(result);
            Assert.AreEqual(MembershipCreateStatus.Success, status);
            Assert.AreEqual("test@cardlabcorp.com", result.EmailAddress);
            Assert.AreEqual("UserIdentifier", result.UserIdentifier);
            Assert.AreEqual("First", result.FirstName);
            Assert.AreEqual("Last", result.LastName);
        }

        [TestMethod]
        public void CreateUserWithRolesFails()
        {
            var response = new CreateUserResponse()
            {
                Status = new ResponseStatusRecord()
                {
                    IsSuccessful = false
                },
                CreateStatus = MembershipCreateStatus.Success,
                User = null
            };

            Guid applicationKey = Guid.NewGuid();
            MembershipCreateStatus status;

            AuthenticationMock.Setup(mock =>
                        mock.CreateUserWithRoles(It.IsAny<CreateUserWithRolesRequest>())).Returns(response);

            var target = new AuthenticationProvider(ProviderFactory, AuthenticationMock.Object);

            UserRecord result = target.CreateUser(applicationKey, "accountGroupIdentifier", "username", "firstname", "lastname", "password", "test@test.com", DateTime.UtcNow, "ssn",
               new List<string> { "role1", "role2" }, new AddressRecord(), new AddressRecord(), true, out status);

            Assert.IsNull(result);
        }

        #endregion

        #region Delete User

        [TestMethod]
        public void DeleteUserSuccess()
        {
            var response = new AuthServiceResponse()
            {
                Status = new ResponseStatusRecord()
                {
                    IsSuccessful = true
                },
                Result = Result.Success
            };
            
            AuthenticationMock.Setup(mock =>
                        mock.DeleteUser(It.IsAny<Guid>(),It.IsAny<string>(),It.IsAny<bool>())).Returns(response);

            var target = new AuthenticationProvider(ProviderFactory, AuthenticationMock.Object);

            var result = target.DeleteUser(new Guid(),"username",true);

            Assert.AreEqual(result, true);
        }

        [TestMethod]
        public void DeleteUser_IsSuccessful_fail()
        {
            var response = new AuthServiceResponse()
            {
                Status = new ResponseStatusRecord()
                {
                    IsSuccessful = false
                },
                Result = Result.Success
            };

            AuthenticationMock.Setup(mock =>
                        mock.DeleteUser(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(response);

            var target = new AuthenticationProvider(ProviderFactory, AuthenticationMock.Object);

            var result = target.DeleteUser(new Guid(), "username", true);

            Assert.AreEqual(result, false);
        }

        [TestMethod]
        public void DeleteUser_Result_fail()
        {
            Random random = new Random();

            var response = new AuthServiceResponse()
            {
                Status = new ResponseStatusRecord()
                {
                    IsSuccessful = false
                },

                Result = GetRandomResult()
            };

            AuthenticationMock.Setup(mock =>
                        mock.DeleteUser(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>())).Returns(response);

            var target = new AuthenticationProvider(ProviderFactory, AuthenticationMock.Object);

            var result = target.DeleteUser(new Guid(), "username", true);

            Assert.AreEqual(result, false);
        }

        #region helper

        private Result GetRandomResult()
        {
            Result result = new Result();
            Random random = new Random();
          
            switch (random.Next(3))
            {
                case 0:
                    result = Result.Error;
                    break;
                case 1:
                    result = Result.InvalidRegistration;
                    break;
                case 2:
                    result = Result.Unknown;
                    break;
            }
            return result;
        }

        #endregion

        #endregion
    }
}
