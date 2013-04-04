using Common.Contracts.Prepaid.Records;
using Common.Contracts.Prepaid.Requests;
using Common.Contracts.Prepaid.Responses;
using Payjr.Core.Adapters;
using Payjr.Core.FinancialAccounts;
using Payjr.Core.FSV.Transactions;
using Payjr.Core.Identifiers;
using Payjr.Core.Providers;
using Payjr.Core.Transactions;
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
            // wait PrepaidCardIdentifier check in.
            Guid prepaidCardID = new Identifiers.UserIdentifier(_cardIdentifier).ID;
            PrepaidCardAccount prepaidCardAccount = PrepaidCardAccount.RetrievePrepaidCardAccountByID(prepaidCardID);
            List<FinancialTransaction> cardTransactions = prepaidCardAccount.RetrieveTransactions(_startDate, _endDate,_pageNumber,_numberPerPage);
            foreach (FinancialTransaction trans in cardTransactions)
            {
                response.CardTransactions.Add
                    (
                    new CardTransactionRecord
                    {
                        ActingUserIdentifier=new CreditCardIdentifier(prepaidCardAccount.AccountID).ToString(),
                        Amount = trans.Amount,
                        Date = DateTime.Parse(trans.DateString),// value Date? maybe equal: Null, N/A. please check
                        Description = trans.Description,
                        LongTransactionTypeDescription = trans.FormattedDescription,
                      //  MerchantCategoryGroup = trans.Mmc.Trim(),  Not found. please check
                        RunningBalance = trans.RunningBalance,
                        ShortTransactionTypeDescription = trans.ShortTransactionTypeDescription,
                        TransactionNumber = trans.TransactionProgram.Trim()
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

            if (request.PageNumber <= 0 )
            {
                throw new ArgumentException("PageNumber must >=0", "request.PageNumber");
            }
            if (request.NumberPerPage <= 0 )
            {
                throw new ArgumentException("PageNumber must >=0", "request.NumberPerPage");
            }
            int _result = DateTime.Compare(request.StartDate, request.EndDate);
            if (_result > 0)
            {
                throw new ArgumentException("StartDate must earlier than is EndDate", "request.StartDate, request.EndDate");
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
