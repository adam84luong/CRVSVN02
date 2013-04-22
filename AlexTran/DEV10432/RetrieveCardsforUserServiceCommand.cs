using Common.Business.Validation;
using Common.Contracts.CreditCard.Records;
using Common.Contracts.CreditCard.Requests;
using Common.Contracts.CreditCard.Responses;
using Payjr.Core.FinancialAccounts;
using Payjr.Core.Identifiers;
using Payjr.Core.Providers;
using Payjr.Core.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Payjr.Entity;

namespace Payjr.Core.ServiceCommands.CreditCardProcessing
{
    public class RetrieveCardsforUserServiceCommand : ProviderServiceCommandBase<RetrieveCardRequest, RetrieveCardResponse>
    {
        private FinancialAccountList<CreditCardAccount> _creditCardAccounts;
  
        public RetrieveCardsforUserServiceCommand(IProviderFactory providers) : base(providers) { }

        protected override bool OnExecute(RetrieveCardResponse response)
        {      
            foreach (CreditCardAccount creditCardAccount in _creditCardAccounts)
            {               
                    response.CreditCards.Add
                    (
                    new CreditCardDetailedRecord
                    {
                        AccountIdentifier = new CreditCardIdentifier(creditCardAccount.AccountID).ToString(),
                        CardNumberLastFour = creditCardAccount.CardNumber.Substring(creditCardAccount.CardNumber.Length - 4, 4),
                        CardType = creditCardAccount.Type,
                        ExpirationMonth = creditCardAccount.ExpirationDate.ToString("MM"),
                        ExpirationYear = creditCardAccount.ExpirationDate.ToString("yyyy"),
                        User = new UserDetailRecord(),
                        UserIdentifier = new UserIdentifier(creditCardAccount.UserID.Value).Identifier          
                    }
                    );
                }      
                                                 
                return true;
        }

        protected override void Validate(RetrieveCardRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request", "request must be set");
            }

            var retrieveCardRecords = request.RetrieveCardRecords;
            if (retrieveCardRecords == null || retrieveCardRecords.Count == 0)
            {
                throw new ArgumentException("RetrieveCardRecords must be set", "request.RetrieveCardRecords");
            }
            
            var retrieveCardRecord = retrieveCardRecords[0];
            var userIdentifier = retrieveCardRecord.UserIdentifier;
            if (string.IsNullOrWhiteSpace(userIdentifier))
            {
                throw new ArgumentException("UserIdentifier must be set", "request.RetrieveCardRecords[0].UserIdentifier");
            }
            var userID = new UserIdentifier(userIdentifier).ID;
            User anUser = User.RetrieveUser(userID);
            if (anUser == null)
            {
                throw new Exception("Could not found an user with user ID = "+ userID);
            }

            if (anUser.RoleType != Entity.RoleType.Parent)
            {
                throw new InvalidOperationException(string.Format("User ID = {0} has invalid role to retrieve credit card", userID));
            }

            Parent cardOwner = anUser as Parent;
            _creditCardAccounts = cardOwner.FinancialAccounts.CreditCardAccounts;
            if (_creditCardAccounts == null)
            {
                throw new ValidationException(
                             string.Format(
                                 "Could not found a CreditCard with UserIdentifier = {0}",
                                 request.RetrieveCardRecords[0].UserIdentifier));
            }  
        
        }
    }
}
