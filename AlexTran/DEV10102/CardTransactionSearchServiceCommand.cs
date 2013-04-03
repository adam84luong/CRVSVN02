using Common.Contracts.Prepaid.Records;
using Common.Contracts.Prepaid.Requests;
using Common.Contracts.Prepaid.Responses;
using Payjr.Core.Adapters;
using Payjr.Core.Providers;
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
           response.CardTransactions.Add
               ( new CardTransactionRecord
               {
               
               }
               )
          EntityCollection<CardTransactionRecord> cardTransactions = AdapterFactory.TransactionAdapter.RetrieveCardTransactionByTranID(AccountID, startDate, endDate);
            FSVTransactionList transactionList = new FSVTransactionList(cardTransactions.Count);
            foreach (CardTransactionEntity trans in cardTransactions)
            {
                transactionList.Add(trans, BusinessParentUser.UserEntity as TeenEntity);
            }
            return transactionList.FinancialTransactions;
           // cai dat code, co the xay ra loi, exception o day
            //response.CardTransactions.Add
            //(
            //    new CardTransactionRecord
            //    {
            //        AccountIdentifier = new CreditCardIdentifier(cardAcc.AccountID).ToString(),
            //        CardNumberLastFour = cardAcc.AccountNumber.Substring(cardAcc.AccountNumber.Length - 4),
            //        CardType = cardAcc.Type,
            //        ExpirationMonth = cardAcc.ExpirationDate.ToString("MM"),
            //        ExpirationYear = cardAcc.ExpirationDate.ToString("yyyy"),
            //        User = new UserDetailRecord(),
            //        UserIdentifier = new UserIdentifier(cardAcc.UserID.Value).Identifier
            //    }
            //);
            return true;
        }

        //public string ActingUserIdentifier { get; set; }
        //public decimal Amount { get; set; }
        //public DateTime Date { get; set; }
        //public string Description { get; set; }
        //public string LongTransactionTypeDescription { get; set; }
        //public string MerchantCategoryGroup { get; set; }
        //public decimal RunningBalance { get; set; }
        //public string ShortTransactionTypeDescription { get; set; }
        //public string TransactionNumber { get; set; }

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

            if ( request.NumberPerPage<=0)
            {
                throw new ArgumentException("request.NumberPerPage must be later than 0", "request");
            }
            if ( request.PageNumber <= 0)
            {
                throw new ArgumentException("request.PageNumber must be later than 0", "request");
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
