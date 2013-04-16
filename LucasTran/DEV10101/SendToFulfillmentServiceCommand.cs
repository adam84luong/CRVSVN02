using Common.Contracts.OrderProcessing.Records;
using Common.Contracts.ProductFulfillment.Records;
using Common.Contracts.ProductFulfillment.Requests;
using Common.Contracts.ProductFulfillment.Responses;
using Payjr.Core.FinancialAccounts;
using Payjr.Core.Identifiers;
using Payjr.Core.Providers;
using Payjr.Core.Users;
using SERVICES = Payjr.Core.Services.UserService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Business.Validation;
using Payjr.Types;
using Payjr.Core.Services;


namespace Payjr.Core.ServiceCommands.ProductFulfillment
{
    public class SendToFulfillmentServiceCommand : ProviderServiceCommandBase<SendToFulfillmentRequest,SendToFulfillmentResponse>
    {
        List<SendToFulfillmentRecord> _fulfillmentRecords = new List<SendToFulfillmentRecord>();

        public SendToFulfillmentServiceCommand(IProviderFactory providers) : base(providers) { }

        protected override bool OnExecute(SendToFulfillmentResponse response)
        {
            //Payjr.Entity.EntityClasses.UserEntity
            Teen teen;
            Guid user;
            PrepaidCardAccount prepaidAccount;
            foreach (SendToFulfillmentRecord record in _fulfillmentRecords)
            {
                user = new UserIdentifier(record.UserIdentifier).ID;
                teen = User.RetrieveUser(user) as Teen;
                //ServiceFactory.UserConfiguration.AssignProductToUser(Product.PPaid_IC, teen);
                prepaidAccount = teen.NewPrepaidCardAccount();
                //prepaidAccount.IsActive = true;
                teen.Save(null);
                response.ResponseRecords.Add(record);
            }
            
            return true;
        }

        protected override void Validate(SendToFulfillmentRequest request)
        {
            if (request == null)
            {
                throw new ValidationException("request must be set");
            }
            
            var sendToFulfillmentRecords = request.RequestRecords;
            if (sendToFulfillmentRecords.Count == 0)
            {
                throw new ValidationException("request.RequestRecords must not be null or empty");
            }
            SendToFulfillmentRecord r = new SendToFulfillmentRecord();

            int i = 0;
            foreach (SendToFulfillmentRecord record in sendToFulfillmentRecords)
            {
                var productFulfillmentLineItems = record.ProductLineItems;
                if (productFulfillmentLineItems.Count == 0)
                {
                    throw new ValidationException(string.Format("request.RequestRecords[{0}].ProductLineItems must not be null or empty", i));
                }
                if (string.IsNullOrWhiteSpace(record.UserIdentifier))
                {
                    throw new ArgumentException(string.Format("request.RequestRecords[{0}].UserIdentifier must be set",i));
                }
                i++;
                _fulfillmentRecords.Add(record);
            }
        }
    }
}
