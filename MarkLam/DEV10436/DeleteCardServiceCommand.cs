using Common.Business.Validation;
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
                CreditCardAccount creditCardAccount =null;
                try
                {
                     Guid creditCardID = new CreditCardIdentifier(deleteCardRecord.AccountIdentifier).PersistableID;
                     creditCardAccount = CreditCardAccount.RetrieveCreditCardAccountByID(creditCardID, out ownerUser);
                }
                catch(Exception ex)
                {
                    Log.Debug(ex.Message);
                }
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
                throw new ValidationException("request must be set value");
            }

            var deleteCardRecords = request.Requests;
            if (deleteCardRecords == null || deleteCardRecords.Count == 0)
            {
                throw new ValidationException("DeleteCardsRecords must be set");
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
       
    }
}
