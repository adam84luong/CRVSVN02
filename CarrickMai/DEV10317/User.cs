#region Copyright PAYjr Inc. 2005-2013
//
// All rights are reserved. Reproduction in whole or in part, in any 
// form or by any means, electronic, mechanical or otherwise, is    
// prohibited  without the prior written consent of the copyright owner.
//
#endregion

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Transactions;
using System.Web;
using Common.Business;
using Common.Business.Validation.Rules;
using Common.Util.Time;
using NLog;
using Payjr.Configuration;
using Payjr.Core.Adapters;
using Payjr.Core.BrandingSite;
using Payjr.Core.FinancialAccounts;
using Payjr.Core.Jobs;
using Payjr.Core.Notifications;
using Payjr.Core.Providers;
using Payjr.Core.Services;
using Payjr.Core.Transactions;
using Payjr.Core.Transactions.Graphs;
using Payjr.Core.UserInfo;
using Payjr.Entity;
using Payjr.Entity.EntityClasses;
using Payjr.Entity.HelperClasses;
using Payjr.Types;
using SD.LLBLGen.Pro.ORMSupportClasses;

namespace Payjr.Core.Users
{
    /// <summary>
    /// Base class for Users
    /// </summary>
    /// <remarks>
    /// The factory methods will try to retrieve an existing user from the request cache (HttpContext.Items). If it can't
    /// find one there it will create a new instance of the user</remarks>
    public abstract class User : BusinessEntityParent<UserEntity>, IUserInfo
    {
        protected static Logger Log = LogManager.GetCurrentClassLogger();

        #region Fields

        private FinancialAccountList<IFinancialAccount> _accounts;
        private Site _site;
        private INotificationService _notificationService = ServiceFactory.NotificationService;
        private bool _exceptionOnUniqueUsername;
        private string _newPassword;
        private ICardProvider _cardProvider;
        private Policy _userPolicy;

        #region Save Bits
        protected bool _passwordReset = false;
        protected bool _sendAccountChangedNotification = false;
        protected bool _lockUser = false;
        protected bool _unLockUser = false;
        protected bool _acceptedTerms = false;
        protected bool _acceptedPrivacy = false;
        protected bool _acceptedCardHolder = false;
        protected bool _acceptedTarget = false;
        #endregion //Save Setting Bits

        #endregion //Fields

        #region Error Msgs

        #region Post Save Error Msgs
        #region Password Reset
        private const string ERROR_PASSWORD_RESET_FAILED_NOTIFICATION = "User {0} failed to save due to failed Reset Password Notification";
        private const string ERROR_PASSWORD_RESET_FAILED_ACTIVITY_CREATION = "User {0} failed to save due to failure of Changed Password Activity Creation";
        #endregion //Password Reset
        #region Account Information Changed
        private const string ERROR_ACCOUNT_INFO_CHANGED_FAILED_NOTIFICATION = "User {0} failed to save due to failed AccountInformation Changed Notification";
        #endregion //Account Information Changed
        #region Lock User
        private const string ERROR_LOCK_USER_FAILED_ACTIVITY_CREATION = "User {0} failed to save due to failed Creation of User Locked Activity";
        #endregion //Lock User
        #region Unlock User
        private const string ERROR_UNLOCK_USER_FAILED_ACTIVITY_CREATION = "User {0} failed to save due to failed Creation of User Unlocked Activity";
        #endregion //Unlock User
        #region Policy Version Change
        private const string ERROR_POLICY_CHANGED_FAILED_ACTIVITY_CREATION = "User {0} failed to save due to failed Creation of Terms Accepted Activity for {1}";
        #endregion //Policy Version Change
        #endregion //Post Save Error Msgs

        #endregion //Error Msgs

        #region Properties

        public INotificationService NotificationService
        {
            get
            {

                return _notificationService;
            }
            set
            {
                _notificationService = value;
            }
        }

        /// <summary>
        /// Set this with a mock for testing
        /// </summary>
        public ICardProvider CardProvider
        {
            get
            {
                if (_cardProvider == null)
                {
                    _cardProvider = Site.GetCardProvider(this) as ICardProvider;
                }
                return _cardProvider;
            }
            set
            {
                _cardProvider = value;
            }
        }

        #region Basic User Properties


        /// <summary>
        /// For derived classes to get to the entity
        /// </summary>
        public UserEntity UserEntity
        {
            get
            {
                return base.BackingEntity;
            }
        }

        /// <summary>
        /// Determines if a username is in use (Ignores the this user)
        /// </summary>
        public bool IsUserNameInUse
        {
            get
            {
                return AdapterFactory.UserAdapter.UserNameExists(UserName, UserID);
            }
        }

        /// <summary>
        /// Returns the marked for deletion bit for the user.
        /// </summary>
        public bool MarkedForDeletion
        {
            get
            {
                return base.BackingEntity.MarkedForDeletion;
            }
        }

        #region IUserInfo Properties

        /// <summary>
        /// The ID of the user
        /// </summary>
        public Guid UserID
        {
            get { return base.BackingEntity.UserId; }
        }

        /// <summary>
        /// The username of the user
        /// </summary>
        public string UserName
        {
            get
            {
                return base.BackingEntity.UserName;
            }
            set
            {
                base.BackingEntity.UserName = value;
                _exceptionOnUniqueUsername = false;
            }
        }

        /// <summary>
        /// Password
        /// </summary>
        public string Password
        {
            get
            {
                return base.BackingEntity.Password;
            }
            set
            {
                base.BackingEntity.Password = value;
            }
        }

        /// <summary>
        /// Password salt
        /// </summary>
        public string PasswordSalt
        {
            get
            {
                return base.BackingEntity.PasswordSalt;
            }
            set
            {
                base.BackingEntity.PasswordSalt = value;
            }
        }

        /// <summary>
        /// Password question
        /// </summary>
        public string PasswordQuestion
        {
            get
            {
                return base.BackingEntity.PasswordQuestion;
            }
            set
            {
                base.BackingEntity.PasswordQuestion = value;
            }
        }

        /// <summary>
        /// Password answer
        /// </summary>
        public string PasswordAnswer
        {
            get
            {
                return base.BackingEntity.PasswordAnswer;
            }
            set
            {
                base.BackingEntity.PasswordAnswer = value;
            }
        }

        /// <summary>
        /// The Type of user
        /// </summary>
        public RoleType RoleType
        {
            get
            {
                return base.BackingEntity.RoleType;
            }
            set
            {
                base.BackingEntity.RoleType = value;
            }
        }

        /// <summary>
        /// Receive Marketing Email
        /// </summary>
        public bool RecieveMarketingEmail
        {
            get
            {
                return base.BackingEntity.RecieveMarketingEmail;
            }
            set
            {
                base.BackingEntity.RecieveMarketingEmail = value;
            }
        }

        /// <summary>
        /// Last Activity Date
        /// </summary>
        public DateTime LastActivityDate
        {
            get
            {
                return base.BackingEntity.LastActivityDate;
            }
            set
            {
                base.BackingEntity.LastActivityDate = value;
            }
        }

        /// <summary>
        /// Last Login Date
        /// </summary>
        public DateTime LastLoginDate
        {
            get
            {
                return base.BackingEntity.LastLoginDate;
            }
            set
            {
                base.BackingEntity.LastLoginDate = value;
            }
        }

        /// <summary>
        /// Last Password Change Date
        /// </summary>
        public DateTime LastPasswordChangeDate
        {
            get
            {
                return base.BackingEntity.LastPasswordChangedDate;
            }
            set
            {
                base.BackingEntity.LastPasswordChangedDate = value;
            }
        }

        /// <summary>
        /// Creation Date
        /// </summary>
        public DateTime CreationDate
        {
            get
            {
                return base.BackingEntity.CreationDate;
            }
            set
            {
                base.BackingEntity.CreationDate = value;
            }
        }

        /// <summary>
        /// Is Online
        /// </summary>
        public bool IsOnLine
        {
            get
            {
                return base.BackingEntity.IsOnLine;
            }
            set
            {
                base.BackingEntity.IsOnLine = value;
            }
        }

        /// <summary>
        /// Is the user locked out
        /// </summary>
        public bool IsLockedOut
        {
            get
            {
                return base.BackingEntity.IsLockedOut;
            }
            set
            {
                base.BackingEntity.IsLockedOut = value;
            }
        }

        /// <summary>
        /// Last Locked Out Date
        /// </summary>
        public DateTime LastLockedOutDate
        {
            get
            {
                return base.BackingEntity.LastLockedOutDate;
            }
            set
            {
                base.BackingEntity.LastLockedOutDate = value;
            }
        }

        /// <summary>
        /// Failed Password Attempt Count
        /// </summary>
        public int FailedPasswordAttemptCount
        {
            get
            {
                return base.BackingEntity.FailedPasswordAttemptCount;
            }
            set
            {
                base.BackingEntity.FailedPasswordAttemptCount = value;
            }
        }

        /// <summary>
        /// Failed Password Attempt Window Start
        /// </summary>
        public DateTime FailedPasswordAttemptWindowStart
        {
            get
            {
                return base.BackingEntity.FailedPasswordAttemptWindowStart;
            }
            set
            {
                base.BackingEntity.FailedPasswordAttemptWindowStart = value;
            }
        }

        /// <summary>
        /// Failed Password Attempt Attempt Count
        /// </summary>
        public int FailedPasswordAttemptAttemptCount
        {
            get
            {
                return base.BackingEntity.FailedPasswordAttemptAttemptCount;
            }
            set
            {
                base.BackingEntity.FailedPasswordAttemptAttemptCount = value;
            }
        }

        /// <summary>
        /// Failed Password Answer Attempt Window Start
        /// </summary>
        public DateTime FailedPasswordAnswerAttemptWindowsStart
        {
            get
            {
                return base.BackingEntity.FailedPasswordAnswerAttemptWindowsStart;
            }
            set
            {
                base.BackingEntity.FailedPasswordAnswerAttemptWindowsStart = value;
            }
        }

        /// <summary>
        /// The time zone of the user
        /// </summary>
        public TimeZoneInformation TimeZone
        {
            get
            {
                return TimeZoneInformation.GetTimeZoneFromString(base.BackingEntity.TimeZone);
            }
            set
            {
                base.BackingEntity.TimeZone = value.ToString();
            }
        }

        /// <summary>
        /// CultureID
        /// </summary>
        public Guid CultureID
        {
            get
            {
                return base.BackingEntity.CultureId;
            }
            set
            {
                base.BackingEntity.CultureId = value;
            }
        }

        /// <summary>
        /// BrandingID
        /// </summary>
        public Guid BrandingId
        {
            get
            {
                return base.BackingEntity.BrandingId;
            }
            set
            {
                base.BackingEntity.BrandingId = value;
            }
        }

        /// <summary>
        /// ThemeID
        /// </summary>
        public Guid ThemeID
        {
            get
            {
                return base.BackingEntity.ThemeId;
            }
            set
            {
                base.BackingEntity.ThemeId = value;
            }
        }

        /// <summary>
        /// Last IP Address
        /// </summary>
        public string LastIPAddress
        {
            get
            {
                return base.BackingEntity.LastIpaddress;
            }
            set
            {
                base.BackingEntity.LastIpaddress = value;
            }
        }

        /// <summary>
        /// Last Host Name
        /// </summary>
        public string LastHostName
        {
            get
            {
                return base.BackingEntity.LastHostName;
            }
            set
            {
                base.BackingEntity.LastHostName = value;
            }
        }

        /// <summary>
        /// The first name of the user
        /// </summary>
        public string FirstName
        {
            get
            {
                return base.BackingEntity.FirstName;
            }
            set
            {
                base.BackingEntity.FirstName = value;
            }
        }

        /// <summary>
        /// The middle name of the user
        /// </summary>
        public string MiddleName
        {
            get
            {
                return base.BackingEntity.MiddleName;
            }
            set
            {
                base.BackingEntity.MiddleName = value;
            }
        }

        /// <summary>
        /// The last name of the user
        /// </summary>
        public string LastName
        {
            get
            {
                return base.BackingEntity.LastName;
            }
            set
            {
                base.BackingEntity.LastName = value;
            }
        }

