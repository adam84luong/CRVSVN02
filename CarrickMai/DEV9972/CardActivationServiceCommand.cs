using Common.Contracts.Prepaid.Records;
using Common.Contracts.Prepaid.Requests;
using Common.Contracts.Prepaid.Responses;
using Payjr.Configuration;
using Payjr.Core.FinancialAccounts;
using Payjr.Core.Identifiers;
using Payjr.Core.Providers;
using Payjr.Core.Transactions;
using Payjr.Core.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payjr.Core.ServiceCommands.PrepaidCard
{
    public class CardActivationServiceCommand : ProviderServiceCommandBase<CardActivationRequest, CardActivationResponse>
    {
        public CardActivationServiceCommand(IProviderFactory providers) : base(providers) { }
        private PrepaidCardAccount _prepaidCardAcount;    
        CardActivationRequestRecord _cardActivationRequestRecord;
        private Parent _parentDOB;
        

        protected override bool OnExecute(CardActivationResponse response)
        {                                    
                VerificationData verificationData = new VerificationData(_parentDOB.DOB.Value);
                if (verificationData != null)
                {
                    bool verifyAccount = _prepaidCardAcount.VerifyAccount(verificationData, null, null, null);
                    response.CardActivations.Add
                    (
                        new CardActivationRecord
                        {
                            ActingUserIdentifier = _cardActivationRequestRecord.ActivatingUserIdentifier,
                            ActivationSuccessful = verifyAccount,
                            CardIdentifier = _prepaidCardAcount.CardIdentifier.ToString()
                        }
                    );
                }
                else
                {
                    throw new ArgumentException("Error");
                    return false;
                }
          
            return true;
        }

        protected override void Validate(CardActivationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request", "request must be set");
            }        

            var cardActivationRecords = request.CardActivations;
            if (cardActivationRecords == null || cardActivationRecords.Count == 0)
            {
                throw new ArgumentException("cardActivationRecords must be set", "request.cardActivationRecords");
            }
            var cardActivationRecord = cardActivationRecords[0];
            
            if (string.IsNullOrWhiteSpace(cardActivationRecord.ActivatingUserIdentifier))
            {
               throw new ArgumentException("ActivatingUserIdentifier must be set", "request.cardActivationRecords[0].ActivatingUserIdentifier");
            }
            if(string.IsNullOrWhiteSpace(cardActivationRecord.ActivationData))
            {
                throw new ArgumentException("ActivationData must be set", "request.cardActivationRecords[0].ActivationData");
            }
          
          
             Guid _prepaidCardID = new Identifiers.PrepaidCardAccountIdentifier(cardActivationRecord.CardIdentifier).PersistableID;
             _prepaidCardAcount = PrepaidCardAccount.RetrievePrepaidCardAccountByID(_prepaidCardID);
            
             Guid userID = new UserIdentifier(cardActivationRecord.ActivatingUserIdentifier).ID;
             _parentDOB = User.RetrieveUser(userID) as Parent;
         
             _cardActivationRequestRecord = request.CardActivations[0];
        }
    }
}
