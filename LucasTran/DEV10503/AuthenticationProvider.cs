using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.Security;
using Authentication.Contracts.Authentication.Requests;
using Common.Contracts.Authentication.Records;
using Common.Contracts.Authentication.Requests;
using Common.Service.Providers;
using Common.Contracts.Shared.Records;
using Authentication.Contracts.Authentication;
using CardLab.CMS.Metrics;

namespace CardLab.CMS.Providers.Authentication
{
	public class AuthenticationProvider : ServiceProviderBase<IAuthentication, IProviderFactory, MetricRecorder>, IAuthenticationProvider
    {
		public AuthenticationProvider(IProviderFactory factory, string endpointName)
			: base(factory, endpointName)
        {
            
        }

		public AuthenticationProvider(IProviderFactory factory)
			: base(factory)
        {
        }

		public AuthenticationProvider(IProviderFactory factory, IAuthentication service)
			: base(factory, service)
        {
        }

		public List<UserRecord> SearchUsers(UserSearchParameters parameters, out int totalRecords)
		{
			var request = new FindUsersRequest();
			if (parameters.IsApplicationKeySet)
			{
				request.ApplicationKey = parameters.ApplicationKey;
			}
			if (parameters.IsEmailSet)
			{
				request.Email = parameters.Email;
			}
			if (parameters.IsNameSet)
			{
				request.Name = parameters.Name;
			}
			if (parameters.IsUserIdentifierSet)
			{
				request.UserIdentifier = parameters.UserIdentifier;
			}

			request.Paging = new PagingRequestRecord();
			request.Paging.PageIndex = parameters.PageNumber;
			request.Paging.PageSize = parameters.PageSize;

            var response = (CallService(() => new GenericResponse<UserCollectionServiceResponse>(CreateInstance().FindUsers(request)))).PayLoad;
			totalRecords = response.TotalRecords;
			switch (response.Result)
			{
				case Result.Success:
					return response.MatchingUsers.ToList().ConvertAll(record => new UserRecord(record));
				case Result.Error:
					Log.Error("AuthenticationProvider SearchUsers error - " + response.Message);
					break;
				case Result.Unknown:
					Log.Error("AuthenticationProvider SearchUsers unknown error - " + response.Message);
					break;
				case Result.InvalidRegistration:
					Log.Warn("AuthenticationProvider SearchUsers warning - " + response.Message);
					break;
			}

			return new List<UserRecord>();
		}

		public UserRecord RetrieveUser(Guid applicationKey, string userIdentifier)
		{
            var response = CallService(() => CreateInstance().GetUser3(applicationKey, userIdentifier));
			switch (response.Result)
			{
				case Result.Success:
					return new UserRecord(response.UserRecord);
				case Result.Error:
					Log.Error("AuthenticationProvider RetrieveUser error - " + response.Message);
					break;
				case Result.Unknown:
					Log.Error("AuthenticationProvider RetrieveUser unknown error - " + response.Message);
					break;
				case Result.InvalidRegistration:
					Log.Warn("AuthenticationProvider RetrieveUser warning - " + response.Message);
					break;
			}

			return null;
		}

        public bool ValidateUser(Guid applicationKey, string username, string password, string ipAddress, string userAgent, out string userIdentifier)
        {
            var request = new ValidateUserRequest();
            request.Configuration = new RetrievalConfigurationRecord();
            request.Configuration.ApplicationKey = applicationKey;
            request.Configuration.BrandingKey = null;
            request.Configuration.TenantKey = null;
            request.Configuration.SystemConfiguration = ProviderFactory.SystemConfiguration.
                    GetSystemConfigurationKey(applicationKey);

            request.UserName = username;
            request.Password = password;
            request.IpAddress = ipAddress;
            request.UserAgent = userAgent;

            request.Header = new RequestHeaderRecord()
                                 {
                                     CallerName = "ValidateUser"
                                 };

            userIdentifier = string.Empty;
            AuthServiceResponse response;
            try
            {
                response = CallService(() => CreateInstance().ValidateUser(applicationKey, request));
            }
            catch (Exception ex)
            {
                Log.ErrorException("An error occurred while processing ValidateUser", ex);
                return false;
            }
            
            if (response.Status.IsSuccessful)
            {
                userIdentifier = (string) response.Data;
                return true;
            }
            Log.Error(String.Format("Failure when trying to ValidateUser {0}. Error: {1}", username, response.Status.ErrorMessage));
            return false;
        }        

        public bool ResetPasswordByEmail(Guid applicationKey, string email, out string newpassword)
        {
            newpassword = String.Empty;
            AuthServiceResponse response;
            try
            {
                response = CallService(() => CreateInstance().ResetPasswordByEmail(applicationKey, email));
            }
            catch (Exception ex)
            {
                Log.ErrorException("An error occurred while resetting password by email", ex);
                return false;
            }
            
            if (response.Status.IsSuccessful)
            {
                newpassword = response.Data.ToString();
                return true;
            }
            Log.Error(String.Format("Failure when trying to ResetPasswordByEmail from email {0}. Error: {1}", email, response.Status.ErrorMessage));
            newpassword = null;
            return false;
        }

