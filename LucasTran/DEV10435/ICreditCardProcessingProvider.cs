using System;
using Common.Contracts.CreditCard.Records;
using Common.Types;

namespace CardLab.CMS.Providers
{
    public interface ICreditCardProcessingProvider : Common.Service.Providers.IServiceProvider
    {
        CreditCardDetailedRecord CreateCreditCard(Guid appicationKey, string userIdentifier, UserDetailRecord userInfo,
                                                  string cardNumber, string cvv2, int expirationMonth,
                                                  int expirationYear, CreditCardType cardType);
        bool DeleteAccount(Guid applicationKey, string accountIdentifier);
    }
}
