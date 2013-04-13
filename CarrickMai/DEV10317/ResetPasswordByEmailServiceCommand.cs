using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Authentication.Contracts.Authentication;
using Common.Contracts.Authentication.Responses;
using Common.Service.Commands;
using Payjr.Core.Adapters;
using Payjr.Core.Providers;
using Payjr.Core.Metrics;
using Common.Contracts.Authentication.Requests;
using Payjr.Entity.EntityClasses;
using Payjr.Core.Users;

namespace Payjr.Core.ServiceCommands.Authentication
{
    public class ResetPasswordByEmailServiceCommand : ProviderServiceCommand<IProviderFactory, MetricRecorder, LocalRequest, AuthServiceResponse>
    {
        private string _email;
        

        public ResetPasswordByEmailServiceCommand(IProviderFactory providerFactory)
            : base(providerFactory)
        {
        }

        protected override void Validate(LocalRequest request)
        {
            if (request == null)
            {
                Log.Error("ResetPasswordByEmailServiceCommand can not process with request is null");
                throw new ArgumentNullException("request");
            }
            if (request.Configuration == null)
            {
                Log.Error("ResetPasswordByEmailServiceCommand can not process with request.Configuration is null");
                throw new ArgumentException("request.Configuration must be set", "request");
            }
            if (!request.Configuration.ApplicationKey.HasValue)
            {
                Log.Error("ResetPasswordByEmailServiceCommand can not process with request.Configuration.ApplicationKey has not value");
                throw new ArgumentException("request.Configuration.ApplicationKey must be set", "request");
            }

            _email = request.Email;            
        }

        protected override bool OnExecute(AuthServiceResponse response)
        {
            List<User> userinfo = User.SearchUsersByEmailAddress(_email);
            try
            {                                         
                if (userinfo.Count == 1)
                {
                    if (userinfo[0].ResetPassword(null) && userinfo[0].Save(null))
                    { 
                            response.Status.IsSuccessful = true;
                            response.Result = Result.Success;
                            response.Data = userinfo[0].NewPassword;
                            response.Status.ErrorMessage = string.Empty;
                            return true;
                    }                    
                 }
                 else
                 {
                      response.Status.ErrorMessage = String.Format("If you do not have an email account setup please contact your parent or customer service for help resetting your password");
                 }            

                Log.Info("Failure when trying to reset password by email:" + _email);                
                response.Status.ErrorMessage = String.Format("ResetPasswordByEmail is failure");                
                response.Status.IsSuccessful = false;
                response.Result = Result.Error;
                return false;
            }
            catch (Exception ex)
            {
                Log.ErrorException("Error when trying to reset password by email:" + _email, ex);
                response.Status.ErrorMessage = String.Format("Error when trying to reset password by email: email address: {0}, error message: {1}", _email, ex.Message);
                response.Status.IsSuccessful = false;
                response.Result = Result.Error;
                return false;
            }            
        }
    }
}