        /// <summary>
        /// The user gender
        /// </summary>
        public string Gender
        {
            get
            {
                return base.BackingEntity.Sex;
            }
            set
            {
                base.BackingEntity.Sex = value;
            }
        }

        /// <summary>
        /// External User Identifier
        /// </summary>
        public string ExternalUserIdentifier
        {
            get
            {
                return base.BackingEntity.ExternalUserIdentifier;
            }
            set
            {
                base.BackingEntity.ExternalUserIdentifier = value;
            }
        }

        /// <summary>
        /// Anniversary Date
        /// </summary>
        public DateTime? AnniversaryDate
        {
            get
            {
                return base.BackingEntity.AnniversaryDate;
            }
            set
            {
                base.BackingEntity.AnniversaryDate = value;
            }
        }

        /// <summary>
        /// Is Active
        /// </summary>
        public bool IsActive
        {
            get
            {
                return base.BackingEntity.IsActive;
            }
            set
            {
                base.BackingEntity.IsActive = value;
            }
        }
        #endregion
        #endregion //Basic User Properties

        #region Email

        Email _email;
        /// <summary>
        /// User's Email address
        /// </summary>
        public Email Email
        {
            get
            {
                if (_email == null)
                {
                    EmailEntity emailEntity = AdapterFactory.EmailDataAdapter.RetrieveStandardEmail(UserID);

                    if (emailEntity != null && emailEntity.EmailType != EmailType.Mobile)
                    {
                        Email = new Email(emailEntity, this, EventBag, this.NotificationService);
                    }
                }
                else if (_email.IsDeleted == true)
                {
                    _email = null;
                }

                return _email;
            }
            private set
            {
                _email = value;
                AddChildForMonitoring(_email);
            }
        }

        /// <summary>
        /// Create a new <see cref="Email"/>
        /// </summary>
        /// <returns></returns>
        public Email NewEmail()
        {
            if (Email != null) throw new InvalidOperationException("Cannot add a new Email if the email already exists");

            EmailEntity emailEntity = AdapterFactory.EmailDataAdapter.CreateNewEmail(UserID);

            Email = new Email(emailEntity, this, EventBag, NotificationService);

            return Email;
        }

        Email _mobileEmail;
        // Email _mobileNumber = null;
        /// <summary>
        /// User's Mobile number
        /// </summary>
        public Email MobileNumber
        {
            get
            {
                if (_mobileEmail == null)
                {
                    EmailEntity emailEntity = AdapterFactory.EmailDataAdapter.RetrieveMobileEmail(UserID);
                    if (emailEntity != null)
                    {
                        MobileNumber = new Email(emailEntity, this, EventBag, NotificationService);
                    }
                }
                else if (_mobileEmail.IsDeleted == true)
                {
                    _mobileEmail = null;

                }

                return _mobileEmail;
            }
            private set
            {
                _mobileEmail = value;
                AddChildForMonitoring(_mobileEmail);
            }
        }

        /// <summary>
        /// Create a new <see cref="Email"/>
        /// </summary>
        /// <returns></returns>
        public Email NewMobileNumber()
        {
            if (MobileNumber != null) throw new InvalidOperationException("Cannot add a new Email if the email already exists");

            EmailEntity emailEntity = AdapterFactory.EmailDataAdapter.CreateNewEmail(UserID);
            emailEntity.EmailType = EmailType.Mobile;

            MobileNumber = new Email(emailEntity, this, EventBag, NotificationService);

            MobileNumber.RequiresValidation = false;

            return MobileNumber;
        }

        /// <summary>
        /// List of all the verified emails for the user
        /// </summary>
        public List<Email> VerifiedEmails
        {
            get
            {
                List<Email> verifiedEmails = new List<Email>();

                if (Email != null)
                {
                    if (Email.IsConfirmed)
                    {
                        verifiedEmails.Add(Email);
                    }
                }
                if (MobileNumber != null)
                {
                    if (MobileNumber.IsConfirmed)
                    {
                        verifiedEmails.Add(MobileNumber);
                    }
                }
                return verifiedEmails;
            }
        }

        #endregion //Email

        #region UserIdentifier

        protected UserIdentifier _sSN = null;
        /// <summary>
        /// Social Security number
        /// </summary>
        public UserIdentifier SSN
        {
            get
            {
                //only load the phone if it hasn't already been loaded
                if (_sSN == null)
                {
                    UserIdentifierEntity userIdentifierEntity = AdapterFactory.UserAdapter.RetrieveUserIdentifierByUserID(UserID);
                    if (userIdentifierEntity != null)
                    {
                        SSN = new UserIdentifier(userIdentifierEntity, this, EventBag);
                    }
                }
                return _sSN;
            }
            set
            {
                _sSN = value;
                AddChildForMonitoring(_sSN);
            }
        }
        public UserIdentifier UpdateSSN(string ssn)
        {
            if (SSN == null)
            {
                SSN = NewUserIdentifier();
            }
            if (SSN != null)
            {
                SSN.Identifier = ssn;
                SSN.Update(SSN);
            }
            return SSN;
        }
        public UserIdentifier NewUserIdentifier()
        {
            if (SSN != null) throw new InvalidOperationException("Cannot create a new UserIdentifier");

            UserIdentifierEntity userIdentifierEntity = AdapterFactory.UserAdapter.CreateUserIdentifier(UserEntity);

            SSN = new UserIdentifier(userIdentifierEntity, this, EventBag);

            return SSN;
        }

        #endregion //UserIdentifier

        #region Persistence Calls



        #endregion //Persistence Calls

        #region Alerts

        private AlertList _alerts = null;

        /// <summary>
        /// The list of alerts
        /// </summary>
        public AlertList Alerts
        {
            get
            {
                FillAlertList(ref  _alerts);
                return _alerts;
            }
        }

        /// <summary>
        /// Create a new Alert
        /// </summary>
        /// <returns></returns>
        public Alert NewAlert(Email email)
        {
            if (email == null)
                throw new ArgumentNullException("email", "You must provide the email to use");

            AlertEntity alertEntity = AdapterFactory.AlertAdapter.CreateAlert();
            alertEntity.StartWindowTime = "00";
            alertEntity.EndWindowTime = "24";
            //By default make the current email the Email for the Alert
            Alert alertCreated = new Alert(email, alertEntity, this, EventBag);

            AddChildForMonitoring(alertCreated);

            FillAlertList(ref _alerts);

            _alerts.Add(alertCreated);

            return alertCreated;

        }

        /// <summary>
        /// Fill the object with Alerts if it's needed
        /// </summary>
        /// <param name="alertList"></param>
        private void FillAlertList(ref AlertList alertList)
        {
            if (alertList == null)
            {
                //Add the alerts
                alertList = new AlertList();
                EntityCollection<AlertEntity> alertEntities = AdapterFactory.AlertAdapter.RetrieveAlerts(UserID);
                foreach (AlertEntity alertEntity in alertEntities)
                {
                    Email alertEmail = null;

                    //Get the email for the Alert
                    if (MobileNumber != null && MobileNumber.ID == alertEntity.EmailId)
                    {
                        alertEmail = MobileNumber;
                    }
                    else
                    {
                        alertEmail = Email;
                    }

                    if (alertEmail != null)
                    {
                        Alert alert = new Alert(alertEmail, alertEntity, this, EventBag);
                        alertList.Add(alert);
                        AddChildForMonitoring(alert);
                    }
                }

            }
            else
            {
                //Take out any deleted Alerts

                List<Alert> nonDeletedAlerts = new List<Alert>(_alerts.Count);

                foreach (Alert alert in _alerts)
                {
                    if (!alert.IsDeleted)
                    {
                        nonDeletedAlerts.Add(alert);
                    }
                }

                alertList = new AlertList(nonDeletedAlerts);

            }
        }


        #endregion //Alerts

        #region Policy

        /// <summary>
        /// The policy versions for the user.
        /// </summary>
        public Policy UserPolicy
        {
            get
            {
                if (_userPolicy == null)
                {
                    if (BackingEntity.Policy != null)
                    {
                        UserPolicy = new Policy(BackingEntity.Policy, this, EventBag, BrandingId);
                    }
                }
                return _userPolicy;
            }
            private set
            {
                _userPolicy = value;
                AddChildForMonitoring(_userPolicy);
            }
        }

        /// <summary>
        /// Create a new policy entity for the user.
        /// </summary>
        /// <returns></returns>
        public Policy NewPolicy()
        {
            if (UserPolicy != null) throw new InvalidOperationException("Cannot create a new policy");

            PolicyEntity policyEntity = AdapterFactory.UserAdapter.CreatePolicyForUser(UserEntity);
            UserPolicy = new Policy(policyEntity, this, EventBag, BrandingId);

            return UserPolicy;
        }

        /// <summary>
        /// Sets the version of the given policy type to the current version
        /// </summary>
        public void AgreeToPolicy(SiteDocument.SiteDocumentType policyType)
        {
            //If the user policy is null throw an exception
            if (UserPolicy == null) { throw new InvalidOperationException("Cannot set the version of the policy if the User Policy is null"); }

            //Get the current site doc for the given type
            SiteDocument sdoc = Site.GetSiteDocument(policyType);
            //If the doc comes back null, throw an exception
            if (sdoc == null) { throw new InvalidOperationException("Cannot set the version of the policy.  A policy for " + policyType.ToString() + " has not been uploaded"); }

            //Set User Policy to current version
            switch (policyType)
            {
                case SiteDocument.SiteDocumentType.PrivacyPolicy:
                    UserPolicy.PolicyVersion = sdoc.Version;
                    break;
                case SiteDocument.SiteDocumentType.SiteTermsAndConditions:
                    UserPolicy.TermsAndConditionVersion = sdoc.Version;
                    break;
                case SiteDocument.SiteDocumentType.CardholderAgreement:
                    UserPolicy.CardHolderTermsVersion = sdoc.Version;
                    break;
                case SiteDocument.SiteDocumentType.TargetGiftCardConditions:
                    UserPolicy.TargetTermsVersion = sdoc.Version;
                    break;
                default:
                    break;
            }
        }



        #endregion //policy

        #region Address


        protected Address _userAddress;
        /// <summary>
        /// User's address
        /// </summary>
        public Address UserAddress
        {
            get
            {
                //only load the address if it hasn't already been loaded
                if (_userAddress == null)
                {
                    AddressEntity addressEntity = AdapterFactory.UserAdapter.RetrieveUserAddress(UserID);
                    if (addressEntity != null)
                    {
                        UserAddress = new Address(addressEntity, this, EventBag);
                    }

                }
                return _userAddress;
            }
            private set
            {
                //Set a new value for the address and 
                //add it to the parent for monitoring

                _userAddress = value;
                AddChildForMonitoring(_userAddress);
            }
        }

        /// <summary>
        /// Create a new Address
        /// </summary>
        /// <returns></returns>
        public Address NewAddress()
        {
            if (UserAddress != null) throw new InvalidOperationException("Cannot create a new address");

            AddressEntity addressEntity = AdapterFactory.UserAdapter.CreateAddressForUser(UserEntity);

            UserAddress = new Address(addressEntity, this, EventBag);

            return UserAddress;
        }

        /// <summary>
        /// Assign an existing address to the user
        /// </summary>
        /// <param name="address"></param>
        public void AssignAddress(Address address)
        {
            if (address == null) throw new ArgumentNullException("address", "The Address cannot be null on assignment");
            if (UserAddress != null) throw new InvalidOperationException("Two Address cannot be assigned to the user");

            AddEntityForSave(AdapterFactory.UserAdapter.AssignAddressToUser(UserEntity, address.AddressId), true);
        }


        #endregion //Address

        #region Phone


        protected Phone _phone;
        /// <summary>
        /// User's address
        /// </summary>
        public Phone Phone
        {
            get
            {
                //only load the address if it hasn't already been loaded
                if (_phone == null)
                {
                    PhoneEntity phoneEntity = AdapterFactory.UserAdapter.RetrieveUserPhoneInfo(UserID);
                    if (phoneEntity != null)
                    {
                        Phone = new Phone(phoneEntity, this, EventBag);
                    }

                }
                return _phone;
            }
            private set
            {
                //Set a new value for the address and 
                //add it to the parent for monitoring

                _phone = value;
                AddChildForMonitoring(_phone);
            }
        }

