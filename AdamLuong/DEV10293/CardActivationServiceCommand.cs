using Common.Contracts.Prepaid.Records;
using Common.Contracts.Prepaid.Requests;
using Common.Contracts.Prepaid.Responses;
using Common.Exceptions;
using Payjr.Configuration;
using Payjr.Core.FinancialAccounts;
using Payjr.Core.Identifiers;
using Payjr.Core.Providers;
using Payjr.Core.Services;
using Payjr.Core.Transactions;
using Payjr.Core.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payjr.Core.ServiceCommands.PrepaidCard
{
    public class CardActivationServiceCommand : ProviderServiceCommandBase<CardActivationRequest, CardActivationResponse>
    {
        public CardActivationServiceCommand(IProviderFactory providers) : base(providers) { }
        private PrepaidCardAccount _prepaidCardAcount;
        private string _actingUserIdentifier;
        private string _prepaidCardIdentifier;
        private string _ipAddress;
        private DateTime _dobForActivation;
        private Teen _teen;
        private Parent _parent;

        protected override bool OnExecute(CardActivationResponse response)
        {
            var responseRecord = new CardActivationRecord
            {
                ActingUserIdentifier = _actingUserIdentifier,
                CardIdentifier = _prepaidCardIdentifier
            };

            response.CardActivations.Add(responseRecord);

            var verificationData = new VerificationData(_dobForActivation);
            var activationSuccessful = false;
            
            try
            {
                _prepaidCardAcount.CardProvider = Providers.CreatePrepaidCardProvider(_parent.Site);
                activationSuccessful = _prepaidCardAcount.VerifyAccount(verificationData, null, null, null);
            }
            catch(Exception ex)
            {
                throw new ProcessingException(
                    string.Format(
                        "Could not active the prepaid card with ID={0} - Account number={1}. The cause is:{2}",
                        _prepaidCardAcount.AccountID,
                        _prepaidCardAcount.AccountNumberMasked,
                        ex.Message));
            }
            // set activation result for response
            responseRecord.ActivationSuccessful = activationSuccessful;
            // Record activation attempt
            ServiceFactory.ActivityService.CreatePrepaidActivationAttemptActivity(
                    _parent,
                    _teen,
                    _prepaidCardAcount.AccountNumber,
                    _dobForActivation,
                    activationSuccessful,
                    _parent.UserID
                );

            string logMsg = ("IpAddress=" + _ipAddress + "|ParentUserID=" + _parent.UserID.ToString() + "|TeenUserID=" + _teen.UserID.ToString() + "|DateTime=" + DateTime.UtcNow.ToString("yyy/MM/dd HH:mm:ss"));
            if(activationSuccessful)
            {
                logMsg +=  "|Step=CardActivateSuccess";
            }
            else
            {
                logMsg += ("|Step=CardActivateFailure" + "|DOBused=" + _dobForActivation.ToString("MM/dd/yyy"));
            }
            Log.Info(logMsg);

            // this is status of service command processing, but not activation result
            return true;
        }

        protected override void Validate(CardActivationRequest request)
        {
            if (request == null)
            {
                throw new ValidationException("request must not be null");
            }

            var cardActivationRecords = request.CardActivations;
            if (cardActivationRecords.Count == 0)
            {
                throw new ValidationException("request.CardActivations must not be null or empty");
            }
            var cardActivationRecord = cardActivationRecords[0];

            if (!DateTime.TryParse(cardActivationRecord.ActivationData, out _dobForActivation))
            {
                throw new ValidationException("request.CardActivations[0].ActivationData must be invalid DateTime value");
            }

            _prepaidCardIdentifier = cardActivationRecord.CardIdentifier;
            if (string.IsNullOrWhiteSpace(_prepaidCardIdentifier))
            {
                throw new ValidationException("request.CardActivations[0].CardIdentifier must not be null or empty");
            }
            Guid prepaidCardID = new Identifiers.PrepaidCardAccountIdentifier(_prepaidCardIdentifier).PersistableID;
            _prepaidCardAcount = PrepaidCardAccount.RetrievePrepaidCardAccountByID(prepaidCardID);
            if (_prepaidCardAcount == null)
            {
                throw new ValidationException(
                    string.Format(
                        "Could not found Prepaid Card with ID={0}",
                        prepaidCardID));
            }
            if (!_prepaidCardAcount.UserID.HasValue)
            {
                throw new ValidationException(
                    string.Format(
                        "Could not determine the user who associated with the Prepaid Card - ID={0}",
                        prepaidCardID));
            }

            // get teen info
            _teen = User.RetrieveUser(_prepaidCardAcount.UserID.Value) as Teen;
            if (_teen == null)
            {
                throw new Exception(
                    string.Format(
                        "Could not retrieve the User - ID={0}",
                        _prepaidCardAcount.UserID.Value));
            }

            // get parent info
            _actingUserIdentifier = cardActivationRecord.ActivatingUserIdentifier;
            _parent = _teen.Parent;
            if (_parent == null)
            {
                throw new Exception(
                    string.Format(
                        "Could not retrieve the Parent for Teen User - ID={0}",
                        _teen.UserID));
            }

            _ipAddress = cardActivationRecord.IPAddress;
        }
    }
}
