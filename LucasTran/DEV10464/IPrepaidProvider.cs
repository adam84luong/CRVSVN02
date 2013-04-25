using System;
using System.Collections.Generic;
using Common.Contracts.Prepaid.Records;


namespace CardLab.CMS.Providers
{
    public interface IPrepaidProvider : Common.Service.Providers.IServiceProvider
    {
        List<PrepaidCardDetailRecord> RetrieveCardDetaislByUserIdentifierCardNumber(Guid applicationKey, string userIdentifier = null, string cardnumber = null);
        
        bool CardActivation(Guid applicationKey, string cardIdentifier, string actingUserIdentifier, string ipAddress, string activeData);

        List<CardTransactionRecord> RetrieveCardTransactions(Guid applicationKey, string cardIdentifier, DateTime startDate, DateTime endDate, int pageNumber, int numberPerPage, out int totalRecord);
    }
}