        public bool ChangePassword(Guid applicationKey, string username, string oldPassword, string newPassword)
        {
            AuthServiceResponse respone;
            try
            {
                respone = CallService(() => CreateInstance().ChangePassword(applicationKey, username, oldPassword, newPassword));
            }
            catch (Exception ex)
            {
                Log.ErrorException("An error occurred while changing password", ex);
                return false;
            }
            
            if (respone.Status.IsSuccessful)
            {
                return true;
            }
            Log.Error(String.Format("Failure when trying to ChangePassword with username:{0}. Error: {1}", username, respone.Status.ErrorMessage));
            return false;
        }

        public UserRecord CreateUser(Guid applicationKey, string firstname, string lastname, string password, string email, out MembershipCreateStatus status)
        {
            CreateUserResponse response;
            try
            {
               response = CallService(() => CreateInstance().CreateUser2(applicationKey, Guid.Empty, firstname, lastname, email, password,
                                                email, string.Empty, string.Empty, true, string.Empty));
            }
            catch (Exception ex)
            {
                Log.ErrorException("An error occurred while creating an user", ex);
                status = MembershipCreateStatus.ProviderError;
                return null;
            }
            if (response.Status.IsSuccessful)
            {
                status = response.CreateStatus;
                MembershipUserRecord user = response.User;
                return new UserRecord(user);
            }
            Log.Error(String.Format("Failure when trying to CreateUser email {0}. Error: {1}", email, response.Status.ErrorMessage));
            status = response.CreateStatus;
            return null;
	    }

        public UserRecord CreateUser(Guid applicationKey, string accountGroupIdentifier, string userName, string firstName, string lastname, string password, string email, DateTime? birthday, string ssn, List<string> roles, AddressRecord billingAddress, AddressRecord shippingAddress, bool isActive, out MembershipCreateStatus status)
        {
            status = MembershipCreateStatus.ProviderError;
            var request = new CreateUserWithRolesRequest();
            request.Configuration = new ConfigurationRecord();
            request.Configuration.ApplicationKey = applicationKey;
            request.Configuration.BrandingKey = Guid.Empty;
            try
            {
                request.Configuration.SystemConfiguration = ProviderFactory.SystemConfiguration.GetSystemConfigurationKey(applicationKey);
            }
            catch (Exception ex)
            {
                Log.ErrorException(String.Format("An error occured when doing GetSystemConfigurationKey with ApplicationKey {0}", applicationKey), ex);
                return null;
            }
            
            request.Header = new RequestHeaderRecord() {CallerName = "CreateUserWithRole"};

            var userRequest = new CreateUserWithRolesRecord();
            userRequest.BillingAddress = billingAddress;
            userRequest.Email = email;
            userRequest.FirstName = firstName;
            userRequest.LastName = lastname;
            userRequest.Password = password;
            userRequest.Roles = roles;
            userRequest.SSN = ssn;
            userRequest.ShippingAddress = shippingAddress;
            userRequest.UserName = userName;
            userRequest.AccountGroupIdentifier = accountGroupIdentifier;
            userRequest.DateOfBirth = birthday;
            request.User = userRequest;
            request.User.IsApproved = isActive;
            
            CreateUserResponse response;
            try
            {
                response = CallService(() => CreateInstance().CreateUserWithRoles(request));
            }
            catch (Exception ex)
            {
                Log.ErrorException(String.Format("An error occurred while creating an user {0}", email), ex);
                return null;
            }
            if (response.Status.IsSuccessful)
            {
                status = response.CreateStatus;
                MembershipUserRecord user = response.User;
                return new UserRecord(user);
            }
            Log.Error(String.Format("An error occurred while creating an user {0}. Error: {1}", email, response.Status.ErrorMessage));
            status = response.CreateStatus;
            return null;
        }

        public bool DeleteUser(Guid applicationKey, string username, bool deleteAllRelatedData)
        {
            AuthServiceResponse response;

            try
            {
                response = CallService(() => CreateInstance().DeleteUser(applicationKey, username, deleteAllRelatedData));
            }
            catch (Exception ex)
            {
                Log.ErrorException(String.Format("An error occurred while delete user with Username: {0}.",
                                        username), ex);
                return false;
            }

            if (!response.Status.IsSuccessful)
            {
                Log.Debug("Delete User with Username: {0} unsuccessful. The error: {1}",
                                        username, response.Status.ErrorMessage);
                return false;
            }

            if (response.Result != Result.Success)
            {
                Log.Debug("Delete User with Username: {0} unsuccessful. {1} : {2}",
                                        username, response.Result, response.Message);
                return false;
            }

            return true;
        }
    }
}