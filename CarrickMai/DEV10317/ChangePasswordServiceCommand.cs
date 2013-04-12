using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Authentication.Contracts.Authentication;
using Common.Contracts.Authentication.Requests;
using Common.Contracts.Authentication.Responses;
using Common.Service.Commands;
using Payjr.Core.Adapters;
using Payjr.Core.Identifiers;
using Payjr.Core.Metrics;
using Payjr.Core.Providers;
using Payjr.Core.Users;
using Payjr.Entity.EntityClasses;
using Payjr.Core.Services;
using Payjr.Core.UserInfo;

namespace Payjr.Core.ServiceCommands.Authentication
{
    public class ChangePasswordServiceCommand : ProviderServiceCommand<IProviderFactory, MetricRecorder, LocalRequest, AuthServiceResponse>
    {
        private string _userName;
        private string _oldPassword;
        private string _newPassword;
        private UserEntity _userEntity;
        bool _sendMessage;
        private NotificationService _notificationService;
        string _subject = "Change Password";

        public ChangePasswordServiceCommand(IProviderFactory providerFactory)
            : base(providerFactory)
        {

        }
        protected override void Validate(LocalRequest request)
        {
            if (request == null)
            {
                Log.Error("ChangePasswordServiceCommand can not process with request is null");
                throw new ArgumentNullException("request");
            }
            if (request.Configuration == null)
            {
                Log.Error("ChangePasswordServiceCommand can not process with request.Configuration is null");
                throw new ArgumentException("request.Configuration must be set", "request");
            }
            if (!request.Configuration.ApplicationKey.HasValue)
            {
                Log.Error("ChangePasswordServiceCommand can not process with request.Configuration.ApplicationKey have not value");
                throw new ArgumentException("request.Configuration.ApplicationKey must be set", "request");
            }
            if (string.IsNullOrEmpty(request.UserName))
            {
                Log.Error("ChangePasswordServiceCommand can not process with request.UserName is null or empty");
                throw new ArgumentException("request.UserName must be set", "request");
            }
            _userName = request.UserName;
            if (string.IsNullOrEmpty(request.OldPassword))
            {
                Log.Error("ChangePasswordServiceCommand can not process with request.OldPassword is null or empty");
                throw new ArgumentException("Old password must be set", "request");
            }
            _oldPassword = request.OldPassword;
            if (string.IsNullOrEmpty(request.NewPassword))
            {
                Log.Error("ChangePasswordServiceCommand can not process with request.NewPassword is null or empty");
                throw new ArgumentException("New password must be set", "request");
            }
            _newPassword = request.NewPassword;
            if (!IsValidUserName(request.UserName, out _userEntity))
            {
                Log.Error("ChangePasswordServiceCommand can not process with a unvalid user");
                throw new ArgumentException(string.Format("User name is invalid, user name: {0}", request.UserName), "request");
            }
            if (!IsValidPassword(_userEntity))
            {
                Log.Error("ChangePasswordServiceCommand can not process with a unvalid password");
                throw new ArgumentException(string.Format("Password is invalid, user name: {0}", request.UserName), "request");
            }
           
        }
        protected override bool OnExecute(AuthServiceResponse response)
        {                     
            try
            {
                _userEntity.Password = AdapterFactory.UserAdapter.CreateSecurePassword(_userEntity.PasswordSalt,
                                                                                        _newPassword);
              
                _sendMessage = _notificationService.SendMessage(_userEntity.BrandingId, _userEntity.Emails, _subject, _userEntity.Password);
                _userEntity.LastPasswordChangedDate = DateTime.UtcNow;
                AdapterFactory.UserAdapter.UpdateUser(_userEntity);
            }
            catch (Exception ex)
            {
                Log.ErrorException(string.Format("Error when trying to change password: user name: {0}", _userName), ex);
                response.Status.ErrorMessage = String.Format("Error when trying to reset password by email: email address: {0}, error message: {1}", "", ex.Message);
                response.Status.IsSuccessful = false;
                response.Result = Result.Error;
                return false;
            }
            response.Result = Result.Success;
            return true;
        }

        private bool IsValidUserName(string userName, out UserEntity result)
        {
            UserEntity userEntity =
                    AdapterFactory.UserAdapter.RetrieveByUserName(userName);
            result = userEntity;
            return userEntity != null;
        }

        private bool IsValidPassword(UserEntity userEntity)
        {
            return AdapterFactory.UserAdapter.CreateSecurePassword(userEntity.PasswordSalt, _oldPassword) == userEntity.Password;
        }
    }
}
