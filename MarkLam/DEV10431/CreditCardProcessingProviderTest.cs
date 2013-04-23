using System;
using CardLab.CMS.Providers;
using Common.Contracts.CreditCard.Records;
using Common.Contracts.CreditCard.Requests;
using Common.Contracts.CreditCard.Responses;
using Common.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CardLab.CMS.Test.Providers
{
    [TestClass]
    public class CreditCardProcessingProviderTest:TestBase
    {
        #region Additional test attributes

        [TestInitialize]
        public override void MyTestInitialize()
        {
            base.MyTestInitialize();
        }

        #endregion

        #region function testing

        [TestMethod()]
        public void CreateCreditCardSuccessfulTest()
        {
            AddCardResponse response = new AddCardResponse();
            response.CreditCards.Add(new CreditCardDetailedRecord()
                                         {
                                             UserIdentifier = "UserIdentifier",
                                             CardNumberLastFour = "0002",
                                             ExpirationMonth = "12",
                                             ExpirationYear = "2016"
                                         });

            CreditCardProcessingMock.Setup(mock => mock.AddCard(It.IsAny<Guid>(), It.IsAny<AddCardRequest>()))
                                   .Returns(response);

            var target = new CreditCardProcessingProvider(ProviderFactory, CreditCardProcessingMock.Object);
            var result = target.CreateCreditCard(new Guid(), "UserIdentifier", new UserDetailRecord(),
                                                 "4000000000000002", "1234", 12, 2016, CreditCardType.VISA);
            Assert.IsNotNull(result);
            Assert.AreEqual("UserIdentifier", result.UserIdentifier);
            Assert.AreEqual("0002", result.CardNumberLastFour);
            Assert.AreEqual("12", result.ExpirationMonth);
            Assert.AreEqual("2016", result.ExpirationYear);
        }

        [TestMethod()]
        public void CreateCreditCardUnSuccessfulTest()
        {
            AddCardResponse response = new AddCardResponse();

            CreditCardProcessingMock.Setup(mock => mock.AddCard(It.IsAny<Guid>(), It.IsAny<AddCardRequest>()))
                                   .Returns(response);

            var target = new CreditCardProcessingProvider(ProviderFactory, CreditCardProcessingMock.Object);
            var result = target.CreateCreditCard(new Guid(), "UserIdentifier", new UserDetailRecord(),
                                                 "4000000000000002", "1234", 12, 2016, CreditCardType.VISA);
            Assert.IsNull(result);
        }

        //retrieve Accounts

        [TestMethod()]
        public void RetrieveAccountsSuccessfullTest()
        {
            RetrieveCardResponse response = new RetrieveCardResponse();
            response.CreditCards.Add(new CreditCardDetailedRecord()
            {
                UserIdentifier = "UserIdentifier",
                CardNumberLastFour = "0002",
                ExpirationMonth = "12",
                ExpirationYear = "2016",
                CardType = CreditCardType.VISA
            });

            CreditCardProcessingMock.Setup(mock => mock.RetrieveCardsforUser(It.IsAny<Guid>(), It.IsAny<RetrieveCardRequest>()))
                                   .Returns(response);

            var target = new CreditCardProcessingProvider(ProviderFactory, CreditCardProcessingMock.Object);
            var result = target.RetrieveAccounts("UserIdentifier");
            Assert.IsNotNull(result);
            Assert.AreEqual("UserIdentifier", result[0].UserIdentifier);
            Assert.AreEqual("0002", result[0].CardNumberLastFour);
            Assert.AreEqual("12", result[0].ExpirationMonth);
            Assert.AreEqual("2016", result[0].ExpirationYear);
            Assert.AreEqual(CreditCardType.VISA, result[0].CardType);
        }
        [TestMethod()]
        public void RetrieveAccountsReturn2CreditCardSuccessfulTest()
        {
            RetrieveCardResponse response = new RetrieveCardResponse();
            response.CreditCards.Add(new CreditCardDetailedRecord()
            {
                UserIdentifier = "UserIdentifier1",
                CardNumberLastFour = "0002",
                ExpirationMonth = "12",
                ExpirationYear = "2016",
                CardType = CreditCardType.VISA
            });
            response.CreditCards.Add(new CreditCardDetailedRecord()
            {
                UserIdentifier = "UserIdentifier2",
                CardNumberLastFour = "0000",
                ExpirationMonth = "07",
                ExpirationYear = "2014",
                CardType = CreditCardType.MASTERCARD
            });

            CreditCardProcessingMock.Setup(mock => mock.RetrieveCardsforUser(It.IsAny<Guid>(), It.IsAny<RetrieveCardRequest>()))
                                   .Returns(response);

            var target = new CreditCardProcessingProvider(ProviderFactory, CreditCardProcessingMock.Object);
            var result = target.RetrieveAccounts("UserIdentifier");
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("0002", result[0].CardNumberLastFour);
            Assert.AreEqual("UserIdentifier1", result[0].UserIdentifier);
            Assert.AreEqual(CreditCardType.VISA, result[0].CardType);
            Assert.AreEqual("UserIdentifier2", result[1].UserIdentifier);
            Assert.AreEqual(CreditCardType.MASTERCARD, result[1].CardType);
        }
        [TestMethod()]
        public void RetrieveAccountsUnSuccessfullTest()
        {
            RetrieveCardResponse response = null;
           

            CreditCardProcessingMock.Setup(mock => mock.RetrieveCardsforUser(It.IsAny<Guid>(), It.IsAny<RetrieveCardRequest>()))
                                   .Returns(response);
            var target = new CreditCardProcessingProvider(ProviderFactory, CreditCardProcessingMock.Object);
            var result = target.RetrieveAccounts("UserIdentifier");
            Assert.IsNull(result);
        }
        #endregion
    }
}
