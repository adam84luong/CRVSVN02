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
using Payjr.Entity.DatabaseSpecific;
using Payjr.Core.Adapters;
using Payjr.Core.Configuration;


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
            CustomCardDesign cardDesign;
            string sku;

            foreach (SendToFulfillmentRecord record in _fulfillmentRecords)
            {
                user = new IDENTIFIERS.UserIdentifier(record.UserIdentifier).ID;
                teen = User.RetrieveUser(user) as Teen;

                prepaidAccount = teen.NewPrepaidCardAccount();
                prepaidAccount.IsActive = true;// phai co thi moi luu xuong DB thanh cong cac bang CustomCardDesign, CustomCardDesignUser, Job, PrepaidCardAccount, PrepaidCardAccountUser
  
                using (DataAccessAdapter adapter = new DataAccessAdapter(true))
                {
                    foreach (ProductFulfillmentLineItem lineitem in record.ProductLineItems)
                        foreach (UpdateProductFulfillmentRecord updaterecord in lineitem.ProductRecords)
                            if (updaterecord.ProductCode == SystemConfiguration.CardProductCode)
                            {
                                sku = updaterecord.Value;
                                var custCardDgns = AdapterFactory.CardDesignsDataAdapter.RetrieveCardDesignRegistration(adapter, sku);
                                if (custCardDgns == null)
                                {
                                    cardDesign = teen.NewCustomCardDesign();
                                    cardDesign.Creator = RoleType.Parent;
                                    cardDesign.SetDesign(sku);
                                    prepaidAccount.CustomCardDesignID = cardDesign.CustomCardDesignID;
                                }
                                else
                                    prepaidAccount.CustomCardDesignID = custCardDgns.CustomCardDesignId;
                               
                                 bool success1 = teen.Save(null);// da co create job va luu xuong db

                                response.ResponseRecords.Add(record);
                            }
                }
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

                if (string.IsNullOrWhiteSpace(record.UserIdentifier))
                {
                    Log.Debug("skiped request.RequestRecords[{0}] because UserIdentifier must not be null or empty", pos);
                    continue;
                }

                if (productFulfillmentLineItems.Count == 0)
                {
                    Log.Debug("skiped request.RequestRecords[{0}] because ProductLineItems must be set", pos);
                    continue;
                }
                else
                    foreach(var lineitem in productFulfillmentLineItems)
                        foreach(var product in lineitem.ProductRecords)
                            if (product.ProductCode == SystemConfiguration.CardProductCode)
                            {
                                _fulfillmentRecords.Add(record);
                                count++;
                                break;
                            }
            }
            Log.Debug("Ending validate the request. {0}/{1} record(s) passed validation", count, request.RequestRecords.Count);
        }
    }
}
