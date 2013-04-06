#region Copyright PAYjr Inc. 2005-2013
//
// All rights are reserved. Reproduction in whole or in part, in any 
// form or by any means, electronic, mechanical or otherwise, is    
// prohibited  without the prior written consent of the copyright owner.
//
#endregion

using System;
using System.Collections.Generic;
using Aspose.iCalendar;
using Common.Business;
using Common.FSV.WebService;
using Common.Logging;
using Common.Scheduling;
using Common.Types;
using Common.Util;
using NLog;
using Payjr.Core.Adapters;
using Payjr.Core.FinancialAccounts.Interfaces;
using Payjr.Core.FinancialAccounts.Operations;
using Payjr.Core.FSV.Transactions;
using Payjr.Core.Jobs;
using Payjr.Core.Providers;
using Payjr.Core.Services;
using Payjr.Core.Transactions;
using Payjr.Core.Transactions.Graphs;
using Payjr.Core.UserInfo;
using Payjr.Core.Users;
using Payjr.Core.Utils;
using Payjr.Entity;
using Payjr.Entity.EntityClasses;
using Payjr.Entity.HelperClasses;
using Payjr.Types;
using Payjr.Core.Journal;
using Payjr.Core.ScheduledItems;
using System.Text;

namespace Payjr.Core.FinancialAccounts
{
    public class PrepaidCardAccount : BusinessEntityChild<PrepaidCardAccountEntity>, IDestinationAccount
    {
        private static Logger Log = LogManager.GetCurrentClassLogger();

        #region Fields

        private PrepaidCardAccountEntity _prepaidCardAccountEntity;
        private BrandingCardDesignEntity _brandingCardDesignEntity;
        private CustomCardDesign _customCardDeisgn;
        private CreateCardJob _cardCreateJob;
        private decimal? _cardBalance;
        private DateTime? _lastTransDate;
        private ICardProvider _cardProvider = null;
        private DateTime? _expirationDate = null;
        private string _fsvCustomerID = null;

        //Post Save Bits
        private bool _activationSuccessful;
        private bool _newAccount;
        private bool _accountSetToActive;
        private bool _accountSetToInActive;
        private bool _accountStatusChanged;
        private bool _updateProviderStatus;
        private bool _accountDeleted;
     

        #endregion //Fields

        #region Error Msgs

        private const string ERROR_ACTIVATE_CARD = "Call to FSV to Activate Card failed.  Prepaid Card Account: {0}. User: {1}";
        private const string ERROR_JOURNAL_ENTRY_CREATION = "Failed to create Journal Entry. Prepaid Card Account: {0}. User: {1}";
        private const string ERROR_CANCEL_GRAPH = "Failed to cancel graph for Job {0}. Prepaid Card Account: {1}. User: {2}";

        #endregion //Error Msgs

        #region Properties

        /// <summary>
        /// Gets/Sets the date the account was sent off for fulfillment
        /// </summary>
        public DateTime? FulfillmentDateSent
        {
            get
            {
                return _prepaidCardAccountEntity.FulfillmentDateSent;
            }
            set
            {
                _prepaidCardAccountEntity.FulfillmentDateSent = value;
            }
        }

        /// <summary>
        /// Gets/Sets the date the account was issued by fulfillment
        /// </summary>
        public DateTime? IssueDate
        {
            get
            {
                return _prepaidCardAccountEntity.IssueDate;
            }
            set
            {
                _prepaidCardAccountEntity.IssueDate = value;
            }
        }

        /// <summary>
        /// Gets whether or not the account is pending a replacement
        /// </summary>
        public bool IsPendingReplacement
        {
            get
            {
                return _prepaidCardAccountEntity.PendingReplacement;
            }
            private set
            {
                _prepaidCardAccountEntity.PendingReplacement = value;
            }
        }

