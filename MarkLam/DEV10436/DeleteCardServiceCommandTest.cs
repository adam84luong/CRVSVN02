using Common.Contracts.CreditCard.Records;
using Common.Contracts.CreditCard.Requests;
using Common.Contracts.Shared.Records;
using Common.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Payjr.Core.FinancialAccounts;
using Payjr.Core.Identifiers;
using Payjr.Core.Modules;
using Payjr.Core.ServiceCommands.CreditCardProcessing;
using Payjr.Core.Users;
using Payjr.Util.Test;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payjr.Core.Test.ServiceCommands.CreditCardProcessing
{
    [TestClass]
    public class DeleteCardServiceCommandTest : TestBase2
    {
       // private PrepaidModule _prepaidModule;
        private string _creditCardIdentifier1;
        private string _creditCardIdentifier2;
        [TestInitialize]
        public override void MyTestInitialize()
        {
            base.MyTestInitialize();

            TestEntityFactory.CreateCreditCardAccount(_parent, true, Entity.AccountStatus.AllowMoneyMovement, MockCreator.CreateFakeCreditCardNumber());
            var ppAcctId1 = _parent.FinancialAccounts.CreditCardAccounts[0].AccountID;
            _creditCardIdentifier1 = new CreditCardIdentifier(ppAcctId1).DisplayableIdentifier;
            _creditCardIdentifier2 = new CreditCardIdentifier(ppAcctId1).DisplayableIdentifier;
        }

        [TestMethod]
        public void Execute_Successful()
        {
            var request = CreateDeleteCardRequest(true);
            //check credit cart when it's just created.
            foreach (var card in request.Requests)
            {
                var isCardDeleted = IsDeletedCreditCard(card.AccountIdentifier);
                Assert.IsNotNull(isCardDeleted);
                Assert.IsFalse(isCardDeleted.Value);
            }
            var target = new DeleteCardServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(2,result.Respones.Count);
            //re-check after excute delete card process
            foreach (var card in result.Respones)
            {
                var isCardDeleted = IsDeletedCreditCard(card.AccountIdentifier);
                Assert.IsNotNull(isCardDeleted);
                Assert.IsTrue(isCardDeleted.Value);
            }
        }
        [TestMethod]
        public void Execute_Successful_With1CardReturned()
        {
            var request = CreateDeleteCardRequest(true);
            //check credit cart when it's just created.
            Assert.AreEqual(2, request.Requests.Count);
            foreach (var card in request.Requests)
            {
                var isCardDeleted = IsDeletedCreditCard(card.AccountIdentifier);
                Assert.IsNotNull(isCardDeleted);
                Assert.IsFalse(isCardDeleted.Value);
            }
            //update to the second card is not found.
            request.Requests[1].AccountIdentifier = "CardIsNotFound";
            var target = new DeleteCardServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
            Assert.AreEqual(1,result.Respones.Count);
            //re-check after excute delete card process
            foreach (var card in result.Respones)
            {
                var isCardDeleted = IsDeletedCreditCard(card.AccountIdentifier);
                Assert.IsNotNull(isCardDeleted);
                Assert.IsTrue(isCardDeleted.Value);
            }
        }
       [TestMethod]
        public void Execute_Failure_RequestIsNull()
        {
            DeleteCardRequest request = null;
            var target = new DeleteCardServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(0, result.Respones.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.IsTrue(result.Status.ErrorMessage.StartsWith("request must be set value"));
        }
        
        [TestMethod]
        public void Execute_Failure_RequestRecordIsEmpty()
        {
            var request = CreateDeleteCardRequest(false);
            Assert.AreEqual(0, request.Requests.Count);
            var target = new DeleteCardServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(0, result.Respones.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.IsTrue(result.Status.ErrorMessage.StartsWith("DeleteCardsRecords must be set"));
        }
        [TestMethod]
        public void Execute_Failure_AccIdentifierNotFound()
        {
            var request = CreateDeleteCardRequest(true);
            Assert.AreEqual(2, request.Requests.Count);
            //update to the both card2 is not found.
            request.Requests[0].AccountIdentifier = "CardIsNotFound1";
            request.Requests[1].AccountIdentifier = "CardIsNotFound2";
            var target = new DeleteCardServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.AreEqual(0, result.Respones.Count);
            Assert.IsNotNull(result.Status);
            Assert.IsTrue(result.Status.IsSuccessful);
        }
        #region helper methods

        private DeleteCardRequest CreateDeleteCardRequest(bool initDeleteCardRecord)
        {
            var result = new DeleteCardRequest
            {
                Header = new RequestHeaderRecord
                {
                    CallerName = "DeleteCardServiceCommandTest"
                }
            };
            if (initDeleteCardRecord)
            {
                //record 1
                result.Requests.Add(
                new DeleteCardRecord
                {
                    AccountIdentifier = _creditCardIdentifier1,
                });
                //record2
                result.Requests.Add(
                new DeleteCardRecord
                {
                    AccountIdentifier = _creditCardIdentifier2,
                });
            }
            return result;
        }
        private bool? IsDeletedCreditCard(string cardIdentifier)
        {
            Guid creditCardID = new CreditCardIdentifier(cardIdentifier).PersistableID;
            CreditCardAccount creditCardAcount = CreditCardAccount.RetrieveCreditCardAccountByID(creditCardID);
            if (creditCardAcount != null && creditCardAcount.MarkedForDeletion == true && creditCardAcount.Status == Entity.AccountStatus.DoNotAllowMoneyMovement)
                return true;
            else if (creditCardAcount != null && creditCardAcount.MarkedForDeletion == false && creditCardAcount.Status == Entity.AccountStatus.AllowMoneyMovement)
                return false;
            return null;
        }
        #endregion

    }
}
