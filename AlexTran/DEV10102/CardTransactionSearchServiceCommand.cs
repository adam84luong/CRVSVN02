using Common.Contracts.Prepaid.Records;
using Common.Contracts.Prepaid.Requests;
using Common.Contracts.Prepaid.Responses;
using Payjr.Core.Adapters;
using Payjr.Core.FSV.Transactions;
using Payjr.Core.Identifiers;
using Payjr.Core.Providers;
using Payjr.Entity.EntityClasses;
using Payjr.Entity.HelperClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payjr.Core.ServiceCommands.Prepaid
{
    public class CardTransactionSearchServiceCommand : ProviderServiceCommandBase<RetrieveTransactionRequest, RetrieveTransactionResponse>
    {
        private string _cardIdentifier;
        private DateTime _endDate;
        private bool _filteredForUserViewing;
        private bool _includeReplacementCards;
        public int _numberPerPage;
        public int _pageNumber;
        private DateTime _startDate;

        public CardTransactionSearchServiceCommand(IProviderFactory providers) : base(providers) { }

        protected override bool OnExecute(RetrieveTransactionResponse response)
        {
            EntityCollection<CardTransactionEntity> cardTransactions = AdapterFactory.TransactionAdapter.RetrieveCardTransactionsSearch(_cardIdentifier, _startDate, _endDate);
            foreach (CardTransactionEntity trans in cardTransactions)
            {

                response.CardTransactions.Add
                    (
                    new CardTransactionRecord
                    {
                        Amount = trans.Amount,
                        Date = trans.TransactionDate,
                        Description = trans.MerchantRef,
                        LongTransactionTypeDescription = trans.Ref1,
                        MerchantCategoryGroup = trans.Mmc.Trim(),
                        RunningBalance = trans.RunningBalance,
                        ShortTransactionTypeDescription = trans.Ref2,
                        TransactionNumber = trans.TransactionType.Trim()
                    }
                    );
            }

          return true;
        }


        protected override void Validate(RetrieveTransactionRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request", "request must be set");
            }
            if (string.IsNullOrWhiteSpace(request.CardIdentifier))
            {
                throw new ArgumentException("CardIdentifier must be set", "request.CardIdentifier");
            }
            if (request.Configuration == null)
            {
                throw new ArgumentException("request.Configuration must be set", "request");
            }
            if (!request.Configuration.ApplicationKey.HasValue)
            {
                throw new ArgumentException("request.Configuration.ApplicationKey must be set", "request");
            }        
            int _result = DateTime.Compare(request.StartDate, request.EndDate);
            if (_result > 0)
            {
                throw new ArgumentException("request.StartDate must earlier than is request.EndDate", "request");
            }

            _cardIdentifier = request.CardIdentifier;
            _endDate = request.EndDate;
            _startDate = request.StartDate;
            _filteredForUserViewing = request.FilteredForUserViewing;
            _includeReplacementCards = request.IncludeReplacementCards;
            _numberPerPage = request.NumberPerPage;
            _pageNumber = request.PageNumber;
        }
    }
}
