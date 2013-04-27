using Authentication.Contracts.Authentication;
using Common.Exceptions;
using Common.Service.Commands;
using Payjr.Core.Metrics;
using Payjr.Core.Providers;
using Payjr.Core.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payjr.Core.ServiceCommands.Authentication
{
    public class DeleteUserServiceCommand : ProviderServiceCommand<IProviderFactory, MetricRecorder, LocalRequest, AuthServiceResponse>
    {
        private string _userName;
        private bool _isdeleteAllRelatedData;
        private User _user;

        public DeleteUserServiceCommand(IProviderFactory providerFactory)
            : base(providerFactory)
        {
        }

        protected override bool OnExecute(AuthServiceResponse response)
        {
            response.Result = Result.Error;
            try
            {
                if (_user.RoleType == Entity.RoleType.RegisteredTeen)
                {
                    Teen teen = _user as Teen;
                    if (teen != null)
                    {
                        Parent parent = teen.Parent;
                        if (parent != null)
                        {
                            teen.CancelService(parent.UserID);
                            response.Result = Result.Success;
                            response.Data = _userName;
                        }
                    }
                }
                else if (_user.RoleType == Entity.RoleType.Parent)
                {
                    Family family = new Family(_user);
                    family.Delete();
                    response.Result = Result.Success;
                    response.Data = _userName;
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Debug("Occur error while processing delete user:" + ex.Message);
                throw new CardLabException (ex.Message);
            }
        }

        protected override void Validate(LocalRequest request)
        {
            if (request == null)
            {
                Log.Error("Delete User can not process with request is null");
                throw new ArgumentNullException("request");
            }
            if (request.Configuration == null)
            {
                Log.Error("Delete User can not process with request.Configuration is null");
                throw new ArgumentException("request.Configuration must be set", "request");
            }
            if (!request.Configuration.ApplicationKey.HasValue)
            {
                Log.Error("Delete User can not process with request.Configuration.ApplicationKey has not value");
                throw new ArgumentException("request.Configuration.ApplicationKey must be set", "request");
            }
            _userName = request.UserName;
            if (string.IsNullOrWhiteSpace(_userName))
            {
                Log.Error("Delete User can not process with request.UserName has not value");
                throw new ArgumentException("request.UserName must be set", "request");
            }
            List<User> users = User.SearchUsersByUserName(_userName);
            var userCount = users.Count;

            if (userCount == 0 || userCount > 1)
            {
                throw new ValidationException(string.Format("Could not determine exactly an User who has user name:{0}", _userName));
            }
            _user = users[0];
        }

    }
}