        /// <summary>
        /// Create a new Address
        /// </summary>
        /// <returns></returns>
        public Phone NewPhone()
        {
            if (Phone != null) throw new InvalidOperationException("Cannot create a new Phone");

            PhoneEntity phoneEntity = AdapterFactory.UserAdapter.CreatePhone(UserEntity);

            Phone = new Phone(phoneEntity, this, EventBag);

            return Phone;
        }

        /// <summary>
        /// Assign an existing phone to the user
        /// </summary>
        /// <param name="address"></param>
        public void AssignPhone(Phone phone)
        {
            if (phone == null) throw new ArgumentNullException("phone", "The Phone cannot be null on assignment");
            if (Phone != null) throw new InvalidOperationException("Two Phones cannot be assigned to the user");

            AddEntityForSave(AdapterFactory.UserAdapter.AssignPhoneToUser(UserEntity, phone.ID), true);
        }





        #endregion //Phone

        #region Financial accounts

        /// <summary>
        /// The collection of financial accounts for the user
        /// </summary>
        /// <value>The financial accounts.</value>
        public FinancialAccountList<IFinancialAccount> FinancialAccounts
        {
            get
            {
                if (_accounts == null)
                {
                    _accounts = new FinancialAccountList<IFinancialAccount>(RetrieveAccounts());
                }
                return _accounts;
            }
        }

        /// <summary>
        /// Creates a new ACH Account
        /// </summary>
        /// <returns></returns>
        public ACHAccount NewACHAccount()
        {
            //Cannot add a new bank if the user has an unverified bank
            if (FinancialAccounts.GetUnverifiedACHAccount() != null) { throw new InvalidOperationException("Cannot create ACH account for user with an existing unverified account"); }

            BankAccountEntity bank = AdapterFactory.FinancialAccountDataAdapter.CreateBankAccount(UserEntity);
            ACHAccount achAccount = new ACHAccount(bank, this, EventBag);

            //clear accounts list
            AddChildForMonitoring(achAccount);
            FinancialAccounts.AddItem(achAccount);

            return achAccount;
        }

        /// <summary>
        /// Creates a new Prepaid Card Account
        /// </summary>
        /// <returns></returns>
        public PrepaidCardAccount NewPrepaidCardAccount()
        {
            PrepaidCardAccountEntity card = AdapterFactory.FinancialAccountDataAdapter.CreatePrepaidCardAccount(UserEntity);
            PrepaidCardAccount cardAccount = new PrepaidCardAccount(card, this, EventBag);

            //clear accounts list
            AddChildForMonitoring(cardAccount);
            FinancialAccounts.AddItem(cardAccount);

            return cardAccount;
        }

        /// <summary>
        /// Creates a new Target account.
        /// </summary>
        /// <returns></returns>
        public TargetAccount NewTargetAccount()
        {
            TargetAccountEntity giftCard = AdapterFactory.TargetAdapter.CreateNewTargetAccountEntity(UserEntity);
            TargetAccount targetAccount = new TargetAccount(giftCard, this, EventBag);

            // Clear accounts list
            AddChildForMonitoring(targetAccount);
            FinancialAccounts.AddItem(targetAccount);

            return targetAccount;
        }

        /// <summary>
        /// Creates a new Savings Card Account
        /// </summary>
        /// <returns></returns>
        public SavingsAccount NewSavingsAccount()
        {
            CustomAccountFieldGroupEntity savingsEntity = new CustomAccountFieldGroupEntity();
            savingsEntity.UserId = UserID;
            SavingsAccount savingsAccount = new SavingsAccount(savingsEntity, this, EventBag);

            AddChildForMonitoring(savingsAccount);
            FinancialAccounts.AddItem(savingsAccount);

            return savingsAccount;
        }

        /// <summary>
        /// News the credit card account.
        /// </summary>
        /// <returns></returns>
        public CreditCardAccount NewCreditCardAccount(bool checkUserCanAddCard = true)
        {
            if (checkUserCanAddCard)
            {
                if (!this.CanAddNewCreditCardAccount)
                {
                    throw new InvalidOperationException("Cannot add a new funding account for this person at this time. Please use" +
                        "CanAddNewCreditCardAccount to check before performing this action to determine if the user has the ability" +
                        "to add a new credit card");
                }
            }

            CreditCardAccountEntity creditCardEntity = new CreditCardAccountEntity();
            creditCardEntity.UserId = UserID;
            CreditCardAccount creditCardAccount = new CreditCardAccount(creditCardEntity, this, EventBag);

            AddChildForMonitoring(creditCardAccount);
            FinancialAccounts.AddItem(creditCardAccount);

            return creditCardAccount;
        }

        /// <summary>
        /// Gets the financial transactions for all of the user's accounts.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns></returns>
        public List<FinancialTransaction> GetFinancialTransactions(DateTime startDate, DateTime endDate)
        {
            List<FinancialTransaction> retList = new List<FinancialTransaction>();

            //Get all the financial transactions
            foreach (IFinancialAccount account in FinancialAccounts)
            {
                retList.AddRange(account.RetrieveTransactions(startDate, endDate));
            }

            //Get all pending allowances
            if (RoleType == RoleType.Parent)
            {
                //Do not get pending allowances if the parent is pending
                if (!FinancialAccounts.IsUserInPendingStatus())
                {
                    //Get the pending allowance jobs for each teen in savings and prepaid
                    foreach (Teen teen in (this as Parent).Teens)
                    {
                        //do not get pending allowances for a Teen if the teen's account is pending
                        if (!teen.FinancialAccounts.IsUserInPendingStatus())
                        {
                            if (teen.IsUserPrepaid || teen.IsUserSavings || teen.IsUserTargetGiftCard)
                            {
                                EntityCollection<AllowanceJobEntity> allowances = ServiceFactory.AllowanceService.RetrievePendingAllowanceJobs(teen.UserID, startDate, endDate);
                                foreach (AllowanceJobEntity allowance in allowances)
                                {
                                    retList.Add(new PendingAllowanceTransaction(allowance, teen));
                                }
                            }
                        }
                    }
                }
            }

            return retList;
        }

        #endregion //financial accounts

        #region MFA

        /// <summary>
        /// Flag indicate whether or not this user requires multi-factor authentication
        /// </summary>
        private bool _isMFASet = false;
        private bool _isMFA;
        public bool IsMFA
        {
            get
            {
                // See if this property was previously set - no need to recalculate it.
                if (!_isMFASet)
                {
                    _isMFASet = true;
                    _isMFA = false;

                    if (base.BackingEntity != null)
                    {
                        // RetrieveProductsByUser should return all products for a user.
                        // In the case of a parent, it should return the aggregate of the children's products.
                        EntityCollection<ProductUserEntity> userProducts = AdapterFactory.UserAdapter.RetrieveProductsByUser(base.BackingEntity.UserId);

                        if ((userProducts != null) &&
                             (userProducts.Count > 0))
                        {
                            // For each product that this site has configured, see if the user's role matches those which
                            // are configured for MFA.  If it matches, then see if the user is enrolled in that product.
                            foreach (SiteConfigurationProduct siteProduct in SiteManager.GetSiteConfiguration(base.BackingEntity.BrandingId).Products)
                            {
                                // Don't bother looping if user is already determined to be MFA
                                if (!_isMFA)
                                {
                                    //If multi-factor is not configured then the object will be null
                                    if (siteProduct.MultiFactorConfiguration != null)
                                    {
                                        foreach (SiteConfigurationProductMultiFactorConfigurationRole role in siteProduct.MultiFactorConfiguration.Roles)
                                        {
                                            // Don't bother looping if user is already determined to be MFA
                                            if (!_isMFA)
                                            {
                                                if (base.BackingEntity.RoleType == (RoleType)role.Type)
                                                {
                                                    foreach (ProductUserEntity userProduct in userProducts)
                                                    {
                                                        if (userProduct.ProductNumber - 100000 == (Product)siteProduct.Number)
                                                        {
                                                            _isMFA = true;
                                                            break;
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return _isMFA;
            }
        }

        #endregion //MFA

        #region Site

        /// <summary>
        /// The Site that the user is enrolled in
        /// </summary>
        public Site Site
        {
            get
            {
                if (_site == null)
                {
                    _site = new Site(BrandingId);
                }
                return _site;
            }
        }

        #endregion //Site

        #endregion //Properties

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="teenID"></param>
        public User(UserEntity userEntity)
            : base(userEntity, new DataAccessAdapterFactory())
        {
            if (userEntity == null) throw new ArgumentNullException("userEntity cannot be null");
            _exceptionOnUniqueUsername = false;

            //initialize save bits
            _passwordReset = false;
            _sendAccountChangedNotification = false;

            EventBag.UpdateExceptionEncountered += new EventHandler<DataExceptionEventArgs>(EventBag_UpdateExceptionEncountered);
        }

        #endregion //Constructor

        #region Factory Methods

        #region Retrieve

        public static User RetrieveUser(Guid userID)
        {
            return RetrieveUser(HttpContext.Current, userID);
        }

        public static User RetrieveUser(UserEntity userEntity)
        {
            return CreateUser(null, userEntity.UserId, userEntity);
        }

        internal static List<Teen> RetrieveTeensByParent(Guid parentID)
        {
            return RetrieveTeensByParent(HttpContext.Current, parentID);
        }

        /// <summary>
        /// Retrieve a list of models based on the parent ID
        /// </summary>
        /// <param name="parentID"></param>
        /// <returns></returns>
        internal static List<Teen> RetrieveTeensByParent(HttpContext context, Guid parentID)
        {
            List<Teen> teenModels = new List<Teen>();
            EntityCollection<RegisteredTeenEntity> teens = AdapterFactory.UserAdapter.RetrieveTeensByParentID(parentID, true);

            foreach (RegisteredTeenEntity teen in teens)
            {
                //Go check the cache first if we find something then return that one instead
                if (context != null)
                {
                    Teen model = (Teen)context.Items[teen.UserId];

                    if (model != null)
                    {
                        teenModels.Add(model);
                        continue;
                    }
                }

                //Create a new one since we didn't find one
                Teen teenModel = new Teen(teen);
                if (context != null)
                {
                    context.Items.Add(teen.UserId, teenModel);
                }
                teenModels.Add(teenModel);
            }

            return teenModels;
        }

        /// <summary>
        /// Create a new user model
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public static User RetrieveUserByUserName(HttpContext context, string username, string hostname, string IPAddress)
        {
            UserEntity user = null;
            if (AdapterFactory.UserAdapter.RetrieveUser(username, hostname, IPAddress, out user))
            {
                if (user != null)
                {
                    User retUser = CreateUser(context, user.UserId, user);
                    retUser.Save(null);
                    return retUser;
                }
            }
            return null;
        }

        /// <summary>
        /// Create a new user model
        /// </summary>
        /// <remarks>This will not retrieve users that have been deleted</remarks>
        /// <param name="userID"></param>
        /// <returns></returns>
        public static User RetrieveUserByUserName(HttpContext context, string username)
        {
            UserEntity user = null;
            user = AdapterFactory.UserAdapter.RetrieveByUserName(username);

            if (user != null)
            {
                //Go check the cache first if we find something then return that one instead
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];

                    if (model != null)
                    {
                        return model;
                    }
                }

                return CreateUser(context, user.UserId, user);
            }
            return null;
        }

        /// <summary>
        /// Create a new user model
        /// </summary>
        /// <remarks>This will include users that have been deleted</remarks>
        /// <param name="userID"></param>
        /// <returns></returns>
        public static User RetrieveUser(HttpContext context, Guid userID)
        {
            //Go check the cache first if we find something then return that one instead
            if (context != null)
            {
                User model = (User)context.Items[userID];

                if (model != null)
                {
                    return model;
                }
            }
            //Go get the entity for the user -- Include deleted users

            UserEntity user = AdapterFactory.UserAdapter.RetrieveUserByUserID(userID, true);

            if (user != null)
            {
                return CreateUser(context, userID, user);
            }
            return null;
        }

        /// <summary>
        /// Retrieve the user based on the email id.
        /// </summary>
        /// <param name="emailID"></param>
        /// <returns></returns>
        public static User RetrieveUserByEmailId(Guid emailID)
        {
            UserEntity user = AdapterFactory.UserAdapter.RetrieveUserByEmail(emailID);

            // Go get the entity for the user.
            if (user != null)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];
                    if (model != null)
                    {
                        return model;
                    }
                }

                return CreateUser(context, user.UserId, user);
            }
            return null;
        }

        /// <summary>
        /// Retrieve the user based on the email address.
        /// </summary>
        /// <param name="Email">Email address of the user to retrieve</param>
        /// <returns></returns>
        public static User RetrieveUserByEmail(String Email)
        {
            UserEntity user = AdapterFactory.UserAdapter.RetrieveUserByEmail(Email);

            // Go get the entity for the user.
            if (user != null)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];
                    if (model != null)
                    {
                        return model;
                    }
                }

                return CreateUser(context, user.UserId, user);
            }
            return null;
        }

        /// <summary>
        /// Retrieve the user based on a <see cref="CustomCardEntity"/> ID
        /// </summary>
        /// <param name="emailID"></param>
        /// <returns></returns>
        public static User RetrieveUserByCustomCardDesignID(Guid customCardDesignID)
        {
            // Go get the entity for the user.
            CustomCardDesignUserEntity cardDesign = AdapterFactory.CardDesignsDataAdapter.RetrieveUserCardDesignFromDesignID(customCardDesignID);

            if (cardDesign != null)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[cardDesign.UserId];
                    if (model != null)
                    {
                        return model;
                    }
                }

                return RetrieveUser(cardDesign.UserId);
            }

            return null;
        }

        /// <summary>
        /// Retrieves a user by the server side ID
        /// </summary>
        /// <param name="serverSideID"></param>
        /// <returns></returns>
        public static User RetieveUserByServerSideID(string serverSideID)
        {
            // Go get the entity for the user.
            CustomCardDesignUserEntity cardDesign = AdapterFactory.CardDesignsDataAdapter.RetrieveUserCardDesignFromServerSideID(serverSideID);

            if (cardDesign != null)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[cardDesign.UserId];
                    if (model != null)
                    {
                        return model;
                    }
                }

                return RetrieveUser(cardDesign.UserId);
            }

