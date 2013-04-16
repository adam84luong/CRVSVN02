using System;
using System.Collections.Generic;
using Common.Contracts.Prepaid.Records;


namespace CardLab.CMS.Providers
{
    public interface IPrepaidProvider : Common.Service.Providers.IServiceProvider
    {
        List<PrepaidCardDetailRecord> RetrieveCardDetaislByUserIdentifier(Guid applicationKey, string userIdentifier);
        
        bool CardActivation(Guid applicationKey, string cardIdentifier, string actingUserIdentifier, string ipAddress, string activeData);

        List<CardTransactionRecord> RetrieveCardTransactions(Guid applicationKey, string cardIdentifier, DateTime startDate, DateTime endDate, int pageNumber, int numberPerPage);
    }
}
