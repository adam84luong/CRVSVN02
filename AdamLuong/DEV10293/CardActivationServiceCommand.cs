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

        private List<CardActivationRequestRecord> _cardActivationRequestRecords;

        protected override bool OnExecute(CardActivationResponse response)
        {
            foreach (var cardActivationRecord in _cardActivationRequestRecords)
            {
                // preparing record response
                var responseRecord = new CardActivationRecord
                {
                    ActingUserIdentifier = cardActivationRecord.ActivatingUserIdentifier,
                    CardIdentifier = cardActivationRecord.CardIdentifier
                };

                // preparing data for activation
                PrepaidCardAccount prepaidCardAcount;
                Teen teen;
                Parent parent;

                if (!GetDataForActivation(cardActivationRecord.CardIdentifier, out prepaidCardAcount, out teen, out parent))
                {
                    continue;
                }

                var dobForActivation = DateTime.Parse(cardActivationRecord.ActivationData);
                var verificationData = new VerificationData(dobForActivation);
                var activationSuccessful = false;
                // this is for testing
                if (Providers.PrepaidCardProvider != null)
                {
                    prepaidCardAcount.CardProvider = Providers.PrepaidCardProvider;
                }
                activationSuccessful = prepaidCardAcount.VerifyAccount(verificationData, null, null, null);

                // set activation result for response
                responseRecord.ActivationSuccessful = activationSuccessful;
                // add processed record to response
                response.CardActivations.Add(responseRecord);

                // Record activation attempt
                ServiceFactory.ActivityService.CreatePrepaidActivationAttemptActivity(
                        parent,
                        teen,
                        prepaidCardAcount.AccountNumber,
                        dobForActivation,
                        activationSuccessful,
                        parent.UserID
                    );

                // Log the activation result
                LogActivationResult(
                    activationSuccessful,
                    cardActivationRecord.IPAddress,
                    parent.UserID,
                    teen.UserID,
                    prepaidCardAcount.AccountNumberMasked,
                    dobForActivation);
            }
            
            // this is status of service command processing, but not activation result
            return true;
        }

        protected override void Validate(CardActivationRequest request)
        {
            Log.Debug("Beginning validate the request");
            if (request == null)
            {
                throw new ValidationException("request must not be null");
            }

            if (request.CardActivations.Count == 0)
            {
                throw new ValidationException("request.CardActivations must not be empty");
            }

            _cardActivationRequestRecords = new List<CardActivationRequestRecord>();
            var pos = -1;
            var count = 0;
            foreach (var cardActivationRecord in request.CardActivations)
            {
                pos++;
                if (!PrepaidCardAccountIdentifier.IsValid(cardActivationRecord.CardIdentifier))
                {
                    Log.Debug("skiped request.CardActivations[{0}] because CardIdentifier is not valid Identifier", pos);
                    continue;
                }

                DateTime dobForActivation;
                if (!DateTime.TryParse(cardActivationRecord.ActivationData, out dobForActivation))
                {
                    Log.Debug("skiped request.CardActivations[{0}] because ActivationData = {1} is invalid DateTime value",
                        pos, cardActivationRecord.ActivationData);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(cardActivationRecord.ActivatingUserIdentifier))
                {
                    Log.Debug("skiped request.CardActivations[{0}] because ActivatingUserIdentifier is null or empty", pos);
                    continue;
                }
                
                _cardActivationRequestRecords.Add(cardActivationRecord);
                count++;
            }

            Log.Debug("Ending validate the request. {0}/{1} record(s) passed validation", count, request.CardActivations.Count);
        }

        #region helper methods

        private bool GetDataForActivation(string cardIdentifier, out PrepaidCardAccount prepaidCardAcount, out Teen teen, out Parent parent)
        {
            prepaidCardAcount = null;
            teen = null;
            parent = null;

            try
            {
                Guid prepaidCardID = new PrepaidCardAccountIdentifier(cardIdentifier).PersistableID;
                prepaidCardAcount = PrepaidCardAccount.RetrievePrepaidCardAccountByID(prepaidCardID);
                if (prepaidCardAcount == null)
                {
                    Log.Debug("Could not found Prepaid Card with ID={0}", prepaidCardID);
                    return false;
                }
                if (!prepaidCardAcount.UserID.HasValue)
                {
                    Log.Debug("Could not determine the user who associated with the Prepaid Card - ID={0}", prepaidCardID);
                    return false;
                }
                // get teen info
                teen = User.RetrieveUser(prepaidCardAcount.UserID.Value) as Teen;
                if (teen == null)
                {
                    Log.Debug("Could not retrieve the User - ID={0}", prepaidCardAcount.UserID.Value);
                    return false;
                }
                // get parent info
                parent = teen.Parent;
                if (parent == null)
                {
                    Log.Debug("Could not retrieve the Parent for Teen User - ID={0}", teen.UserID);
                    return false;
                }
            }
            catch(Exception ex)
            {
                Log.Debug(ex.Message);
                return false;
            }
            return true;
        }

        private void LogActivationResult(
            bool isSuccessful,
            string ipAddress,
            Guid parentId,
            Guid teenId,
            string accountNumber,
            DateTime dobForActivation)
        {
            string logMsg = string.Format(
                "IpAddress={0}|ParentUserID={1}|TeenUserID={2}|AccountNumber={3}|DateTime={4}",
                ipAddress,
                parentId,
                teenId,
                accountNumber,
                DateTime.UtcNow.ToString("yyy/MM/dd HH:mm:ss"));

            if (isSuccessful)
            {
                logMsg += "|Step=CardActivateSuccess";
            }
            else
            {
                logMsg += ("|Step=CardActivateFailure" + "|DOBused=" + dobForActivation.ToString("MM/dd/yyy"));
            }
            Log.Debug(logMsg);
        }

        #endregion
    }
}