            return null;
        }

        /// <summary>
        /// Retrieves the user by ACH account ID.
        /// </summary>
        /// <param name="AccountID">The account ID.</param>
        /// <returns></returns>
        public static User RetrieveUserByACHAccountID(Guid accountID)
        {
            UserEntity user = AdapterFactory.UserAdapter.RetrieveUserByACHAccountID(accountID);

            // Go get the entity for the user.
            if (user != null)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];
                    if (model != null)
                    {
                        return model;
                    }
                }

                return CreateUser(context, user.UserId, user);
            }
            return null;
        }

        /// <summary>
        /// Retrieves the user by Credit Card account ID.
        /// </summary>
        /// <param name="AccountID">The account ID.</param>
        /// <returns></returns>
        public static User RetrieveUserByCreditCardAccountID(Guid accountID)
        {
            UserEntity user = AdapterFactory.UserAdapter.RetrieveUserByCreditCardAccountID(accountID);

            // Go get the entity for the user.
            if (user != null)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];
                    if (model != null)
                    {
                        return model;
                    }
                }

                return CreateUser(context, user.UserId, user);
            }
            return null;
        }

        /// <summary>
        /// Retrieves the user by savings account ID.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public static User RetrieveUserBySavingsAccountID(Guid accountID)
        {
            UserEntity user = AdapterFactory.UserAdapter.RetrieveUserBySavingsAccountID(accountID);

            // Go get the entity for the user.
            if (user != null)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];
                    if (model != null)
                    {
                        return model;
                    }
                }

                return CreateUser(context, user.UserId, user);
            }
            return null;
        }

        /// <summary>
        /// Retrieves the user by prepaid card account ID.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public static User RetrieveUserByPrepaidCardAccountID(Guid accountID)
        {
            UserEntity user = AdapterFactory.UserAdapter.RetrieveUserByPrepaidCardAccountID(accountID);

            // Go get the entity for the user.
            if (user != null)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];
                    if (model != null)
                    {
                        return model;
                    }
                }

                return CreateUser(context, user.UserId, user);
            }
            return null;

        }

        /// <summary>
        /// Retrieves the user by prepaid card number.
        /// </summary>
        /// <param name="cardNumber">The card number.</param>
        /// <returns></returns>
        public static User RetrieveUserByPrepaidCardNumber(string cardNumber)
        {
            UserEntity user = AdapterFactory.UserAdapter.RetrieveUserByPrepaidCardNumber(cardNumber);

            // Go get the entity for the user.
            if (user != null)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];
                    if (model != null)
                    {
                        return model;
                    }
                }

                return CreateUser(context, user.UserId, user);
            }
            return null;
        }

        public static bool RetrieveUserForLoginByEmail(string email, string password, string userHostName, string userIPAddress, string fulldescription, out User user, out Error error)
        {
            String logMessage = email + " is trying to login in @ " + DateTime.UtcNow.ToString("r");
            Log.Info(logMessage);

            if (HttpContext.Current != null)
            {
                HttpBrowserCapabilities bc = HttpContext.Current.Request.Browser;

                Log.Info("Login = " + email + "|" +
                          "Password = " + password + "|" +
                          "IPAddress = " + userIPAddress + "|" +
                          "Type = " + bc.Type + "|" +
                            "Name = " + bc.Browser + "|" +
                            "Version = " + bc.Version + "|" +
                            "Major Version = " + bc.MajorVersion + "|" +
                            "Minor Version = " + bc.MinorVersion + "|" +
                            "Platform = " + bc.Platform + "|" +
                            "Is Beta = " + bc.Beta + "|" +
                            "Is Crawler = " + bc.Crawler + "|" +
                            "Is AOL = " + bc.AOL + "|" +
                            "Is Win16 = " + bc.Win16 + "|" +
                            "Is Win32 = " + bc.Win32 + "|" +
                            "Supports Frames = " + bc.Frames + "|" +
                            "Supports Tables = " + bc.Tables + "|" +
                            "Supports Cookies = " + bc.Cookies + "|" +
                            "Supports VB Script = " + bc.VBScript + "|" +
                            "Supports JavaScript = " + bc.EcmaScriptVersion + "|" +
                            "Supports Java Applets = " + bc.JavaApplets + "|" +
                            "Supports ActiveX Controls = " + bc.ActiveXControls + "|" +
                            "CDF = " + bc.CDF + "|"
                    //"CLR = " + bc.ClrVersion + "|"
                    );
            }
            else
            {
                Log.Info("Login = " + email + "|" +
                    "Password = " + password + "|" +
                    "IPAddress = " + userIPAddress + "|No context available");
            }

            User findUser = RetrieveUserByEmail(email);
            bool success = false;
            error = new Error(ErrorCode.unknown);

            if (findUser != null)
            {
                if (findUser.IsActive == false)
                {
                    ServiceFactory.ActivityService.CreateLoginFailedActivity(findUser.UserID, "User is not Active.Host:" + userHostName + " IP: " + userIPAddress + "Agent:" + fulldescription);

                    error = new Error(ErrorCode.UserLoginNotActivated);
                    success = false;
                }
                else
                {
                    string hashedPwd = AdapterFactory.UserAdapter.CreateSecurePassword(findUser.PasswordSalt, password);

                    //We start the lockout process if the password is incorrect.
                    if (hashedPwd.Trim() != findUser.Password)
                    {
                        //Change the attempt count first then find the endwindow before updating start window
                        findUser.FailedPasswordAttemptCount += 1;
                        DateTime windowEnd = findUser.FailedPasswordAttemptWindowStart.AddMinutes(30);
                        findUser.FailedPasswordAttemptWindowStart = DateTime.UtcNow;

                        //If they have waited past the 30 mins.
                        if (DateTime.UtcNow > windowEnd)
                        {
                            //Reset the counter to one to restart the lockout process.
                            findUser.FailedPasswordAttemptCount = 1;
                            error = new Error(ErrorCode.UserPasswordMismatch);
                        }

                        //If their count is higher than alloted attempts we tell them they are locked out.
                        else if (findUser.FailedPasswordAttemptCount >= 3)
                        {
                            //Slide the window so that they are locked out for another 30 mins.
                            findUser.FailedPasswordAttemptCount = 3;
                            error = new Error(ErrorCode.UserPasswordMismatch);
                        }
                        else
                        {
                            error = new Error(ErrorCode.UserPasswordMismatch);
                        }
                        //Save out the user.
                        findUser.Save(null);

                        Log.Info("Login = " + email + "|" +
                                "Role = " + findUser.RoleType.ToString() + "|" +
                                "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                "LastIPAddress = " + userIPAddress + "|" +
                                "FailedPasswordAttemptCount = " + findUser.FailedPasswordAttemptCount + "|" +
                                "FailedPasswordAttemptWindowStart = " + findUser.FailedPasswordAttemptWindowStart + "|Password mismatch");

                        if (ServiceFactory.ActivityService.CreateLoginFailedActivity(findUser.UserID, " Attempt:" + findUser.FailedPasswordAttemptCount + " Start Window:" + findUser.FailedPasswordAttemptWindowStart + " Host:" + userHostName + " IP: " + userIPAddress + "Agent:" + fulldescription))
                        {
                            Log.Info("Login = " + email + "|" +
                                    "Role = " + findUser.RoleType.ToString() + "|" +
                                    "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                    "LastIPAddress = " + userIPAddress + "|successfully created failed login activity");
                        }
                        else
                        {
                            Log.Info("Login = " + email + "|" +
                                "Role = " + findUser.RoleType.ToString() + "|" +
                                "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                "LastIPAddress = " + userIPAddress + "|failed to create login activity");

                        }

                        success = false;
                    }
                    else
                    {
                        //Success login


                        // TODO: Understand the error scenario for Host name length == 0.
                        if (userHostName.Length != 0)
                        {
                            //Find the unlock time from the start window of the user.
                            DateTime windowEnd = findUser.FailedPasswordAttemptWindowStart.AddMinutes(30);
                            //If they have waited past the 30 mins.
                            if ((DateTime.UtcNow > windowEnd) || (findUser.FailedPasswordAttemptCount <= 2))
                            {
                                AdapterFactory.UserAdapter.UpdateDBWithBrowserSetting(userHostName, userIPAddress, findUser.UserEntity);
                                //Update the Security info with the information
                                findUser.LastLoginDate = DateTime.UtcNow;
                                //Update the attempt count to zero if they login.
                                findUser.FailedPasswordAttemptCount = 0;

                                findUser.Save(null);
                                error = null;
                                success = true;

                                Log.Info("Login = " + email + "|" +
                                    "Role = " + findUser.RoleType.ToString() + "|" +
                                    "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                    "LastIPAddress = " + userIPAddress + "|Successful Login");

                                //Ok we're good to go -- let's log it
                                if (ServiceFactory.ActivityService.CreateLoginActivity(findUser.UserID, "Host:" + userHostName + " IP: " + userIPAddress + "Agent:" + fulldescription))
                                {
                                    Log.Info("Login = " + email + "|" +
                                        "Role = " + findUser.RoleType.ToString() + "|" +
                                        "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                        "LastIPAddress = " + userIPAddress + "|successfully created login activity");
                                }
                                else
                                {
                                    Log.Info("Login = " + email + "|" +
                                        "Role = " + findUser.RoleType.ToString() + "|" +
                                        "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                        "LastIPAddress = " + userIPAddress + "|failed to create login activity");
                                    error = new Error(ErrorCode.SystemLoginError);
                                    success = false;
                                }

                            }
                            else
                            {
                                Log.Info("Login = " + email + "|" +
                                        "Role = " + findUser.RoleType.ToString() + "|" +
                                        "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                        "LastIPAddress = " + userIPAddress + "|" +
                                        "FailedPasswordAttemptCount = " + findUser.FailedPasswordAttemptCount + "|" +
                                        "FailedPasswordAttemptWindowStart = " + findUser.FailedPasswordAttemptWindowStart + "|User is temporarily locked out");
                                error = new Error(ErrorCode.UserLockedOut);
                                success = false;

                                if (ServiceFactory.ActivityService.CreateLoginFailedActivity(findUser.UserID, "User is temporarily locked out"))
                                {
                                    Log.Info("Login = " + email + "|" +
                                            "Role = " + findUser.RoleType.ToString() + "|" +
                                            "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                            "LastIPAddress = " + userIPAddress + "|successfully created failed login activity");
                                }
                                else
                                {
                                    Log.Info("Login = " + email + "|" +
                                        "Role = " + findUser.RoleType.ToString() + "|" +
                                        "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                        "LastIPAddress = " + userIPAddress + "|failed to create login activity");
                                    error = new Error(ErrorCode.SystemLoginError);
                                    success = false;
                                }
                            }
                        }
                        else
                        {
                            Log.Info("Login = " + email + "|" +
                                "Role = " + findUser.RoleType.ToString() + "|" +
                                "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                "LastIPAddress = " + userIPAddress + "|No user host name was found");
                            error = new Error(ErrorCode.NoUserHostName);
                            success = false;
                        }

                    }
                }
            }
            else
            {
                Log.Info("User |" + email + "| was not found|");
                error = new Error(ErrorCode.UserLoginNotFound);
                success = false;
            }
            if (success)
            {
                user = findUser;
            }
            else
            {
                user = null;
            }
            return success;
        }

        /// <summary>
        /// Retrieves a user using the user name and password.  If the password does not match that of the user retrieved the 
        /// user's failed login attempt count and window start time are set and the method fails.  If the login is successful the 
        /// attempt count is reset
        /// </summary>
        /// <param name="userName">UserName</param>
        /// <param name="password">Password</param>
        /// <param name="userHostName">Host Name</param>
        /// <param name="userIPAddress">IP Address</param>
        /// <param name="fulldescription"></param>
        /// <param name="user">User output</param>
        /// <param name="error">Error output</param>
        /// <returns>returns true if the user is found/ false if the user is not found</returns>
        public static bool RetrieveUserForLogin(string userName, string password, string userHostName, string userIPAddress, string fulldescription, out User user, out Error error)
        {
            String logMessage = userName + " is trying to login in @ " + DateTime.UtcNow.ToString("r");
            Log.Info(logMessage);

            if (HttpContext.Current != null)
            {
                HttpBrowserCapabilities bc = HttpContext.Current.Request.Browser;

                Log.Info("Login = " + userName + "|" +
                          "Password = " + password + "|" +
                          "IPAddress = " + userIPAddress + "|" +
                          "Type = " + bc.Type + "|" +
                            "Name = " + bc.Browser + "|" +
                            "Version = " + bc.Version + "|" +
                            "Major Version = " + bc.MajorVersion + "|" +
                            "Minor Version = " + bc.MinorVersion + "|" +
                            "Platform = " + bc.Platform + "|" +
                            "Is Beta = " + bc.Beta + "|" +
                            "Is Crawler = " + bc.Crawler + "|" +
                            "Is AOL = " + bc.AOL + "|" +
                            "Is Win16 = " + bc.Win16 + "|" +
                            "Is Win32 = " + bc.Win32 + "|" +
                            "Supports Frames = " + bc.Frames + "|" +
                            "Supports Tables = " + bc.Tables + "|" +
                            "Supports Cookies = " + bc.Cookies + "|" +
                            "Supports VB Script = " + bc.VBScript + "|" +
                            "Supports JavaScript = " + bc.EcmaScriptVersion + "|" +
                            "Supports Java Applets = " + bc.JavaApplets + "|" +
                            "Supports ActiveX Controls = " + bc.ActiveXControls + "|" +
                            "CDF = " + bc.CDF + "|"
                    //"CLR = " + bc.ClrVersion + "|"
                    );
            }
            else
            {
                Log.Info("Login = " + userName + "|" +
                    "Password = " + password + "|" +
                    "IPAddress = " + userIPAddress + "|No context available");
            }

            User findUser = RetrieveUserByUserName(HttpContext.Current, userName, userHostName, userIPAddress);
            bool success = false;
            error = new Error(ErrorCode.unknown);

            if (findUser != null)
            {
                if (findUser.IsActive == false)
                {
                    ServiceFactory.ActivityService.CreateLoginFailedActivity(findUser.UserID, "User is not Active.Host:" + userHostName + " IP: " + userIPAddress + "Agent:" + fulldescription);

                    error = new Error(ErrorCode.UserLoginNotActivated);
                    success = false;
                }
                else
                {
                    string hashedPwd = AdapterFactory.UserAdapter.CreateSecurePassword(findUser.PasswordSalt, password);

                    //We start the lockout process if the password is incorrect.
                    if (hashedPwd.Trim() != findUser.Password)
                    {
                        //Change the attempt count first then find the endwindow before updating start window
                        findUser.FailedPasswordAttemptCount += 1;
                        DateTime windowEnd = findUser.FailedPasswordAttemptWindowStart.AddMinutes(30);
                        findUser.FailedPasswordAttemptWindowStart = DateTime.UtcNow;

                        //If they have waited past the 30 mins.
                        if (DateTime.UtcNow > windowEnd)
                        {
                            //Reset the counter to one to restart the lockout process.
                            findUser.FailedPasswordAttemptCount = 1;
                            error = new Error(ErrorCode.UserPasswordMismatch);
                        }

                        //If their count is higher than alloted attempts we tell them they are locked out.
                        else if (findUser.FailedPasswordAttemptCount >= 3)
                        {
                            //Slide the window so that they are locked out for another 30 mins.
                            findUser.FailedPasswordAttemptCount = 3;
                            error = new Error(ErrorCode.UserPasswordMismatch);
                        }
                        else
                        {
                            error = new Error(ErrorCode.UserPasswordMismatch);
                        }
                        //Save out the user.
                        findUser.Save(null);

                        Log.Info("Login = " + userName + "|" +
                                "Role = " + findUser.RoleType.ToString() + "|" +
                                "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                "LastIPAddress = " + userIPAddress + "|" +
                                "FailedPasswordAttemptCount = " + findUser.FailedPasswordAttemptCount + "|" +
                                "FailedPasswordAttemptWindowStart = " + findUser.FailedPasswordAttemptWindowStart + "|Password mismatch");

                        if (ServiceFactory.ActivityService.CreateLoginFailedActivity(findUser.UserID, " Attempt:" + findUser.FailedPasswordAttemptCount + " Start Window:" + findUser.FailedPasswordAttemptWindowStart + " Host:" + userHostName + " IP: " + userIPAddress + "Agent:" + fulldescription))
                        {
                            Log.Info("Login = " + userName + "|" +
                                    "Role = " + findUser.RoleType.ToString() + "|" +
                                    "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                    "LastIPAddress = " + userIPAddress + "|successfully created failed login activity");
                        }
                        else
                        {
                            Log.Info("Login = " + userName + "|" +
                                "Role = " + findUser.RoleType.ToString() + "|" +
                                "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                "LastIPAddress = " + userIPAddress + "|failed to create login activity");

                        }

                        success = false;
                    }
                    else
                    {
                        //Success login


                        // TODO: Understand the error scenario for Host name length == 0.
                        if (userHostName.Length != 0)
                        {
                            //Find the unlock time from the start window of the user.
                            DateTime windowEnd = findUser.FailedPasswordAttemptWindowStart.AddMinutes(30);
                            //If they have waited past the 30 mins.
                            if ((DateTime.UtcNow > windowEnd) || (findUser.FailedPasswordAttemptCount <= 2))
                            {
                                AdapterFactory.UserAdapter.UpdateDBWithBrowserSetting(userHostName, userIPAddress, findUser.UserEntity);
                                //Update the Security info with the information
                                findUser.LastLoginDate = DateTime.UtcNow;
                                //Update the attempt count to zero if they login.
                                findUser.FailedPasswordAttemptCount = 0;

                                findUser.Save(null);
                                error = null;
                                success = true;

                                Log.Info("Login = " + userName + "|" +
                                    "Role = " + findUser.RoleType.ToString() + "|" +
                                    "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                    "LastIPAddress = " + userIPAddress + "|Successful Login");

                                //Ok we're good to go -- let's log it
                                if (ServiceFactory.ActivityService.CreateLoginActivity(findUser.UserID, "Host:" + userHostName + " IP: " + userIPAddress + "Agent:" + fulldescription))
                                {
                                    Log.Info("Login = " + userName + "|" +
                                        "Role = " + findUser.RoleType.ToString() + "|" +
                                        "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                        "LastIPAddress = " + userIPAddress + "|successfully created login activity");
                                }
                                else
                                {
                                    Log.Info("Login = " + userName + "|" +
                                        "Role = " + findUser.RoleType.ToString() + "|" +
                                        "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                        "LastIPAddress = " + userIPAddress + "|failed to create login activity");
                                    error = new Error(ErrorCode.SystemLoginError);
                                    success = false;
                                }

                            }
                            else
                            {
                                Log.Info("Login = " + userName + "|" +
                                        "Role = " + findUser.RoleType.ToString() + "|" +
                                        "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                        "LastIPAddress = " + userIPAddress + "|" +
                                        "FailedPasswordAttemptCount = " + findUser.FailedPasswordAttemptCount + "|" +
                                        "FailedPasswordAttemptWindowStart = " + findUser.FailedPasswordAttemptWindowStart + "|User is temporarily locked out");
                                error = new Error(ErrorCode.UserLockedOut);
                                success = false;

                                if (ServiceFactory.ActivityService.CreateLoginFailedActivity(findUser.UserID, "User is temporarily locked out"))
                                {
                                    Log.Info("Login = " + userName + "|" +
                                            "Role = " + findUser.RoleType.ToString() + "|" +
                                            "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                            "LastIPAddress = " + userIPAddress + "|successfully created failed login activity");
                                }
                                else
                                {
                                    Log.Info("Login = " + userName + "|" +
                                        "Role = " + findUser.RoleType.ToString() + "|" +
                                        "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                        "LastIPAddress = " + userIPAddress + "|failed to create login activity");
                                    error = new Error(ErrorCode.SystemLoginError);
                                    success = false;
                                }
                            }
                        }
                        else
                        {
                            Log.Info("Login = " + userName + "|" +
                                "Role = " + findUser.RoleType.ToString() + "|" +
                                "LastLoginDate = " + findUser.LastLoginDate.ToString("r") + "|" +
                                "LastIPAddress = " + userIPAddress + "|No user host name was found");
                            error = new Error(ErrorCode.NoUserHostName);
                            success = false;
                        }

                    }
                }
            }
            else
            {
                Log.Info("User |" + userName + "| was not found|");
                error = new Error(ErrorCode.UserLoginNotFound);
                success = false;
            }
            if (success)
            {
                user = findUser;
            }
            else
            {
                user = null;
            }
            return success;
        }

        /// <summary>
        /// Retrieves all users
        /// </summary>
        /// <returns></returns>
        public static List<User> RetrieveAllUsers()
        {
            HttpContext context = HttpContext.Current;
            List<User> retList = new List<User>();
            EntityCollection<UserEntity> users = AdapterFactory.UserAdapter.RetrieveAllUsers();
            foreach (UserEntity user in users)
            {
                retList.Add(CreateUser(context, user.UserId, user));
            }

            return retList;
        }

        /// <summary>
        /// Retrieves all the users in side the given range
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static List<User> RetrieveAllUsers(int pageIndex, int pageSize)
        {
            HttpContext context = HttpContext.Current;
            List<User> retList = new List<User>();
            EntityCollection<UserEntity> users = AdapterFactory.UserAdapter.RetrieveAllUsers(pageIndex, pageSize);
            foreach (UserEntity user in users)
            {
                retList.Add(CreateUser(context, user.UserId, user));
            }

            return retList;
        }

        /// <summary>
        /// Retrieves all users that have logged an activity since the given date
        /// </summary>
        /// <param name="activeAfterTime"></param>
        /// <returns></returns>
        public static List<User> RetrieveAllUsers(DateTime activeAfterTime)
        {
            HttpContext context = HttpContext.Current;
            List<User> retList = new List<User>();
            EntityCollection<UserEntity> users = AdapterFactory.UserAdapter.RetrieveAllUsers(activeAfterTime);
            foreach (UserEntity user in users)
            {
                retList.Add(CreateUser(context, user.UserId, user));
            }

            return retList;
        }

        /// <summary>
        /// Retrieves all users that have logged an activity sine the given date
        /// and are part of the given branding
        /// </summary>
        /// <param name="activeAfterTime"></param>
        /// <param name="brandingID"></param>
        /// <returns></returns>
        public static List<User> RetrieveAllUsers(DateTime activeAfterTime, Guid brandingID)
        {
            HttpContext context = HttpContext.Current;
            List<User> retList = new List<User>();
            EntityCollection<UserEntity> users = AdapterFactory.UserAdapter.RetrieveAllUsers(activeAfterTime, brandingID);
            foreach (UserEntity user in users)
            {
                retList.Add(CreateUser(context, user.UserId, user));
            }

            return retList;

        }

        /// <summary>
        /// Retrieves all users of the given branding
        /// </summary>
        /// <param name="brandingID"></param>
        /// <returns></returns>
        public static List<User> RetrieveAllUsers(Guid brandingID)
        {
            HttpContext context = HttpContext.Current;
            List<User> retList = new List<User>();
            EntityCollection<UserEntity> users = AdapterFactory.UserAdapter.RetrieveAllUsers(brandingID);
            foreach (UserEntity user in users)
            {
                retList.Add(CreateUser(context, user.UserId, user));
            }

            return retList;

        }

        /// <summary>
        /// Retrieves the count of all users
        /// </summary>
        /// <returns></returns>
        public static int RetrieveAllUsersCount()
        {
            return AdapterFactory.UserAdapter.RetrieveAllUsersCount();
        }

        /// <summary>
        /// Retrieves all users count.
        /// </summary>
        /// <param name="activeAfterTime">The active after time.</param>
        /// <returns></returns>
        public static int RetrieveAllUsersCount(DateTime activeAfterTime)
        {
            return RetrieveAllUsersCount(activeAfterTime, UserFields.LastActivityDate);
        }

        /// <summary>
        /// Retrieves all users count.
        /// </summary>
        /// <param name="afterTime">The after time.</param>
        /// <param name="cmpField">The compare field.</param>
        /// <returns></returns>
        public static int RetrieveAllUsersCount(DateTime afterTime, EntityField2 cmpField)
        {
            return AdapterFactory.UserAdapter.RetrieveAllUsersCount(afterTime, cmpField);
        }

        /// <summary>
        /// Retrieves all users count.
        /// </summary>
        /// <returns></returns>
        public static int RetrieveAllUsersCount(Guid brandingID)
        {
            return AdapterFactory.UserAdapter.RetrieveAllUsersCount(brandingID);
        }

        /// <summary>
        /// Retrieves all users count.
        /// </summary>
        /// <param name="activeAfterTime">The active after time.</param>
        /// <param name="brandingID">The branding ID.</param>
        /// <returns></returns>
        public static int RetrieveAllUsersCount(DateTime activeAfterTime, Guid brandingID)
        {
            return RetrieveAllUsersCount(activeAfterTime, UserFields.LastActivityDate, brandingID);
        }

        /// <summary>
        /// Retrieves all users count.
        /// </summary>
        /// <param name="afterTime">The after time.</param>
        /// <param name="cmpField">The compare field.</param>
        /// <param name="brandingID">The branding ID.</param>
        /// <returns></returns>
        public static int RetrieveAllUsersCount(DateTime afterTime, EntityField2 cmpField, Guid brandingID)
        {
            return AdapterFactory.UserAdapter.RetrieveAllUsersCount(afterTime, cmpField, brandingID);
        }


        #endregion //Retrieve

        #region Search

        /// <summary>
        /// Returns a list of users matching the given criteria
        /// </summary>
        /// <param name="passedBucket"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static List<User> SearchUsers(IRelationPredicateBucket passedBucket, int pageNumber, int pageSize)
        {
            return SearchUsers(passedBucket, 0, null, pageNumber, pageSize);
        }

        public static List<User> SearchUsers(IRelationPredicateBucket passedBucket, int maxItems, ISortExpression passedSorter, int pageNumber, int pageSize)
        {
            List<User> retList = new List<User>();
            EntityCollection<UserEntity> users = AdapterFactory.UserAdapter.SearchUsers(passedBucket, maxItems, passedSorter, pageNumber, pageSize);
            foreach (UserEntity user in users)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];
                    if (model != null)
                    {
                        retList.Add(model);
                    }
                }
                retList.Add(CreateUser(context, user.UserId, user));
            }

            return retList;
        }

        /// <summary>
        /// Returns the number of users matching the given criteria
        /// </summary>
        /// <param name="passedBucket"></param>
        /// <param name="hitCount"></param>
        /// <returns></returns>
        public static bool SearchUsersCount(IRelationPredicateBucket passedBucket, out int hitCount)
        {
            return AdapterFactory.UserAdapter.SearchUsersCount(passedBucket, out hitCount);
        }

        /// <summary>
        /// Returns all the users with the given first name
        /// </summary>
        /// <param name="FirstName"></param>
        /// <returns></returns>
        public static List<User> SearchUsersByFirstName(string FirstName)
        {
            List<User> retList = new List<User>();
            EntityCollection<UserEntity> users = AdapterFactory.UserAdapter.SearchUsersByFirstName(FirstName);
            foreach (UserEntity user in users)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];
                    if (model != null)
                    {
                        retList.Add(model);
                    }
                }
                retList.Add(CreateUser(context, user.UserId, user));
            }

            return retList;
        }

        /// <summary>
        /// Returns all the users with the given last name
        /// </summary>
        /// <param name="LastName"></param>
        /// <returns></returns>
        public static List<User> SearchUsersByLastName(string LastName)
        {
            List<User> retList = new List<User>();
            EntityCollection<UserEntity> users = AdapterFactory.UserAdapter.SearchUsersByLastName(LastName);
            foreach (UserEntity user in users)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];
                    if (model != null)
                    {
                        retList.Add(model);
                    }
                }
                retList.Add(CreateUser(context, user.UserId, user));
            }

            return retList;

        }

        /// <summary>
        /// Search User by User Name.
        /// </summary>
        /// <param name="UserName">User Name.</param>
        /// <returns></returns>
        public static List<User> SearchUsersByUserName(string UserName)
        {
            List<User> retList = new List<User>();
            EntityCollection<UserEntity> users = AdapterFactory.UserAdapter.SearchUsersByUserName(UserName);
            foreach (UserEntity user in users)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];
                    if (model != null)
                    {
                        retList.Add(model);
                    }
                }
                retList.Add(CreateUser(context, user.UserId, user));
            }

            return retList;
        }

        /// <summary>
        /// Returns all users with the given email address
        /// </summary>
        /// <param name="EmailAddress"></param>
        /// <returns></returns>
        public static List<User> SearchUsersByEmailAddress(string EmailAddress)
        {
            List<User> retList = new List<User>();
            EntityCollection<UserEntity> users = AdapterFactory.UserAdapter.SearchUsersByEmailAddress(EmailAddress);
            foreach (UserEntity user in users)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];
                    if (model != null)
                    {
                        retList.Add(model);
                    }
                }
                retList.Add(CreateUser(context, user.UserId, user));
            }

            return retList;
        }

        /// <summary>
        /// Returns all the users with the given address line
        /// </summary>
        /// <param name="MailingAddressAddressLine"></param>
        /// <returns></returns>
        public static List<User> SearchUsersByMailingAddressAddressLine(string MailingAddressAddressLine)
        {
            List<User> retList = new List<User>();
            EntityCollection<UserEntity> users = AdapterFactory.UserAdapter.SearchUsersByMailingAddressAddressLine(MailingAddressAddressLine);
            foreach (UserEntity user in users)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];
                    if (model != null)
                    {
                        retList.Add(model);
                    }
                }
                retList.Add(CreateUser(context, user.UserId, user));
            }

            return retList;

        }

        /// <summary>
        /// Returns all the users with the given city
        /// </summary>
        /// <param name="MailingAddressCity"></param>
        /// <returns></returns>
        public static List<User> SearchUsersByMailingAddressCity(string MailingAddressCity)
        {
            List<User> retList = new List<User>();
            EntityCollection<UserEntity> users = AdapterFactory.UserAdapter.SearchUsersByMailingAddressCity(MailingAddressCity);
            foreach (UserEntity user in users)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];
                    if (model != null)
                    {
                        retList.Add(model);
                    }
                }
                retList.Add(CreateUser(context, user.UserId, user));
            }

            return retList;
        }

        /// <summary>
        /// Returns all the users with the given state
        /// </summary>
        /// <param name="MailingAddressState"></param>
        /// <returns></returns>
        public static List<User> SearchUsersByMailingAddressState(string MailingAddressState)
        {
            List<User> retList = new List<User>();
            EntityCollection<UserEntity> users = AdapterFactory.UserAdapter.SearchUsersByMailingAddressState(MailingAddressState);
            foreach (UserEntity user in users)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];
                    if (model != null)
                    {
                        retList.Add(model);
                    }
                }
                retList.Add(CreateUser(context, user.UserId, user));
            }

            return retList;
        }

        /// <summary>
        /// Returns all the users with the given zip code
        /// </summary>
        /// <param name="MailingAddressZipCode"></param>
        /// <returns></returns>
        public static List<User> SearchUsersByMailingAddressZipCode(string MailingAddressZipCode)
        {
            List<User> retList = new List<User>();
            EntityCollection<UserEntity> users = AdapterFactory.UserAdapter.SearchUsersByMailingAddressZipCode(MailingAddressZipCode);
            foreach (UserEntity user in users)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];
                    if (model != null)
                    {
                        retList.Add(model);
                    }
                }
                retList.Add(CreateUser(context, user.UserId, user));
            }

            return retList;
        }

        /// <summary>
        /// Returns all the users with the given phone number
        /// </summary>
        /// <param name="PhoneNumber"></param>
        /// <returns></returns>
        public static List<User> SearchUsersByPhoneNumber(string PhoneNumber)
        {
            List<User> retList = new List<User>();
            EntityCollection<UserEntity> users = AdapterFactory.UserAdapter.SearchUsersByPhoneNumber(PhoneNumber);
            foreach (UserEntity user in users)
            {
                HttpContext context = HttpContext.Current;
                if (context != null)
                {
                    User model = (User)context.Items[user.UserId];
                    if (model != null)
                    {
                        retList.Add(model);
                    }
                }
                retList.Add(CreateUser(context, user.UserId, user));
            }

            return retList;

        }


        #endregion //Search

        #region Create

        /// <summary>
        /// Create a model from the USerEntity
        /// </summary>
        /// <param name="context"></param>
        /// <param name="userID"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        private static User CreateUser(HttpContext context, Guid userID, UserEntity user)
        {
            //Go check the cache first if we find something then return that one instead
            if (context != null)
            {
                User model = (User)context.Items[userID];

                if (model != null)
                {
                    return model;
                }
            }

            //See what kind of user this is and create the correct model
            switch (user.RoleType)
            {
                case RoleType.RegisteredTeen:
                    Teen teen = new Teen((RegisteredTeenEntity)user);
                    if (context != null)
                    {
                        context.Items[userID] = teen;
                    }
                    return teen;
                case RoleType.Parent:
                    Parent parent = new Parent((ParentEntity)user);
                    if (context != null)
                    {
                        context.Items[userID] = parent;
                    }
                    return parent;
                // BUG #1703:  Add masteradmin role so user can be added
                // (WBS)       to the cache.  Fixes the issue where RetrieveUser
                //             returns NULL for masteradmin user types.
                case RoleType.MasterAdmin:
                    MasterAdmin masterAdmin = new MasterAdmin((MasterAdminEntity)user);
                    if (context != null)
                    {
                        context.Items[userID] = masterAdmin;
                    }
                    return masterAdmin;
                // SCENARIO #1962:  Add admin role so user can be added
                // (WBS)            to the cache.  Fixes the issue where RetrieveUser
                //                  returns NULL for admin user types.
                case RoleType.Admin:
                    Admin admin = new Admin((AdminEntity)user);
                    if (context != null)
                    {
                        context.Items.Add(userID, admin);

                    }
                    return admin;
                default:
                    return null;
            }
        }

        #endregion //Create

        #endregion //Factory Methods

        #region Methods

        /// <summary>
        /// updates the user with the info in the IUserInfo
        /// </summary>
        /// <param name="userInfo"></param>
        /// <returns></returns>
        public bool UpdateUser(IUserInfo userInfo)
        {
            AnniversaryDate = userInfo.AnniversaryDate;
            BrandingId = userInfo.BrandingId;
            CreationDate = userInfo.CreationDate;
            CultureID = userInfo.CultureID;
            ExternalUserIdentifier = userInfo.ExternalUserIdentifier;
            FailedPasswordAnswerAttemptWindowsStart = userInfo.FailedPasswordAnswerAttemptWindowsStart;
            FailedPasswordAttemptAttemptCount = userInfo.FailedPasswordAttemptAttemptCount;
            FailedPasswordAttemptCount = userInfo.FailedPasswordAttemptCount;
            FailedPasswordAttemptWindowStart = userInfo.FailedPasswordAttemptWindowStart;
            FirstName = userInfo.FirstName;
            Gender = userInfo.Gender;
            IsActive = userInfo.IsActive;
            IsLockedOut = userInfo.IsLockedOut;
            IsOnLine = userInfo.IsOnLine;
            LastActivityDate = userInfo.LastActivityDate;
            LastHostName = userInfo.LastHostName;
            LastIPAddress = userInfo.LastIPAddress;
            LastLockedOutDate = userInfo.LastLockedOutDate;
            LastLoginDate = userInfo.LastLoginDate;
            LastName = userInfo.LastName;
            LastPasswordChangeDate = userInfo.LastPasswordChangeDate;
            MiddleName = userInfo.MiddleName;
            Password = userInfo.Password;
            PasswordAnswer = userInfo.PasswordAnswer;
            PasswordQuestion = userInfo.PasswordQuestion;
            PasswordSalt = userInfo.PasswordSalt;
            RecieveMarketingEmail = userInfo.RecieveMarketingEmail;
            RoleType = userInfo.RoleType;
            ThemeID = userInfo.ThemeID;
            TimeZone = userInfo.TimeZone;
            UserName = userInfo.UserName;
            return true;
        }

        /// <summary>
        /// Resets a users password
        /// </summary>
        /// <param name="answer"></param>
        /// <returns></returns>
        public bool ResetPassword(string answer)
        {
            if (answer == null || answer.Length <= 0)
            {
                return false;
            }
            if (!AdapterFactory.UserAdapter.ResetUserPassowrd(UserEntity, answer, out _newPassword))
            {
                    return false;
            }
            _passwordReset = true;
            return true;
        }


        public Product GetProduct()
        {
            return ServiceFactory.UserConfiguration.RetrieveUserProduct(base.BackingEntity.UserId);
        }

        public  void Delete()
        {
            if (!base.BackingEntity.MarkedForDeletion)
            {
                //Allow rollback if something happens
                try
                {
                    using (TransactionScope scope = new TransactionScope())
                    {
                        //Disable Account
                        base.BackingEntity.MarkedForDeletion = true;
                        base.BackingEntity.DeletedDate = DateTime.UtcNow;
                        base.BackingEntity.IsLockedOut = true;
                        base.BackingEntity.RecieveMarketingEmail = false;
                        base.BackingEntity.IsActive = false;
                        base.BackingEntity.IsOnLine = false;

                        //Set User Properties
                        base.BackingEntity.UserName = String.Format("{0}|{1}", base.BackingEntity.UserName, base.BackingEntity.UserId.ToString());
                        if (Email != null)
                        {
                            Email.Address = String.Format("{0}|{1}", Email.Address, base.BackingEntity.UserId.ToString());
                            Email.RequiresValidation = true;
                        }

                        foreach (Payjr.Core.Notifications.Alert alert in Alerts)
                        {
                            alert.Delete();
                        }

                        foreach (JobEntity jobEnt in ServiceFactory.JobService.RetrieveLockedJobs(base.BackingEntity.UserId, null))
                        {
                            Error error;
                            Graph graph = Graph.LoadGraphFromJob(Job.RetrieveJob(jobEnt));
                            graph.Cancel(null, out error);
                        }

                        Save(null);

                        scope.Complete();
                    }
                } // Bad to catch all exceptions, but because of how the scope is used, we need it
                catch (Exception e)
                {
                    Log.ErrorException("Error when delete usser", e);
                }
            }

        }
        #endregion //Methods

        #region Financial Account Methods

        /// <summary>
        /// Retrieves the accounts.
        /// </summary>
        /// <returns></returns>
        protected List<IFinancialAccount> RetrieveAccounts()
        {
            List<IFinancialAccount> accounts = new List<IFinancialAccount>();

            //ACH
            EntityCollection<BankAccountEntity> bankAccounts = AdapterFactory.FinancialAccountDataAdapter.RetrieveBankAccounts(UserID);
            foreach (BankAccountEntity bank in bankAccounts)
            {
                ACHAccount achAccount = new ACHAccount(bank, this, EventBag);
                accounts.Add(achAccount);
                AddChildForMonitoring(achAccount);
            }

            //Credit Cards
            EntityCollection<CreditCardAccountEntity> creditCards = AdapterFactory.FinancialAccountDataAdapter.RetrieveCreditCardAccountsByUser(UserID);
            foreach (CreditCardAccountEntity creditCard in creditCards)
            {
                CreditCardAccount creditCardAccount = new CreditCardAccount(creditCard, this, EventBag);
                accounts.Add(creditCardAccount);
                AddChildForMonitoring(creditCardAccount);
            }

            //Prepaid
            EntityCollection<PrepaidCardAccountEntity> prepaidCards;
            AdapterFactory.FinancialAccountDataAdapter.RetrievePrepaidCardAccountsByUser(UserID, out prepaidCards);
            foreach (PrepaidCardAccountEntity prepaidCard in prepaidCards)
            {
                PrepaidCardAccount card = new PrepaidCardAccount(prepaidCard, this, EventBag);
                accounts.Add(card);
                AddChildForMonitoring(card);
            }

            //Savings
            EntityCollection<CustomAccountFieldGroupEntity> savingsAccounts = AdapterFactory.FinancialAccountDataAdapter.RetrieveAllCustomGroupsByUserId(UserID);
            foreach (CustomAccountFieldGroupEntity savings in savingsAccounts)
            {
                SavingsAccount savingsAccount = new SavingsAccount(savings, this, EventBag);
                accounts.Add(savingsAccount);
                AddChildForMonitoring(savingsAccount);
            }

            //Target GiftCard
            EntityCollection<TargetAccountEntity> targetAccounts = AdapterFactory.TargetAdapter.RetrieveAllTargetAccountsByTeenID(UserID);
            foreach (TargetAccountEntity oneAccount in targetAccounts)
            {
                TargetAccount targetAccount = new TargetAccount(oneAccount, this, EventBag);
                accounts.Add(targetAccount);
                AddChildForMonitoring(targetAccount);
            }


            //Manual Payment
            ManualPaymentAccount manualPayment = new ManualPaymentAccount(this, EventBag);
            accounts.Add(manualPayment);
            AddChildForMonitoring(manualPayment);

            return accounts;
        }

        #endregion //Financial Account Methods

        #region Financial Account Properties

        /// <summary>
        /// Gets whether the parent is able to add a new credit card based on system rules
        /// </summary>
        public bool CanAddNewCreditCardAccount
        {
            get
            {
                if (String.IsNullOrEmpty(System.Configuration.ConfigurationManager.AppSettings["MaxCreditCardsIn3Months"]))
                    throw new System.Configuration.ConfigurationErrorsException("MaxCreditCardsIn3Months is missing from the configuration file");


                int cardCount = AdapterFactory.FinancialAccountDataAdapter.RetrieveCountCreditCardAccountsByUserLast3Months(UserID);

                if (cardCount >= Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["MaxCreditCardsIn3Months"]))
                    return false;

                //Check for unverified accounts
                foreach (Payjr.Core.FinancialAccounts.CreditCardAccount account in FinancialAccounts.CreditCardAccounts)
                    if (account.Status == AccountStatus.Unverified)
                        return false;

                return true;
            }
        }

        /// <summary>
        /// Determines whether an account number already exists in the database for the user
        /// </summary>
        public bool AccountIsExisted(string accountNumber)
        {
            foreach (CreditCardAccount account in FinancialAccounts.CreditCardAccounts)
            {
                if (!account.MarkedForDeletion && account.AccountNumber == accountNumber.Trim())
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Rules

        /// <summary>
        /// Override this method in your business class to
        /// be notified when you need to set up business
        /// rules.
        /// </summary>
        /// <remarks>
        /// This method is automatically called by the base object
        /// when your object should associate per-instance
        /// validation rules with its properties.
        /// </remarks>
        protected override void AddBusinessRules()
        {

            //Validate the email address format
            ValidationRules.AddRule<User>(CheckUserNameIsValidRule, "UserName");
            ValidationRules.AddRule<User>(CheckUserNameInUseRule, "UserName");

            base.AddBusinessRules();
        }

        /// <summary>
        /// Validates username via reg expression
        /// </summary>
        /// <param name="target"></param>
        /// <param name="e"></param>
        private bool CheckUserNameIsValidRule(User target, RuleArgs e)
        {
            #region HACK: For existing Users that were deleted. This SHOULD be removed soon

            //HACK: For existing Users that were deleted. This SHOULD be removed soon
            string tempUser;
            String[] splitUser;

            splitUser = UserName.Split(new Char[] { '|' });

            if (splitUser.Length > 2)
            {
                e.Description = "Username is not properly formatted. Username: " + UserName;
                return false;
            }
            else if (splitUser.Length == 2)
            {
                tempUser = splitUser[0];
                try
                {
                    (new Guid(splitUser[1])).ToString();
                }
                catch
                {
                    e.Description = "Username is not properly formatted. Username: " + UserName;
                    return false;
                }
            }
            else
            {
                tempUser = UserName;
            }

            #endregion


            Regex rx = new Regex(CommonRules.RegExRuleArgs.GetPattern(CommonRules.RegExPatterns.Username));
            if (!rx.IsMatch(tempUser))
            {
                e.Description = "Username is not properly formatted. Username: " + UserName;
                return false;
            }
            return true;
        }

        /// <summary>
        /// validates a username is not in use
        /// </summary>
        /// <param name="target"></param>
        /// <param name="e"></param>
        private bool CheckUserNameInUseRule(User target, RuleArgs e)
        {
            if (IsUserNameInUse)
            {
                e.Description = "Username is already in use. Please use another";
                return false;
            }
            return true;
        }

        #endregion //Rules

        #region Errors

        /// <summary>
        /// Gets an error messages for the <see cref="IDataErrorInfo"/> interface. The default behavior is
        /// to retrieve error messages from broken rules in the <see cref="ValidationRules"/> instance in this
        /// object
        /// </summary>
        /// <returns></returns>
        /// <remarks>You can override this method to return custom error messages or add to the existing
        /// messages from the broken rules in the <see cref="ValidationRules"/> object</remarks>
        protected override string GetErrorMessages()
        {
            //If we have an exception on the unique email constraint then return an error message
            if (_exceptionOnUniqueUsername)
            {
                string errorMessage = UserName + " is already in use. Please use another" + Environment.NewLine;
                return errorMessage + base.GetErrorMessages();
            }
            else
            {
                return base.GetErrorMessages();
            }

        }

        /// <summary>
        /// Handles the UpdateExceptionEncountered event of the ParentEvents control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Payjr.Core.Business.DataExceptionEventArgs"/> instance containing the event data.</param>
        protected void EventBag_UpdateExceptionEncountered(object sender, DataExceptionEventArgs e)
        {
            if (e.Contains("IX_Username_Unique"))
            {
                _exceptionOnUniqueUsername = true;
            }
        }

        #endregion //Errors

        #region Overrides

        /// <summary>
        /// Set the save bits before save
        /// </summary>
        /// <param name="actingUserID"></param>
        /// <returns></returns>
        public override bool Save(object actingUser)
        {
            SetSaveBits();

            if (!base.Save(actingUser))
            {
                try
                {
                    Log.Error(string.Format("Error occurred while saving user. Error: {0} UserID: {1} Username:{2}", this.Error, this.UserID.ToString(),
                        this.UserName));
                }
                catch (System.Exception e)
                {
                    Log.ErrorException("A fatal error occurred while saving a user and attempting to log the error.  This needs to be manually investigated", e);
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Process Save bits after save
        /// </summary>
        /// <param name="entityToSave"></param>
        /// <returns></returns>
        protected override bool OnAfterUpdateData(UserEntity entityToSave, object actingUser)
        {
            Guid? actingUserID = null;

            if (actingUser != null)
            {
                actingUserID = new Guid(actingUser.ToString());
            }

            if (!ProcessSaveBits(actingUserID))
            {
                ResetSaveBits();
                return false;
            }

            ResetSaveBits();
            return true;
        }

        #endregion //Overrides

        #region Saving Bit Processing

        #region Set

        /// <summary>
        /// Sets the Save bits
        /// </summary>
        private void SetSaveBits()
        {
            SetAccountChangedNotificationBit();
            SetLockUserBit();
            SetPolicyVersionChangedBit();
        }

        /// <summary>
        /// Set SendAccountChangedNotification Bit
        /// </summary>
        private void SetAccountChangedNotificationBit()
        {
            //Send if the user is dirty and not new
            if (IsDirty && !IsNew)
            {
                //Do not send if we are resetting the password
                if (!_passwordReset)
                {
                    //only send the notification if certain things have changed
                    if (AdapterFactory.UserAdapter.IsPropertyDirty(UserEntity, (int)UserFieldIndex.FirstName) ||
                       AdapterFactory.UserAdapter.IsPropertyDirty(UserEntity, (int)UserFieldIndex.MiddleName) ||
                       AdapterFactory.UserAdapter.IsPropertyDirty(UserEntity, (int)UserFieldIndex.LastName) ||
                       AdapterFactory.UserAdapter.IsPropertyDirty(UserEntity, (int)UserFieldIndex.UserName) ||
                       AdapterFactory.UserAdapter.IsPropertyDirty(UserEntity, (int)UserFieldIndex.Password) ||
                       AdapterFactory.UserAdapter.IsPropertyDirty(UserEntity, (int)UserFieldIndex.PasswordAnswer) ||
                       AdapterFactory.UserAdapter.IsPropertyDirty(UserEntity, (int)UserFieldIndex.PasswordQuestion) ||
                       AdapterFactory.UserAdapter.IsPropertyDirty(UserEntity, (int)UserFieldIndex.Sex) ||
                       (UserAddress != null && UserAddress.IsDirty && !UserAddress.IsNew) ||
                       (Phone != null && Phone.IsDirty && !Phone.IsNew) ||
                       (SSN != null && SSN.IsDirty && !SSN.IsNew))
                    {
                        _sendAccountChangedNotification = true;
                    }
                }
            }
        }

        /// <summary>
        /// Determines if the lock or unlock bits need to be processed
        /// </summary>
        private void SetLockUserBit()
        {
            //Set if the user is dirty and not new
            if (IsDirty && !IsNew)
            {
                if (AdapterFactory.UserAdapter.IsPropertyDirty(UserEntity, (int)UserFieldIndex.IsLockedOut))
                {
                    //If the value is true set the lock bit
                    if (IsLockedOut)
                    {
                        _lockUser = true;
                        _unLockUser = false;
                    }
                    //Else set the unlock bit
                    else
                    {
                        _lockUser = false;
                        _unLockUser = true;
                    }
                }
            }
        }

        /// <summary>
        /// Determines if the policy version changed
        /// </summary>
        private void SetPolicyVersionChangedBit()
        {
            //Set if the user is dirty and not new
            if (IsDirty && !IsNew)
            {
                if (UserPolicy != null)
                {
                    _acceptedCardHolder = UserPolicy.IsPropertyDirty((int)PolicyFieldIndex.CardHolderTermsVersion);
                    _acceptedPrivacy = UserPolicy.IsPropertyDirty((int)PolicyFieldIndex.PolicyVersion);
                    _acceptedTerms = UserPolicy.IsPropertyDirty((int)PolicyFieldIndex.TermsAndCoditionVersion);
                    _acceptedTarget = UserPolicy.IsPropertyDirty((int)PolicyFieldIndex.TargetTerms);
                }
            }
        }

        #endregion //Set

        #region Process
        /// <summary>
        /// Processes the Save Bits
        /// </summary>
        private bool ProcessSaveBits(Guid? actingUserID)
        {
            //Password Reset
            if (_passwordReset)
            {
                if (!ProcessPasswordResetBit(actingUserID))
                {
                    return false;
                }
            }

            //Account Changed Notification
            if (_sendAccountChangedNotification)
            {
                if (!ProcessAccountChangedNotificationBit())
                {
                    return false;
                }
            }

            //Lock User
            if (_lockUser)
            {
                if (!ProcessLockUserBit(actingUserID))
                {
                    return false;
                }
            }

            //UnLock user
            if (_unLockUser)
            {
                if (!ProcessUnlockUserBit(actingUserID))
                {
                    return false;
                }
            }

            //Policy Version Changed
            if (_acceptedCardHolder || _acceptedPrivacy || _acceptedTerms || _acceptedTarget)
            {
                if (!ProcessPolicyVersionChangedBit(actingUserID))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Processes the Password reset setting
        /// </summary>
        /// <returns></returns>
        private bool ProcessPasswordResetBit(Guid? actingUserID)
        {
            if (!_notificationService.PasswordReset(UserID, _newPassword))
            {
                base.WritePostSaveErrorToLog(this.ToString(), "ProcessPasswordResetBit", string.Format(ERROR_PASSWORD_RESET_FAILED_NOTIFICATION, UserName));
                return false;
            }

            if (!ServiceFactory.ActivityService.CreateChangedPasswordActivity(UserID, actingUserID, _newPassword))
            {
                base.WritePostSaveErrorToLog(this.ToString(), "ProcessPasswordResetBit", string.Format(ERROR_PASSWORD_RESET_FAILED_ACTIVITY_CREATION, UserName));
                return false;
            }

            Log.Debug("Reset password for " + UserName + " @ " + LastPasswordChangeDate.ToString() +
               ".  Password >" + _newPassword + "< stored as >" + Password + "< using salt >" + PasswordSalt + "<");

            return true;
        }

        /// <summary>
        /// Processes the account Changed Notification Bit
        /// </summary>
        private bool ProcessAccountChangedNotificationBit()
        {
            if (!_passwordReset)
            {
                //Only send if the user has an email
                if (Email != null)
                {
                    //Send the Notification
                    if (!_notificationService.AccountInformationChanged(UserID))
                    {
                        base.WritePostSaveErrorToLog(this.ToString(), "ProcessAccountChangedNotificationBit",
                            string.Format(ERROR_ACCOUNT_INFO_CHANGED_FAILED_NOTIFICATION, UserName));
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Processes the Lock User Bit
        /// </summary>
        /// <param name="actingUserID"></param>
        /// <returns></returns>
        private bool ProcessLockUserBit(Guid? actingUserID)
        {
            if (!ServiceFactory.ActivityService.CreateUserLockedActivity(UserID, actingUserID))
            {
                base.WritePostSaveErrorToLog(this.ToString(), "ProcessLockUserBit", string.Format(ERROR_LOCK_USER_FAILED_ACTIVITY_CREATION, UserName));
                return false;
            }
            return true;
        }

        /// <summary>
        /// Processes the Unlock user Bit
        /// </summary>
        /// <param name="actingUserID"></param>
        /// <returns></returns>
        private bool ProcessUnlockUserBit(Guid? actingUserID)
        {
            if (!ServiceFactory.ActivityService.CreateUserUnlockedActivity(UserID, actingUserID))
            {
                base.WritePostSaveErrorToLog(this.ToString(), "ProcessUnlockUserBit", string.Format(ERROR_UNLOCK_USER_FAILED_ACTIVITY_CREATION, UserName));
                return false;
            }
            return true;
        }

        /// <summary>
        /// Processes the Policy Version Changed bit
        /// </summary>
        /// <param name="actingUserID"></param>
        /// <returns></returns>
        private bool ProcessPolicyVersionChangedBit(Guid? actingUserID)
        {
            //Create a policy version acceptance activity for each policy accepted
            if (_acceptedCardHolder)
            {
                if (!ServiceFactory.ActivityService.CreateTermsAcceptedActivity(UserID, ActivityType.AcceptUpdatedCardHolderTerms))
                {
                    base.WritePostSaveErrorToLog(this.ToString(), "ProcessPolicyVersionChangedBit", string.Format(ERROR_POLICY_CHANGED_FAILED_ACTIVITY_CREATION,
                        UserName, "Card Holder Agreement"));
                    return false;
                }
            }
            if (_acceptedPrivacy)
            {
                if (!ServiceFactory.ActivityService.CreateTermsAcceptedActivity(UserID, ActivityType.AcceptUpdatedPrivacyPolicy))
                {
                    base.WritePostSaveErrorToLog(this.ToString(), "ProcessPolicyVersionChangedBit", string.Format(ERROR_POLICY_CHANGED_FAILED_ACTIVITY_CREATION,
                        UserName, "Privacy Policy"));
                    return false;
                }
            }
            if (_acceptedTerms)
            {
                if (!ServiceFactory.ActivityService.CreateTermsAcceptedActivity(UserID, ActivityType.AcceptUpdatedTermsandConditions))
                {
                    base.WritePostSaveErrorToLog(this.ToString(), "ProcessPolicyVersionChangedBit", string.Format(ERROR_POLICY_CHANGED_FAILED_ACTIVITY_CREATION,
                        UserName, "Terms and Conditions"));
                    return false;
                }
            }
            if (_acceptedTarget)
            {
                if (!ServiceFactory.ActivityService.CreateTermsAcceptedActivity(UserID, ActivityType.AcceptUpdatedTargetConditions))
                {
                    Log.Error("User " + UserID + " failed to save due to failed Creation of Target GiftCard Conditions Accepted Activity");
                    return false;
                }
            }

            return true;
        }
        #endregion //Process

        #region Reset
        /// <summary>
        /// Resets all the save bits
        /// </summary>
        private void ResetSaveBits()
        {
            _passwordReset = false;
            _newPassword = "";
            _sendAccountChangedNotification = false;
            _lockUser = false;
            _unLockUser = false;
            _acceptedCardHolder = false;
            _acceptedPrivacy = false;
            _acceptedTerms = false;
            _acceptedTarget = false;
        }
        #endregion //Reset

        #endregion //Saving Bit Processing

        #region Misc Methods
        public static int RetrieveUserCount(
            RoleType roleType,
            Product? product,
            DateTime? creationDateRangeBegin,
            DateTime? creationDateRangeEnd,
            DateTime? enrollmentDateRangeBegin,
            DateTime? enrollmentDateRangeEnd
            )
        {
            return AdapterFactory.UserAdapter.RetrieveUserCount(
                roleType,
                product,
                creationDateRangeBegin,
                creationDateRangeEnd,
                enrollmentDateRangeBegin,
                enrollmentDateRangeEnd
                );
        }
        #endregion

        #region IUserInfo Members


        /// <summary>
        /// Gets the address1.
        /// </summary>
        /// <value>The address1.</value>
        public string Address1
        {
            get { return UserAddress != null ? UserAddress.Address1 : null; }
        }

        /// <summary>
        /// Gets the address2.
        /// </summary>
        /// <value>The address2.</value>
        public string Address2
        {
            get { return UserAddress != null ? UserAddress.Address2 : null; }
        }

        /// <summary>
        /// Gets the city.
        /// </summary>
        /// <value>The city.</value>
        public string City
        {
            get { return UserAddress != null ? UserAddress.City : null; }
        }

        /// <summary>
        /// Gets the province.
        /// </summary>
        /// <value>The province.</value>
        public string Province
        {
            get { return UserAddress != null ? UserAddress.State : null; }
        }

        /// <summary>
        /// Gets the postal code.
        /// </summary>
        /// <value>The postal code.</value>
        public string PostalCode
        {
            get { return UserAddress != null ? UserAddress.ZipCode : null; }
        }

        /// <summary>
        /// Gets the country.
        /// </summary>
        /// <value>The country.</value>
        public string Country
        {
            get { return UserAddress != null ? UserAddress.Country : null; }
        }

        /// <summary>
        /// Gets the email address.
        /// </summary>
        /// <value>The email address.</value>
        public string EmailAddress
        {
            get { return Email != null ? Email.Address : null; }
        }

        #endregion
    }
}
