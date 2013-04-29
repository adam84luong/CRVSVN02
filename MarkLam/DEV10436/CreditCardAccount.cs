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
using Common.Business.Validation.Rules;
using Common.CreditGateway;
using Common.Types;
using Common.Util;
using NLog;
using Payjr.Core.Adapters;
using Payjr.Core.FinancialAccounts.Interfaces;
using Payjr.Core.FinancialAccounts.Operations;
using Payjr.Core.Jobs;
using Payjr.Core.Providers;
using Payjr.Core.Services;
using Payjr.Core.Transactions;
using Payjr.Core.UserInfo;
using Payjr.Core.Users;
using Payjr.Entity;
using Payjr.Entity.EntityClasses;
using Payjr.Entity.HelperClasses;
using Payjr.Types;

namespace Payjr.Core.FinancialAccounts
{
    public class CreditCardAccount : BusinessEntityChild<CreditCardAccountEntity>, IFundingAccount, ICreditCardInfo
    {
        private static Logger Log = LogManager.GetCurrentClassLogger();

        #region Fields

        private string _cid;
        private string _par;

        #region Lazy Load Fields

        private CreditCardTransactionJob _verficationJob;
        private ICreditCardProvider _creditCardProvider;
        private bool? _isBlacklisted;

        #endregion //Lazy Load Fields

        #region Post Save Fields

        private PropertyBag _postSaveJournalData = new PropertyBag();
        private bool _accountStatusChanged;
        private bool _accountDeleted;

        #endregion //Post Save Fields

        #endregion //Fields

        #region Error Msgs
        private const string ERROR_JOURNAL_ENTRY_CREATION = "Failed to create Journal Entry. Credit Card Account: {0}. User: {1}";
        private const string ERROR_LOCK_JOB = "Failed to lock Job {0}. Credit Card Account: {1}. User: {2}";

        #region Rule Error Msgs
        /// <summary>
        /// An attempt was made to create a black listed account
        /// </summary>
        public string NEW_ACCOUNT_BLACK_LISTED
        { get { return "Account is blacklisted"; } }
        #endregion //Rule Error Msgs

        #endregion //Error Msgs

        #region Properties

