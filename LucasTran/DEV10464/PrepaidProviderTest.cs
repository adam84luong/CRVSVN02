using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CardLab.CMS.Providers;
using Common.Contracts.Prepaid.Records;
using Common.Contracts.Prepaid.Requests;
using Common.Contracts.Prepaid.Responses;
using Common.Contracts.Shared.Records;
using Common.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace CardLab.CMS.Test.Providers
{
    [TestClass]
    public class PrepaidProviderTest : TestBase
    {
        #region Additional test attributes

        [TestInitialize]
        public override void MyTestInitialize()
        {
            base.MyTestInitialize();
        }

        #endregion

        [TestMethod]
        public void RetrieveCardDetaislByUserIdentifersSuccess_OneCard()
        {
            // response            
            var response = new PrepaidCardSearchResponse();
            response.Status = new ResponseStatusRecord()
                                  {
                                      IsSuccessful = true
                                  };
            response.Records.Add(new PrepaidCardDetailRecord()
                                     {
                                         ActivationType = "activation type",
                                         CardBalance = 10,
                                         CardHolder = new ContactInformation() { AddressLine1 = "adr1", AddressLine2 = "adr2", EmailAddress = "test@test.com"},
                                         CardMessage = "message",
                                         CardNumberLastFour = "1234",
                                         CardNumberMasked = "masked",
                                         CardStatus = "status",
                                         CardStatus2 = PrepaidCardStatus2.Activated
                                     });
            
            // test function
            PrepaidMock.Setup(mock =>
                        mock.GetCardDetails(It.IsAny<RetrievalConfigurationRecord>(), It.IsAny<PrepaidCardSearchRequest>())).Returns(response);

            var target = new PrepaidProvider(ProviderFactory, PrepaidMock.Object);

            List<PrepaidCardDetailRecord> result = target.RetrieveCardDetaislByUserIdentifierCardNumber(new Guid(), Guid.NewGuid().ToString());

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Count);            
            Assert.AreEqual("activation type", result[0].ActivationType);
        }

        [TestMethod]
        public void RetrieveCardDetaislByUserIdentifersSuccess_ManyCard()
        {
            // response            
            var response = new PrepaidCardSearchResponse();
            response.Status = new ResponseStatusRecord()
            {
                IsSuccessful = true
            };
            response.Records.Add(new PrepaidCardDetailRecord()
            {
                ActivationType = "activation type",
                CardBalance = 10,
                CardHolder = new ContactInformation() { AddressLine1 = "adr1", AddressLine2 = "adr2", EmailAddress = "test@test.com" },
                CardMessage = "message",
                CardNumberLastFour = "1234",
                CardNumberMasked = "masked",
                CardStatus = "status",
                CardStatus2 = PrepaidCardStatus2.Activated
            });

            response.Records.Add(new PrepaidCardDetailRecord()
            {
                ActivationType = "activation type 2",
                CardBalance = 20,
                CardHolder = new ContactInformation() { AddressLine1 = "adr1", AddressLine2 = "adr2", EmailAddress = "test2@test.com" },
                CardMessage = "message 2",
                CardNumberLastFour = "1234",
                CardNumberMasked = "masked",
                CardStatus = "status",
                CardStatus2 = PrepaidCardStatus2.PendingActivation
            });

            // test function
            PrepaidMock.Setup(mock =>
                        mock.GetCardDetails(It.IsAny<RetrievalConfigurationRecord>(), It.IsAny<PrepaidCardSearchRequest>())).Returns(response);

            var target = new PrepaidProvider(ProviderFactory, PrepaidMock.Object);

            List<PrepaidCardDetailRecord> result = target.RetrieveCardDetaislByUserIdentifierCardNumber(new Guid(), Guid.NewGuid().ToString());

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("activation type", result[0].ActivationType);
            Assert.AreEqual(PrepaidCardStatus2.Activated, result[0].CardStatus2);
            Assert.AreEqual(PrepaidCardStatus2.PendingActivation, result[1].CardStatus2);
        }

        [TestMethod]
        public void RetrieveCardDetaislByUserIdentifersFail()
        {
            // response            
            var response = new PrepaidCardSearchResponse();
            response.Status = new ResponseStatusRecord()
            {
                IsSuccessful = false
            };
            
            // test function
            PrepaidMock.Setup(mock =>
                        mock.GetCardDetails(It.IsAny<RetrievalConfigurationRecord>(), It.IsAny<PrepaidCardSearchRequest>())).Returns(response);

            var target = new PrepaidProvider(ProviderFactory, PrepaidMock.Object);

            List<PrepaidCardDetailRecord> result = target.RetrieveCardDetaislByUserIdentifierCardNumber(new Guid(), Guid.NewGuid().ToString());

            Assert.IsNull(result);           
        }

        [TestMethod]
        public void CardActivation_Success()
        {           
            var cardActivationResponse = new CardActivationResponse();
            cardActivationResponse.Status = new ResponseStatusRecord()
            {
                IsSuccessful = true
            };
            CardActivationRecord cardActived = new CardActivationRecord()
            {
                ActingUserIdentifier = "ActingUserIdentifier",
                ActivationSuccessful = true,
                CardIdentifier = "CardIdentifier"
            };
            cardActivationResponse.CardActivations.Add(cardActived);
            PrepaidMock.Setup(mock =>
                        mock.CardActivation(It.IsAny<RetrievalConfigurationRecord>(), It.IsAny<CardActivationRequest>())).Returns(cardActivationResponse);

            var target = new PrepaidProvider(ProviderFactory, PrepaidMock.Object);

            bool result = target.CardActivation(new Guid(),"cardIdentifier","actingUserIdentifier","ipAddrress","activeData");

            Assert.IsTrue(result);
        }

        [TestMethod]
        public void CardActivation_Fail()
        {
            var cardActivationResponse = new CardActivationResponse();
            cardActivationResponse.Status = new ResponseStatusRecord()
            {
                IsSuccessful = false
            };
            CardActivationRecord cardActived = new CardActivationRecord()
            {
                ActingUserIdentifier = "ActingUserIdentifier",
                ActivationSuccessful = false,
                CardIdentifier = "CardIdentifier"
            };
            cardActivationResponse.CardActivations.Add(cardActived);
            PrepaidMock.Setup(mock =>
                        mock.CardActivation(It.IsAny<RetrievalConfigurationRecord>(), It.IsAny<CardActivationRequest>())).Returns(cardActivationResponse);

            var target = new PrepaidProvider(ProviderFactory, PrepaidMock.Object);

            bool result = target.CardActivation(new Guid(), "cardIdentifier", "actingUserIdentifier", "ipAddrress", "activeData");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void CardActivation_Exception_Fail()
        {
            var target = new PrepaidProvider(ProviderFactory, "http://abc");
            bool result = target.CardActivation(new Guid(), "cardIdentifier", "actingUserIdentifier", "ipAddrress", "activeData");
            Assert.IsFalse(result);
        }
    }
}
