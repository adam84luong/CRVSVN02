using Common.Contracts.CreditCard.Records;
using Common.Contracts.CreditCard.Requests;
using Common.Contracts.CreditCard.Responses;
using Payjr.Core.FinancialAccounts;
using Payjr.Core.Identifiers;
using Payjr.Core.Providers;
using Payjr.Core.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payjr.Core.ServiceCommands.CreditCardProcessing
{
    public class DeleteCardServiceCommand:ProviderServiceCommandBase<DeleteCardRequest, DeleteCardResponse>
    {
        public DeleteCardServiceCommand(IProviderFactory providers) : base(providers) { }
        private List<DeleteCardRecord> _deleteCardRecords;
        protected override bool OnExecute(DeleteCardResponse response)
        {
            foreach (var deleteCardRecord in _deleteCardRecords)
            {
                // preparing record response
                var responseRecord = new DeleteCardResponseRecord
                {
                    AccountIdentifier = deleteCardRecord.AccountIdentifier
                };

                User ownerUser= null;
                CreditCardAccount creditCardAccount = this.RetrieveCreditCardAccountByID(deleteCardRecord.AccountIdentifier, out ownerUser);
                if (creditCardAccount == null)
                {
                    continue;
                }
                var deletedSuccessful = creditCardAccount.DeleteAccount();
                // set flag is deleted/undeleted for response
                if (deletedSuccessful)
                {
                    deletedSuccessful = creditCardAccount.Save(null);
                    if (deletedSuccessful)
                        deletedSuccessful = ownerUser.Save(null);
                }
                responseRecord.IsDeleted = deletedSuccessful;
                // add processed record to response
                response.Respones.Add(responseRecord);
            }
            return true;
        }

        protected override void Validate(DeleteCardRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException("request", "request must be set value");
            }

            var deleteCardRecords = request.Requests;
            if (deleteCardRecords == null || deleteCardRecords.Count == 0)
            {
                throw new ArgumentException("DeleteCardsRecords must be set", "request.AddCardsRecords");
            }
            _deleteCardRecords = new List<DeleteCardRecord>();
            var pos = -1;
            var count = 0;
            foreach (var deleteCard in deleteCardRecords)
            {
                pos++;
                if (string.IsNullOrWhiteSpace(deleteCard.AccountIdentifier))
                {
                    Log.Debug("skiped request.DeleteCardRecords[{0}] because AccountIdentifier is null or empty", pos);
                    continue;
                }
                _deleteCardRecords.Add(deleteCard);
                count++;
            }
            //if (count < deleteCardRecords.Count)
            Log.Debug("Ending validate the request. {0}/{1} record(s) passed validation", count, deleteCardRecords.Count);
        }

        #region helper
        private CreditCardAccount RetrieveCreditCardAccountByID(string cardIdentifier, out User userOwner)
        {
            CreditCardAccount creditCardAcount = null;
            userOwner = null;
            try
            {
                Guid creditCardID = new CreditCardIdentifier(cardIdentifier).PersistableID;
                creditCardAcount = CreditCardAccount.RetrieveCreditCardAccountByID(creditCardID);
                if (creditCardAcount == null)
                {
                    Log.Debug("Could not found Credit Card with ID={0}", creditCardID);
                    return null;
                }
                if (!creditCardAcount.UserID.HasValue)
                {
                    Log.Debug("Could not determine the user who associated with the credit card - ID={0}", creditCardID);
                    return null;
                }
                // get teen info
                userOwner = User.RetrieveUser(creditCardAcount.UserID.Value);
                if (userOwner == null)
                {
                    Log.Debug("Could not retrieve the User - ID={0}", creditCardAcount.UserID.Value);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex.Message);
                return null;
            }
            return creditCardAcount;
        }
        #endregion
    }
}