        /// <summary>
        /// Gets or sets the card number.
        /// </summary>
        /// <value>The card number.</value>
        public string CardNumber
        {
            get
            {
                return base.Entity.CardNumber.ToString();
            }
            set
            {
                //Set data change data
                if (!IsNew && (value != CardNumber))
                {
                    string dataChangeValue = "Card Number Changed. " + CardNumberMasked + " --> " + Config.AccountNumber(value);
                    if (!_postSaveJournalData.ContainsKey("CardNumber")) { _postSaveJournalData.Add("CardNumber", dataChangeValue); }
                    else { _postSaveJournalData["CardNumber"] = dataChangeValue; }
                }

                base.Entity.CardNumber = value;
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
        /// Gets the credit card creation date in our database
        /// </summary>
        public DateTime CreatedDate
        {
            get
            {
                return base.Entity.CreatedTime;
            }
        }

        /// <summary>
        /// Gets the card identifier.
        /// </summary>
        /// <value>The card identifier.</value>
        public string CardIdentifier
        {
            get
            {
                if (base.Entity.CardIdentifier != null)
                {
                    return base.Entity.CardIdentifier.ToString();
                }
                return string.Empty;
            }
            set
            {
                base.Entity.CardIdentifier = value;
            }
        }

        /// <summary>
        /// Gets or sets the name of the nick.
        /// </summary>
        /// <value>The name of the nick.</value>
        public string NickName
        {
            get
            {
                return base.Entity.NickName;
            }
            set
            {
                //Set data change data
                if (!IsNew && (value != NickName))
                {
                    string dataChangeValue = "Account Nick Name Changed. " + NickName + " --> " + value;
                    if (!_postSaveJournalData.ContainsKey("NickName")) { _postSaveJournalData.Add("NickName", dataChangeValue); }
                    else { _postSaveJournalData["NickName"] = dataChangeValue; }
                }

                base.Entity.NickName = value;
            }
        }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public CreditCardType Type
        {
            get
            {
                return base.Entity.Type;
            }
            set
            {
                //Set data change data
                if (!IsNew && (value != Type))
                {
                    string dataChangeValue = "Account Card Type Changed. " + Type + " --> " + value;
                    if (!_postSaveJournalData.ContainsKey("Type")) { _postSaveJournalData.Add("Type", dataChangeValue); }
                    else { _postSaveJournalData["Type"] = dataChangeValue; }
                }

                base.Entity.Type = value;
            }
        }

        /// <summary>
        /// Gets or sets the expiration date.
        /// </summary>
        /// <value>The expiration date.</value>
        public DateTime ExpirationDate
        {
            get
            {
                int month;
                int year;
                if (!int.TryParse(base.Entity.ExpirationMonth, out month)) { return DateTime.MinValue; }
                if (!int.TryParse(base.Entity.ExpirationYear, out year)) { return DateTime.MinValue; }
                return new DateTime(year, month, DateTime.DaysInMonth(year, month));
            }
            set
            {
                //Set data change data
                if (!IsNew && ((value.Month != ExpirationDate.Month) || (value.Year != ExpirationDate.Year)))
                {
                    DateTime newDate = new DateTime(value.Year, value.Month, DateTime.DaysInMonth(value.Year, value.Month));
                    string dataChangeValue = "Card Expiration Date Changed. " + ExpirationDate.ToShortDateString() + " --> " + newDate.ToShortTimeString();
                    if (!_postSaveJournalData.ContainsKey("ExpirationDate")) { _postSaveJournalData.Add("ExpirationDate", dataChangeValue); }
                    else { _postSaveJournalData["ExpirationDate"] = dataChangeValue; }
                }

                base.Entity.ExpirationMonth = value.ToString("MM");
                base.Entity.ExpirationYear = value.ToString("yyyy");
            }
        }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public AccountStatus Status
        {
            get
            {
                return base.Entity.Status;
            }
            set
            {
                //Set data change data
                if (!IsNew && (value != Status))
                {
                    if (IsBlackListed()) { throw new InvalidOperationException("Can't change the status of a black listed account"); }

                    string dataChangeValue = "Account Status Changed. " + Status + " --> " + value;
                    if (!_postSaveJournalData.ContainsKey("Status")) { _postSaveJournalData.Add("Status", dataChangeValue); }
                    else { _postSaveJournalData["Status"] = dataChangeValue; }

                    _accountStatusChanged = true;
                }

                base.Entity.Status = value;
            }
        }

        /// <summary>
        /// Gets the string value to show the user.
        /// </summary>
        /// <value>The status string.</value>
        public string StatusString
        {
            get
            {
                switch (base.Entity.Status)
                {
                    case AccountStatus.AllowMoneyMovement:
                        return "Good";
                    case AccountStatus.DoNotAllowMoneyMovement:
                        return "Bad";
                    case AccountStatus.Unverified:
                        return "Unverified";
                }
                return "Bad";
            }
        }

        /// <summary>
        /// Sets the CID.
        /// </summary>
        /// <value>The CID.</value>
        public string CID
        {
            get
            {
                return _cid;
            }
            set
            {
                _cid = value;
            }
        }

        /// <summary>
        /// Gets or sets the credit card provider.
        /// </summary>
        /// <value>The credit card provider.</value>
        public ICreditCardProvider creditCardProvider
        {
            get
            {
                if (_creditCardProvider == null)
                {
                    _creditCardProvider = BusinessParentUser.Site.PrepaidCreditProvider;
                }
                return _creditCardProvider;
            }
            set
            {
                _creditCardProvider = value;
            }
        }

        /// <summary>
        /// Gets the business parent user.
        /// </summary>
        /// <value>The business parent user.</value>
        private User BusinessParentUser
        {
            get
            {
                return base.BusinessParent as User;
            }
        }

        /// <summary>
        /// Gets the verification job.
        /// </summary>
        /// <value>The verification job.</value>
        private CreditCardTransactionJob VerificationJob
        {
            get
            {
                if (_verficationJob == null)
                {
                    CreditCardTransactionJobEntity jobEntity = AdapterFactory.FinancialAccountDataAdapter.RetrieveVerificationJobForAccount(AccountID, TransactionType.CreditCardValidation);
                    if (jobEntity != null)
                    {
                        _verficationJob = new CreditCardTransactionJob(jobEntity);
                    }
                }
                return _verficationJob;
            }
        }

        /// <summary>
        /// Gets the initial load job.
        /// </summary>
        /// <value>The initial load job.</value>
        private CreditCardTransactionJob InitialLoadJob
        {
            get
            {
                if (_verficationJob == null)
                {
                    CreditCardTransactionJobEntity jobEntity = AdapterFactory.FinancialAccountDataAdapter.RetrieveVerificationJobForAccount(AccountID, TransactionType.InitialLoad);
                    if (jobEntity != null)
                    {
                        _verficationJob = new CreditCardTransactionJob(jobEntity);
                    }
                }
                return _verficationJob;
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
                return base.Entity.IsDefault;
            }
            set
            {
                //Set data change data
                if (!IsNew && (value != IsActive))
                {
                    string dataChangeValue = "Account Active Status Changed. " + IsActive + " --> " + value;
                    if (!_postSaveJournalData.ContainsKey("IsActive")) { _postSaveJournalData.Add("IsActive", dataChangeValue); }
                    else { _postSaveJournalData["IsActive"] = dataChangeValue; }
                }

                base.Entity.IsDefault = value;

                //if the value is true set all other credit cards to false
                if (value)
                {
                    //Set all other account to inactive
                    if (BusinessParentUser != null)
                    {
                        foreach (CreditCardAccount creditCardAccount in BusinessParentUser.FinancialAccounts.CreditCardAccounts)
                        {
                            if (creditCardAccount.AccountID != this.AccountID)
                            {
                                creditCardAccount.IsActive = false;
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
            get { return base.Entity.CreditCardAccountId; }
        }

        /// <summary>
        /// Set to true if the account is marked for deletion; otherwise, false
        /// </summary>
        /// <value></value>
        public bool MarkedForDeletion
        {
            get { return base.Entity.MarkedForDeletion; }
        }

        /// <summary>
        /// Gets the user ID
        /// </summary>
        /// <value></value>
        public Guid? UserID
        {
            get { return base.Entity.UserId; }
        }

        /// <summary>
        /// Gets the display name.
        /// </summary>
        /// <value>The display name.</value>
        public string DisplayName
        {
            get { return NickName + " " + CardNumberMasked; }
        }

        /// <summary>
        /// Determines if the account is in a pending state
        /// </summary>
        /// <value></value>
        public bool IsAccountPending
        {
            get
            {
                //Account is pending if the status of the account is unverified
                return Status == AccountStatus.Unverified;
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
            if (teen.IsUserTargetGiftCard)
                return Product.Target_CreditCard_Load_Fee_IC;
            if (teen.IsUserPrepaid)
                return Product.PPaid_CreditCard_Load_Fee_IC;
            return null;
        }

        /// <summary>
        /// Gets the emergency load product.
        /// </summary>
        /// <param name="teen">The teen.</param>
        /// <returns></returns>
        public Product? GetEmergencyLoadProduct(Teen teen)
        {
            if (teen.IsUserPrepaid)
                return Product.PPaid_Emergency_Load_Fee_IC;

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
            IDestinationAccount destinationAccount,
            IUserInfo destinationUser,
            Guid? actingUserID,
            out TransactionJob moneyMovementJob,
            out Error error
            )
        {
            //Initialize output
            error = null;
            moneyMovementJob = null;

            //Make sure the provider is an ICreditCardProvider provider
            if (!(provider is ICreditCardProvider))
            {
                throw new InvalidOperationException(
                    string.Format(
                        "Cannot create a Credit Card Transaction with a {0} provider",
                        provider != null ? provider.GetType().ToString() : "null"
                        )
                    );
            }

            //Validate Account
            if (BusinessParentUser == null) 
            { 
                throw new InvalidOperationException(
                    "Error Creating Credit Card Transaction for account" + AccountID + ".  Account must be associated with a user"
                    ); 
            }
            if (!CanSupportTransfer())
            {
                if (transactionType != TransactionType.Billing
                    &&
                    transactionType != TransactionType.Monthly_Service_Fee
                    &&
                    transactionType != TransactionType.Refund_Prcessing_Fee
                    &&
                    transactionType != TransactionType.Customer_Service_Fee
                    &&
                    transactionType != TransactionType.Shipping_Fee)
                {
                    error = new Error(ErrorCode.BankNotValid);
                    return false;
                }
            }

            //Create Transaction
            //If this is an initial load and we have a pending verification job use it
            if (transactionType == TransactionType.InitialLoad)
            {
                if (InitialLoadJob != null)
                {
                    //Make sure the verification job matches
                    if (InitialLoadJob.Status == JobStatus.Paused)
                    {
                        if (InitialLoadJob.Amount != amount)
                        {
                            error = new Error(ErrorCode.BankNotValid);
                            return false;
                        }
                        else
                        {
                            moneyMovementJob = InitialLoadJob;
                            moneyMovementJob.SaveJob(initialStatus);
                            return true;
                        }
                    }
                }
            }


            CreditCardTransactionJob creditTransactionJob = new CreditCardTransactionJob(
                BusinessParentUser,
                null,
                amount,
                description,
                transactionType,
                transactionDirection,
                AccountID,
                billingAmounts,
                provider as ICreditCardProvider
                );
            moneyMovementJob = creditTransactionJob;

            creditTransactionJob.SaveNew();
            creditTransactionJob.SaveBillingAmounts();

            Log.Debug("Saved the Credit card transaction job: " + creditTransactionJob.JobID);

            //Authorize the Transaction
            bool failedAVS;

            creditTransactionJob.Provider = creditCardProvider;

            Guid? destinationUserID = null;
            int? atmWithdrawalCount = null;
            decimal? atmWithdrawalTotal = null;
            int? enrolledDays = null;

            if (destinationUser is Teen)
            {
                enrolledDays = ((Teen)destinationUser).CurrentProgramTotalDaysEnrolled;
            }

            if (destinationUser != null)
            {
                destinationUserID = destinationUser.UserID;
            }

            if (destinationAccount is PrepaidCardAccount)
            {
                int prepaidAtmWithdrawalCount;
                decimal prepaidAtmWithdrawalTotal;
                ((PrepaidCardAccount)destinationAccount).GetAtmStatistics(out prepaidAtmWithdrawalCount, out prepaidAtmWithdrawalTotal);
                atmWithdrawalCount = prepaidAtmWithdrawalCount;
                atmWithdrawalTotal = prepaidAtmWithdrawalTotal;
            }

            decimal? loadAmount = null;
            Product? childProduct = null;

            if (destinationUser is Teen)
            {
                Teen teen = (Teen)destinationUser;
                loadAmount = amount;
                childProduct = teen.CurrentProduct.ProductNumber;
            }
            Log.Debug("Going to authorize the CC transaction: " + creditTransactionJob.JobID);

            if (!creditTransactionJob.Authorization(this, BusinessParentUser.UserID, destinationUserID, atmWithdrawalCount, atmWithdrawalTotal, BusinessParentUser.FinancialAccounts.BlacklistedCount, actingUserID, enrolledDays, childProduct, out failedAVS))
            {
                if (failedAVS) 
                { 
                    error = new Error(ErrorCode.CreditCardFailedAVS); 
                }
                else 
                { 
                    error = new Error(ErrorCode.CreditCardAuthorizationFailed); 
                }
                if (transactionType == TransactionType.MonthlyBill)
                {
                    CreditCardAccount.ProcessBillingAquisitionFailure(creditTransactionJob); 
                }

                Log.Debug("CC Job Failed: " + creditTransactionJob.JobID);

                return false;
            }

            //set the job to waiting
            creditTransactionJob.SaveJob(initialStatus);

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
            EntityCollection<CreditCardTransactionJobEntity> transactionJobs;
            EntityCollection<CreditCardCreditEntity> creditEntries;
            List<FinancialTransaction> retList;
            CreditCardTransactionJob cardJob;

            transactionJobs = AdapterFactory.FinancialAccountDataAdapter.RetrieveCreditCardTransactionsForAccount(AccountID, startDate, endDate);
            creditEntries = AdapterFactory.FinancialAccountDataAdapter.RetrieveCreditCardCreditsForAccount(AccountID, startDate, endDate);
            retList = new List<FinancialTransaction>();

            foreach (CreditCardTransactionJobEntity transJob in transactionJobs)
            {
                cardJob = new CreditCardTransactionJob(transJob);
                retList.Add(new CreditCardFinancialTransaction(cardJob));
            }

            foreach (CreditCardCreditEntity credit in creditEntries)
            {
                retList.Add(new CreditCardCreditFinancialTransaction(credit));
            }

            return retList;
        }

        /// <summary>
        /// Gets the available transfer counts based on the velocity limits for the account.
        /// </summary>
        /// <returns>
        /// The number of allowed transfer counts left for the account based on the velocity limits for the account
        /// </returns>
        public int GetAvailableTransferCount()
        {
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
            return Status == AccountStatus.AllowMoneyMovement;
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
            //Velocities not implemented for Credit Cards
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
            //Velocities not implemented for Credit Cards
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
            //Velocities not implemented for Credit Cards
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
            //Velocities not implemented for Credit Cards
            return true;
        }

        /// <summary>
        /// Verifies the account.
        /// </summary>
        /// <param name="verificationData">The verification data.</param>
        /// <returns>
        /// 	<c>true</c> if the account is successfully verified; otherwise, <c>false</c>.
        /// </returns>
        public bool VerifyAccount(VerificationData verificationData, IUserInfo destinationUser, IDestinationAccount destinationAccount, Guid? actingUserID)
        {
            //Validate verification Data
            if (string.IsNullOrEmpty(verificationData.CID)) { throw new ArgumentNullException("verificationData.CID", "CID must be set to create Credit Card Verification"); }
            CID = verificationData.CID;

            //if the account is already verified return
            if (Status != AccountStatus.Unverified) { return true; }

            //Get the Verification Job
            if (VerificationJob == null) { throw new InvalidOperationException("Error verifying Credit Card Account " + AccountID + ".  Account does not have a verification Job"); }

            Guid? destinationUserID = null;
            int? atmWithdrawalCount = null;
            decimal? atmWithdrawalTotal = null;
            int? enrolledDays = null;

            if (destinationUser != null)
            {
                destinationUserID = destinationUser.UserID;
            }

            if (destinationUser is Teen)
            {
                enrolledDays = ((Teen)destinationUser).CurrentProgramTotalDaysEnrolled;
            }

            if (destinationAccount is PrepaidCardAccount)
            {
                int prepaidAtmWithdrawalCount;
                decimal prepaidAtmWithdrawalTotal;
                ((PrepaidCardAccount)destinationAccount).GetAtmStatistics(out prepaidAtmWithdrawalCount, out prepaidAtmWithdrawalTotal);
                atmWithdrawalCount = prepaidAtmWithdrawalCount;
                atmWithdrawalTotal = prepaidAtmWithdrawalTotal;
            }

            decimal? loadAmount = null;
            Product? childProduct = null;

            if (destinationUser is Teen)
            {
                Teen teen = (Teen)destinationUser;
                loadAmount = VerificationJob.Amount;
                childProduct = teen.CurrentProduct.ProductNumber;
            }

            //Only validate if we have a par
            if (!string.IsNullOrEmpty(verificationData.PAR))
            {//Authenticate Verification
                IGatewayReply reply;
                VerificationJob.Provider = creditCardProvider;
                if (!VerificationJob.AuthenticateValidation(this, verificationData.PAR, out reply))
                {
                    //Verification Failed
                    Status = AccountStatus.DoNotAllowMoneyMovement;
                    return false;
                }

                //Authorization
                bool failedAVS;
                if (!VerificationJob.Authorization(this, reply, BusinessParentUser.UserID, destinationUserID, atmWithdrawalCount, atmWithdrawalTotal, BusinessParentUser.FinancialAccounts.BlacklistedCount, actingUserID, enrolledDays, childProduct, out failedAVS))
                {
                    //Verification Failed
                    Status = AccountStatus.DoNotAllowMoneyMovement;
                }
            }
            //Else just authorize the Transaction
            else
            {
                bool failedAVS;
                if (!VerificationJob.Authorization(this, BusinessParentUser.UserID, destinationUserID, atmWithdrawalCount, atmWithdrawalTotal, BusinessParentUser.FinancialAccounts.BlacklistedCount, actingUserID, enrolledDays, childProduct, out failedAVS))
                {
                    //Verification Failed
                    Status = AccountStatus.DoNotAllowMoneyMovement;
                }
            }

            //Verification Succeeded
            Status = AccountStatus.AllowMoneyMovement;
            return true;
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
            //Create the transaction
            CreditCardTransactionJob validationTransaction = new CreditCardTransactionJob(
                BusinessParentUser,
                null,
                0.00M,
                "Verification Auth",
                TransactionType.CreditCardValidation,
                TransactionDirection.Debit,
                AccountID,
                new List<BillingAmount>(),
                verificationData.FundingProvider as ICreditCardProvider
                );

            validationTransaction.SaveNew();
            validationTransaction.SaveBillingAmounts();

            //Authenticate Enrollment
            string url;
            string Par;
            validationTransaction.Provider = creditCardProvider;
            if (!validationTransaction.AuthenticateEnrollment(this, out url, out Par))
            {
                //If the enrollment failed the account gets set to DNAMM
                Status = AccountStatus.DoNotAllowMoneyMovement;
                return false;
            }
            verificationData.EnrollmentURL = url;
            verificationData.PAR = Par;

            return true;
        }

        /// <summary>
        /// Gets the accounts balance.
        /// </summary>
        /// <returns>The balance of the account</returns>
        public decimal? GetBalance()
        {
            //We do not have a way to get the Credit Card Balance
            return 0.0M;
        }

        /// <summary>
        /// Gets the accounts balance.
        /// </summary>
        /// <returns>The balance of the account as text</returns>
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
            return AdapterFactory.FinancialAccountDataAdapter.CreateCreditCardJournalEntry(AccountID, UserID.Value, actingUserID, description);
        }

        /// <summary>
        /// Gets the journal entries for the account.
        /// </summary>
        /// <returns>Account's journal entries.</returns>
        public EntityCollection<JournalEntity> GetJournalEntries()
        {
            return AdapterFactory.FinancialAccountDataAdapter.RetrieveCreditCardJournalEntries(AccountID);
        }

        /// <summary>
        /// Determines whether this account is Black Listed.
        /// </summary>
        /// <returns>
        /// 	<c>true</c> if this account is black listed; otherwise, <c>false</c>.
        /// </returns>
        public bool IsBlackListed()
        {
            if (!_isBlacklisted.HasValue)
                _isBlacklisted = AccountBlackLister.IsAccountBlackListed(this);
            return _isBlacklisted.Value;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Initializes the account.
        /// </summary>
        /// <param name="cardNumber">The card number.</param>
        /// <param name="nickName">Name of the nick.</param>
        /// <param name="type">The type.</param>
        /// <param name="expirationDate">The expiration date.</param>
        /// <param name="status">The status.</param>
        public void InitializeAccount(string cardNumber, string nickName, CreditCardType type, DateTime expirationDate, AccountStatus status)
        {
            CardNumber = cardNumber;
            NickName = nickName;
            Type = type;
            ExpirationDate = expirationDate;
            Status = status;

            base.Entity.MarkedForDeletion = false;
            base.Entity.CreatedTime = DateTime.Now;
            base.Entity.ModifiedDate = DateTime.Now;
            base.Entity.CreatedBy = string.Empty;
            base.Entity.ModifiedBy = string.Empty;

            if (BusinessParentUser != null)
            {
                //If the user already has an active account and it is good set this account to inactive
                if (BusinessParentUser.FinancialAccounts.ActiveCreditCardAccount != null && BusinessParentUser.FinancialAccounts.ActiveCreditCardAccount.Status == AccountStatus.AllowMoneyMovement)
                {
                    this.IsActive = false;
                }
                else
                {
                    this.IsActive = true;
                }
            }
            else
            {
                this.IsActive = true;
            }
        }

        /// <summary>
        /// Deletes the account.
        /// </summary>
        /// <returns></returns>
        public bool DeleteAccount()
        {
            bool foundAccountToActivate = false;
            //If this is the Active Account find another one to be the active account
            if (IsActive)
            {
                foundAccountToActivate = FindAccountToSetToActive();
            }

            //If another account was found and activated, deactivate this account.
            if (foundAccountToActivate)
            {
                IsActive = false;
            }

            //Set this account to Marked for Deletion
            base.Entity.MarkedForDeletion = true;

            //Set account status to DNAMM
            Status = AccountStatus.DoNotAllowMoneyMovement;

            _accountDeleted = true;

            return true;
        }

        /// <summary>
        /// Creates the recurring payment auth.
        /// </summary>
        /// <param name="CID">The CID.</param>
        /// <returns></returns>
        public bool CreateRecurringPaymentAuth(string cid, ICreditCardProvider provider, out bool failedAVS, IUserInfo destinationUser, IDestinationAccount destinationAccount, Guid? actingUserID)
        {
            this.CID = cid;

            //Create the transaction
            CreditCardTransactionJob trans = new CreditCardTransactionJob(
                BusinessParentUser,
                null,
                0.00M,
                "Recurring Payment Auth",
                TransactionType.CreditCardValidation,
                TransactionDirection.Debit,
                AccountID,
                new List<BillingAmount>(),
                provider
                );

            trans.SaveNew();
            trans.SaveBillingAmounts();

            int? atmWithdrawalCount = null;
            decimal? atmWithdrawalTotal = null;
            int? enrolledDays = null;
            Guid? childID = null;

            if (destinationUser is Teen)
            {
                enrolledDays = ((Teen)destinationUser).CurrentProgramTotalDaysEnrolled;
                childID = destinationUser.UserID;
            }

            if (destinationAccount is PrepaidCardAccount)
            {
                int prepaidAtmWithdrawalCount;
                decimal prepaidAtmWithdrawalTotal;
                ((PrepaidCardAccount)destinationAccount).GetAtmStatistics(out prepaidAtmWithdrawalCount, out prepaidAtmWithdrawalTotal);
                atmWithdrawalCount = prepaidAtmWithdrawalCount;
                atmWithdrawalTotal = prepaidAtmWithdrawalTotal;
            }

            Product? childProduct = null;

            if (destinationUser is Teen)
            {
                Teen teen = (Teen)destinationUser;
                childProduct = teen.CurrentProduct.ProductNumber;
            }

            if (!trans.Authorization(this, BusinessParentUser.UserID, childID, atmWithdrawalCount, atmWithdrawalTotal, BusinessParentUser.FinancialAccounts.BlacklistedCount, actingUserID, enrolledDays, childProduct, out failedAVS))
            {
                this.Status = AccountStatus.DoNotAllowMoneyMovement;
                return false;
            }

            return true;
        }

        #endregion //Methods

        #region Rules

        /// <summary>
        /// Adds the business rules.
        /// </summary>
        protected override void AddBusinessRules()
        {
            ValidationRules.AddRule<CreditCardAccount>(CheckNewAccountBlackListing, "NewAccount");

            base.AddBusinessRules();
        }

        /// <summary>
        /// Checks that a new account is not black listed.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="e">The e.</param>
        /// <returns></returns>
        private bool CheckNewAccountBlackListing(CreditCardAccount target, RuleArgs e)
        {
            if (target.IsNew && target.IsBlackListed())
            {
                e.Description = NEW_ACCOUNT_BLACK_LISTED;
                return false;
            }

            return true;
        }

        #endregion //Rules

        #region Post Save Processing

        protected override void ParentEvents_UpdateData(object sender, UpdateDataEventArgs args)
        {

            base.ParentEvents_UpdateData(sender, args);
        }

        /// <summary>
        /// Handles the UpdateDataSuccess event of the ParentEvents control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected override void ParentEvents_UpdateDataSuccess(object sender, UpdateSuccessEventArgs e)
        {
            //Journal Entries
            if (_postSaveJournalData.Count > 0)
            {
                if (!ProcessDataChangedJournalEntries(e.ConvertActingUserToGuid()))
                {
                    e.CancelUpdate = true;
                }
            }
            //Account Status Changed
            if (_accountStatusChanged)
            {
                if (!ProcessAccountStatusChangedBit(e.ConvertActingUserToGuid()))
                {
                    e.CancelUpdate = true;
                }
            }
            //Account Deleted
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
            _postSaveJournalData.Clear();
            _accountStatusChanged = false;
            _accountDeleted = false;
        }

        /// <summary>
        /// Creates a journal entry for each key in the _postSaveJournalData property bag
        /// </summary>
        /// <param name="actingUserID">The acting user ID.</param>
        /// <returns></returns>
        private bool ProcessDataChangedJournalEntries(Guid? actingUserID)
        {
            //Create a journal entry for each key in the bag
            foreach (string key in _postSaveJournalData.Keys)
            {
                if (!CreateJournalEntry(_postSaveJournalData[key].ToString(), actingUserID))
                {
                    base.WritePostSaveErrorToLog(this.ToString(), "ProcessDataChangedJournalEntries",
                        string.Format(ERROR_JOURNAL_ENTRY_CREATION, AccountID.ToString(), BusinessParentUser.UserName));
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Processes the account status changed bit.
        /// </summary>
        /// <param name="actingUserID">The acting user ID.</param>
        /// <returns></returns>
        private bool ProcessAccountStatusChangedBit(Guid? actingUserID)
        {
            //If the status was changed to DNAMM Cancel all pending jobs
            if (Status == AccountStatus.DoNotAllowMoneyMovement)
            {
                //Delete all allowances using this funding account
                EntityCollection<ScheduledItemEntity> allowances = AdapterFactory.FinancialAccountDataAdapter.RetrieveAllowancesUsingAccountForFundingAccount(AccountID);
                foreach (ScheduledItemEntity allowance in allowances)
                {
                    // Retrieve the child who the allowance belongs to.
                    Teen allowanceChild = User.RetrieveUser(allowance.UserId) as Teen;
                    if (ServiceFactory.AllowanceService.DeleteAllowance(allowance.ScheduledItemId))
                    {
                        if (allowanceChild != null)
                        {
                            // Send a notification for the cancelled allowance.
                            ServiceFactory.NotificationService.AllowanceCanceled(BusinessParentUser as Parent, allowanceChild, allowance);
                        }
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Processes the account deleted post save bit.
        /// </summary>
        /// <param name="actingUserID">The acting user ID.</param>
        /// <returns></returns>
        private bool ProcessAccountDeleted(Guid? actingUserID)
        {
            //Set all teen threshold that uses this funding account to null
            foreach (Teen teen in (BusinessParentUser as Parent).Teens)
            {
                if (teen.PaymentThreshold.TransactionAccountInfo != null && teen.PaymentThreshold.TransactionAccountInfo.FundingAccountId == AccountID)
                {
                    AdapterFactory.UserAdapter.ResetPaymentThreshold(teen.UserEntity as RegisteredTeenEntity);
                }
            }

            //Send Notification
            if (BusinessParentUser != null && (BusinessParentUser as Parent) != null)
            {
                BusinessParentUser.NotificationService.CreditCardAccountDeleted(BusinessParentUser as Parent, this);
            }

            //Create Journal Entry
            CreateJournalEntry("Account Deleted", actingUserID);

            return true;
        }

        #endregion //Post Save Processing

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CreditCardAccount"/> class.
        /// </summary>
        /// <param name="creditCardAccount">The credit card account.</param>
        /// <param name="parentObj">The parent obj.</param>
        /// <param name="parentEventBag">The parent event bag.</param>
        public CreditCardAccount(CreditCardAccountEntity creditCardAccount, BusinessBase parentObj, IParentEventBag parentEventBag)
            : base(creditCardAccount, parentObj, parentEventBag, new DataAccessAdapterFactory())
        {

        }

        #endregion //Constructor

        #region Private Methods

        /// <summary>
        /// Determines whether [is job child of credit card transaction for this account] [the specified job].
        /// </summary>
        /// <param name="job">The job.</param>
        /// <returns>
        /// 	<c>true</c> if [is job child of credit card transaction for this account] [the specified job]; otherwise, <c>false</c>.
        /// </returns>
        private bool IsJobChildOfCreditCardTransactionForThisAccount(JobEntity job)
        {
            //Get the primary Job ID
            EntityCollection<LinkedJobEntity> primaryJobs = ServiceFactory.JobService.RetrievePrimaryLinkedJobs(job.JobId);
            if (primaryJobs.Count == 0) return false;
            else
            {
                //Get the job
                JobEntity parentJob = ServiceFactory.JobService.RetrieveJob(primaryJobs[0].PrimaryJobId);

                if (parentJob.JobType != JobType.CreditCardTransactionJob)
                {
                    return IsJobChildOfCreditCardTransactionForThisAccount(parentJob);
                }
                else
                {
                    CreditCardTransactionJobEntity cardJob = parentJob as CreditCardTransactionJobEntity;
                    return (cardJob.CreditCardAccountId == AccountID);
                }
            }
        }

        /// <summary>
        /// Finds an account to set to active.
        /// </summary>
        /// <returns></returns>
        private bool FindAccountToSetToActive()
        {
            bool foundAccountToActivate = false;

            //Try to find an active account first
            foreach (CreditCardAccount creditCardAccount in BusinessParentUser.FinancialAccounts.CreditCardAccounts)
            {
                if (creditCardAccount.AccountID != AccountID && creditCardAccount.IsActive)
                {
                    foundAccountToActivate = true;
                    return foundAccountToActivate;
                }
            }

            //Try to find a good account (AMM) second
            foreach (CreditCardAccount creditCardAccount in BusinessParentUser.FinancialAccounts.CreditCardAccounts)
            {
                if (creditCardAccount.AccountID != AccountID && creditCardAccount.Status == AccountStatus.AllowMoneyMovement)
                {
                    creditCardAccount.IsActive = true;
                    foundAccountToActivate = true;
                    return foundAccountToActivate;
                }
            }

            //If no accounts are found just leave this one as active
            return foundAccountToActivate;
        }

        #endregion //Private Methods

        #region ICreditCardInfo Members

        public bool IsDefault
        {
            get
            {
                return IsActive;
            }
            set
            {
                IsActive = value;
            }
        }

        public string PAR
        {
            get
            {
                return _par;
            }
            set
            {
                _par = value;
            }
        }

        #endregion

        #region Misc Methods
        /// <summary>
        /// Retrieves the total number of active CreditCardAccounts
        /// </summary>
        /// <returns></returns>
        public static int RetrieveActiveAccountTotal()
        {
            return AdapterFactory.FinancialAccountDataAdapter.RetrieveActiveCreditAccountTotal();
        }

        public static CreditCardAccount RetrieveCreditCardAccountByID(Guid accountID)
        {
            User owningUser = User.RetrieveUserByCreditCardAccountID(accountID);
            return owningUser.FinancialAccounts.GetAccountByID(accountID) as CreditCardAccount;
        }

        public static CreditCardAccount RetrieveCreditCardAccountByID(Guid accountID, out User owner)
        {
            owner = User.RetrieveUserByCreditCardAccountID(accountID) as User;
            if (owner != null)
            {
                return owner.FinancialAccounts.GetAccountByID(accountID) as CreditCardAccount;
            }

            return null;
        }
        #endregion

        #region Capture Error Methods

        /// <summary>
        /// Processes the billing aquisition failure.
        /// </summary>
        /// <param name="creditCardJob">The credit card job.</param>
        /// <returns></returns>
        public static bool ProcessBillingAquisitionFailure(CreditCardTransactionJob creditCardJob)
        {
            Error error = null;
            EntityCollection<BillingAmountEntity> billingAmounts = ServiceFactory.TransactionService.RetrieveBillingAmounts(creditCardJob.JobID, out error);
            foreach (BillingAmountEntity amount in billingAmounts)
            {
                if (amount.Product == (int)Product.PPaid_Monthly_Service_Fee_IC)
                {

                    Parent parent = User.RetrieveUser(creditCardJob.UserID) as Parent;
                    if (parent != null)
                    {
                        // Create an activity for failed monthly billing.
                        string message = "Credit card billing failed.";
                        ServiceFactory.ActivityService.CreateMonthlyBillingFailedActivity(parent.UserEntity, message);

                        if (parent.FinancialAccounts.ActiveCreditCardAccount != null)
                        {
                            parent.FinancialAccounts.ActiveCreditCardAccount.Status = AccountStatus.DoNotAllowMoneyMovement;
                            new Family(parent).Lock();
                            return true;
                            //return parent.FinancialAccounts.ActiveCreditCardAccount.LockUserAndFamily();
                        }
                    }
                }
            }
            return true;
        }

        #endregion //Capture Error Methods

    }
}
