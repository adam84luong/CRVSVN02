using Common.Contracts.OrderProcessing.Records;
using Common.Contracts.ProductFulfillment.Records;
using Common.Contracts.ProductFulfillment.Requests;
using Common.Contracts.ProductFulfillment.Responses;
using Payjr.Core.FinancialAccounts;
using IDENTIFIERS = Payjr.Core.Identifiers;
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
using Payjr.Core.Jobs;
using Payjr.Core.UserInfo;
using Payjr.Entity;


namespace Payjr.Core.ServiceCommands.ProductFulfillment
{
    public class SendToFulfillmentServiceCommand : ProviderServiceCommandBase<SendToFulfillmentRequest,SendToFulfillmentResponse>
    {
        private List<SendToFulfillmentRecord> _fulfillmentRecords;

        public SendToFulfillmentServiceCommand(IProviderFactory providers) : base(providers) { }

        protected override bool OnExecute(SendToFulfillmentResponse response)
        {
            Teen teen;
            Guid user;
            PrepaidCardAccount prepaidAccount;
            CreateCardJob createCardJob;
            CustomCardDesign cardDesign;

            foreach (SendToFulfillmentRecord record in _fulfillmentRecords)
            {
                user = new IDENTIFIERS.UserIdentifier(record.UserIdentifier).ID;
                teen = User.RetrieveUser(user) as Teen;
                bool success1 = teen.Save(null);

                cardDesign = teen.NewCustomCardDesign();
                cardDesign.Creator = RoleType.RegisteredTeen;
                cardDesign.SetDesign("ServerNew");
                bool success2 = cardDesign.Save(null);

                prepaidAccount = teen.NewPrepaidCardAccount();
                prepaidAccount.FulfillmentDateSent = DateTime.Now;
                prepaidAccount.IssueDate = DateTime.Now;
                prepaidAccount.IsActive = true;
                prepaidAccount.CustomCardDesignID = cardDesign.CustomCardDesignID;
                bool success3 = prepaidAccount.Save(null);

                createCardJob = (CreateCardJob)Job.RetrieveJob(prepaidAccount.CardCreateJob.JobID); 
                createCardJob.SaveJob(DateTime.Now);


                response.ResponseRecords.Add(record);
            }
            
            return true;
        }

        protected override void Validate(SendToFulfillmentRequest request)
        {
            Log.Debug("Beginning validate the request");
            if (request == null)
            {
                throw new ValidationException("request must be set");
            }
            
            var sendToFulfillmentRecords = request.RequestRecords;
            if (sendToFulfillmentRecords.Count == 0)
            {
                throw new ValidationException("request.RequestRecords must not be null or empty");
            }

            _fulfillmentRecords = new List<SendToFulfillmentRecord>();
            var pos = -1;
            var count = 0;
            foreach (SendToFulfillmentRecord record in sendToFulfillmentRecords)
            {
                pos++;
                var productFulfillmentLineItems = record.ProductLineItems;
                if (productFulfillmentLineItems.Count == 0)
                {
                    Log.Debug("skiped request.RequestRecords[{0}] because ProductLineItems must be set", pos);
                    continue;
                }
                if (string.IsNullOrWhiteSpace(record.UserIdentifier))
                {
                    Log.Debug("skiped request.RequestRecords[{0}] because UserIdentifier must not be null or empty", pos);
                    continue;
                }
                _fulfillmentRecords.Add(record);
                count++;
            }
            Log.Debug("Ending validate the request. {0}/{1} record(s) passed validation", count, request.RequestRecords.Count);
        }
    }
}
