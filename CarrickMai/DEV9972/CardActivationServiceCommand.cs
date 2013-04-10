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
        private PrepaidCardAccount prepaidCardAcount;    
        List<CardActivationRequestRecord> CardActivationRequestRecord;    
        
        protected override bool OnExecute(CardActivationResponse response)
        {           
            
            foreach (CardActivationRequestRecord cardActivation in CardActivationRequestRecord)
            {                          
                Guid userID = new UserIdentifier(cardActivation.ActivatingUserIdentifier).ID;
                Parent parent = User.RetrieveUser(userID) as Parent;
                VerificationData verificationData = new VerificationData(parent.DOB.Value);
                if (verificationData != null)
                {
                    response.CardActivations.Add
                    (
                        new CardActivationRecord
                        {
                            
                            ActingUserIdentifier = cardActivation.ActivatingUserIdentifier,
                            ActivationSuccessful = prepaidCardAcount.VerifyAccount(verificationData, null, null, null),
                            CardIdentifier = prepaidCardAcount.CardIdentifier.ToString()
                        }
                    );
                }
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
                throw new ArgumentException("UserIdentifier must be set", "request.AddCardsRecord[0].UserIdentifier");
            }
            if(string.IsNullOrWhiteSpace(cardActivationRecord.ActivationData))
            {
                throw new ArgumentException("ActivationData must be set", "request");
            }
           
            var cardIdentifier = cardActivationRecord.CardIdentifier;//Validate cho thang nay
            if (string.IsNullOrWhiteSpace(cardActivationRecord.CardIdentifier))
            {
                throw new ArgumentException("CardIdentifier must be set", "request");
            }
            //
            if(string.IsNullOrWhiteSpace(cardActivationRecord.IPAddress))
            {
                throw new ArgumentException("ipAddress must be set", "request");
            }
            try
            {
                Guid _prepaidCardID = new Identifiers.PrepaidCardAccountIdentifier(cardIdentifier).PersistableID;
                prepaidCardAcount = PrepaidCardAccount.RetrievePrepaidCardAccountByID(_prepaidCardID);
            }           
        
            catch
            {
                throw new Exception(string.Format("Could not found a CardActivation with CardIdentifier = {0}",cardIdentifier));
            }
              if (string.IsNullOrWhiteSpace(prepaidCardAcount.CardNumber))
                {
                    throw new Exception(string.Format("Could not found a CardActivation with CardIdentifier = {0}", cardIdentifier));
       
                }
                CardActivationRequestRecord = request.CardActivations; 
         
        }
    }
}
