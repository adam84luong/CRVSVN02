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
            CustomCardDesign cardDesign = null;
            string sku;
            
            foreach (SendToFulfillmentRecord record in _fulfillmentRecords)
            {
                user = new IDENTIFIERS.UserIdentifier(record.UserIdentifier).ID;
                teen = User.RetrieveUser(user) as Teen;   

                if (teen != null)
                {
                    prepaidAccount = teen.NewPrepaidCardAccount();
                    if (prepaidAccount != null)
                    {
                        prepaidAccount.IsActive = true;
                        foreach (ProductFulfillmentLineItem lineitem in record.ProductLineItems)
                            foreach (UpdateProductFulfillmentRecord updaterecord in lineitem.ProductRecords)
                                if (updaterecord.ProductCode == SystemConfiguration.CardProductCode)
                                {
                                    sku = updaterecord.Value;
                                    var customCardDesign = CustomCardDesign.RetrieveCardDesignByServerSideId(sku);
                                    if (customCardDesign == null)
                                    {
                                        cardDesign = teen.NewCustomCardDesign();
                                        cardDesign.Creator = RoleType.RegisteredTeen;
                                        cardDesign.SetDesign(sku);
                                        prepaidAccount.CustomCardDesignID = cardDesign.CustomCardDesignID;
                                    }
                                    else
                                        prepaidAccount.CustomCardDesignID = customCardDesign.CustomCardDesignId;
                                 }
          
                    }
                    if (teen.Save(null))
                        response.ResponseRecords.Add(record);
                    else
                        Log.Error("Error saving teen with UserIdentifier: " + record.UserIdentifier);     
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
                {
                    _fulfillmentRecords.Add(record);
                    count++;
                }
                
            }
            Log.Debug("Ending validate the request. {0}/{1} record(s) passed validation", count, request.RequestRecords.Count);
        }
    }
}