        /// <summary>
        /// Gets or sets the activate date.
        /// </summary>
        /// <value>The activate date.</value>
        public DateTime? ActivateDate
        {
            get
            {
                return _prepaidCardAccountEntity.ActiveteDateTime;
            }
            set
            {
                _prepaidCardAccountEntity.ActiveteDateTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the lost stolen date.
        /// </summary>
        /// <value>The lost stolen date.</value>
        public DateTime? LostStolenDate
        {
            get
            {
                return _prepaidCardAccountEntity.LostStolenDateTime;
            }
            set
            {
                _prepaidCardAccountEntity.LostStolenDateTime = value;
            }
        }

        /// <summary>
        /// Gets or sets the activation method.
        /// </summary>
        /// <value>The activation method.</value>
        public PrepaidActivationMethod? ActivationMethod
        {
            get
            {
                return _prepaidCardAccountEntity.ActivationMethod;
            }
            set
            {
                _prepaidCardAccountEntity.ActivationMethod = value;
            }
        }

        /// <summary>
        /// Gets or sets the branding card design ID.
        /// </summary>
        /// <value>The branding card design ID.</value>
        public Guid? BrandingCardDesignID
        {
            get
            {
                return _prepaidCardAccountEntity.BrandingCardDesignId;
            }
            set
            {
                _prepaidCardAccountEntity.BrandingCardDesignId = value;
            }
        }

        /// <summary>
        /// Gets the branding card design.
        /// </summary>
        /// <value>The branding card design.</value>
        public BrandingCardDesignEntity BrandingCardDesign
        {
            get
            {
                if (BrandingCardDesignID.HasValue && _brandingCardDesignEntity == null)
                {
                    Error error;
                    ServiceFactory.BrandingService.RetrieveBrandingCardDesign(BrandingCardDesignID.Value, out _brandingCardDesignEntity, out error);
                }
                return _brandingCardDesignEntity;
            }
        }

        /// <summary>
        /// Gets or sets the custom card design ID.
        /// </summary>
        /// <value>The custom card design ID.</value>
        public Guid? CustomCardDesignID
        {
            get
            {
                return _prepaidCardAccountEntity.UserCardDesignId;
            }
            set
            {
                _prepaidCardAccountEntity.UserCardDesignId = value;
            }
        }

        /// <summary>
        /// Gets the custom card design.
        /// </summary>
        /// <value>The custom card design.</value>
        public CustomCardDesign CustomCardDesign
        {
            get
            {
                if (CustomCardDesignID.HasValue && _customCardDeisgn == null)
                {
                    Teen teen = base.BusinessParent as Teen;
                    if (teen != null)
                    {
                        foreach (CustomCardDesign cardDesign in teen.CustomCardDesigns)
                        {
                            if (cardDesign.CustomCardDesignID == CustomCardDesignID.Value)
                            {
                                _customCardDeisgn = cardDesign;
                                break;
                            }
                        }
                    }
                }
                return _customCardDeisgn;
            }
        }

        /// <summary>
        /// Gets the card design URL.
        /// </summary>
        /// <value>The card design URL.</value>
        public string CardDesignUrl
        {
            get
            {
                if (CustomCardDesignID.HasValue)
                {
                    if (CustomCardDesign.Picture != null)
                    {
                        return CustomCardDesign.Picture.PictureURL;
                    }
                }
                else if (BrandingCardDesignID.HasValue)
                {
                    if (BrandingCardDesign.StockCardDesign != null && BrandingCardDesign.StockCardDesign.PictureId.HasValue)
                    {
                        UrlParameterPasser configParams = new UrlParameterPasser();
                        configParams["PictureID"] = BrandingCardDesign.StockCardDesign.PictureId.Value.ToString();
                        return configParams.CreateQueryLink("~/brandingTheme/images/ImageViewer.ashx");
                    }
                }

                return Common.Util.Utils.FixupUrl("~/brandingTheme/images/nonBrandedImages/processing.png");

            }
        }

        /// <summary>
        /// Gets or sets the card number.
        /// </summary>
        /// <value>The card number.</value>
        public string CardNumber
        {
            get
            {
                if (_prepaidCardAccountEntity.CardNumber != null)
                {
                    return _prepaidCardAccountEntity.CardNumber.ToString();
                }
                return string.Empty;
            }
            set
            {
                _prepaidCardAccountEntity.CardNumber = value;
            }
        }



        /// <summary>
        /// Gets the card number masked.
        /// </summary>
        /// <value>The card number masked.</value>
        public string CardNumberMasked
        {
            get
            {
                return Config.AccountNumber(CardNumber);
            }
        }

        /// <summary>
        /// Gets or sets the card identifier.
        /// </summary>
        /// <value>The card identifier.</value>
        public Byte[] CardIdentifier
        {
            get
            {
                return _prepaidCardAccountEntity.CardIdentifier;
            }
            set
            {
                _prepaidCardAccountEntity.CardIdentifier = value;
            }
        }

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <value>The status.</value>
        public PrepaidCardStatus Status
        {
            get
            {
                return _prepaidCardAccountEntity.Status;
            }
        }

        /// <summary>
        /// Gets the card create job.
        /// </summary>
        /// <value>The card create job.</value>
        public CreateCardJob CardCreateJob
        {
            get
            {
                if (_cardCreateJob == null)
                {
                    JobEntity cardCreateJob = AdapterFactory.FinancialAccountDataAdapter.RetrieveCardCreateJobForAccount(AccountID);
                    if (cardCreateJob != null)
                        _cardCreateJob = (CreateCardJob)Job.RetrieveJob(cardCreateJob);
                }
                return _cardCreateJob;
            }
        }

        /// <summary>
        /// Gets the business parent as a user.
        /// </summary>
        /// <value>The business parent as a user.</value>
        private User BusinessParentUser
        {
            get
            {
                return base.BusinessParent as User;
            }
        }

        public PrepaidCardStatus FSVStatus
        {
            get
            {
                PrepaidCardStatus fsvStatus;
                string description;
                string lastFourOrBalanceOrSomething;
                Error error;
                if (CardProvider.GetStatus(this, out fsvStatus, out description, out lastFourOrBalanceOrSomething, out error))
                {
                    return fsvStatus;
                }
                return PrepaidCardStatus.Unknown;
            }
        }

        /// <summary>
        /// Gets or sets the card provider.
        /// </summary>
        /// <value>The card provider.</value>
        public ICardProvider CardProvider
        {
            get
            {
                if (_cardProvider == null)
                {
                    _cardProvider = BusinessParentUser.CardProvider;
                }
                return _cardProvider;
            }
            set
            {
                _cardProvider = value;
            }
        }

        /// <summary>
        /// Confirms account type supports fee transaction
        /// </summary>
        public bool SupportsFeeTransfer
        {
            get
            {
                // Type of money movement depends on card type:
                //   MasterCard == does not support fee transfer
                //   Visa == supports fee transfer
                return CardNumber.StartsWith("4");
            }
        }

        #endregion //Properties

        #region IFinancialAccount Members

        /// <summary>
        /// Active status of the Account
        /// </summary>
        /// <value></value>
        public bool IsActive
        {
            get
            {
                return _prepaidCardAccountEntity.Active;
            }
            set
            {
                _prepaidCardAccountEntity.Active = value;
                _accountSetToActive = value;
                _accountSetToInActive = !value;

                //Account Set to Active
                if (_accountSetToActive)
                {
                    //If this account was set to active, set all other accounts to inactive
                    if (BusinessParentUser != null)
                    {
                        foreach (PrepaidCardAccount cardAccount in BusinessParentUser.FinancialAccounts.PrepaidCardAccounts)
                        {
                            if (cardAccount.AccountID != this.AccountID)
                            {
                                cardAccount.IsActive = false;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Account ID
        /// </summary>
        /// <value></value>
        public Guid AccountID
        {
            get
            {
                return _prepaidCardAccountEntity.PrepaidCardAccountId;
            }
        }

        /// <summary>
        /// Set to true if the account is marked for deletion; otherwise, false
        /// </summary>
        /// <value></value>
        public bool MarkedForDeletion
        {
            get
            {
                return _prepaidCardAccountEntity.MarkedForDeletion;
            }
        }

        /// <summary>
        /// Gets the user ID
        /// </summary>
        /// <value></value>
        public Guid? UserID
        {
            get
            {
                if (BusinessParentUser != null)
                {
                    return BusinessParentUser.UserID;
                }
                return null;
            }
        }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        /// <value>The display name.</value>
        public string DisplayName
        {
            get
            {
                return CardNumberMasked;
            }
        }

        /// <summary>
        /// Determines if the account is in a pending state
        /// </summary>
        /// <value></value>
        public bool IsAccountPending
        {
            get
            {
                //Account is pending if the status of the account is pending
                return String.IsNullOrEmpty(CardNumber);
            }
        }

        /// <summary>
        /// Gets the account number
        /// </summary>
        public string AccountNumber
        {
            get { return CardNumber; }
        }

        /// <summary>
        /// Gets a "masked" account number for display purposes.  Most of the characters are hidden.
        /// </summary>
        public string AccountNumberMasked
        {
            get { return CardNumberMasked; }
        }

        /// <summary>
        /// Gets the load fee product.
        /// </summary>
        /// <param name="teen">The teen.</param>
        /// <returns></returns>
        public Product? GetLoadFeeProduct(Teen teen)
        {
            return null;
        }

        /// <summary>
        /// Gets the emergency load product.
        /// </summary>
        /// <param name="teen">The teen.</param>
        /// <returns></returns>
        public Product? GetEmergencyLoadProduct(Teen teen)
        {
            return null;
        }

        /// <summary>
        /// Creates a money movement job.
        /// </summary>
        /// <param name="amount">The amount of money to move.</param>
        /// <param name="billingAmounts">The billing amounts.</param>
        /// <param name="description">The description.</param>
        /// <param name="transactionType">Type of the transaction.</param>
        /// <param name="transactionDirection">The transaction direction.</param>
        /// <param name="transactionOccurrence">The transaction occurrence.</param>
        /// <param name="initialStatus">The initial status.</param>
        /// <param name="provider">The provider used to create the transaction.</param>
        /// <param name="moneyMovementJob">The transaction job created.</param>
        /// <param name="error">Error output</param>
        /// <returns>
        /// True if the job is created successfully.  False if the creation of the job fails
        /// </returns>
        public bool CreateMoneyMovementJob(
            decimal amount,
            List<BillingAmount> billingAmounts,
            string description,
            TransactionType transactionType,
            TransactionDirection transactionDirection,
            TransactionOccurrenceType transactionOccurrence,
            JobStatus initialStatus,
            IProvider provider,
            out TransactionJob moneyMovementJob,
            out Error error
            )
        {
            //Initialize output
            moneyMovementJob = null;
            error = null;

            //Make sure the provider is a card provider
            if (provider != null)
            {
                if (!(provider is ICardProvider)) throw new InvalidOperationException(string.Format("Cannot create a Prepaid Card Transaction with a {0} provider", provider != null ? provider.GetType().ToString() : "null"));
                CardProvider = (ICardProvider)provider;
            }

            //Validate Account
            if (BusinessParentUser == null) throw new InvalidOperationException("Error Creating Money Movement prepaid account " + AccountID + ".  Account must be associated with a user");

            if (!CanSupportTransfer())
            {
                //If this card load is for a challenge job or Initial Card Load then allow it
                if (transactionType != TransactionType.ChallengeDeposit && transactionType != TransactionType.InitialLoad)
                {
                    error = new Error(ErrorCode.BankNotValid);
                    return false;
                }
            }

            //create Job
            switch (transactionType)
            {
                case TransactionType.Reversal:
                    moneyMovementJob = new FSVReversalCardLoadJob(
                        BusinessParentUser,
                        null,
                        CardProvider.ReturnCounts,
                        transactionDirection,
                        transactionType,
                        amount,
                        billingAmounts,
                        description,
                        this,
                        CardProvider
                        );
                    break;
                case TransactionType.Monthly_Service_Fee:
                case TransactionType.Refund_Prcessing_Fee:
                case TransactionType.Customer_Service_Fee:
                case TransactionType.Shipping_Fee:
                    moneyMovementJob = new CardFeeJob(
                        BusinessParentUser,
                        null,
                        CardProvider.ReturnCounts,
                        transactionDirection,
                        transactionType,
                        amount,
                        billingAmounts,
                        description,
                        this,
                        CardProvider
                        );
                    break;
                default:
                    moneyMovementJob = new FSVCardTransactionJob(
                        BusinessParentUser,
                        null,
                        CardProvider.ReturnCounts,
                        transactionDirection,
                        transactionType,
                        amount,
                        null,
                        description,
                        this,
                        CardProvider
                        );
                    break;
            }

            moneyMovementJob.SaveNew();
            moneyMovementJob.SaveBillingAmounts();

            ServiceFactory.JobService.SetJobStatus(moneyMovementJob.JobID, initialStatus, out error);

            return true;
        }

        /// <summary>
        /// Gets the transactions for this account.
        /// </summary>
        /// <param name="startDate">The start date of the date range. Ex: 1/1/2007 would be the start date of the range 1/1/2007 - 12/31/2007</param>
        /// <param name="endDate">The end date of the date range. Ex: 12/31/2007 would be the end date of the range 1/1/2007 - 12/31/2007</param>
        /// <returns>
        /// List of Financial Transactions meeting the given criteria.
        /// </returns>
        public List<FinancialTransaction> RetrieveTransactions(DateTime startDate, DateTime endDate)
        {
            if (BusinessParentUser == null) { throw new InvalidOperationException("Error Creating Money Movement prepaid account " + AccountID + ".  Account must be associated with a user"); }

            EntityCollection<CardTransactionEntity> cardTransactions = AdapterFactory.FinancialAccountDataAdapter.RetrieveCardTransactionsByAccount(AccountID, startDate, endDate);
            FSVTransactionList transactionList = new FSVTransactionList(cardTransactions.Count);
            foreach (CardTransactionEntity trans in cardTransactions)
            {
                transactionList.Add(trans, BusinessParentUser.UserEntity as TeenEntity);
            }
            return transactionList.FinancialTransactions;
        }
        public List<FinancialTransaction> RetrieveTransactions(DateTime startDate, DateTime endDate, int pageNumber, int pageSize)
        {
            if (BusinessParentUser == null) { throw new InvalidOperationException("Error Creating Money Movement prepaid account " + AccountID + ".  Account must be associated with a user"); }

            EntityCollection<CardTransactionEntity> cardTransactions = AdapterFactory.FinancialAccountDataAdapter.RetrieveCardTransactionsByAccount(AccountID, startDate, endDate, pageNumber, pageSize);
            FSVTransactionList transactionList = new FSVTransactionList(cardTransactions.Count);
            foreach (CardTransactionEntity trans in cardTransactions)
            {
                transactionList.Add(trans, BusinessParentUser.UserEntity as TeenEntity);
            }
            return transactionList.FinancialTransactions;
        }

        /// <summary>
        /// Gets the available transfer counts based on the velocity limits for the account.
        /// </summary>
        /// <returns>
        /// The number of allowed transfer counts left for the account based on the velocity limits for the account
        /// </returns>
        public int GetAvailableTransferCount()
        {
            //Velocities have not been implemented for this account
            return int.MaxValue;
        }

        /// <summary>
        /// Gets the available transfer amount based on the velocity limits for the account.
        /// </summary>
        /// <returns>
        /// The remaining amount that can be transfer using this account based on the velocity limits for the account
        /// </returns>
        public decimal GetAvailableTransferAmount()
        {
            //Velocities have not been implemented for this account
            return decimal.MaxValue;
        }

        /// <summary>
        /// Determines whether this instance can support a transfer based on the account's status.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this instance can support a transfer; otherwise, <c>false</c>.
        /// </returns>
        public bool CanSupportTransfer()
        {
            //Do not allow transfers if the account has not been created
            if (string.IsNullOrEmpty(CardNumber))
            {
                return false;
            }

            return (
                IsActive &&
                Status != PrepaidCardStatus.Closed &&
                Status != PrepaidCardStatus.Replaced &&
                Status != PrepaidCardStatus.Unknown
                );
        }

        /// <summary>
        /// Determines whether this instance can support a transfer of the specified amount based on the accounts velocity limits.
        /// </summary>
        /// <param name="amount">The amount of the proposed transfer.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can support a transfer of the specified amount; otherwise, <c>false</c>.
        /// </returns>
        public bool CanSupportTransfer(decimal amount)
        {
            return CanSupportTransfer();
        }

        /// <summary>
        /// Determines whether this instance can support the given recurring transfer with the given amount and recurrence pattern based on the accounts velocity limits.
        /// </summary>
        /// <param name="amount">The amount of the recurring transfer.</param>
        /// <param name="pattern">The recurrence pattern of the recurring transfer.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can support the given recurring transfer; otherwise, <c>false</c>.
        /// </returns>
        public bool CanSupportTransfer(decimal amount, RecurrencePattern pattern)
        {
            //Velocities have not been implemented for this account
            return true;
        }

        /// <summary>
        /// Determines whether this instance can support a new threshold based transfer based on the account's velocity limits .
        /// </summary>
        /// <param name="amount">The amount of the new threshold based transfer.</param>
        /// <param name="pattern">The pattern of the new threshold based transfer.</param>
        /// <param name="threshold">The threshold of the user.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can support the new threshold based transfer; otherwise, <c>false</c>.
        /// </returns>
        public bool CanSupportTransfer(decimal amount, RecurrencePattern pattern, decimal threshold)
        {
            //Velocities have not been implemented for this account
            return true;
        }

        /// <summary>
        /// Determines whether this instance can support changing the given user's threshold to the given threshold based on the accounts velocity limits.
        /// </summary>
        /// <param name="newThreshold">The new threshold.</param>
        /// <param name="teen">The teen the new threshold will be applied to.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can support changing the given user's threshold to the given threshold; otherwise, <c>false</c>.
        /// </returns>
        public bool CanSupportThresholdChange(decimal newThreshold, Teen teen)
        {
            //Velocities have not been implemented for this account
            return true;
        }

        /// <summary>
        /// Verifies the account.
        /// </summary>
        /// <param name="verificationData">The verification data.</param>
        /// <returns>
        /// 	<c>true</c> if the account is successfully verified; otherwise, <c>false</c>.
        /// </returns>
        public bool VerifyAccount(VerificationData verificationData, IUserInfo destinationUser, IDestinationAccount destinationAccount, Guid? actingUser)
        {
            bool retVal = false;
            if (BusinessParentUser == null) { throw new InvalidOperationException("Error activating prepaid account " + AccountID.ToString() + ".  Account must be associated with a user"); }

            //Validate Input
            if (verificationData == null) { throw new ArgumentNullException("verificationData", "Verification data must be set"); }
            if (!verificationData.DOB.HasValue) { throw new ArgumentNullException("verificationData.DOB", "DOB must be set to activate prepaid account"); }

            //Validate DOB
            if (CardProvider.Type == ProviderType.FSVMetaBankProvider)
            {
                Teen teen = BusinessParentUser as Teen;
                if (teen.DOB == verificationData.DOB)
                {
                    UpdateStatus(PrepaidCardStatus.Good, true);
                    IsActive = true;
                    _activationSuccessful = true;
                    retVal = true;
                }
            }
            else
            {
                Parent parent = User.RetrieveUser((BusinessParentUser as Teen).ParentID) as Parent;
                if (parent.DOB == verificationData.DOB)
                {
                    UpdateStatus(PrepaidCardStatus.Good, true);
                    IsActive = true;
                    _activationSuccessful = true;
                    retVal = true;
                }
            }

            return retVal;
        }

        /// <summary>
        /// Creates the verification for the account.
        /// </summary>
        /// <param name="verificationData">The verification data.</param>
        /// <returns>
        /// 	<c>true</c> if the account verification is successfully created; otherwise, <c>false</c>.
        /// </returns>
        public bool CreateVerification(VerificationData verificationData)
        {
            //There is nothing to create
            return true;
        }

        /// <summary>
        /// Gets the accounts balance.
        /// </summary>
        /// <param name="cardProvider">The card provider.</param>
        /// <returns></returns>
        public decimal? GetBalance(ICardProvider cardProvider)
        {
            if (BusinessParentUser == null) { throw new InvalidOperationException("Error retrieving balance for prepaid account " + AccountID.ToString() + ".  Account must be associated with a user"); }
            if (cardProvider != null)
            {
                CardProvider = cardProvider;
            }

            if (!_cardBalance.HasValue)
            {
                decimal ledgerBalance;
                decimal availableBalance;
                Error error;

                if (!CardProvider.RetrieveBalance(out availableBalance, out ledgerBalance, out error, this))
                {
                    _cardBalance = null;
                }
                else
                {
                    _cardBalance = availableBalance;
                }
            }

            return _cardBalance;
        }

        /// <summary>
        /// Gets the accounts balance.
        /// </summary>
        /// <returns>The balance of the account</returns>
        public decimal? GetBalance()
        {
            return GetBalance(null);
        }

        /// <summary>
        /// Gets the accounts balance.
        /// </summary>
        /// <param name="cardProvider">The card provider.</param>
        /// <returns></returns>
        public string GetBalanceText(ICardProvider cardProvider)
        {
            decimal? tempbal = GetBalance(cardProvider);
            string baltext = string.Empty;

            if (tempbal.HasValue)
                baltext = tempbal.Value.ToString(Money.MONEY_STRING_FORMAT);
            else
                baltext = "Temp. Unavail.";

            return baltext;
        }

        /// <summary>
        /// Gets the account balance.
        /// </summary>
        /// <returns>The balance of the account as a string</returns>
        public string GetBalanceText()
        {
            decimal? tempbal = GetBalance();
            string baltext = string.Empty;

            if (tempbal.HasValue)
                baltext = tempbal.Value.ToString(Money.MONEY_STRING_FORMAT);
            else
                baltext = "Temp. Unavail.";

            return baltext;
        }

        /// <summary>
        /// Creates a journal entry.
        /// </summary>
        /// <param name="description">The description of the journal entry.</param>
        /// <param name="actingUserID">The acting user ID.</param>
        /// <returns>
        /// 	<c>true</c> if the journal entry is successfully created; otherwise, <c>false</c>.
        /// </returns>
        public bool CreateJournalEntry(string description, Guid? actingUserID)
        {
            if (BusinessParentUser == null) { throw new InvalidOperationException("Error creating journal entry for prepaid account " + AccountID.ToString() + ".  Account must be associated with a user"); }
            return AdapterFactory.FinancialAccountDataAdapter.CreatePrepaidCardAccountJournalEntry(AccountID, BusinessParentUser.UserID, actingUserID, description);
        }

        /// <summary>
        /// Gets the journal entries for the account.
        /// </summary>
        /// <returns>Account's journal entries.</returns>
        public EntityCollection<JournalEntity> GetJournalEntries()
        {
            return AdapterFactory.FinancialAccountDataAdapter.RetirevePrepaidCardAccountJournalEntries(AccountID);
        }

        /// <summary>
        /// Determines whether this account is Black Listed.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this account is black listed; otherwise, <c>false</c>.
        /// </returns>
        public bool IsBlackListed()
        {
            return AccountBlackLister.IsAccountBlackListed(this);
        }

        /// <summary>
        /// Updates for failed billing.
        /// </summary>
        /// <param name="amount">The amount.</param>
        /// <param name="product">The product.</param>
        public void UpdateForFailedBilling(decimal amount, Product product)
        {
            Teen teen = (Teen)BusinessParentUser;
            teen.MarkUserDelinquent(amount, product);

            // Request 6425: MH - Spring did not want the family locked for delinquency.
            //Family family = new Family(teen);
            //family.Lock();

            new PrepaidCardJournal(teen, "Prepaid monthly billing failed against this account for user " + teen.UserName, null, this).Save();
        }

        /// <summary>
        /// Deletes the account.
        /// </summary>
        /// <returns></returns>
        public bool DeleteAccount()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Updates the status.
        /// </summary>
        /// <param name="newStatus">The new status.</param>
        /// <param name="updateFSV">if set to <c>true</c> [update FSV].</param>
        public void UpdateStatus(PrepaidCardStatus newStatus, bool updateFSV)
        {
            if (Status != newStatus)
            {
                _prepaidCardAccountEntity.Status = newStatus;
                _accountStatusChanged = true;
                if (updateFSV)
                {
                    _updateProviderStatus = true;
                }
            }
        }

        /// <summary>
        /// Sets the card design.
        /// </summary>
        /// <param name="cardDesignIdentifier">The card design identifier.</param>
        /// <returns>True if a card design is set.  False if the card design is not found.</returns>
        public bool SetCardDesign(string cardDesignIdentifier)
        {
            Guid? brandingCardDesignID = GetBrandingCardDesignID(cardDesignIdentifier);
            if (brandingCardDesignID.HasValue)
            {
                BrandingCardDesignID = brandingCardDesignID;
                return true;
            }

            Guid? customCardDesignID = GetUserCardDesignID(cardDesignIdentifier);
            if (customCardDesignID.HasValue)
            {
                CustomCardDesignID = customCardDesignID;
                return true;
            }

            return false;
        }

        /// <summary>
        /// checks the system velocities before sending out ACHs.
        /// </summary>
        /// <param name="fullList">The full list of user transactions.</param>
        /// <param name="fileList">The ACH file list that will be generated.</param>
        /// <param name="canceledList">The canceled list of transactions.</param>
        /// <returns></returns>
        public static bool SystemVelocityChecker(
            List<FNBOACHTransactionJob> fullList,
            out List<FNBOACHTransactionJob> fileList,
            out List<FNBOACHTransactionJob> canceledList
            )
        {
            // Create the bucket.
            PropertyBag _amountBucket = new PropertyBag();
            // Set up the list of files.
            fileList = new List<FNBOACHTransactionJob>();
            canceledList = new List<FNBOACHTransactionJob>();
            Error error;

            decimal bucketAmount = 0.00M;

            // Go through each of the transaction jobs to fill the bucket.
            foreach (FNBOACHTransactionJob trans in fullList)
            {
                Teen teen;
                Parent parent = User.RetrieveUser(trans.UserID) as Parent;
                if (parent == null)
                {
                    Log.Error("(SYSTEM VELOCITY CHECK) parent returned null: " + trans.UserID.ToString());
                    return false;
                }
                Job job;
                TransactionJob cardJob = null;
                // Get the max amount from the Site object.
                decimal maxAmount = parent.Site.RetrieveMaxTransferAmount(Product.PPaid_Standard_Branding_I);

                // Retrieve initial job.
                job = Job.RetrieveJob(trans.JobID);
                // Retrieve the card load job.
                cardJob = FinancialAccountUtil.GetLoadJob(job.JobID);
                if (cardJob != null)
                {
                    #region bucket process
                    teen = User.RetrieveUser(cardJob.UserID) as Teen;
                    if (teen == null)
                    {
                        Log.Error("(SYSTEM VELOCITY CHECK) child returned null: " + cardJob.UserID.ToString());
                        return false;
                    }
                    // Add the bucket amounts together.
                    if (_amountBucket.ContainsKey(teen.UserID.ToString()))
                    {
                        // Add to the amount.
                        decimal.TryParse(_amountBucket[teen.UserID.ToString()].ToString(), out bucketAmount);
                        bucketAmount += trans.Amount;
                        // Check to see if an overflow occurred.
                        if (bucketAmount <= maxAmount)
                        {
                            // Save the amount back into the bucket.
                            _amountBucket[teen.UserID.ToString()] = bucketAmount;
                            fileList.Add(trans);
                        }
                        else
                        {
                            // We will add the transaction to the cancelled list.
                            canceledList.Add(trans);
                            Graph graph = Graph.LoadGraphFromJob(trans);
                            if (graph.Cancel(null, out error))
                            {
                                // Send the notification about the cancelled transaction.
                                if (!ServiceFactory.NotificationService.ACHCancelledDueToSystemCardVelocityNotification(parent, teen, trans))
                                {
                                    Log.Error("(SYSTEM VELOCITY CHECK) ACHCancelledDueToSystemCardVelocityNotification did not send.");
                                    return false;
                                }
                            }
                            else
                            {
                                Log.Error("(SYSTEM VELOCITY CHECK) job could not be cancelled: " + trans.JobID.ToString());
                                return false;
                            }
                        }
                    }
                    else
                    {
                        // This is the first card load for this child so we create add to the bag.
                        if (trans.Amount <= maxAmount)
                        {
                            _amountBucket.Add(teen.UserID.ToString(), trans.Amount);
                            fileList.Add(trans);
                        }
                        else
                        {
                            // We will add the transaction to the cancelled list.
                            canceledList.Add(trans);
                            Graph graph = Graph.LoadGraphFromJob(trans);
                            if (graph.Cancel(null, out error))
                            {
                                // Send the notification about the cancelled transaction.
                                if (!ServiceFactory.NotificationService.ACHCancelledDueToSystemCardVelocityNotification(parent, teen, trans))
                                {
                                    Log.Error("(SYSTEM VELOCITY CHECK) ACHCancelledDueToSystemCardVelocityNotification did not send.");
                                    return false;
                                }
                            }
                            else
                            {
                                Log.Error("(SYSTEM VELOCITY CHECK) job could not be cancelled: " + trans.JobID.ToString());
                                return false;
                            }
                        }
                    }
                    #endregion //bucket process
                }
                else
                {
                    // This is not a card load so we allow it to go through.
                    fileList.Add(trans);
                }
            }
            return true;
        }

        /// <summary>
        /// Deletes the prepaid card account.
        /// </summary>
        /// <returns></returns>
        public bool Deprecated_DeleteAccount()
        {
            // Set this account to Marked for Deletion
            base.Entity.MarkedForDeletion = true;

            // Set the active for this card to false.
            base.Entity.Active = false;

            // Set card status to closed.
            UpdateStatus(PrepaidCardStatus.Closed, true);

            // Set the card to be deleted.
            _accountDeleted = true;

            return true;
        }

        /// <summary>
        /// Gets the date of the last non $0 transaction for this account. (Checks the last 30 days, returns null if no transactions found)
        /// </summary>
        /// <returns></returns>
        public DateTime? GetLastTransactionDate(ICardProvider prepaidProvider)
        {
            if (prepaidProvider != null)
            {
                CardProvider = prepaidProvider;
            }

            if (_lastTransDate == null)
            {

                //go through pending loads and see if any exist
                EntityCollection<FsvcardTransactionJobEntity> pendingLoads = AdapterFactory.FinancialAccountDataAdapter.RetrieveScheduledFSVCardTransactions(UserID.Value);
                if (pendingLoads.Count > 0)
                {
                    foreach (FsvcardTransactionJobEntity load in pendingLoads)
                    {
                        if ((_lastTransDate == null || load.ScheduledStartTime > _lastTransDate) && load.Status == JobStatus.Waiting)
                        {
                            _lastTransDate = load.ScheduledStartTime;
                        }
                    }
                }

                TransactionHistory transHistory;
                if (!CardProvider.RetrieveTransactions(this.CardNumber, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(1), false, false, out transHistory))
                {
                    //The call failed, return null
                    return null;
                }

                List<TransactionHistory.Transaction> transactions = transHistory.GetTransactionRecords();
                //Go through each transaction and find latest date
                foreach (TransactionHistory.Transaction trans in transactions)
                {
                    DateTime transDate = DateTime.Parse(trans.xtransDate);
                    if (_lastTransDate == null || transDate > _lastTransDate)
                    {
                        _lastTransDate = transDate;
                    }
                }
            }

            return _lastTransDate;
        }

        /// <summary>
        /// Gets the date of the last non $0 transaction for this account. (Checks the last 30 days, returns null if no transactions found)
        /// </summary>
        /// <returns></returns>
        public DateTime? GetLastUserTransactionDate(ICardProvider prepaidProvider)
        {
            if (prepaidProvider != null)
            {
                CardProvider = prepaidProvider;
            }

            if (_lastTransDate == null)
            {

                //go through pending loads and see if any exist
                EntityCollection<FsvcardTransactionJobEntity> pendingLoads = AdapterFactory.FinancialAccountDataAdapter.RetrieveScheduledFSVCardTransactions(UserID.Value);
                if (pendingLoads.Count > 0)
                {
                    foreach (FsvcardTransactionJobEntity load in pendingLoads)
                    {
                        if ((_lastTransDate == null || load.ScheduledStartTime > _lastTransDate) && load.Status == JobStatus.Waiting)
                        {
                            _lastTransDate = load.ScheduledStartTime;
                        }
                    }
                }

                TransactionHistory transHistory;
                if (!CardProvider.RetrieveTransactions(this.CardNumber, DateTime.Today.AddDays(-30), DateTime.Today.AddDays(1), false, false, out transHistory))
                {
                    //The call failed, return null
                    return null;
                }

                List<TransactionHistory.Transaction> transactions = transHistory.GetTransactionRecords();
                //Go through each transaction and find latest date
                foreach (TransactionHistory.Transaction trans in transactions)
                {
                    /*
                    1101 (ATM)
                    1102 (AUTH)
                    1103 (RECN)
                    1105 (SETL)
                    1107 (RVRS)
                    1109 (RFND)
                    1151 (ATM)
                    2121 (DIRDEP)
                    3150 (C2CTXFR)
                    3203 (PAYJR LOAD) (only if to card)
                    3102/3109/3253/3263 (REPL)
                    */
                    Decimal tranamt;
                    if (!Decimal.TryParse(trans.xbaseAmount, out tranamt))
                        tranamt = 0;

                    if (trans.xtransType.Equals("1101") ||
                        trans.xtransType.Equals("1102") ||
                        trans.xtransType.Equals("1103") ||
                        trans.xtransType.Equals("1105") ||
                        trans.xtransType.Equals("1107") ||
                        trans.xtransType.Equals("1109") ||
                        trans.xtransType.Equals("1151") ||
                        trans.xtransType.Equals("2121") ||
                        trans.xtransType.Equals("3150") ||
                        (trans.xtransType.Equals("3203") && tranamt > 0) ||
                        trans.xtransType.Equals("3102") ||
                        trans.xtransType.Equals("3109") ||
                        trans.xtransType.Equals("3253") ||
                        trans.xtransType.Equals("3263"))
                    {
                        DateTime trandate;
                        if (!DateTime.TryParse(trans.xtransDate, out trandate))
                            trandate = DateTime.Parse("01/01/2000");

                        if (_lastTransDate == null || trandate > _lastTransDate)
                        {
                            _lastTransDate = trandate;
                        }
                    }
                }
            }

            return _lastTransDate;
        }

        /// <summary>
        /// Deactivates the card, charges a cancellation Fee if given, and refunds the remaining balance to the given refund account
        /// </summary>
        /// <param name="cancellationFees">The cancellation fees.</param>
        /// <param name="refundAccount">The refund account.</param>
        /// <returns></returns>
        public bool ShutDownAccount(ICardProvider cardProvider, out Error error)
        {
            // HACK: Testing to see whether not setting the provider
            // will get the correct one once we call for deactivate card.
            //if (cardProvider != null)
            //{
            //    CardProvider = cardProvider;
            //}

            Teen teen = BusinessParentUser as Teen;

            //Initialize output
            error = null;

            //Clear the balance
            decimal? balance = GetBalance();
            if (balance.HasValue && (balance.Value > 0))
            {
                //Move the money
                MoneyMover moneyMover = null;

                if (SupportsFeeTransfer)
                    moneyMover = new MoneyMover(null, null, teen, MoneyMoverType.InstantMonthlyServiceFee);
                else
                    moneyMover = new MoneyMover(null, null, teen, MoneyMoverType.InstantReverseCardLoad);

                moneyMover.DestinationAccount = this;

                // Bug 6489: MH - add the right amount of billing to the account shutdown fee.
                List<BillingAmount> billingAmounts = new List<BillingAmount>();
                billingAmounts.Add(new BillingAmount(Product.PPaid_Card_Liquidation_IC, balance.Value));
                moneyMover.BillingAmounts = billingAmounts;

                // Compose string for description
                String desc = String.Empty;
                {
                    int cntDQ = teen.UnresolvedDelinquencies.Count;

                    decimal amtDQ = 0;
                    foreach (DelinquentAccount da in teen.UnresolvedDelinquencies)
                    {
                        amtDQ += da.Amount;
                    }

                    if (cntDQ == 0 || amtDQ == 0)
                        desc = String.Format("Account Closure - {0}", teen.UserName);
                    else
                        desc = String.Format("Delinquent Account Closure - {0} ({1} :: {2})", teen.UserName, cntDQ, amtDQ.ToString("C"));
                }

                TransactionJob fundingJob;
                if (!moneyMover.MoveMoney(0.0M, desc, string.Empty, null, out fundingJob, out error))
                {
                    Log.Error("Failed to move money off the card when Shutting Down prepaid account.  AccountID: " + AccountID.ToString());
                    return false;
                }
            }

            //Deactivate the Card
            string confirmationID;
            //If the card number is null (error out or pending card create), do not try to deactivate it - just set local status to closed.
            if (this.CardNumber != String.Empty)
            {
                if (!CardProvider.DeactivateCard(this.CardNumber, string.Empty, out confirmationID, out error))
                {
                    Log.Error("Failed deactivating the card when Shutting Down prepaid account.  AccountID: " + AccountID.ToString());
                    return false;
                }
            }

            //Set status of the card to closed
            // NOTE:  This should already be done by the DeactivateCard call (if successful).
            UpdateStatus(PrepaidCardStatus.Closed, false);

            return true;
        }

        private void RetrieveCardholderInformation()
        {
            DateTime expirationDate;
            CardProvider.GetCardholderInformation(this, out expirationDate, out _fsvCustomerID);

            _expirationDate = expirationDate;
        }

        #endregion //Methods

        #region Post Save Handling

        /// <summary>
        /// Handles the UpdateData event of the ParentEvents control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="Payjr.Core.Business.UpdateDataEventArgs"/> instance containing the event data.</param>
        protected override void ParentEvents_UpdateData(object sender, UpdateDataEventArgs args)
        {
            //New Account
            if (this.IsNew)
            {
                //Initialize properties
                _prepaidCardAccountEntity.Status = PrepaidCardStatus.Pending;
                _prepaidCardAccountEntity.MarkedForDeletion = false;
                _newAccount = true;
            }

            //If the activation was successful, set activation date
            if (_activationSuccessful)
            {
                _prepaidCardAccountEntity.ActiveteDateTime = DateTime.UtcNow;
            }

            base.ParentEvents_UpdateData(sender, args);
        }

        /// <summary>
        /// Handles the UpdateDataSuccess event of the ParentEvents control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected override void ParentEvents_UpdateDataSuccess(object sender, UpdateSuccessEventArgs e)
        {
            //Successful Activation
            if (_activationSuccessful)
            {
                if (!ProcessActivationSuccessful(e.ConvertActingUserToGuid()))
                {
                    e.CancelUpdate = true;
                }
            }

            //New Account
            if (_newAccount)
            {
                if (!ProcessNewAccount(e.ConvertActingUserToGuid()))
                {
                    e.CancelUpdate = true;
                }
            }

            //Account Set to Active
            if (_accountSetToActive)
            {
                if (!ProcessAccountSetToActive(e.ConvertActingUserToGuid()))
                {
                    e.CancelUpdate = true;
                }
            }

            //Account Set to InActive
            if (_accountSetToInActive)
            {
                if (!ProcessAccountSetToInActive(e.ConvertActingUserToGuid()))
                {
                    e.CancelUpdate = true;
                }
            }

            //Account Status Changed
            if (_accountStatusChanged)
            {
                if (!ProcessAccountStatusChanged(e.ConvertActingUserToGuid()))
                {
                    e.CancelUpdate = true;
                }
            }

            // Account to be deleted
            if (_accountDeleted)
            {
                if (!ProcessAccountDeleted(e.ConvertActingUserToGuid()))
                {
                    e.CancelUpdate = true;
                }
            }

            ResetPostSaveBits();
            base.ParentEvents_UpdateDataSuccess(sender, e);
        }

        /// <summary>
        /// Resets the post save bits.
        /// </summary>
        private void ResetPostSaveBits()
        {
            _activationSuccessful = false;
            _newAccount = false;
            _accountSetToActive = false;
            _accountSetToInActive = false;
            _accountStatusChanged = false;
            _updateProviderStatus = false;
            _accountDeleted = false;
        }

        /// <summary>
        /// Processes the activation successful post save bit.
        /// </summary>
        /// <returns></returns>
        private bool ProcessActivationSuccessful(Guid? actingUserID)
        {
            //Activate the card at FSV
            Error error;
            string confirmationID;

            if (!CardProvider.ActivateCard(CardNumber, string.Empty, out confirmationID, out error))
            {
                base.WritePostSaveErrorToLog(this.ToString(), "ProcessActivationSuccessful",
                    string.Format(ERROR_ACTIVATE_CARD, AccountID.ToString(), BusinessParentUser.UserName));
                return false;
            }

            IsActive = true;

            return true;
        }

        /// <summary>
        /// Processes the new account post save bit.
        /// </summary>
        /// <returns></returns>
        private bool ProcessNewAccount(Guid? actingUserID)
        {
            //Only create a card create job if the card number is not set
            if (string.IsNullOrEmpty(CardNumber))
            {
                if (BusinessParentUser != null && BusinessParentUser.RoleType == RoleType.RegisteredTeen)
                {
                    //Set the scheduled Start time based on the parent's ach account status
                    DateTime scheduledStartTime;
                    Parent parent = User.RetrieveUser((BusinessParentUser as Teen).ParentID) as Parent;

                    //Bug# 3174 TY: Just like bank account, we need to pull config from active CC account
                    //If the parent's active account is good, set the start time to now
                    if (parent != null &&
                       parent.FinancialAccounts.ActiveACHAccount != null &&
                       parent.FinancialAccounts.ActiveACHAccount.Status == AccountStatus.AllowMoneyMovement)
                    {
                        scheduledStartTime = DateTime.Now;
                    }
                    else if (parent != null &&
                        parent.FinancialAccounts.ActiveCreditCardAccount != null &&
                        parent.FinancialAccounts.ActiveCreditCardAccount.Status == AccountStatus.AllowMoneyMovement)
                    {
                        int days = parent.Site.PrepaidCreditProvider.CardCreateDelayDays;
                        if (days == 0)
                        {
                            scheduledStartTime = DateTime.UtcNow;
                        }
                        else
                        {
                            DateTime timeOfDay = parent.Site.PrepaidCreditProvider.CardCreateTimeOfDay;
                            scheduledStartTime = SchedulingUtils.GetPostDate(DateTime.Now, days - 1, SchedulingUtils.DateDirection.Forward);
                            scheduledStartTime = new DateTime(scheduledStartTime.Year, scheduledStartTime.Month, scheduledStartTime.Day,
                                                                timeOfDay.Hour, timeOfDay.Minute, 0, DateTimeKind.Local);
                        }
                    }
                    //else set the start time to three business days later
                    else
                    {
                        DateTime scheduledTime = SchedulingUtils.GetPostDate(DateTime.Now, 2, SchedulingUtils.DateDirection.Forward);
                        //Set the hour to 7AM
                        scheduledStartTime = new DateTime(scheduledTime.Year, scheduledTime.Month, scheduledTime.Day, 7, scheduledTime.Minute, 0, DateTimeKind.Local);
                    }


                    //Determine if the job needs to be locked or not
                    bool isLocked;
                    //if the branding card design is set the job is not locked
                    if (this.BrandingCardDesignID.HasValue)
                    {
                        isLocked = false;
                    }
                    //else if the custom card design id is set and the card has not been retrieved, lock the job
                    else if (this.CustomCardDesignID.HasValue)
                    {
                        isLocked = !this.CustomCardDesign.IsRetrieved;
                    }
                    //A card design has not been set
                    else
                    {
                        isLocked = true;
                    }

                    //Create Card Create Job
                    CreateCardJob createCardJob = new CreateCardJob(BusinessParentUser, scheduledStartTime, isLocked, AccountID);
                    createCardJob.SaveNew();
                }
            }
            return true;
        }

        /// <summary>
        /// Processes the account set to active Post save bit.
        /// </summary>
        /// <param name="actingUserID">The acting user ID.</param>
        /// <returns></returns>
        private bool ProcessAccountSetToActive(Guid? actingUserID)
        {
            //Create Journal Entry
            if (!this.CreateJournalEntry("Account Set to Active", actingUserID))
            {
                base.WritePostSaveErrorToLog(this.ToString(), "ProcessAccountSetToActive",
                    string.Format(ERROR_JOURNAL_ENTRY_CREATION, AccountID.ToString(), BusinessParentUser.UserName));
                return false;
            }

            List<ScheduledItem> allowances = ScheduledItem.RetrieveAllowancesByTeen(BusinessParentUser.UserID);
            TransactionAccountInfoEntity allowanceAccountInfo;
            foreach (ScheduledItem allowance in allowances)
            {
                allowanceAccountInfo = TransactionAccountInfo.RetrieveByScheduledItem(allowance.TransactionAccountInfoID.Value);
                allowanceAccountInfo.DestinationAccountId1 = AccountID;
                TransactionAccountInfo.Save(allowanceAccountInfo);
            }

            return true;
        }

        /// <summary>
        /// Processes the account set to in active Post Save Bit.
        /// </summary>
        /// <param name="actingUserID">The acting user ID.</param>
        /// <returns></returns>
        private bool ProcessAccountSetToInActive(Guid? actingUserID)
        {
            //Create Journal Entry
            if (!this.CreateJournalEntry("Account Set to Inactive", actingUserID))
            {
                base.WritePostSaveErrorToLog(this.ToString(), "ProcessAccountSetToInActive",
                    string.Format(ERROR_JOURNAL_ENTRY_CREATION, AccountID.ToString(), BusinessParentUser.UserName));
                return false;
            }
            return true;
        }

        /// <summary>
        /// Processes the account status changed Post Save Bit.
        /// </summary>
        /// <param name="actingUserID">The acting user ID.</param>
        /// <returns></returns>
        private bool ProcessAccountStatusChanged(Guid? actingUserID)
        {
            // If the account is deleted cancel all transactions.
            if (_accountDeleted)
            {
                Error error;
                Teen teen = BusinessParentUser as Teen;
                if (teen != null)
                {
                    // Retrieve any pending transactions for the User.
                    EntityCollection<TransactionJobEntity> jobs = ServiceFactory.TransactionService.RetrievePendingTransactionJobsByUserID(teen.UserID);
                    if (jobs.Count > 0)
                    {
                        foreach (TransactionJobEntity job in jobs)
                        {
                            // Dont cancel the downgrading job.
                            if (job.JobType != JobType.InitiateDowngradingJob)
                            {
                                // Cancel each of the pending transaction jobs.
                                Graph graph = Graph.LoadGraphFromJob(Job.RetrieveJob(job));
                                if (!graph.Cancel(actingUserID, out error))
                                {
                                    base.WritePostSaveErrorToLog(this.ToString(), "ProcessAccountStatusChanged",
                                        string.Format(ERROR_CANCEL_GRAPH, job.JobId.ToString(), AccountID.ToString(), BusinessParentUser.UserName));
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            //If the status was changed peform the appropriate action
            if (_updateProviderStatus)
            {
                Error error;
                string confirmationID;

                bool success = false;

                switch (this.Status)
                {
                    case PrepaidCardStatus.Good:
                        success = CardProvider.ActivateCard(this.CardNumber, string.Empty, out confirmationID, out error);
                        break;
                    case PrepaidCardStatus.Suspended:
                        success = CardProvider.SuspendCard(this.CardNumber, string.Empty, out confirmationID, out error);
                        break;
                    // Don't allow setting FSV status to closed - this is handled by deleting cancelled / delinquent accounts.
                    case PrepaidCardStatus.Closed:
                        success = false;
                        break;
                    // Throw error if requesting to change to these states:
                    case PrepaidCardStatus.Pending:
                    case PrepaidCardStatus.Replaced:
                    case PrepaidCardStatus.Unknown:
                    default:
                        success = false;
                        break;
                }

                if (!success)
                {
                    base.WritePostSaveErrorToLog(this.ToString(), "ProcessAccountStatusChanged", string.Format("Could not change card status for {0} at FSV to {2} for user {1}", Common.Util.Utils.DisplayLastFourDigits(this.CardNumber),
                        this.BusinessParentUser.UserName, this.Status.ToString()));
                    return false;
                }
                else
                {   // Status was changed - add a journal entry.
                    if (!this.CreateJournalEntry("Account Status Changed to " + this.Status.ToString(), actingUserID))
                    {
                        base.WritePostSaveErrorToLog(this.ToString(), "ProcessAccountStatusChanged",
                            string.Format(ERROR_JOURNAL_ENTRY_CREATION, AccountID.ToString(), BusinessParentUser.UserName));
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Processes that need to be done when an account is deleted.
        /// </summary>
        /// <param name="actingUserID">The acting user ID.</param>
        /// <returns></returns>
        private bool ProcessAccountDeleted(Guid? actingUserID)
        {
            // Create a journal entry for deletion.
            if (!this.CreateJournalEntry("Account Deleted.", actingUserID))
            {
                base.WritePostSaveErrorToLog(this.ToString(), "ProcessAccountDeleted",
                    string.Format(ERROR_JOURNAL_ENTRY_CREATION, AccountID.ToString(), BusinessParentUser.UserName));
                return false;
            }
            return true;
        }

        #endregion //Post Save Handling

        #region Business Child Overrides

        /// <summary>
        /// Is the object dirty (need saving)
        /// </summary>
        /// <value></value>
        public override bool IsDirty
        {
            get
            {
                if (this.IsNew)
                {
                    return true;
                }

                return base.IsDirty;
            }
        }

        #endregion //Business Child Overrides

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PrepaidCardAccount"/> class.
        /// </summary>
        /// <param name="prepaidCardAccountEntity">The prepaid card account entity.</param>
        /// <param name="parentObj">The parent obj.</param>
        /// <param name="parentEventBag">The parent event bag.</param>
        public PrepaidCardAccount(PrepaidCardAccountEntity prepaidCardAccountEntity, BusinessBase parentObj, IParentEventBag parentEventBag)
            :
            base(prepaidCardAccountEntity, parentObj, parentEventBag, new DataAccessAdapterFactory())
        {
            _prepaidCardAccountEntity = prepaidCardAccountEntity;
        }

        #endregion //Constructor

        #region Private Methods

        private Guid? GetBrandingCardDesignID(string cardIdentifier)
        {
            //If the card identifier is only one digit add a 0 to it
            if (cardIdentifier.Length == 1)
            {
                cardIdentifier = "0" + cardIdentifier;
            }

            Error error;
            BrandingCardDesignEntity cardDesign;
            ServiceFactory.BrandingService.RetrieveBrandingCardDesignByCardIdentifier(BusinessParentUser.BrandingId, cardIdentifier, out cardDesign, out error);

            if (cardDesign == null)
            {
                return null;
            }

            return cardDesign.BrandingCardDesignId;
        }

        private Guid? GetUserCardDesignID(string cardIdentifier)
        {
            Teen teen = BusinessParentUser as Teen;

            if (teen == null)
            {
                return null;
            }

            foreach (CustomCardDesign cardDesign in teen.CustomCardDesigns)
            {
                if (cardDesign.ServerSideDesignID == cardIdentifier)
                {
                    return cardDesign.CustomCardDesignID;
                }
            }

            return null;
        }

        #endregion //Private Methods

        #region Misc Methods
        /// <summary>
        /// Retrieves the total number of active PrepaidCardAccounts
        /// </summary>
        /// <returns></returns>
        public static int RetrieveActiveAccountTotal()
        {
            return AdapterFactory.FinancialAccountDataAdapter.RetrieveActivePrepaidAccountTotal();
        }

        public static PrepaidCardAccount RetrievePrepaidCardAccountByID(Guid accountID)
        {
            User owningUser = User.RetrieveUserByPrepaidCardAccountID(accountID);
            return owningUser.FinancialAccounts.GetAccountByID(accountID) as PrepaidCardAccount;
        }

        #endregion


        /// <summary>
        /// Gets the atm statistics.
        /// </summary>
        /// <param name="atmWithdrawalCount">The atm withdrawal count.</param>
        /// <param name="atmWithdrawalTotal">The atm withdrawal total.</param>
        public void GetAtmStatistics(out int atmWithdrawalCount, out decimal atmWithdrawalTotal)
        {
            atmWithdrawalCount = 0;
            atmWithdrawalTotal = 0;

            // Only look at transactions in the last week
            List<FinancialTransaction> transactions = RetrieveTransactions(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            FSVDBTransaction fsvTransaction;

            foreach (FinancialTransaction transaction in transactions)
            {
                fsvTransaction = transaction as FSVDBTransaction;

                if (fsvTransaction != null)
                {
                    if (
                        fsvTransaction.TransactionNumber.Equals("1101")
                        ||
                        fsvTransaction.TransactionNumber.Equals("1151")
                        )
                    {
                        atmWithdrawalCount++;
                        atmWithdrawalTotal = +fsvTransaction.Amount;
                    }
                }
            }
        }

        #region Card Replacement

        /// <summary>
        /// Makes a call to the provider to replace the account and outputs the new account if no fee is charged
        /// </summary>
        /// <param name="chargeFee">Whether or not to charge a fee for replacement or to replace now</param>
        /// <param name="actingUserId">User requesting the replacement</param>
        /// <param name="cardProvider">ICardProvider to use during calls</param>
        /// <param name="newAccount">(OUT) Newly created PrepaidCardAccount from the provider. NULL if fee is charged because of the need of a file response first.</param>
        /// <returns>Boolean based on the success of the call to replace</returns>
        public bool InitiateCardReplacement(bool chargeFee, Guid? actingUserId, ICardProvider cardProvider, out PrepaidCardAccount newAccount)
        {
            if (cardProvider != null)
            {
                CardProvider = cardProvider;
            }

            newAccount = null;

            if (chargeFee)
                return ChargeReplacementFee(actingUserId, CardProvider);
            else
                return ReplaceCard(actingUserId, CardProvider, out newAccount);
        }

        /// <summary>
        /// Makes the call to replace a PrepaidCardAccount and create a new one
        /// </summary>
        /// <param name="actingUserId">User requesting the replacement</param>
        /// <param name="cardProvider">ICardProvider to use for calls</param>
        /// <param name="newAccount">(Out) New prepaid account reference created and linked</param>
        /// <returns>Boolean based on the success of the call</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cardProvider"/> is null</exception>
        /// <exception cref="DataAccessException">Thrown if the user cannot be saved after the update</exception>
        internal bool ReplaceCard(Guid? actingUserId, ICardProvider cardProvider, out PrepaidCardAccount newAccount)
        {
            //Initialize output
            newAccount = null;

            if (cardProvider != null)
            {
                CardProvider = cardProvider;
            }

            //--------Replace the Card--------//          
            Error outError = null;
            String newAccountNumber = null;
            if (!CardProvider.ReplaceCard(this.CardNumber, out newAccountNumber, out outError))
            {
                Log.Error(string.Format("A problem occurred when calling the FSVProvider to Replace a card. Card Number: {0} Teen: {1} Error: {2}",
                    this.CardNumberMasked, this.BusinessParentUser.UserID, outError.ErrorCode));
                return false;
            }

            //Make the new account
            newAccount = this.BusinessParentUser.NewPrepaidCardAccount();
            newAccount.IsActive = true;
            newAccount.CardNumber = newAccountNumber;
            newAccount.CustomCardDesignID = this.CustomCardDesignID;
            newAccount.BrandingCardDesignID = this.BrandingCardDesignID;

            //--------Mark this card as Replaced--------//
            this.UpdateStatus(PrepaidCardStatus.Replaced, false);
            this.IsPendingReplacement = false;

            //--------Save the card--------//
            try
            {
                if (!this.BusinessParentUser.Save(actingUserId))
                {
                    Log.Error(string.Format("A problem occurred when marking a account as replaced.  The replacement call succeeded but and was not rolled back. Old Card Number:{0} New Card Number: {1} Teen:{2}",
                        this.CardNumberMasked, newAccount.CardNumberMasked, this.BusinessParentUser.UserID));
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.ErrorException(string.Format("A problem occurred when marking a gift card as replaced.  The replacement call succeeded but and was not rolled back. Old Card Number:{0} New Card Number: {1} Teen:{2}",
                    this.CardNumberMasked, newAccount.CardNumberMasked, this.BusinessParentUser.UserID), e);

                throw new DataAccessException("Failed Save call for PrepaidCardAccount.InitiateReplacementWithFee", e);
            }

            //--------Create an order Note--------//
            CreateJournalEntry("Replaced Card.  Replaced by " + newAccount.CardNumberMasked, actingUserId);
            newAccount.CreateJournalEntry("Replacement Card.  This card replaced " + this.CardNumberMasked, actingUserId);
            new UserJournal(this.BusinessParentUser,
                            string.Format("Prepaid card replaced.  Old Card: {0}, New Card: {1}", this.CardNumberMasked,
                                          newAccount.CardNumberMasked), null, ActivityType.PrepaidCardReplaced).Save();


            return true;
        }

        /// <summary>
        /// Makes a call to replace this Gift Card by telling the provider to charge a fee
        /// </summary>
        /// <param name="actingUserId">User requesting the replacement</param>
        /// <param name="cardProvider">ICardProvider to use for calls</param>
        /// <returns>Boolean based on the success of the call to replace</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="cardProvider"/> is null</exception>
        /// <exception cref="DataAccessException">Thrown if the user cannot be saved after the update</exception>
        private bool ChargeReplacementFee(Guid? actingUserId, ICardProvider cardProvider)
        {
            Error errorCode = null;

            if (cardProvider != null)
            {
                CardProvider = cardProvider;
            }

            //Make call to charge fee
            if (!CardProvider.ChargeReversalFee(this.CardNumber, out errorCode))
            {
                Log.Error(string.Format("A problem occurred when charging a replacement fee while processing a card replacement.  Card Number:{0} Teen:{1} Error: {2}",
                    this.CardNumberMasked, this.BusinessParentUser.UserID, errorCode == null ? "Unknown" : errorCode.LongDescription));
                return false;
            }

            this.IsPendingReplacement = true;

            try
            {
                if (!this.BusinessParentUser.Save(actingUserId))
                {
                    Log.Error(string.Format("A problem occurred when marking a card as replaced. The replacement call succeeded but and was not rolled back. Card Number:{0} Teen:{1}",
                        this.CardNumberMasked, this.BusinessParentUser.UserID));
                    return false;
                }
            }
            catch (Exception e)
            {
                Log.ErrorException(string.Format("A problem occurred when marking a card as replaced.  The replacement call succeeded but and was not rolled back. Card Number:{0} Teen:{1}",
                    this.CardNumberMasked, this.BusinessParentUser.UserID), e);

                throw new DataAccessException("Failed Save call for PrepaidCardAccount.ChargeReplacementFee", e);
            }

            //--------Create an order Note--------//
            CreateJournalEntry("Charged replacement fee through card provider", actingUserId);

            return true;
        }

        #endregion

        #region IPrepaidCard Members

        public string CardMessage
        {
            get { return null; }
        }

        public string EmbossName
        {
            get
            {
                Teen teen = (Teen)BusinessParentUser;
                return teen.FirstName + " " + teen.LastName;
            }
        }

        /// <summary>
        /// Gets the expiration date.
        /// </summary>
        /// <value>The expiration date.</value>
        public DateTime ExpirationDate
        {
            get
            {
                if (!_expirationDate.HasValue)
                {
                    RetrieveCardholderInformation();
                }
                return _expirationDate.Value;
            }
        }

        public string FSVCustomerID
        {
            get
            {
                if (String.IsNullOrEmpty(_fsvCustomerID))
                {
                    RetrieveCardholderInformation();
                }
                return _fsvCustomerID;
            }
        }

        public string ServerSideIdentifier
        {
            get
            {
                if (CustomCardDesignID.HasValue)
                {
                    return CustomCardDesign.ServerSideDesignID;
                }
                return null;
            }
        }

        public bool IsExpedited
        {
            get
            {
                return _prepaidCardAccountEntity.IsExpedited;
            }
            set
            {
                _prepaidCardAccountEntity.IsExpedited = value;
            }
        }

        /// <summary>
        /// Gets/Sets whether or not this card is a replacement account
        /// </summary>
        public bool IsReplacement
        {
            get
            {
                return _prepaidCardAccountEntity.IsReplacementCard;
            }
            set
            {
                _prepaidCardAccountEntity.IsReplacementCard = value;
            }
        }

        public bool IsRush
        {
            get
            {
                return _prepaidCardAccountEntity.IsRush;
            }
            set
            {
                _prepaidCardAccountEntity.IsRush = value;
            }
        }


        public string MailingAddressLine1
        {
            get
            {
                Teen teen = (Teen)BusinessParentUser;
                Parent parent = teen.Parent;

                bool isFirstNameNull = String.IsNullOrEmpty(parent.FirstName);
                bool isMiddleNameNull = String.IsNullOrEmpty(parent.MiddleName);
                bool isLastNameNull = String.IsNullOrEmpty(parent.LastName);

                StringBuilder stringBuilder = new StringBuilder();
                if (!isFirstNameNull)
                {
                    stringBuilder.Append(parent.FirstName);
                }
                if (!isMiddleNameNull)
                {
                    if (!isFirstNameNull)
                    {
                        stringBuilder.Append(" ");
                    }
                    stringBuilder.Append(parent.MiddleName);
                }
                if (!isLastNameNull)
                {
                    if (!isFirstNameNull || !isMiddleNameNull)
                    {
                        stringBuilder.Append(" ");
                    }
                    stringBuilder.Append(parent.LastName);
                }
                return stringBuilder.ToString();
            }
        }

        public string MailingAddressLine2
        {
            get
            {
                Teen teen = (Teen)BusinessParentUser;
                Parent parent = teen.Parent;

                return parent.Address1;
            }
        }

        public string MailingAddressLine3
        {
            get
            {
                Teen teen = (Teen)BusinessParentUser;
                Parent parent = teen.Parent;

                if (!String.IsNullOrEmpty(parent.Address2))
                {
                    return parent.Address2;
                }
                return parent.City + ", " + parent.Province + " " + parent.PostalCode;
            }
        }

        public string MailingAddressLine4
        {
            get
            {
                Teen teen = (Teen)BusinessParentUser;
                Parent parent = teen.Parent;

                if (!String.IsNullOrEmpty(parent.Address2))
                {
                    return parent.City + ", " + parent.Province + " " + parent.PostalCode;
                }
                return null;
            }
        }

        public Guid PackagingKey
        {
            get
            {
                // Return a seperate packaging for custom cards.
                if (this.CustomCardDesignID.HasValue)
                    return CardProvider.CustomPackagingKey;
                else
                    return CardProvider.PackagingKey;
            }
        }

        #endregion

        /// <summary>
        /// Retrieves all PrepaidCardAccounts ready to fulfill
        /// </summary>
        /// <returns>List of PrepaidCardAccount with all matching accounts</returns>
        public static List<PrepaidCardAccount> RetrieveUnfulfilledPrepaidAccounts()
        {
            List<PrepaidCardAccount> accounts = new List<PrepaidCardAccount>();

            foreach (PrepaidCardAccountEntity entity in AdapterFactory.FinancialAccountDataAdapter.RetrieveUnfulfilledPrepaidAccounts())
                accounts.Add(PrepaidCardAccount.RetrievePrepaidCardAccountByID(entity.PrepaidCardAccountId));

            return accounts;
        }

        /// <summary>
        /// Retrieves all PrepaidCardAccounts ready to complete fulfillment
        /// </summary>
        /// <returns>List of PrepaidCardAccount with all matching accounts</returns>
        public static List<PrepaidCardAccount> RetrieveUnissuedPrepaidAccounts()
        {
            List<PrepaidCardAccount> accounts = new List<PrepaidCardAccount>();

            foreach (PrepaidCardAccountEntity entity in AdapterFactory.FinancialAccountDataAdapter.RetrieveUnissuedPrepaidAccounts())
                accounts.Add(PrepaidCardAccount.RetrievePrepaidCardAccountByID(entity.PrepaidCardAccountId));

            return accounts;
        }

        /// <summary>
        /// Calls save on the parent to save this object's changes
        /// </summary>
        /// <param name="actingUser">User making the request</param>
        /// <returns>Boolean based on the success of the call</returns>
        public bool Save(object actingUser)
        {
            return BusinessParent.Save(actingUser);
        }



        /// <summary>
        /// Calls FSV to generate a new passcode for this instance
        /// </summary>
        /// <returns>String with the new passcode if the call was successful otherwise null</returns>
        public string GeneratePasscode()
        {
            Random random = new Random();
            String tempPassCode;

            //FSV Does not allow 0000 as a valid code so make it anywhere in between
            tempPassCode = random.Next(1, 9999).ToString().PadLeft(4, '0');

            if (CardProvider.ChangePasscode(this.CardNumber, tempPassCode))
                return tempPassCode;
            else
                return null;
        }
    }
}
