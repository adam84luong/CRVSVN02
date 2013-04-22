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
        private List<RetrieveCardRecord> _retrieveCardRecords;
        public RetrieveCardsforUserServiceCommand(IProviderFactory providers) : base(providers) { }

        protected override bool OnExecute(RetrieveCardResponse response)
        {
            foreach (var retrieveCardRecord in _retrieveCardRecords)
            {
                FinancialAccountList<CreditCardAccount> creditCardAccounts;
                if (GetCreditCardAccount(retrieveCardRecord.UserIdentifier, out creditCardAccounts))
                {
                    foreach (CreditCardAccount creditCardAccount in creditCardAccounts)
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
                       
                }
            }
                                  
                return true;
        }

        protected override void Validate(RetrieveCardRequest request)
        {
            Log.Debug("Beginning validate the request");

            if (request == null)
            {
                throw new ArgumentNullException("request", "request must be set");
            }
            if (request.RetrieveCardRecords.Count == 0)
            {
                throw new ArgumentException("RetrieveCardRecords must be set", "request.RetrieveCardRecords");
            }

            _retrieveCardRecords = new List<RetrieveCardRecord>();
            var pos = -1;
            var count = 0;
            foreach (var retrieveCardRecord in request.RetrieveCardRecords)
            {
                pos++;
                if (string.IsNullOrWhiteSpace(retrieveCardRecord.UserIdentifier))
                {
                    Log.Debug("skiped request.RetrieveCardRecords[{0}] because UserIdentifier is null or empty", pos);
                    continue;
                }
                _retrieveCardRecords.Add(retrieveCardRecord);
                count++;
            }
            Log.Debug("Ending validate the request. {0}/{1} record(s) passed validation", count, request.RetrieveCardRecords.Count);

        }

        #region helper methods

        private bool GetCreditCardAccount(string userIdentifier, out FinancialAccountList<CreditCardAccount> creditCardAccounts)
        {
            creditCardAccounts = null;       

            try
            {
                var userID = new UserIdentifier(userIdentifier).ID;
                User anUser = User.RetrieveUser(userID);
                if (anUser == null)
                {
                    Log.Debug("Could not found an user with user ID={0}", userID);
                    return false;           
                }

                if (anUser.RoleType != Entity.RoleType.Parent)
                {
                    Log.Debug("User ID = {0} has invalid role to retrieve credit card", userID);
                }

                Parent cardOwner = anUser as Parent;
                creditCardAccounts = cardOwner.FinancialAccounts.CreditCardAccounts;
                if (creditCardAccounts == null)
                {
                    Log.Debug("Could not found a CreditCard with UserIdentifier = {0}", userIdentifier);
                    return false;
                }            
            }
            catch (Exception ex)
            {
                Log.Debug(ex.Message);
                return false;
            }
            return true;
        }

    
        #endregion
    }
}
