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
        private ICardProvider _cardProvider;
        private CardActivationRecord cardActivationRecord;
        private PrepaidCardAccount prepaidCardAcount;
        private Guid _prepaidCardID;
        private AccountDetail accountDetail;
        List<CardActivationRequestRecord> CardActivationRequestRecord;
        private Parent parent;
        
        protected override bool OnExecute(CardActivationResponse response)
        {           
 
            foreach (CardActivationRequestRecord cardActivation in CardActivationRequestRecord)
            {                       
             // List<FinancialTransaction> retrieveTransactions = prepaidCardAcount.RetrieveTransactions();
               
                VerificationData verificationData = new VerificationData(parent.DOB);                
                response.CardActivations.Add
                (
                    new CardActivationRecord
                    {
                        ActingUserIdentifier = cardActivation.ActivatingUserIdentifier,
                        ActivationSuccessful = prepaidCardAcount.VerifyAccount(verificationData, null, null, null),
                        CardIdentifier = prepaidCardAcount.CardNumber
                    }
                );
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

            var _userIdentifier = cardActivationRecord.ActivatingUserIdentifier;
            if(string.IsNullOrWhiteSpace(_userIdentifier))
            {
                throw new ArgumentException("UserIdentifier must be set", "request.AddCardsRecord[0].UserIdentifier");
            }
            var dataAcive = cardActivationRecord.ActivationData;
            if(string.IsNullOrWhiteSpace(dataAcive))
            {
                throw new ArgumentException("dataAcive must be set", "request");
            }
            var cardIdentifier = prepaidCardAcount.CardNumber;
            if(string.IsNullOrWhiteSpace(cardIdentifier))
            {
                throw new ArgumentException("CardNumber must be set", "request");
            }
            var ipAddress = cardActivationRecord.IPAddress;
            if(string.IsNullOrWhiteSpace(ipAddress))
            {
                throw new ArgumentException("ipAddress must be set", "request");
            }
            try
            {
                _prepaidCardID = new Identifiers.PrepaidCardAccountIdentifier(cardIdentifier).PersistableID;
                prepaidCardAcount = PrepaidCardAccount.RetrievePrepaidCardAccountByID(_prepaidCardID);
                
            }
            catch
            {
                throw new Exception(string.Format("Could not found a CardActivation with CardIdentifier = {0}",cardIdentifier));
            }
            CardActivationRequestRecord = request.CardActivations; 
        }
    }
}
