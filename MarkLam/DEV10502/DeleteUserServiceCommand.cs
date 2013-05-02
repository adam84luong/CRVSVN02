using Authentication.Contracts.Authentication;
using Common.Exceptions;
using Common.Service.Commands;
using Payjr.Core.FinancialAccounts;
using Payjr.Core.Identifiers;
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
        private User _user;

        public DeleteUserServiceCommand(IProviderFactory providerFactory)
            : base(providerFactory)
        {
        }

        protected override bool OnExecute(AuthServiceResponse response)
        {
            response.Result = Result.Error;
            switch (_user.RoleType)
            {
                case Entity.RoleType.RegisteredTeen:
                    Teen teen = _user as Teen;
                    if (teen != null)
                    {
                        Parent parent = teen.Parent;
                        if (parent != null)
                        {
                            if (teen.CancelService(parent.UserID))
                            {
                                response.Result = Result.Success;
                                return true;
                            }
                        }
                    }
                    break;
                case Entity.RoleType.Parent:
                    Family family = new Family(_user);
                    // this is only for testing
                    if (Providers.PrepaidCardProvider != null)
                    {
                        foreach (var itemTeen in family.Teens)
                        {
                            foreach (var prepaidAccount in itemTeen.FinancialAccounts.PrepaidCardAccounts)
                            {
                                prepaidAccount.CardProvider = Providers.PrepaidCardProvider;
                            }
                            itemTeen.Site.PrepaidProvider = Providers.PrepaidCardProvider;
                        }
                    }

                    family.Delete();
                    response.Result = Result.Success;
                    break;
                default:
                    throw new InvalidOperationException("Delete User function only process for user that role is parent or teen");
            }
           if (response.Result != Result.Success)
                return false;
            return true;
        }

        protected override void Validate(LocalRequest request)
        {
            if (request == null)
            {
                throw new ValidationException("Delete User can not process with request is null");
            }
            string userIdentifier = request.UserIdentifier;
            if (string.IsNullOrWhiteSpace(userIdentifier))
            {
                throw new ValidationException("Delete User can not process with request.UserIdentifier has not value");
            }
            var userID = new UserIdentifier(userIdentifier).ID;
            _user = User.RetrieveUser(userID);

            if (_user == null)
            {
                throw new ValidationException(string.Format("Could not determine exactly an User who has user identifier:{0}", userIdentifier));
            }
        }

    }
}
