using Common.Contracts.Prepaid.Records;
using Common.Contracts.Prepaid.Requests;
using Common.Contracts.Prepaid.Responses;
using Common.Contracts.Shared.Records;
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
        public int _numberPerPage;
        public int _pageNumber;
        private DateTime _startDate;
        private PrepaidCardAccount _prepaidCardAccount;
   

        public CardTransactionSearchServiceCommand(IProviderFactory providers) : base(providers) { }

        protected override bool OnExecute(RetrieveTransactionResponse response)
        {
                List<FinancialTransaction> cardTransactions = _prepaidCardAccount.RetrieveTransactions(_startDate, _endDate, _pageNumber, _numberPerPage);
                FSVDBTransaction fsvtrans;
                foreach (FinancialTransaction trans in cardTransactions)
                    {
                        fsvtrans = trans as FSVDBTransaction;
                        if (fsvtrans != null)
                        {
                            response.CardTransactions.Add
                            (
                            new CardTransactionRecord
                            {
                                ActingUserIdentifier = new Identifiers.UserIdentifier(_prepaidCardAccount.UserID.Value).Identifier,
                                Amount = fsvtrans.Amount,
                                Date = DateTime.Parse(fsvtrans.DateString),
                                Description = fsvtrans.Description,
                                LongTransactionTypeDescription = fsvtrans.LongTransactionTypeDescription,
                                MerchantCategoryGroup = fsvtrans.CardTransaction.Mmc,
                                RunningBalance = fsvtrans.RunningBalance,
                                ShortTransactionTypeDescription = fsvtrans.ShortTransactionTypeDescription,
                                TransactionNumber = fsvtrans.TransactionNumber.Trim()
                            }
                            );
                        }
                    }
                    response.CardIdentifier = _cardIdentifier;
                    response.TotalTransactions = cardTransactions.Count;                                                
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
            _pageNumber = request.PageNumber;
            if (_pageNumber < 0)
            {
                _pageNumber = 0;                  
            }
            _numberPerPage = request.NumberPerPage;
            if (_numberPerPage < 0)
            {
                _numberPerPage = 0;    
            }
         
            int _result = DateTime.Compare(request.StartDate, request.EndDate);
            if (_result > 0)
            {
                throw new ArgumentException("StartDate must earlier than is EndDate", "request.StartDate, request.EndDate");
            }
            try
            {
                Guid _prepaidCardID = new Identifiers.PrepaidCardAccountIdentifier(request.CardIdentifier).PersistableID;
                _prepaidCardAccount = PrepaidCardAccount.RetrievePrepaidCardAccountByID(_prepaidCardID);
             }
            catch 
            {
                throw new Exception(string.Format("Could not found a CardTransaction with CardIdentifier = {0}", request.CardIdentifier));
             }

            _cardIdentifier = request.CardIdentifier;
            _endDate = request.EndDate;
            _startDate = request.StartDate;        
         
        }
    }
}
