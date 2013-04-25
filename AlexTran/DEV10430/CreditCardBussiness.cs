using System;
using CardLab.CMS.Providers;
using CardLab.CMS.SiteSystem;
using Common.Contracts.CreditCard.Records;
using Common.Contracts.Shared.Records;
using Common.Types;

namespace CardLab.CMS.PayjrSites.Bussiness
{
    public class CreditCardBussiness
    {
        protected IPayjrSystemInfoProvider PayjrSystem;
        protected IProviderFactory Provider;

        #region Constructor

        protected CreditCardBussiness(IPayjrSystemInfoProvider payjrSystem)
        {
            PayjrSystem = payjrSystem;
            Provider = payjrSystem.ProviderFactory;
        }

        #endregion

        #region Static Method

        public static CreditCardBussiness Instance(IPayjrSystemInfoProvider payjrSystem)
        {
            return new CreditCardBussiness(payjrSystem);
        }

        #endregion //Method


        #region Public Method

        public CreditCardDetailedRecord CreateCreditCard(string cardNumber, string cvv, int expirationMonth, int expirationYear, CreditCardType cardType, string userIdentifier, AddressRecord address)
        {
            var ccUserInfo = new UserDetailRecord();
            ccUserInfo.FirstName = address.FirstName;
            ccUserInfo.LastName = address.LastName;
            ccUserInfo.Email = address.EmailAddress;
            ccUserInfo.Street1 = address.AddressLine1;
            ccUserInfo.Street2 = address.AddressLine2;
            ccUserInfo.City = address.City;
            ccUserInfo.PostalCode = String.Format("{0}-{1}", address.ZipCode, address.ZipPlusFour);
            ccUserInfo.Country = "US";
            ccUserInfo.PhoneNumber = String.Empty;

            CreditCardDetailedRecord cc = Provider.CreditCardProcessing.CreateCreditCard(PayjrSystem.ApplicationKey, userIdentifier, ccUserInfo,
                                                                 cardNumber, cvv, expirationMonth, expirationYear, cardType);
            return cc;
        }

        #endregion
    }
}
