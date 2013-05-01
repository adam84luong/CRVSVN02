using System;
using System.Collections.Generic;
using System.Web.Security;
using Authentication.Contracts.Authentication;
using Common.Contracts.Shared.Records;

namespace CardLab.CMS.Providers.Authentication
{
	public interface IAuthenticationProvider : Common.Service.Providers.IServiceProvider
	{
		List<UserRecord> SearchUsers(UserSearchParameters parameters, out int recordCount);

		UserRecord RetrieveUser(Guid applicationKey, string userIdentifier);

        bool ValidateUser(Guid applicationKey, string username, string password, string ipAddress, string userAgent, out string userIdentifier);

	    bool ResetPasswordByEmail(Guid applicationKey, string email, out string newpassword);

	    bool ChangePassword(Guid applicationKey, string username, string oldPassword, string newPassword);

	    UserRecord CreateUser(Guid applicationKey, string firstname, string lastname, string password, string email, out MembershipCreateStatus status);

	    UserRecord CreateUser(Guid applicationKey, string accountGroupIdentifier, string userName, string firstName,
	                          string lastname, string password, string email, DateTime? dateOfBirth, string ssn,
	                          List<string> roles, AddressRecord billingAddress, AddressRecord shippingAddress,
	                          bool isActive, out MembershipCreateStatus status);
        
       bool DeleteUser(Guid applicationKey, string username, bool deleteAllRelatedData);
	}
}