using Common.Contracts.OrderProcessing.Records;
using Common.Contracts.ProductFulfillment.Records;
using Common.Contracts.ProductFulfillment.Requests;
using Common.Contracts.ProductFulfillment.Responses;
using Payjr.Core.Providers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Payjr.Core.ServiceCommands.ProductFulfillment
{
    public class SendToFulfillmentServiceCommand : ProviderServiceCommandBase<SendToFulfillmentRequest,SendToFulfillmentResponse>
    {
        SendToFulfillmentRecord fulfillmentRecords = new SendToFulfillmentRecord();

        public SendToFulfillmentServiceCommand(IProviderFactory providers) : base(providers) { }

        protected override bool OnExecute(SendToFulfillmentResponse response)
        {
			return true;// nho Adam review
        }

        protected override void Validate(SendToFulfillmentRequest request)
        {
			if (request == null)
            {
                throw new ArgumentNullException("request", "request must be set");
            }
            if (request.Configuration == null)
            {
                throw new ArgumentException("request.Configuration must be set", "request");
            }
            if (request.Configuration.ApplicationKey == null)
            {
                throw new ArgumentException("request.Configuration.ApplicationKey must be set", "request");
            }
            if (request.Header == null)
            {
                throw new ArgumentException("request.Header must be set", "request");
            }
            if (request.Header.CallerName == null)
            {
                throw new ArgumentException("request.Header.CallerName must be set", "request");
            }

            if (request.RequestRecords == null)
            {
                throw new ArgumentException("request.RequestRecords must be set", "request");
            }
            if (request.RequestRecords.Count <= 0)
            {
                throw new ArgumentException("request.RequestRecords must have one record", "request");
            }
            SendToFulfillmentRecord fulfillmentRecord = request.RequestRecords[0];
            if (String.IsNullOrWhiteSpace(fulfillmentRecord.CustomerType) == null)
            {
                throw new ArgumentException("request.RequestRecords[0].CustomerType must be set", "request");
            }
            if (String.IsNullOrWhiteSpace(fulfillmentRecord.Ref1) == null)
            {
                throw new ArgumentException("request.RequestRecords[0].Ref1 must be set", "request");
            } 
            if (String.IsNullOrWhiteSpace(fulfillmentRecord.UserIdentifier) == null)
            {
                throw new ArgumentException("request.RequestRecords[0].UserIdentifier must be set", "request");
            }

            if (fulfillmentRecord.ProductLineItems == null)
            {
                throw new ArgumentException("request.RequestRecords[0].ProductLineItems must be set", "request");
            }
            if(fulfillmentRecord.ProductLineItems.Count <= 0)
            {
                throw new ArgumentException("request.RequestRecords[0].ProductLineItems must have record", "request");
            }
            foreach (ProductFulfillmentLineItem item in fulfillmentRecord.ProductLineItems)
            {
                if (item.Configuration == null)
                {
                    throw new ArgumentException("request.RequestRecords[0].ProductLineItems.Configuration must be set", "request");
                }
                if (String.IsNullOrWhiteSpace(item.LineItemIdentifier))
                {
                    throw new ArgumentException("request.RequestRecords[0].ProductLineItems.LineItemIdentifier must be set", "request");
                }
                if (item.ProductRecords == null)
                {
                    throw new ArgumentException("request.RequestRecords[0].ProductLineItems.ProductRecords must be set", "request");
                }
            }

            if (fulfillmentRecord.ShipmentPackaging == null)
            {
                throw new ArgumentException("request.RequestRecords[0].ShipmentPackaging must be set", "request");
            }
            if(fulfillmentRecord.ShipmentPackaging.Count <= 0)
            {
                throw new ArgumentException("request.RequestRecords[0].ShipmentPackaging must have record", "request");
            }

            foreach (ShipmentPackaging item in fulfillmentRecord.ShipmentPackaging)
            {
                if (item.ShippingMethod == null)
                {
                    throw new ArgumentException("request.RequestRecords[0].ShipmentPackaging.ShippingMethod must be set", "request");
                }
                if (item.LineItemBundles == null)
                {
                    throw new ArgumentException("request.RequestRecords[0].ShipmentPackaging.LineItemBundles must be set", "request");
                }
            }

            if (fulfillmentRecord.TransactionRecords == null)
            {
                throw new ArgumentException("request.RequestRecords[0].TransactionRecords must be set", "request");
            }
            if(fulfillmentRecord.TransactionRecords.Count <= 0)
            {
                throw new ArgumentException("request.RequestRecords[0].TransactionRecords must have record", "request");
            }
            foreach (Common.Contracts.ProductFulfillment.Records.TransactionRecord item in fulfillmentRecord.TransactionRecords)
            {
                if (item.Amount < 0)
                {
                    throw new ArgumentException("request.RequestRecords[0].TransactionRecords.Amount must be positive or zero ", "request");
                }
                if(item.PaymentType == null)
                {
                    throw new ArgumentException("request.RequestRecords[0].TransactionRecords.PaymentType must be set", "request");
                }
                if(item.TransactionIdentifier == null)
                {
                    throw new ArgumentException("request.RequestRecords[0].TransactionRecords.TransactionIdentifier must be set", "request");
                }
            }

            fulfillmentRecords = request.RequestRecords[0];
        }
    }
}
