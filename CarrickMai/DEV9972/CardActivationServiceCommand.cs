using Common.Contracts.Prepaid.Records;
using Common.Contracts.Prepaid.Requests;
using Common.Contracts.Prepaid.Responses;
using Payjr.Core.Providers;
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
        private PrepaidCardDetailRecord cardDetailRecord;

        public ICardProvider CardProvider
        {
            get
            {
                return _cardProvider;
            }
            set
            {
                _cardProvider = value;
            }
        }

        protected override bool OnExecute(CardActivationResponse response)
        {
            if (cardActivationRecord.CardIdentifier ==  null)
            {
                return false;
            }
            response.CardActivations.Add
            (
                new CardActivationRecord
                {
                    ActingUserIdentifier = new PrepaidCardDetailRecord().ToString(),
                    ActivationSuccessful = true,
                    CardIdentifier = new PrepaidCardDetailRecord().ToString()
                }
            );
            return true;
        }

        protected override void Validate(CardActivationRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request", "request must be set");
            }
            var addCardActivationsRecord = request.CardActivations;

            if (addCardActivationsRecord == null)
            {
                throw new ArgumentException("request.Configuration must be set", "request");
            }
            if (addCardActivationsRecord.Count == 0)
            {
                throw new ArgumentException("request.Requests must have item", "request");
            }
            
            if(cardActivationRecord.ActingUserIdentifier == null)
            {
                throw new ArgumentException("request.Requests must have item", "request");
            }
        }
    }
}
