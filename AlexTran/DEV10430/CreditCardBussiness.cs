using System;
using CardLab.CMS.Providers;
using CardLab.CMS.SiteSystem;
using Common.Contracts.CreditCard.Records;
using Common.Contracts.Shared.Records;
using Common.Types;
using CardLab.CMS.PayjrSites.DTO;
using System.Collections.Generic;

namespace CardLab.CMS.PayjrSites.Bussiness
{
    public class CreditCardBussiness
    {
        protected IPayjrSystemInfoProvider PayjrSystem;
        protected static IProviderFactory Provider;

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

        public static List<FundingCreditCard> GetFundingCreditCard(string UserIdentifier)
        {
            List<CreditCardDetailedRecord> creditCardAccounts= Provider.CreditCardProcessing.RetrieveAccounts(UserIdentifier);
            List<FundingCreditCard> fundingSourcesItems = new List<FundingCreditCard>();
            foreach (CreditCardDetailedRecord creditCardAccount in creditCardAccounts)
            {
                fundingSourcesItems.Add(ConvertCreditCardtoFundingCreditCard(creditCardAccount));
            }
            return fundingSourcesItems;
        }
        public static FundingCreditCard ConvertCreditCardtoFundingCreditCard(CreditCardDetailedRecord creditCardAccount)
        {
            FundingCreditCard fundingSourcesItem = new FundingCreditCard();
            fundingSourcesItem.AccountIdentifier = creditCardAccount.AccountIdentifier;
            fundingSourcesItem.CardType = creditCardAccount.CardType.ToString();
            fundingSourcesItem.CardNumber = "XXXX-XXXX-XXXX-" + creditCardAccount.CardNumberLastFour;
            string status;
            fundingSourcesItem.Expiration = GetExpiration(creditCardAccount.ExpirationMonth, creditCardAccount.ExpirationYear, out status);
            fundingSourcesItem.Status = status;
            return fundingSourcesItem;
        }

        public static string GetExpiration(string month, string year, out string status)
        {
            string expireDay = month + "/" + year.Substring(2, 2);
            string expirestatus = "";
            if (DateTime.Now.Year < Int32.Parse(year))
            {
                status = "Good";
            }
            else if (DateTime.Now.Year == Int32.Parse(year))
            {
                if (DateTime.Now.Month < Int32.Parse(month))
                {
                    status = "Good";
                }
                else
                {
                    status = "Bad";
                    expirestatus = " (expired)";
                }
            }
            else
            {
                status = "Bad";
                expirestatus = " (expired)";
            }
            return expireDay + expirestatus;
        }
        #endregion
    }
}
