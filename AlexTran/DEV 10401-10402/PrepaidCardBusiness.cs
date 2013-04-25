using CardLab.CMS.Providers;
using CardLab.CMS.SiteSystem;
using Common.Contracts.Prepaid.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardLab.CMS.PayjrSites.Business
{
   public  class PrepaidCardBusiness
    {
        protected IPayjrSystemInfoProvider PayjrSystem;
        protected IProviderFactory Provider;
       
        #region Constructor

        protected PrepaidCardBusiness(IPayjrSystemInfoProvider payjrSystem)
        {
            PayjrSystem = payjrSystem;
            Provider = payjrSystem.ProviderFactory;
        }

        #endregion

        #region Static Method
        public static PrepaidCardBusiness Instance(IPayjrSystemInfoProvider payjrSystem)
        {
            return new PrepaidCardBusiness(payjrSystem); 
        }

        #endregion

        #region Public Method
       
        public  List<CardTransactionRecord> GetCardTransaction( string cardIdentifier, DateTime startDate, DateTime endDate, int pageNumber, int numberPerPage, out int totalRecord)
        {
            List<CardTransactionRecord> cardTransactionRecords = Provider.Prepaid.RetrieveCardTransactions(PayjrSystem.ApplicationKey, cardIdentifier, startDate, endDate, pageNumber, numberPerPage, out totalRecord);
     
            return cardTransactionRecords;
        }
     

        #endregion
    }
}
