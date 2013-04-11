#region Copyright PAYjr Inc. 2005-2013
//
// All rights are reserved. Reproduction in whole or in part, in any 
// form or by any means, electronic, mechanical or otherwise, is    
// prohibited  without the prior written consent of the copyright owner.
////Filename: ErrorAdapter.cs
//
#endregion

using System;
using System.Data;
using System.Linq;
using System.Transactions;
using System.Web.Caching;
using Common.Util;
using Common.Util.Time;
using NLog;
using SD.LLBLGen.Pro.ORMSupportClasses;
using Payjr.Entity;
using Payjr.Entity.DatabaseSpecific;
using Payjr.Entity.EntityClasses;
using Payjr.Entity.FactoryClasses;
using Payjr.Entity.HelperClasses;
using Payjr.Types;
using System.Data.SqlClient;
using System.Web.Security;


namespace Payjr.Core.Adapters
{

    /// <summary>
    /// Cache Adapter for the User Entity
    /// </summary>
    public class UserAdapter : CacheAdapter
    {
        protected static readonly Logger Log = LogManager.GetCurrentClassLogger();
 
        #region Constructors
        public UserAdapter(CacheType type)
            : base(type)
        { }

        public UserAdapter()
            : base(CacheType.ASPNetCacheProvider)
        { }
        #endregion        

        #region private methods

        /// <summary>
        /// This function retrieves the user entity from the database using the UserID 
        /// </summary>
        /// <param name="userID">The User ID</param>
        /// <param name="insert">Is store into the Cache?</param>
        /// <returns>Return the user entity fetched from DB</returns>
        private UserEntity RetrieveUserByUserIDFromDB(Guid userID, Boolean isInsertIntoCache, Boolean includeDeleted)
        {
            try
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter())
                {
                    EntityCollection<UserEntity> users = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(UserFields.UserId == userID);                   
                    IPrefetchPath2 path = new PrefetchPath2(EntityType.UserEntity);
                    path.Add(UserEntity.PrefetchPathPolicy);
                    path.Add(UserEntity.PrefetchPathEmails);
                    path.Add(UserEntity.PrefetchPathUserIdentifiers);
                    path.Add(UserEntity.PrefetchPathAddressUsers).SubPath.Add(AddressUserEntity.PrefetchPathAddress);
                    adapter.FetchEntityCollection(users, bucket, path);
                    if (users.Count > 0)
                    {
                        UserEntity user = users.First();
                        if (isInsertIntoCache == true)
                        {
                            this.Add(userID, user);
                            String[] keys = { user.UserId.ToString() };
                            this.Add(user.UserName, user, new CacheDependency(null, keys));
                        }
                        if (user.MarkedForDeletion && !includeDeleted)
                        {
                            return null;
                        }
                        return user;
                    }
                    return null;
                }
            }
            catch (ORMException)
            {
                // Log.WriteException(exceptionMessage);
                return null;
            }

            //try
            //{
            //    using (DataAccessAdapter adapter = new DataAccessAdapter())
            //    {

            //        EntityCollection<UserEntity> users = new EntityCollection<UserEntity>(new UserEntityFactory());
            //        IRelationPredicateBucket bucket = new RelationPredicateBucket();
            //        bucket.PredicateExpression.Add(UserFields.UserId == userID);
            //        IPrefetchPath2 path = new PrefetchPath2((int)EntityType.UserEntity);
            //        path.Add(UserEntity.PrefetchPathPolicy);

            //        adapter.FetchEntityCollection(users, bucket, path);

            //        if (users.Count > 0)
            //        {
            //            UserEntity user = users[0];



            //            if (isInsertIntoCache == true)
            //            {
            //                this.Add(userID, user);
            //                String[] keys = { user.UserId.ToString() };
            //                this.Add(user.UserName, user, new CacheDependency(null, keys));
            //            }
            //            if (user.MarkedForDeletion && !includeDeleted)
            //            {
            //                return null;
            //            }
            //            return user;
            //        }
            //        return null;
            //    }
            //}
            //catch (ORMException)
            //{
            //    // Log.WriteException(exceptionMessage);
            //    return null;
            //}
        }

        /// <summary>
        /// This function retrieves the user entity using the userName
        /// </summary>
        /// <param name="UserName">The username to fetch</param>
        /// <param name="insert">In the user entity inserted </param>
        /// <returns></returns>
        private UserEntity RetrieveUserByUserNameFromDB(String UserName, Boolean insert)
        {

            try
            {

                using (DataAccessAdapter adapter = new DataAccessAdapter(true))
                {
                    EntityCollection<UserEntity> Users = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(UserFields.UserName == UserName);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.UserEntity);
                    path.Add(UserEntity.PrefetchPathPolicy);
                    adapter.FetchEntityCollection(Users, bucket, path);
                    if (Users.Count > 0)
                    {
                        UserEntity user = Users[0];
                        if (insert == true)
                        {
                            this.Add(user.UserId.ToString(), user);
                            String[] keys = { user.UserId.ToString() };
                            this.Add(user.UserName, user, new CacheDependency(null, keys));
                        }
                        return user;
                    }
                    else
                    {
                        return null;
                    }
                }

            }
            catch (ORMException)
            {
                // Log.WriteException(exceptionMessage);
                return null;
            }
        }

        /// <summary>
        /// Retrieves the user product  from DB
        /// </summary>
        /// <param name="UserId">The user Id</param>
        /// <returns></returns>
        private Product RetrieveUserProductFromDB(Guid userID)
        {
            UserEntity user = RetrieveUserByUserID(userID, true);

            if (user.RoleType == RoleType.Parent)
            {
                EntityCollection<RegisteredTeenEntity> teens = RetrieveTeensByParentID(userID, true);
                foreach (RegisteredTeenEntity teen in teens)
                {
                    EntityCollection<ProductUserEntity> products = RetrieveProductsByUser(teen.UserId);
                    if (products.Count > 0)
                    {
                        if (products[0].ProductNumber != Product.Experience_IC)
                            return products[0].ProductNumber;
                    }
                }
            }
            else if (user.RoleType == RoleType.RegisteredTeen)
            {
                EntityCollection<ProductUserEntity> products = RetrieveProductsByUser(userID);
                if (products.Count > 0)
                    return products[0].ProductNumber;
            }
            else if (user.RoleType == RoleType.MasterAdmin)
            {
                return Product.PPaid_IC;
            }

            return Product.Experience_IC;
        }

        /// <summary>
        /// Retrieves the Teens using the parentID
        /// </summary>
        /// <param name="ParentID"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private EntityCollection<RegisteredTeenEntity> RetrieveTeensbyParentFromDB(Guid ParentID, String key, Boolean includeDeleted)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(false))
            {
                EntityCollection<RegisteredTeenEntity> collection = new EntityCollection<RegisteredTeenEntity>(new RegisteredTeenEntityFactory());
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                bucket.PredicateExpression.Add(RegisteredTeenFields.ParentId == ParentID);
                IPrefetchPath2 path = new PrefetchPath2((int)EntityType.UserEntity);
                path.Add(UserEntity.PrefetchPathPolicy);

                if (!includeDeleted)
                    bucket.PredicateExpression.Add(UserFields.MarkedForDeletion == false);

                adapter.FetchEntityCollection(collection, bucket, path);
                if (collection.Count > 0)
                {
                    collection.Sort((int)RegisteredTeenFieldIndex.FirstName, System.ComponentModel.ListSortDirection.Ascending);
                }
                return collection;
            }
        }

        /// <summary>
        /// Creates the time activity filter.
        /// </summary>
        /// <param name="activeAfterTime">The active after time.</param>
        /// <returns></returns>
        private IRelationPredicateBucket CreateTimeActivityFilter(DateTime activeAfterTime)
        {
            IRelationPredicateBucket filterBucket = new RelationPredicateBucket();
            filterBucket.PredicateExpression.Add(
                UserFields.LastActivityDate >= activeAfterTime);
            return filterBucket;
        }

        /// <summary>
        /// Creates the time filter.
        /// </summary>
        /// <param name="afterTime">The after time.</param>
        /// <param name="cmpField">The compare field.</param>
        /// <returns></returns>
        private IRelationPredicateBucket CreateTimeFilter(DateTime afterTime, EntityField2 cmpField)
        {
            IRelationPredicateBucket filterBucket = new RelationPredicateBucket();
            filterBucket.PredicateExpression.Add(
                cmpField >= afterTime);
            return filterBucket;
        }

        #endregion

        #region User

        #region Create

        /// <summary>
        /// Creates a new ParentEntity
        /// </summary>
        /// <returns></returns>
        public ParentEntity CreateParent()
        {
            ParentEntity parent = new ParentEntity();
            parent.UserId = Guid.NewGuid();

            //Set properties we no longer use and do not want to expose
            parent.CommunicationPhrase = "";

            return parent;
        }

        /// <summary>
        /// Creates new ParentEntity with parameters 
        /// </summary>
        /// <returns></returns>
        public ParentEntity CreateParent(string firstname, string lastname, string password, string username, Guid brandingId, Guid themeId, 
            Guid cultureId, out MembershipCreateStatus result)
        {
            UserEntity userByUserName = RetrieveUserByUserNameFromDB(username, false);
            if (userByUserName != null)
            {                
                result = MembershipCreateStatus.DuplicateEmail;
                return null;
            }
            
            TimeZoneInformation timeZone = TimeZoneInformation.CentralTimeZone;
            string salt = Guid.NewGuid().ToString().Trim();
            
            Guid userId = Guid.NewGuid();
            ParentEntity parent = new ParentEntity();
            parent.UserId = userId;
            parent.FirstName = firstname;
            parent.LastName = lastname;
            parent.UserName = username;
            parent.PasswordSalt = salt;
            parent.Password = CreateSecurePassword(salt, password);            
            parent.PasswordQuestion = string.Empty;
            parent.PasswordAnswer = string.Empty;
            parent.CommunicationPhrase = string.Empty;
            parent.RoleType = RoleType.Parent;
            parent.RecieveMarketingEmail = true;
            parent.LastActivityDate = DateTime.UtcNow;
            parent.LastLoginDate = DateTime.UtcNow;
            parent.LastPasswordChangedDate = DateTime.UtcNow;
            parent.CreationDate = DateTime.UtcNow;
            parent.IsOnLine = false;
            parent.IsLockedOut = false;
            parent.LastLockedOutDate = DateTime.UtcNow;
            parent.FailedPasswordAttemptCount = 0;
            parent.FailedPasswordAttemptWindowStart = DateTime.UtcNow;
            parent.FailedPasswordAttemptAttemptCount = 0;
            parent.FailedPasswordAnswerAttemptWindowsStart = DateTime.UtcNow;
            parent.TimeZone = timeZone.ToString();
            parent.CultureId = cultureId;
            parent.BrandingId = brandingId;
            parent.ThemeId = themeId;
            parent.LastIpaddress = string.Empty;
            parent.LastHostName = string.Empty;
            parent.Sex = "m";
            parent.MarkedForDeletion = false;
            parent.IsActive = true;
            parent.ReceivePaperStatement = false;
            parent.FailedChallengeAttemps = 0;
            parent.CanEmergencyLoad = true;

            if (AdapterFactory.UserAdapter.UpdateUser(parent))
            {
                result = MembershipCreateStatus.Success;
                return parent;
            }
            result = MembershipCreateStatus.UserRejected;
            return null;            
        }

        public ParentEntity CreateParent(string firstname, string lastname, string password, string username, string email,
            string passwordQuestion, string passwordAnswer, DateTime? dateOfBirth, string ssn, string addressLine1, string addresLine2,
            string city, string state, string zipcode, Guid brandingId, Guid themeId, Guid cultureId, bool isActive, out MembershipCreateStatus result)
        {
            //Check duplicate UserName
            UserEntity existingUser = RetrieveUserByUserNameFromDB(username, false);
            if (existingUser != null)
            {
                result = MembershipCreateStatus.DuplicateUserName;
                return null;
            }

            //Check duplicate the email
            existingUser = RetrieveUserByEmail(email);
            if (existingUser != null)
            {
                result = MembershipCreateStatus.DuplicateEmail;
                return null;
            }

            TimeZoneInformation timeZone = TimeZoneInformation.CentralTimeZone;
            string salt = Guid.NewGuid().ToString();

            var parent = new ParentEntity();
            parent.UserId = Guid.NewGuid();
            parent.FirstName = firstname;
            parent.LastName = lastname;
            parent.UserName = username;
            parent.PasswordSalt = salt;
            parent.Password = CreateSecurePassword(salt, password);
            parent.PasswordQuestion = passwordQuestion;
            parent.PasswordAnswer = passwordAnswer;
            parent.Dob = dateOfBirth;
            parent.RoleType = RoleType.Parent;
            parent.IsActive = isActive;

            parent.CommunicationPhrase = string.Empty;
            parent.RecieveMarketingEmail = true;
            parent.LastActivityDate = DateTime.UtcNow;
            parent.LastLoginDate = DateTime.UtcNow;
            parent.LastPasswordChangedDate = DateTime.UtcNow;
            parent.CreationDate = DateTime.UtcNow;
            parent.IsOnLine = false;
            parent.IsLockedOut = false;
            parent.LastLockedOutDate = DateTime.UtcNow;
            parent.FailedPasswordAttemptCount = 0;
            parent.FailedPasswordAttemptWindowStart = DateTime.UtcNow;
            parent.FailedPasswordAttemptAttemptCount = 0;
            parent.FailedPasswordAnswerAttemptWindowsStart = DateTime.UtcNow;
            parent.TimeZone = timeZone.ToString();
            parent.CultureId = cultureId;
            parent.BrandingId = brandingId;
            parent.ThemeId = themeId;
            parent.LastIpaddress = string.Empty;
            parent.LastHostName = string.Empty;
            parent.Sex = "m";
            parent.MarkedForDeletion = false;
            parent.ReceivePaperStatement = false;
            parent.FailedChallengeAttemps = 0;
            parent.CanEmergencyLoad = true;

            //Email
            var emailEntity = new EmailEntity();
            emailEntity.Address = email;
            emailEntity.EmailType = EmailType.Mobile;
            emailEntity.IsDefault = true;
            emailEntity.Password = String.Empty;
            emailEntity.PasswordSalt = String.Empty;
            parent.Emails.Add(emailEntity);

            //SSN
            if (!String.IsNullOrEmpty(ssn))
            {
                var userIdentifierEntity = new UserIdentifierEntity();
                userIdentifierEntity.IdentifierType = IdentifierType.SSN;
                if (!String.IsNullOrEmpty(ssn))
                {
                    ssn = Utils.TryTrim(ssn);
                }
                userIdentifierEntity.Identifier = ssn;
                parent.UserIdentifiers.Add(userIdentifierEntity);
            }

            //Address
            if (!String.IsNullOrEmpty(addressLine1) ||
                !String.IsNullOrEmpty(addresLine2) ||
                !String.IsNullOrEmpty(city) ||
                !String.IsNullOrEmpty(state) ||
                !String.IsNullOrEmpty(zipcode))
            {
                var address = new AddressEntity();
                address.Address1 = addressLine1;
                address.Address2 = addresLine2;
                address.City = city;
                address.Locale = state;
                address.PostalCode = zipcode;
                address.Country = "US";
                var addressUser = new AddressUserEntity();
                addressUser.Address = address;
                parent.AddressUsers.Add(addressUser);
            }

            bool save;
            try
            {
                save = AdapterFactory.UserAdapter.UpdateUser(parent);
            }
            catch (Exception ex)
            {
                Log.ErrorException("An error occured when creating an Parent User", ex);
                result = MembershipCreateStatus.UserRejected;
                return null;
            }

            if (save)
            {
                result = MembershipCreateStatus.Success;
                return parent;
            }
            result = MembershipCreateStatus.UserRejected;
            return null;
        }

        /// <summary>
        /// Create Admin entity.
        /// </summary>
        /// <returns></returns>
        public AdminEntity CreateAdmin()
        {
            AdminEntity admin = new AdminEntity();
            admin.UserId = Guid.NewGuid();

            admin.RoleType = RoleType.Admin;
            admin.RecieveMarketingEmail = false;
            admin.LastActivityDate = DateTime.UtcNow;
            admin.LastLoginDate = DateTime.UtcNow;
            admin.IsActive = true;
            admin.AnniversaryDate = DateTime.UtcNow;
            admin.CreationDate = DateTime.UtcNow;
            admin.ExternalUserIdentifier = null;
            admin.FailedPasswordAnswerAttemptWindowsStart = DateTime.UtcNow;
            admin.FailedPasswordAttemptAttemptCount = 0;
            admin.FailedPasswordAttemptCount = 0;
            admin.FailedPasswordAttemptWindowStart = DateTime.UtcNow;
            admin.IsLockedOut = false;
            admin.IsOnLine = false;
            admin.LastActivityDate = DateTime.UtcNow;
            admin.LastHostName = "0.0.0.0";
            admin.LastIpaddress = "0.0.0.0";
            admin.CommunicationPhrase = string.Empty;
            admin.LastLockedOutDate = DateTime.UtcNow;
            admin.LastLoginDate = DateTime.UtcNow;
            admin.LastPasswordChangedDate = DateTime.UtcNow;
            admin.PasswordSalt = Guid.NewGuid().ToString().Trim();

            return admin;
        }

        /// <summary>
        /// Create master admin entity.
        /// </summary>
        /// <returns></returns>
        public MasterAdminEntity CreateMasterAdmin()
        {
            MasterAdminEntity masterAdminEntity = new MasterAdminEntity();
            masterAdminEntity.UserId = Guid.NewGuid();

            masterAdminEntity.RoleType = RoleType.MasterAdmin;
            masterAdminEntity.IsSupervisor = true;
            masterAdminEntity.RecieveMarketingEmail = false;
            masterAdminEntity.LastActivityDate = DateTime.UtcNow;
            masterAdminEntity.LastLoginDate = DateTime.UtcNow;
            masterAdminEntity.IsActive = true;
            masterAdminEntity.AnniversaryDate = DateTime.UtcNow;
            masterAdminEntity.CreationDate = DateTime.UtcNow;
            masterAdminEntity.ExternalUserIdentifier = null;
            masterAdminEntity.FailedPasswordAnswerAttemptWindowsStart = DateTime.UtcNow;
            masterAdminEntity.FailedPasswordAttemptAttemptCount = 0;
            masterAdminEntity.FailedPasswordAttemptCount = 0;
            masterAdminEntity.FailedPasswordAttemptWindowStart = DateTime.UtcNow;
            masterAdminEntity.IsLockedOut = false;
            masterAdminEntity.IsOnLine = false;
            masterAdminEntity.LastActivityDate = DateTime.UtcNow;
            masterAdminEntity.LastHostName = "0.0.0.0";
            masterAdminEntity.LastIpaddress = "0.0.0.0";
            masterAdminEntity.CommunicationPhrase = string.Empty;
            masterAdminEntity.LastLockedOutDate = DateTime.UtcNow;
            masterAdminEntity.LastLoginDate = DateTime.UtcNow;
            masterAdminEntity.LastPasswordChangedDate = DateTime.UtcNow;
            masterAdminEntity.PasswordSalt = Guid.NewGuid().ToString().Trim();

            return masterAdminEntity;
        }

        /// <summary>
        /// Creates a new Teen Entity
        /// </summary>
        /// <returns></returns>
        public RegisteredTeenEntity CreateTeen()
        {
            RegisteredTeenEntity teen = new RegisteredTeenEntity();
            teen.UserId = Guid.NewGuid();

            //Set properties we no longer use and do not want to expose
            teen.CommunicationPhrase = "";
            //teen.ProgramType = ProgramType.Experience;

            return teen;
        }

        /// <summary>
        /// Creates a new PreRegistered Teen Entity
        /// </summary>
        /// <returns></returns>
        public RegisteredTeenEntity CreateRegisteredTeen(Guid parentId, string firstname, string lastname, string password, string username,
            string email, string passwordQuestion, string passwordAnswer, DateTime dateOfBirth, string ssn, string addressLine1, string addresLine2,
            string city, string state, string zipcode, string refCode, Guid brandingId, Guid themeId, Guid cultureId, bool isActive, out MembershipCreateStatus result)
        {
            //Check duplicate UserName
            UserEntity existingUser = RetrieveUserByUserNameFromDB(username, false);
            if (existingUser != null)
            {
                result = MembershipCreateStatus.DuplicateUserName;
                return null;
            }

            //Check duplicate the email
            existingUser = RetrieveUserByEmail(email);
            if (existingUser != null)
            {
                result = MembershipCreateStatus.DuplicateEmail;
                return null;
            }

            TimeZoneInformation timeZone = TimeZoneInformation.CentralTimeZone;
            string salt = Guid.NewGuid().ToString();

            var teen = new RegisteredTeenEntity();
            teen.UserId = Guid.NewGuid();
            teen.FirstName = firstname;
            teen.LastName = lastname;
            teen.UserName = username;
            teen.PasswordSalt = salt;
            teen.Password = CreateSecurePassword(salt, password);
            teen.PasswordQuestion = passwordQuestion;
            teen.PasswordAnswer = passwordAnswer;
            teen.Dob = dateOfBirth;
            teen.IsActive = isActive;
            teen.RoleType = RoleType.RegisteredTeen;
            teen.ReferralCode = refCode;
            teen.ParentId = parentId;

            teen.CommunicationPhrase = string.Empty;
            teen.RecieveMarketingEmail = true;
            teen.LastActivityDate = DateTime.UtcNow;
            teen.LastLoginDate = DateTime.UtcNow;
            teen.LastPasswordChangedDate = DateTime.UtcNow;
            teen.CreationDate = DateTime.UtcNow;
            teen.IsOnLine = false;
            teen.IsLockedOut = false;
            teen.LastLockedOutDate = DateTime.UtcNow;
            teen.FailedPasswordAttemptCount = 0;
            teen.FailedPasswordAttemptWindowStart = DateTime.UtcNow;
            teen.FailedPasswordAttemptAttemptCount = 0;
            teen.FailedPasswordAnswerAttemptWindowsStart = DateTime.UtcNow;
            teen.TimeZone = timeZone.ToString();
            teen.CultureId = cultureId;
            teen.BrandingId = brandingId;
            teen.ThemeId = themeId;
            teen.LastIpaddress = string.Empty;
            teen.LastHostName = string.Empty;
            teen.Sex = "m";
            teen.MarkedForDeletion = false;

            //Email
            if (!String.IsNullOrEmpty(email))
            {
                var emailEntity = new EmailEntity();
                emailEntity.Address = email;
                emailEntity.EmailType = EmailType.Mobile;
                emailEntity.IsDefault = true;
                emailEntity.Password = String.Empty;
                emailEntity.PasswordSalt = String.Empty;
                teen.Emails.Add(emailEntity);
            }

            //SSN
            if (!String.IsNullOrEmpty(ssn))
            {
                var userIdentifierEntity = new UserIdentifierEntity();
                userIdentifierEntity.IdentifierType = IdentifierType.SSN;
                if (!String.IsNullOrEmpty(ssn))
                {
                    ssn = Utils.TryTrim(ssn);
                }
                userIdentifierEntity.Identifier = ssn;
                teen.UserIdentifiers.Add(userIdentifierEntity);
            }

            //Address
            if (!String.IsNullOrEmpty(addressLine1) ||
                !String.IsNullOrEmpty(addresLine2) ||
                !String.IsNullOrEmpty(city) ||
                !String.IsNullOrEmpty(state) ||
                !String.IsNullOrEmpty(zipcode))
            {
                var address = new AddressEntity();
                address.Address1 = addressLine1;
                address.Address2 = addresLine2;
                address.City = city;
                address.Locale = state;
                address.PostalCode = zipcode;
                address.Country = "US";
                var addressUser = new AddressUserEntity();
                addressUser.Address = address;
                teen.AddressUsers.Add(addressUser);
            }

            bool save;
            try
            {
                save = AdapterFactory.UserAdapter.UpdateUser(teen);
            }
            catch (Exception ex)
            {
                Log.ErrorException("An error occured when creating an Teen User", ex);
                result = MembershipCreateStatus.UserRejected;
                return null;
            }

            if (save)
            {
                result = MembershipCreateStatus.Success;
                return teen;
            }
            result = MembershipCreateStatus.UserRejected;
            return null;
        }     

        #endregion //Create

        #region Retrieve

        /// <summary>
        /// Returns a User entity with the given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public UserEntity RetrieveUserByUserID(String key)
        {
            return RetrieveUserByUserID(new Guid(key), false);
        }

        /// <summary>
        /// Returns a User entity with the given user id
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public UserEntity RetrieveUserByUserID(Guid key, Boolean includeDeleted)
        {
            UserEntity user = RetrieveUserByUserIDFromDB(key, false, includeDeleted);

            //if ((user!=null) && user.IsDirty)
            //    return RetrieveUserByUserIDFromDB(key, false, includeDeleted);



            return user;
        }

        /// <summary>
        /// Retrieve the user entity using the user name
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        public UserEntity RetrieveByUserName(String UserName)
        {
            UserEntity user = RetrieveUserByUserNameFromDB(UserName, false);
            if (user != null && user.IsDirty)
                return RetrieveUserByUserNameFromDB(UserName, false);

            return user;


        }

        public EntityCollection<RegisteredTeenEntity> RetrieveTeensByParentID(Guid ParentID, Boolean IncludeDeleted)
        {
            String key = "Teens" + ParentID;
            return RetrieveTeensbyParentFromDB(ParentID, key, IncludeDeleted);
        }

        /// <summary>
        /// Retrieves the user by email id.
        /// </summary>
        /// <param name="emailAddress">The email id.</param>
        /// <returns></returns>
        public UserEntity RetrieveUserByEmail(Guid emaiID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> collection = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(UserEntity.Relations.EmailEntityUsingUserId);
                    bucket.PredicateExpression.Add(EmailFields.EmailId == emaiID);
                    bucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);

                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.UserEntity);

                    adapter.FetchEntityCollection(collection, bucket, path);

                    if (collection.Count > 0)
                    {
                        return collection[0];
                    }
                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error user by email ID", exc);
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves the user by email address text
        /// </summary>
        /// <param name="Email">The email address of the user to retrieve</param>
        /// <returns>UserEntity of the user found during the search</returns>
        public UserEntity RetrieveUserByEmail(String email)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> collection = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(UserEntity.Relations.EmailEntityUsingUserId);
                    bucket.PredicateExpression.Add(EmailFields.Address == email);
                    bucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);

                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.UserEntity);

                    adapter.FetchEntityCollection(collection, bucket, path);

                    if (collection.Count > 0)
                    {
                        return collection[0];
                    }
                }
                catch (ORMException exc)
                {
                    Log.ErrorException(String.Format("An error occured when retrieving user by email {0}", email), exc);
                    throw new DataAccessException("Error user by email ID", exc);
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves the user by email address text
        /// </summary>
        /// <param name="Email">The email address of the user to retrieve</param>
        /// <returns>UserEntity of the user found during the search</returns>
        public EntityCollection<UserEntity> RetrieveUserByLastName(string lastName)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> collection = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(UserFields.LastName == lastName);
                    bucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);

                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.UserEntity);

                    adapter.FetchEntityCollection(collection, bucket, path);


                    return collection;
                    
                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error user by email ID", exc);
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieves the user by ACH account ID.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public UserEntity RetrieveUserByACHAccountID(Guid accountID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> users = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(UserEntity.Relations.BankAccountUserEntityUsingUserId);
                    bucket.PredicateExpression.Add(BankAccountUserFields.BankAccountId == accountID);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.UserEntity);

                    adapter.FetchEntityCollection(users, bucket, path);

                    if (users.Count > 0)
                    {
                        return users[0];
                    }
                    return null; ;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the user by Credit Card account ID.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public UserEntity RetrieveUserByCreditCardAccountID(Guid accountID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> users = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(UserEntity.Relations.CreditCardAccountEntityUsingUserId);
                    bucket.PredicateExpression.Add(CreditCardAccountFields.CreditCardAccountId == accountID);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.UserEntity);

                    adapter.FetchEntityCollection(users, bucket, path);

                    if (users.Count > 0)
                    {
                        return users[0];
                    }
                    return null; ;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the user by savings account ID.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public UserEntity RetrieveUserBySavingsAccountID(Guid accountID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> users = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(UserEntity.Relations.CustomAccountFieldGroupEntityUsingUserId);
                    bucket.PredicateExpression.Add(CustomAccountFieldGroupFields.CustomAccountFieldGroupId == accountID);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.UserEntity);

                    adapter.FetchEntityCollection(users, bucket, path);

                    if (users.Count > 0)
                    {
                        return users[0];
                    }
                    return null; ;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the user by prepaid card account ID.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public UserEntity RetrieveUserByPrepaidCardAccountID(Guid accountID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> users = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(UserEntity.Relations.PrepaidCardAccountUserEntityUsingUserId);
                    bucket.PredicateExpression.Add(PrepaidCardAccountUserFields.PrepaidCardAccountId == accountID);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.UserEntity);

                    adapter.FetchEntityCollection(users, bucket, path);
                    if (users.Count > 0)
                    {
                        return users[0];
                    }
                    return null;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the user by prepaid card number.
        /// </summary>
        /// <param name="cardNumber">The card number.</param>
        /// <returns></returns>
        public UserEntity RetrieveUserByPrepaidCardNumber(AesCryptoString.AesCryptoString cardNumber)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> users = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(UserEntity.Relations.PrepaidCardAccountUserEntityUsingUserId);
                    bucket.Relations.Add(PrepaidCardAccountUserEntity.Relations.PrepaidCardAccountEntityUsingPrepaidCardAccountId);
                    bucket.PredicateExpression.Add(PrepaidCardAccountFields.CardNumber == cardNumber);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.UserEntity);

                    adapter.FetchEntityCollection(users, bucket, path);

                    if (users.Count > 0)
                    {
                        return users[0];
                    }
                    return null;

                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the user using the <paramref name="UserId"/>
        /// </summary>
        /// <param name="UserId">The user id.</param>
        /// <param name="userHostName">Name of the user host.</param>
        /// <param name="userIPAddress">The user IP address.</param>
        /// <returns></returns>
        /// <remarks>This will update the database with the LastActivityDate and ipaddress</remarks>
        public bool RetrieveUser(string userName, string userHostName, string userIPAddress, out UserEntity user)
        {
            user = RetrieveByUserName(userName);

            if (user != null)
            {
                if (userHostName.Length != 0)
                {
                    UpdateDBWithBrowserSetting(userHostName, userIPAddress, user);
                    //Update the Security info with the information
                    user.LastLoginDate = DateTime.UtcNow;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Retrieves all users.
        /// </summary>
        /// <returns></returns>
        public EntityCollection<UserEntity> RetrieveAllUsers()
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                EntityCollection<UserEntity> entities = new EntityCollection<UserEntity>(new UserEntityFactory());
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                bucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);

                adapter.FetchEntityCollection(entities, bucket);

                return entities;
            }
        }

        /// <summary>
        /// Retrieves all users.
        /// </summary>
        /// <param name="pageIndex">Index of the page.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns></returns>
        public EntityCollection<UserEntity> RetrieveAllUsers(int pageIndex, int pageSize)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> collection = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    adapter.FetchEntityCollection(collection, bucket, 0, null, pageIndex, pageSize);

                    return collection;
                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error Retrieving all users", exc);
                }
            }

        }

        /// <summary>
        /// Retrieves all users that have been active after <paramref name="activeAfterTime"/>
        /// </summary>
        /// <param name="activeAfterTime">The active after time.</param>
        /// <returns></returns>
        public EntityCollection<UserEntity> RetrieveAllUsers(DateTime activeAfterTime)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> collection = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket filterBucket = CreateTimeFilter(activeAfterTime, UserFields.LastActivityDate);
                    filterBucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    adapter.FetchEntityCollection(collection, filterBucket);

                    return collection;
                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error Retrieving all users", exc);
                }
            }
        }

        /// <summary>
        /// Retrieves all users that have been active after <paramref name="activeAfterTime"/>
        /// </summary>
        /// <param name="activeAfterTime">The active after time.</param>
        /// <returns></returns>
        public EntityCollection<UserEntity> RetrieveAllUsers(DateTime activeAfterTime, Guid brandingID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> collection = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket filterBucket = CreateTimeFilter(activeAfterTime, UserFields.LastActivityDate);
                    filterBucket.PredicateExpression.Add(UserFields.BrandingId == brandingID);
                    filterBucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    adapter.FetchEntityCollection(collection, filterBucket);

                    return collection;
                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error Retrieving all users", exc);
                }
            }
        }

        /// <summary>
        /// Retrieves all users 
        /// for a Branding
        /// </summary>
        /// <param name="brandingID">The branding ID.</param>
        /// <returns></returns>
        public EntityCollection<UserEntity> RetrieveAllUsers(Guid brandingID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> collection = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket filterBucket = new RelationPredicateBucket();
                    filterBucket.PredicateExpression.Add(UserFields.BrandingId == brandingID);
                    filterBucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    adapter.FetchEntityCollection(collection, filterBucket);

                    return collection;
                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error Retrieving all users", exc);
                }
            }

        }

        /// <summary>
        /// Retrieves all users count.
        /// </summary>
        /// <returns></returns>
        public int RetrieveAllUsersCount()
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    IRelationPredicateBucket filterBucket = new RelationPredicateBucket();
                    filterBucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    return (int)adapter.GetDbCount(new UserEntityFactory().CreateFields(), filterBucket, null, false);

                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error Retrieving all users", exc);
                }
            }
        }

        /// <summary>
        /// Retrieves all users count.
        /// </summary>
        /// <param name="afterTime">The after time.</param>
        /// <param name="cmpField">The compare field.</param>
        /// <returns></returns>
        public int RetrieveAllUsersCount(DateTime afterTime, EntityField2 cmpField)
        {

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    IRelationPredicateBucket filterBucket = CreateTimeFilter(afterTime, cmpField);
                    filterBucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    return (int)adapter.GetDbCount(new UserEntityFactory().CreateFields(), filterBucket, null, false);
                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error Retrieving all users", exc);
                }
            }
        }

        /// <summary>
        /// Retrieves all users count.
        /// </summary>
        /// <returns></returns>
        public int RetrieveAllUsersCount(Guid brandingID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    IRelationPredicateBucket filterBucket = new RelationPredicateBucket();
                    filterBucket.PredicateExpression.Add(UserFields.BrandingId == brandingID);
                    filterBucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    return (int)adapter.GetDbCount(new UserEntityFactory().CreateFields(), filterBucket, null, false);

                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error Retrieving all users", exc);
                }
            }
        }

        /// <summary>
        /// Retrieves all users count.
        /// </summary>
        /// <param name="afterTime">The after time.</param>
        /// <param name="cmpField">The compare field.</param>
        /// <param name="brandingID">The branding ID.</param>
        /// <returns></returns>
        public int RetrieveAllUsersCount(DateTime afterTime, EntityField2 cmpField, Guid brandingID)
        {

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    IRelationPredicateBucket filterBucket = CreateTimeFilter(afterTime, cmpField);
                    filterBucket.PredicateExpression.Add(UserFields.BrandingId == brandingID);
                    filterBucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    return (int)adapter.GetDbCount(new UserEntityFactory().CreateFields(), filterBucket, null, false);
                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error Retrieving all users", exc);
                }
            }
        }

        public int RetrieveAllAdminsCount()
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    IRelationPredicateBucket filterBucket = new RelationPredicateBucket();
                    filterBucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    filterBucket.PredicateExpression.Add(UserFields.RoleType == RoleType.Admin);
                    return (int)adapter.GetDbCount(new UserEntityFactory().CreateFields(), filterBucket, null, false);

                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error retrieving all Admins", exc);
                }
            }
        }

        public int RetrieveAllMasterAdminsCount()
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    IRelationPredicateBucket filterBucket = new RelationPredicateBucket();
                    filterBucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    filterBucket.PredicateExpression.Add(UserFields.RoleType == RoleType.MasterAdmin);
                    return (int)adapter.GetDbCount(new UserEntityFactory().CreateFields(), filterBucket, null, false);

                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error retrieving all MasterAdmins", exc);
                }
            }
        }

        public EntityCollection<AdminEntity> RetrieveAllAdmins(int pageIndex, int pageSize)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<AdminEntity> collection = new EntityCollection<AdminEntity>(new AdminEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    bucket.PredicateExpression.Add(UserFields.RoleType == RoleType.Admin);

                    SortExpression sortExpression = new SortExpression();
                    sortExpression.Add(UserFields.UserName | SortOperator.Descending);

                    adapter.FetchEntityCollection(collection, bucket, 0, sortExpression, pageIndex, pageSize);

                    return collection;
                }
                catch (ORMException e)
                {
                    throw new DataAccessException("Error retrieving all Admins", e);
                }
            }

        }

        public EntityCollection<MasterAdminEntity> RetrieveAllMasterAdmins(int pageIndex, int pageSize)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<MasterAdminEntity> collection = new EntityCollection<MasterAdminEntity>(new MasterAdminEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    bucket.PredicateExpression.Add(UserFields.RoleType == RoleType.MasterAdmin);

                    SortExpression sortExpression = new SortExpression();
                    sortExpression.Add(UserFields.UserName | SortOperator.Descending);

                    adapter.FetchEntityCollection(collection, bucket, 0, sortExpression, pageIndex, pageSize);

                    return collection;
                }
                catch (ORMException e)
                {
                    throw new DataAccessException("Error retrieving all MasterAdmins", e);
                }
            }

        }


        #endregion //Retrieve

        #region Search

        public EntityCollection<UserEntity> SearchUsers(IRelationPredicateBucket passedBucket, int pageNumber, int pageSize)
        {
            return SearchUsers(passedBucket, 0, null, pageNumber, pageSize);
        }

        /// <summary>
        /// Searches for users matching the criteria given
        /// </summary>
        /// <param name="passedBucket"></param>
        /// <param name="maxItems"></param>
        /// <param name="passedSorter"></param>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public EntityCollection<UserEntity> SearchUsers(IRelationPredicateBucket passedBucket, int maxItems, ISortExpression passedSorter, int pageNumber, int pageSize)
        {
            TransactionOptions option = new TransactionOptions();
            option.IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted;

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> collection = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket = passedBucket;
                    bucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.UserEntity);
                    path.Add(UserEntity.PrefetchPathEmails);
                    path.Add(UserEntity.PrefetchPathBranding);
                    path.Add(UserEntity.PrefetchPathProductUsers);
                    path.Add(UserEntity.PrefetchPathAddressUsers);
                    path.Add(UserEntity.PrefetchPathPrepaidCardAccountUsers);

                    adapter.FetchEntityCollection(collection, bucket, maxItems, passedSorter, path, pageNumber, pageSize);
                    if ((collection.Count > 1) && (passedSorter == null))
                    {
                        collection.Sort((int)UserFieldIndex.UserName, System.ComponentModel.ListSortDirection.Ascending);
                    }

                    return collection;
                }
                catch (ORMException exception)
                {
                    throw new DataAccessException("Error Searching users", exception);
                }
            }
        }

        /// <summary>
        /// Retrieves the number of users that will be retrieved using the given criteria
        /// </summary>
        /// <param name="passedBucket"></param>
        /// <param name="hitCount"></param>
        /// <returns></returns>
        public bool SearchUsersCount(IRelationPredicateBucket passedBucket, out int hitCount)
        {
            TransactionOptions option = new TransactionOptions();
            option.IsolationLevel = System.Transactions.IsolationLevel.ReadUncommitted;

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> collection = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket = passedBucket;
                    bucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);

                    hitCount = (int)adapter.GetDbCount(collection, bucket);

                    return true;
                }
                catch (ORMException exception)
                {
                    throw new DataAccessException("Error Searching users", exception);
                }
            }
        }

        /// <summary>
        /// Search for a user by user field and value
        /// </summary>
        /// <param name="field">UserEntity field for the search</param>
        /// <param name="value">Target value for the search</param>
        /// <returns></returns>
        private EntityCollection<UserEntity> SearchUsersByUserField(EntityField2 field, string value)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> collection = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(new FieldLikePredicate(field, null, "%" + value + "%"));
                    bucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    ISortExpression sorter = new SortExpression();
                    sorter.Add(SortClauseFactory.Create(UserFieldIndex.LastName, SortOperator.Ascending));
                    sorter.Add(SortClauseFactory.Create(UserFieldIndex.FirstName, SortOperator.Ascending));
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.UserEntity);
                    path.Add(UserEntity.PrefetchPathEmails);
                    path.Add(UserEntity.PrefetchPathBranding);

                    adapter.FetchEntityCollection(collection, bucket, 100000, sorter, path);
                    return collection;
                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error Searching users", exc);
                }
            }
        }

        /// <summary>
        /// Search User by First Name.
        /// </summary>
        /// <param name="FirstName">User First Name.</param>
        /// <param name="userType">Type of user should be search (Admin=1, Teen=2, Parent=3).</param>
        /// <returns></returns>
        public EntityCollection<UserEntity> SearchUsersByFirstName(string FirstName)
        {
            return SearchUsersByUserField(UserFields.FirstName, FirstName);
        }

        /// <summary>
        /// Search User by Last Name.
        /// </summary>
        /// <param name="LastName">User Last Name.</param>
        /// <returns></returns>
        public EntityCollection<UserEntity> SearchUsersByLastName(string LastName)
        {
            return SearchUsersByUserField(UserFields.LastName, LastName);
        }

        /// <summary>
        /// Search User by User Name.
        /// </summary>
        /// <param name="UserName">User Name.</param>
        /// <returns></returns>
        public EntityCollection<UserEntity> SearchUsersByUserName(string UserName)
        {
            return SearchUsersByUserField(UserFields.UserName, UserName);
        }

        /// <summary>
        /// Search User by Email address.
        /// </summary>
        /// <param name="EmailAddress">Email address.</param>
        /// <returns></returns>
        public EntityCollection<UserEntity> SearchUsersByEmailAddress(string EmailAddress)
        {

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> collection = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(EmailEntity.Relations.UserEntityUsingUserId);
                    bucket.PredicateExpression.Add(new FieldLikePredicate(EmailFields.Address, null, "%" + EmailAddress + "%"));
                    bucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    ISortExpression sorter = new SortExpression();
                    sorter.Add(SortClauseFactory.Create(UserFieldIndex.LastName, SortOperator.Ascending));
                    sorter.Add(SortClauseFactory.Create(UserFieldIndex.FirstName, SortOperator.Ascending));
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.UserEntity);
                    path.Add(UserEntity.PrefetchPathEmails);
                    path.Add(UserEntity.PrefetchPathBranding);

                    adapter.FetchEntityCollection(collection, bucket, 100000, sorter, path);
                    return collection;
                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error Searching users", exc);
                }
            }

        }

        /// <summary>
        /// Search User by Mailing Address Address Line.
        /// </summary>
        /// <param name="MailingAddressAddressLine">User Mailing Address Address Line.</param>
        /// <returns></returns>
        public EntityCollection<UserEntity> SearchUsersByMailingAddressAddressLine(string MailingAddressAddressLine)
        {

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> collection = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    bucket.PredicateExpression.Add(new FieldLikePredicate(UserFields.RecieveMarketingEmail, null, "%" + MailingAddressAddressLine + "%"));
                    adapter.FetchEntityCollection(collection, bucket);

                    return collection;
                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error Searching users", exc);
                }
            }

        }

        /// <summary>
        /// Search User by Mailing Address City.
        /// </summary>
        /// <param name="MailingAddressCity">User Mailing Address City.</param>
        /// <returns></returns>
        public EntityCollection<UserEntity> SearchUsersByMailingAddressCity(string MailingAddressCity)
        {

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> collection = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    //bucket.PredicateExpression.Add(new FieldLikePredicate(UserFields., null, "%" + MailingAddressCity + "%"));
                    bucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    adapter.FetchEntityCollection(collection, bucket);

                    return collection;
                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error Searching users", exc);
                }
            }

        }

        /// <summary>
        /// Search User by Mailing Address State.
        /// </summary>
        /// <param name="MailingAddressState">User Mailing Address State.</param>
        /// <returns></returns>
        public EntityCollection<UserEntity> SearchUsersByMailingAddressState(string MailingAddressState)
        {

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> collection = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    //bucket.PredicateExpression.Add(new FieldLikePredicate(UserFields.FirstName, null, "%" + MailingAddressState + "%"));
                    bucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    adapter.FetchEntityCollection(collection, bucket);

                    return collection;
                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error Searching users", exc);
                }
            }

        }

        /// <summary>
        /// Search User by Mailing Address Zip Code.
        /// </summary>
        /// <param name="MailingAddressZipCode">User Mailing Address Zip Code.</param>
        /// <returns></returns>
        public EntityCollection<UserEntity> SearchUsersByMailingAddressZipCode(string MailingAddressZipCode)
        {

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> collection = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    //bucket.PredicateExpression.Add(new FieldLikePredicate(UserFields., null, "%" + MailingAddressZipCode + "%"));
                    bucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    adapter.FetchEntityCollection(collection, bucket);

                    return collection;
                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error Searching users", exc);
                }
            }

        }

        /// <summary>
        /// Search User by Phone Number.
        /// </summary>
        /// <param name="PhoneNumber">User Phone Number.</param>
        /// <returns></returns>
        public EntityCollection<UserEntity> SearchUsersByPhoneNumber(string PhoneNumber)
        {

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserEntity> collection = new EntityCollection<UserEntity>(new UserEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    //bucket.PredicateExpression.Add(new FieldLikePredicate(UserFields.FirstName, null, "%" + firstName + "%"));
                    //bucket.PredicateExpression.Add(new FieldCompareValuePredicate(UserFields.FirstName, ComparisonOperator.Equal, strRoleType));
                    bucket.PredicateExpression.Add(UserFields.MarkedForDeletion != true);
                    adapter.FetchEntityCollection(collection, bucket);

                    return collection;
                }
                catch (ORMException exc)
                {
                    throw new DataAccessException("Error Searching users", exc);
                }
            }

        }

        #endregion //Search

        #region Update

        /// <summary>
        /// Resets the user's password
        /// </summary>
        /// <param name="user">User to reset the password for</param>
        /// <param name="answer">answer given to reset the password</param>
        /// <param name="newPassword">out put for new password</param>
        /// <returns>true if success/ fales if failure</returns>
        public bool ResetUserPassowrd(UserEntity user, string answer, out string newPassword)
        {
            newPassword = "";

            // Remove case-sensitivity and make sure we aren't going to have problems with whitespace.
            if (user.PasswordAnswer.ToLower().Trim() != answer.ToLower().Trim())
            {
                return false;
            }

            newPassword = GeneratePassword(user);

            return true;
        }

        /// <summary>
        /// Resets the user's password by email
        /// </summary>
        /// <param name="user">User to reset the password for</param>
        /// <param name="answer">user's email</param>
        /// <param name="newPassword">out put for new password</param>
        /// <returns>true if success/ fales if failure</returns>
        public bool ResetUserPasswordByEmail(string email, out string newPassword)
        {
            newPassword = "";
            UserEntity user = RetrieveUserByEmail(email);
            
            if (user != null)
            {
                newPassword = GeneratePassword(user);                
                return true;
            }            
            return false;                                               
        }

        /// <summary>
        /// Update the database settings 
        /// </summary>
        /// <param name="userHostName">Name of the user host.</param>
        /// <param name="userIPAddress">The user IP address.</param>
        /// <param name="user">The user.</param>
        public void UpdateDBWithBrowserSetting(string userHostName, string userIPAddress, UserEntity user)
        {
            //Update the Security info with the information
            user.LastHostName = userHostName;
            user.LastIpaddress = userIPAddress;
            user.LastActivityDate = DateTime.UtcNow;
            user.IsOnLine = true;
        }

        #endregion //Update

        #endregion //User

        #region Address

        #region Create

        /// <summary>
        /// Create a new address for the user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public AddressEntity CreateAddressForUser(UserEntity user)
        {

            AddressEntity address = new AddressEntity();
            AddressUserEntity addressUser = new AddressUserEntity();

            addressUser.AddressUserId = Guid.NewGuid();
            addressUser.User = user;
            addressUser.Address = address;

            user.AddressUsers.Add(addressUser);

            return address;
        }
        /// <summary>
        /// Create a new address for the user
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="addressID">The address ID.</param>
        public AddressUserEntity AssignAddressToUser(UserEntity user, Guid addressID)
        {

            AddressUserEntity addressUser = new AddressUserEntity();

            addressUser.AddressUserId = Guid.NewGuid();
            addressUser.User = user;
            addressUser.AddressId = addressID;

            return addressUser;
        }

        #endregion //Create

        #region Retrieve

        /// <summary>
        /// Retrieves the address of a user with UserID.
        /// </summary>
        public AddressEntity RetrieveUserAddress(Guid UserId)
        {
            AddressEntity addressEntity = null;
            try
            {
                EntityCollection<AddressEntity> addresses = new EntityCollection<AddressEntity>(new AddressEntityFactory());

                using (DataAccessAdapter adapter = new DataAccessAdapter(true))
                {
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(AddressEntity.Relations.AddressUserEntityUsingAddressId);
                    bucket.PredicateExpression.Add(AddressUserFields.UserId == UserId);
                    adapter.FetchEntityCollection(addresses, bucket);
                }

                if (addresses.Count > 0)
                {
                    addressEntity = addresses[0];
                }

            }
            catch (ORMException exceptionMessage)
            {
                throw new DataAccessException("Unable to retrieve AddressEntity for the user", exceptionMessage);
            }

            return addressEntity;
        }

        /// <summary>
        /// Retrieve a collection of addresses
        /// </summary>
        /// <param name="addressID"></param>
        /// <returns></returns>
        public EntityCollection<AddressUserEntity> RetrieveAddressUsersByAddressId(Guid addressID)
        {
            try
            {
                EntityCollection<AddressUserEntity> addresses = new EntityCollection<AddressUserEntity>(new AddressUserEntityFactory());

                using (DataAccessAdapter adapter = new DataAccessAdapter(true))
                {
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(AddressUserEntity.Relations.AddressEntityUsingAddressId);
                    bucket.PredicateExpression.Add(AddressFields.AddressId == addressID);
                    adapter.FetchEntityCollection(addresses, bucket);
                }
                return addresses;
            }
            catch (ORMException exceptionMessage)
            {
                throw new DataAccessException("Unable to retrieve AddressEntity ", exceptionMessage);

            }
        }

        /// <summary>
        /// Retrieves a list of duplicate address values with counts and dates of occurance.  Based primarily on the street number and postal code.
        /// </summary>
        /// <returns>DataSet containing a list of duplicate address values</returns>
        public DataSet RetrieveDuplicateUserAddresses()
        {
            try
            {
                DataSet returnDataSet = new DataSet();
                using (DataAccessAdapter adapter = new DataAccessAdapter(true))
                {
                    adapter.CallRetrievalStoredProcedure("up_DuplicateMailingAddresses", new SqlParameter[0], returnDataSet);
                }
                return returnDataSet;
            }
            catch (ORMException exception)
            {
                throw new DataAccessException("Unable to retrieve duplicate user addresses", exception);
            }
        }

        /// <summary>
        /// Retrieves a list of duplicate addresses for a given street number and postal code.  Includes information about the associated users.
        /// </summary>
        /// <param name="streetNumber">The street number to match</param>
        /// <param name="postalCode">The postal code to match</param>
        /// <returns>DataSet containing a listing of address and user information</returns>
        public DataSet RetrieveDuplicateUserAddressDetails(string streetNumber, string postalCode)
        {
            try
            {
                SqlParameter[] parameters = new SqlParameter[2] { new SqlParameter("Number", streetNumber), new SqlParameter("PostalCode", postalCode) };
                DataSet returnDataSet = new DataSet();
                using (DataAccessAdapter adapter = new DataAccessAdapter(true))
                {
                    adapter.CallRetrievalStoredProcedure("up_getDuplicateMailingDetail", parameters, returnDataSet);
                }
                return returnDataSet;
            }
            catch (ORMException exception)
            {
                throw new DataAccessException("Unable to retrieve duplicate user addresses", exception);
            }
        }
        #endregion //Retrieve

        #endregion //Address

        #region Products

        #region Retrieve

        /// <summary>
        /// Retrieve the Product user by UserID
        /// </summary>
        /// <param name="UserID">the UserID</param>
        /// <returns>products</returns>
        public EntityCollection<ProductUserEntity> RetrieveProductsByUser(Guid UserID)
        {
            try
            {
                EntityCollection<ProductUserEntity> productUsers = new EntityCollection<ProductUserEntity>(new ProductUserEntityFactory());

                using (DataAccessAdapter adapter = new DataAccessAdapter())
                {
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(ProductUserFields.UserId == UserID);
                    bucket.PredicateExpression.Add(ProductUserFields.Status == ProductUserStatus.Active);

                    adapter.FetchEntityCollection(productUsers, bucket);
                }

                // If there are no products for this user, it is most likely a parent.
                // Try to retrieve the products for the children of the parent.
                if ((productUsers == null) || (productUsers.Count == 0))
                {
                    // Retrieve this user's children - may be empty if this is not a parent
                    EntityCollection<RegisteredTeenEntity> teens = RetrieveTeensByParentID(UserID, false);

                    // Add each child's products to the product list
                    foreach (RegisteredTeenEntity teen in teens)
                    {
                        productUsers.AddRange(RetrieveProductsByUser(teen.UserId));
                    }

                    // If there are more than 1 products in the collection, sort it
                    // and remove an duplicates before returning.
                    if (productUsers.Count > 1)
                    {
                        productUsers.Sort((int)ProductUserFieldIndex.ProductNumber, System.ComponentModel.ListSortDirection.Ascending);

                        for (int i = productUsers.Count - 1; i > 0; i--)
                        {
                            if (productUsers[i].ProductNumber == productUsers[i - 1].ProductNumber)
                                productUsers.RemoveAt(i);
                        }
                    }
                }

                return productUsers;
            }
            catch (ORMException /*exception*/)
            {
                return null;
            }
        }

        public Product RetrieveUserProduct(Guid userID)
        {
            String key = "PROD:" + userID.ToString();
            Product product = Product.Experience_IC;

            if (Exists(key))
            {
                product = (Product)this[key];
            }
            else
            {
                product = RetrieveUserProductFromDB(userID);
            }

            return product;
        }

        /// <summary>
        /// Retrieves the user product pricing.
        /// </summary>
        /// <param name="userID">The user ID.</param>
        /// <param name="product">The product.</param>
        /// <returns></returns>
        public UserProductPricingEntity RetrieveUserProductPricing(Guid userID, Product product)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                try
                {
                    EntityCollection<UserProductPricingEntity> productPricings = new EntityCollection<UserProductPricingEntity>(new UserProductPricingEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(UserProductPricingFields.ProductNumber == product);
                    bucket.PredicateExpression.Add(UserProductPricingFields.UserId == userID);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.ProductPricingEntity);

                    adapter.FetchEntityCollection(productPricings, bucket, path);

                    if (productPricings.Count > 0)
                    {
                        return productPricings[0];
                    }
                    return null;
                }
                catch (ORMException)
                {
                    return null;
                }
            }
        }

        #endregion //Retrieve

        #endregion //Products

        #region Helper Methods

        /// <summary>
        /// Create a Secure Password
        /// </summary>
        /// <param name="salt">The salt.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public string CreateSecurePassword(string salt, string password)
        {
            string encPassword = string.Concat(password.Trim(), salt);
            string hashedPwd = FormsAuthentication.HashPasswordForStoringInConfigFile(encPassword, "sha1");

            return hashedPwd.Trim();
        }

        private string GeneratePassword(UserEntity user)
        {            
            // Scenario #1603: BEGIN
            //    Change the Password Reset option to One letter and five numbers
            string newPassword = string.Empty;

            // Get 7 random non-zero bytes.
            // * One case identifier (upper/lower)
            // * One letter
            // * 5 numbers
            byte[] data = new byte[7];
            System.Security.Cryptography.RandomNumberGenerator rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetNonZeroBytes(data);

            // Create case modifier, compress into the range 0 to 1
            // Multiply by 32 - the numeric distance between upper and lower ASCII characters.
            int caseModifier = 32 * Convert.ToInt16(Math.Floor(2.0 * Convert.ToDouble(data[0]) / 256.0));

            // Create letter, compress into the range 0 to 25
            int asciiLetter = Convert.ToInt16(Math.Floor(26.0 * Convert.ToDouble(data[1]) / 256.0));

            // Set first character of the password
            // Add 65 (base for ASCII 'A') to the generated number and the case modifier.
            newPassword = Convert.ToChar(65 + asciiLetter + caseModifier).ToString();

            // For each byte, compress into the range 0 to 9
            // and append the digit to the end of the password.
            for (int i = 2; i < 7; i++)
            {
                newPassword += Math.Floor(10.0 * Convert.ToDouble(data[i]) / 256.0).ToString("0");
            }
            // Scenario #1603: END

            // Make sure the password doesn't have any whitespace around it.
            newPassword = newPassword.Trim();

            user.Password = AdapterFactory.UserAdapter.CreateSecurePassword(user.PasswordSalt, newPassword);
            user.LastPasswordChangedDate = DateTime.UtcNow;

            //Reset the Password counts
            user.FailedPasswordAttemptCount = 0;

            AdapterFactory.UserAdapter.UpdateUser(user);

            return newPassword;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="UserName"></param>
        /// <returns></returns>
        public bool UserNameExists(String UserName)
        {
            return (RetrieveByUserName(UserName) != null);
        }

        /// <summary>
        /// Check if the user name exists
        /// </summary>
        /// <param name="UserName"></param>
        /// <param name="UserId"></param>
        /// <returns></returns>
        public bool UserNameExists(String UserName, Guid UserId)
        {

            UserEntity user = RetrieveByUserName(UserName);
            if (user != null)
            {
                if (user.UserId != UserId)
                    return true;
                else
                    return false;
            }
            else
                return false;
        }

        /// <summary>
        /// Determines if a user entity propery is dirty
        /// </summary>
        /// <param name="user"></param>
        /// <param name="fieldIndex"></param>
        /// <returns></returns>
        public bool IsPropertyDirty(UserEntity user, int fieldIndex)
        {
            return user.Fields[fieldIndex].IsChanged;
        }


        ///<summary>
        /// create a new policy for user
        /// </summary>
        public PolicyEntity CreatePolicyForUser(UserEntity user)
        {
            PolicyEntity policy = new PolicyEntity();
            policy.User = user;
            user.Policy = policy;
            return policy;

        }

        /// <summary>
        /// Determines if a parent entity property is dirty
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="fieldIndex"></param>
        /// <returns></returns>
        public bool IsPropertyDirty(ParentEntity parent, int fieldIndex)
        {
            return parent.Fields[fieldIndex].IsChanged;
        }

        /// <summary>
        /// Determines if a teen entity property is dirty
        /// </summary>
        /// <param name="teen"></param>
        /// <param name="fieldIndex"></param>
        /// <returns></returns>
        public bool IsPropertyDirty(RegisteredTeenEntity teen, int fieldIndex)
        {
            return teen.Fields[fieldIndex].IsChanged;
        }

        #endregion //Helper Methods

        #region Phone

        #region Create

        /// <summary>
        /// Creates a new phone for the user
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="number"></param>
        /// <param name="type"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public PhoneEntity CreatePhone(UserEntity user)
        {
            PhoneEntity phone = new PhoneEntity();
            phone.PhoneId = Guid.NewGuid();

            PhoneUserEntity phoneUser = new PhoneUserEntity();
            phoneUser.Phone = phone;
            phoneUser.User = user;

            user.PhoneUsers.Add(phoneUser);

            return phone;
        }

        /// <summary>
        /// Create a new address for the user
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="addressID">The address ID.</param>
        public PhoneUserEntity AssignPhoneToUser(UserEntity user, Guid phoneID)
        {

            PhoneUserEntity phoneUser = new PhoneUserEntity();

            phoneUser.PhoneUserId = Guid.NewGuid();
            phoneUser.User = user;
            phoneUser.PhoneId = phoneID;

            return phoneUser;
        }

        #endregion //Create

        #region Retrieve

        /// <summary>
        /// Retrieves the phone of a user with UserID.
        /// </summary>
        public PhoneEntity RetrieveUserPhoneInfo(Guid UserId)
        {
            try
            {
                EntityCollection<PhoneEntity> phones = new EntityCollection<PhoneEntity>(new PhoneEntityFactory());

                using (DataAccessAdapter adapter = new DataAccessAdapter(true))
                {
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(PhoneEntity.Relations.PhoneUserEntityUsingPhoneId);
                    bucket.PredicateExpression.Add(PhoneUserFields.UserId == UserId);
                    adapter.FetchEntityCollection(phones, bucket);
                }

                if (phones.Count > 0)
                {
                    return phones[0];
                }
                else
                {
                    return null;
                }
            }
            catch (ORMException exceptionMessage)
            {
                throw new DataAccessException("Error Retrieving Phone", exceptionMessage);
            }
        }

        public EntityCollection<PhoneUserEntity> RetrievePhoneUsersByPhoneId(Guid phoneID)
        {
            try
            {
                EntityCollection<PhoneUserEntity> phones = new EntityCollection<PhoneUserEntity>(new PhoneUserEntityFactory());

                using (DataAccessAdapter adapter = new DataAccessAdapter(true))
                {
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(PhoneUserEntity.Relations.PhoneEntityUsingPhoneId);
                    bucket.PredicateExpression.Add(PhoneFields.PhoneId == phoneID);
                    adapter.FetchEntityCollection(phones, bucket);
                }

                return phones;
            }
            catch (ORMException exceptionMessage)
            {
                throw new DataAccessException("Error Retrieving phones by user", exceptionMessage);
            }
        }

        #endregion

        #endregion

        #region User Identifier

        #region Create

        /// <summary>
        /// Creates a new User Identifier Entity
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public UserIdentifierEntity CreateUserIdentifier(UserEntity user)
        {
            UserIdentifierEntity userIdentifier = new UserIdentifierEntity();
            userIdentifier.User = user;
            userIdentifier.UserIdentifierId = Guid.NewGuid();

            user.UserIdentifiers.Add(userIdentifier);

            return userIdentifier;
        }

        #endregion //Create

        #region Retrieve

        /// <summary>
        /// Retrieves a user Identifier by a user Identifier ID
        /// </summary>
        /// <param name="userIdentifierID"></param>
        /// <returns></returns>
        public UserIdentifierEntity RetrieveUserIdentifierByID(Guid userIdentifierID)
        {
            UserIdentifierEntity userIdentifier = new UserIdentifierEntity(userIdentifierID);
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    if (!adapter.FetchEntity(userIdentifier))
                    {
                        DataAccessException exception = new DataAccessException("Adapter Fetch Entity failed while Retrieving User Identifier By ID");
                        throw exception;
                    }

                    return userIdentifier;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// retrieves a user identifier by a user id
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public UserIdentifierEntity RetrieveUserIdentifierByUserID(Guid userID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UserIdentifierEntity> userIdentifiers = new EntityCollection<UserIdentifierEntity>(new UserIdentifierEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(UserIdentifierFields.UserId == userID);

                    adapter.FetchEntityCollection(userIdentifiers, bucket);

                    if (userIdentifiers.Count > 0)
                    {
                        return userIdentifiers[0];
                    }

                    return null;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        #endregion //Retrieve

        #endregion //User Identifier

        #region MFA

        #region Create

        /// <summary>
        /// Save the user's MFA data (assurance data and question/answer data)
        /// </summary>
        /// <param name="userAssurance">UserAssuranceEntity containing the data selected by the user</param>
        /// <param name="userQuestionAnswers">UserQuestionAnswerEntity collection containing the questions and answers selected by the user</param>
        /// <returns>true if success, false if failure</returns>
        public bool SaveUserMFAData(UserAssuranceEntity userAssurance, EntityCollection<UserQuestionAnswerEntity> userQuestionAnswers)
        {
            bool success = false;

            using (TransactionScope scope = new TransactionScope())
            {
                success = SaveUserAssurance(userAssurance);
                success = success && SaveUserQuestionAnswers(userQuestionAnswers);

                if (success)
                    scope.Complete();
            }

            return success;
        }

        /// <summary>
        /// Saves user assurance data
        /// </summary>
        /// <param name="userAssurance">UserAssuranceEntity to save</param>
        /// <returns>true if success, false if failure</returns>
        private bool SaveUserAssurance(UserAssuranceEntity userAssurance)
        {
            bool success = false;

            using (TransactionScope scope = new TransactionScope())
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter(false))
                {
                    try
                    {
                        success = adapter.SaveEntity(userAssurance);

                        if (success)
                            scope.Complete();
                    }
                    catch (ORMException)
                    {
                        success = false;
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// Saves user question and answer data
        /// </summary>
        /// <param name="userQuestionAnswers">UserQuestionAnswerEntity collection to save</param>
        /// <returns>true if success, false if failure</returns>
        private bool SaveUserQuestionAnswers(EntityCollection<UserQuestionAnswerEntity> userQuestionAnswers)
        {
            bool success = false;

            using (TransactionScope scope = new TransactionScope())
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter(false))
                {
                    try
                    {
                        int numSaved = adapter.SaveEntityCollection(userQuestionAnswers);

                        if (numSaved == userQuestionAnswers.Count)
                        {
                            success = true;
                            scope.Complete();
                        }
                    }
                    catch (ORMException)
                    {
                        success = false;
                    }
                }
            }

            return success;
        }

        #endregion //Create

        #region Retrieve

        /// <summary>
        /// Retrieve the user's assurance information by user ID
        /// </summary>
        /// <param name="UserId">UserId of the user to retrieve assurance data</param>
        /// <returns>null if no data was found</returns>
        public UserAssuranceEntity RetrieveUserAssurance(Guid UserId)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter())
                {
                    try
                    {
                        EntityCollection<UserAssuranceEntity> userAssurances = new EntityCollection<UserAssuranceEntity>(new UserAssuranceEntityFactory());
                        IRelationPredicateBucket bucket = new RelationPredicateBucket();
                        bucket.PredicateExpression.Add(UserAssuranceFields.UserId == UserId);

                        adapter.FetchEntityCollection(userAssurances, bucket);

                        if (userAssurances.Count != 1)
                        {
                            return null;
                        }
                        else
                        {
                            scope.Complete();
                            return userAssurances[0];
                        }
                    }
                    catch (ORMException)
                    {
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves user question and answer data
        /// </summary>
        /// <param name="userId">UserId of the user to retrieve question and answer data</param>
        /// <param name="numQuestions">Number of questions to return</param>
        /// <returns>null if no data was found</returns>
        public EntityCollection<UserQuestionAnswerEntity> RetrieveAuthenticationAnswers(Guid userId, int numQuestions)
        {
            EntityCollection<UserQuestionAnswerEntity> allQuestionAnswers = RetrieveUserQuestionAnswers(userId);

            if (allQuestionAnswers != null)
            {
                //Return null if there aren't enough questions.
                if (allQuestionAnswers.Count >= numQuestions)
                {
                    DateTime fixedUtcNow = DateTime.UtcNow;

                    double[] timesUsed = new double[allQuestionAnswers.Count];
                    double maxTimesUsed = 1.0;

                    double[] daysSinceLastUsed = new double[allQuestionAnswers.Count];
                    double maxDaysSinceLastUsed = 1.0;

                    double[] weighting = new double[allQuestionAnswers.Count];
                    double totalWeight = 0.0;

                    double[] distribution = new double[allQuestionAnswers.Count];
                    bool[] isQuestionSelected = new bool[allQuestionAnswers.Count];

                    int[] selectedQuestionNumbers = new int[numQuestions];

                    // Initialize data structure for finding weighted distribution of questions used.
                    int i;
                    for (i = 0; i < allQuestionAnswers.Count; i++)
                    {
                        // Set question selected to false
                        isQuestionSelected[i] = false;

                        // Initialize times used weighting array and find max value
                        timesUsed[i] = (double)allQuestionAnswers[i].TimesUsed;
                        if (timesUsed[i] > maxTimesUsed)
                            maxTimesUsed = timesUsed[i];

                        // Initialize days since last used weighting array and find max value
                        daysSinceLastUsed[i] = ((TimeSpan)fixedUtcNow.Subtract(allQuestionAnswers[i].LastTimeUsed)).TotalDays;
                        if (daysSinceLastUsed[i] > maxDaysSinceLastUsed)
                            maxDaysSinceLastUsed = daysSinceLastUsed[i];
                    }

                    for (i = 0; i < allQuestionAnswers.Count; i++)
                    {
                        // Calculate weighting factors
                        timesUsed[i] = 1.0 / (1.0 + (timesUsed[i] / maxTimesUsed));
                        daysSinceLastUsed[i] = 1.0 + (daysSinceLastUsed[i] / maxDaysSinceLastUsed);

                        // Assign weight as a combination of least used AND least recently used
                        weighting[i] = timesUsed[i] + daysSinceLastUsed[i];
                        totalWeight += weighting[i];
                    }

                    for (i = 0; i < allQuestionAnswers.Count; i++)
                    {
                        // Normalize the weights and calculate the cumulative distribution
                        weighting[i] = weighting[i] / totalWeight;

                        if (i == 0)
                            distribution[i] = weighting[i];
                        else
                            distribution[i] = distribution[i - 1] + weighting[i];
                    }

                    // Initialize random vector
                    Random randValue = new Random();

                    int j = 0;
                    while (j < numQuestions)
                    {
                        // Get next random value
                        double curRandValue = randValue.NextDouble();

                        // Look up random value in cumulative distribution
                        // and determine if it was already selected
                        for (i = 0; i < allQuestionAnswers.Count; i++)
                        {
                            if (curRandValue <= distribution[i])
                            {
                                if (!isQuestionSelected[i])
                                {
                                    selectedQuestionNumbers[j] = i;
                                    isQuestionSelected[i] = true;

                                    // Increment to next question to select
                                    j++;
                                }

                                // A match was found, and even though it may have
                                // already been selected, force loop to terminate
                                break;
                            }
                        }
                    }

                    // Remove the unselected questions from the collection
                    // and update the last time used and number of times used
                    for (i = (allQuestionAnswers.Count - 1); i >= 0; i--)
                    {
                        if (!isQuestionSelected[i])
                        {
                            allQuestionAnswers.RemoveAt(i);
                        }
                        else
                        {
                            allQuestionAnswers[i].TimesUsed++;
                            allQuestionAnswers[i].LastTimeUsed = fixedUtcNow;
                        }
                    }

                    // Try to update the statistics - if successful, return
                    // the list of questions, otherwise return null.
                    if (!UpdateUserQuestionAnswers(allQuestionAnswers))
                        return null;
                    else
                        return allQuestionAnswers;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieves user question and answer data
        /// </summary>
        /// <param name="userId">UserId of the user to retrieve question and answer data</param>
        /// <returns>null if no data was found</returns>
        private EntityCollection<UserQuestionAnswerEntity> RetrieveUserQuestionAnswers(Guid userId)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter(false))
                {
                    try
                    {
                        //Retrieve question/answer data, sorted by last time used, then number of times used.
                        EntityCollection<UserQuestionAnswerEntity> userQuestionAnswers = new EntityCollection<UserQuestionAnswerEntity>(new UserQuestionAnswerEntityFactory());
                        IRelationPredicateBucket bucket = new RelationPredicateBucket();
                        bucket.PredicateExpression.Add(UserQuestionAnswerFields.UserId == userId);
                        SortExpression sorter = new SortExpression();
                        sorter.Add(UserQuestionAnswerFields.LastTimeUsed | SortOperator.Ascending);
                        sorter.Add(UserQuestionAnswerFields.TimesUsed | SortOperator.Ascending);
                        IPrefetchPath2 path = new PrefetchPath2((int)EntityType.UserQuestionAnswerEntity);
                        path.Add(UserQuestionAnswerEntity.PrefetchPathQuestion);

                        adapter.FetchEntityCollection(userQuestionAnswers, bucket, 0, sorter, path);

                        if (userQuestionAnswers.Count == 0)
                        {
                            return null;
                        }
                        else
                        {
                            scope.Complete();
                            return userQuestionAnswers;
                        }
                    }
                    catch (ORMException)
                    {
                        return null;
                    }
                }
            }
        }

        #endregion //Retrieve

        #region Update

        /// <summary>
        /// Updates the user question and answers collection with number of times used and last used datetime
        /// </summary>
        /// <param name="userQuestionAnswers">Collection of questions and answer to update</param>
        /// <returns>true if succes / false if failure</returns>
        public bool UpdateUserQuestionAnswers(EntityCollection<UserQuestionAnswerEntity> userQuestionAnswers)
        {
            bool success = false;

            using (TransactionScope scope = new TransactionScope())
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter(false))
                {
                    try
                    {
                        int numSaved = adapter.SaveEntityCollection(userQuestionAnswers, true, false);

                        if (numSaved == userQuestionAnswers.Count)
                        {
                            success = true;
                            scope.Complete();
                        }
                    }
                    catch (ORMException)
                    {
                        success = false;
                    }
                }
            }

            return success;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userEntity"></param>
        /// <returns></returns>
        public bool UpdateUser(UserEntity userEntity)
        {
            bool success = false;

            using (TransactionScope scope = new TransactionScope())
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter(false))
                {
                    try
                    {
                        bool isSaved = adapter.SaveEntity(userEntity, true);

                        if (isSaved)
                        {
                            success = true;
                            scope.Complete();
                        }
                    }
                    catch (ORMException ex)
                    {
                        Log.ErrorException("An error occurred when updating user.", ex);
                        success = false;
                    }
                }
            }

            return success;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="userId"> </param>
        /// <param name="address1"> </param>
        /// <param name="address2"> </param>
        /// <param name="city"> </param>
        /// <param name="locale"> </param>
        /// <param name="country"> </param>
        /// <returns></returns>
        public bool UpdateUserAddress(Guid userId, string address1, string address2, string city, string locale, string zipCode, string country = null)
        {
            var address = AdapterFactory.UserAdapter.RetrieveUserAddress(userId);
            if (address == null)
            {
                return false;
            }
            if (!string.IsNullOrEmpty(address1))
            {
                address.Address1 = address1;
            }
            if (!string.IsNullOrEmpty(address2))
            {
                address.Address2 = address2;
            }
            if (!string.IsNullOrEmpty(city))
            {
                address.City = city;
            }
            if (!string.IsNullOrEmpty(locale))
            {
                address.Locale = locale;
            }
            if (!string.IsNullOrEmpty(zipCode))
            {
                address.PostalCode = zipCode;
            }
            if (!string.IsNullOrEmpty(country))
            {
                address.Country = country;
            }

            bool success = false;
            using (TransactionScope scope = new TransactionScope())
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter(false))
                {
                    try
                    {
                        bool isSaved = adapter.SaveEntity(address, true);

                        if (isSaved)
                        {
                            success = true;
                            scope.Complete();
                        }
                    }
                    catch (ORMException ex)
                    {
                        success = false;
                    }
                }
            }

            return success;
        }
        #endregion //Update

        #endregion //MFA

        #region Transaction Account Info

        /// <summary>
        /// Retrieves a Transaction Account Info Entity
        /// </summary>
        /// <param name="transInfoID"></param>
        /// <returns></returns>
        public TransactionAccountInfoEntity RetrieveTransactionAccountInfo(Guid transInfoID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                try
                {
                    TransactionAccountInfoEntity transInfo = new TransactionAccountInfoEntity(transInfoID);
                    adapter.FetchEntity(transInfo);
                    return transInfo;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the related transaction account info.
        /// </summary>
        /// <param name="destinationID1">The destination I d1.</param>
        /// <returns></returns>
        public EntityCollection<TransactionAccountInfoEntity> RetrieveRelatedTransactionAccountInfo(Guid destinationID1)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(false))
            {
                EntityCollection<TransactionAccountInfoEntity> allRelated = new EntityCollection<TransactionAccountInfoEntity>(new TransactionAccountInfoEntityFactory());
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                bucket.PredicateExpression.Add(TransactionAccountInfoFields.DestinationAccountId1 == destinationID1);

                adapter.FetchEntityCollection(allRelated, bucket);
                return allRelated;
            }
        }

        #endregion //Transaction Account Info

        #region Payment Threshold

        /// <summary>
        /// Resets the payment threshold.
        /// </summary>
        /// <param name="teen">The teen.</param>
        /// <returns></returns>
        public bool ResetPaymentThreshold(RegisteredTeenEntity teen)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    TransactionAccountInfoEntity transAccountInfo = null;
                    if (teen.TransactionAccountInfo != null)
                    {
                        transAccountInfo = teen.TransactionAccountInfo;
                    }
                    else if (teen.TransactionAccountInfoId.HasValue)
                    {
                        transAccountInfo = RetrieveTransactionAccountInfo(teen.TransactionAccountInfoId.Value);
                    }

                    //Clear Threshold
                    teen.PaymentThreshold = null;
                    teen.TransactionAccountInfoId = null;
                    if (!adapter.SaveEntity(teen, true))
                    {
                        return false;
                    }

                    //Delete TransAccountInfo
                    if (transAccountInfo != null)
                    {
                        if (!adapter.DeleteEntity(transAccountInfo))
                        {
                            return false;
                        }
                    }

                    return true;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        #endregion //Payment Threshold

        #region Misc
        public int RetrieveUserCount(RoleType? roleType, Product? product, DateTime? creationDateRangeBegin, DateTime? creationDateRangeEnd, DateTime? enrollmentDateRangeBegin, DateTime? enrollmentDateRangeEnd)
        {
            int count = 0;
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    PredicateExpression predicate = new PredicateExpression();
                    IRelationCollection relationCollection = new RelationCollection();

                    if (roleType.HasValue)
                    {
                        predicate.Add(UserFields.RoleType == roleType.Value);

                        if (roleType.HasValue && roleType.Value == RoleType.RegisteredTeen)
                        {
                            if (product.HasValue)
                            {
                                relationCollection.Add(UserEntity.Relations.ProductUserEntityUsingUserId);

                                predicate.Add(ProductUserFields.Status == ProductUserStatus.Active);
                                predicate.Add(ProductUserFields.ProductNumber == product.Value);

                                if (enrollmentDateRangeBegin.HasValue)
                                {
                                    predicate.Add(ProductUserFields.EnrolledDate > enrollmentDateRangeBegin);
                                }
                                if (enrollmentDateRangeEnd.HasValue)
                                {
                                    predicate.Add(ProductUserFields.EnrolledDate < enrollmentDateRangeEnd);
                                }
                            }
                        }
                    }
                    if (creationDateRangeBegin.HasValue)
                    {
                        predicate.Add(UserFields.CreationDate > creationDateRangeBegin);
                    }
                    if (creationDateRangeEnd.HasValue)
                    {
                        predicate.Add(UserFields.CreationDate < creationDateRangeEnd);
                    }
                    predicate.Add(UserFields.MarkedForDeletion != true);


                    count = (int)adapter.GetScalar(
                        UserFields.UserId,
                        null,
                        AggregateFunction.Count,
                        predicate,
                        null,
                        relationCollection
                    );
                }
                catch (ORMException e)
                {
                    throw new DataAccessException(e.Message);
                }
            }
            return count;
        }

        /// <summary>
        /// Retrieves the user cancellation for user.
        /// </summary>
        /// <param name="UserID">The user ID.</param>
        /// <returns></returns>
        public UserCancellationEntity RetrieveUserCancellationForUser(Guid UserID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    UserCancellationEntity userCancellation = new UserCancellationEntity(UserID);

                    if (!adapter.FetchEntity(userCancellation))
                    {
                        return null;
                    }

                    return userCancellation;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Creates a cancellation entry for a user cancelling their service
        /// </summary>
        /// <param name="TeenID">Teen being cancelled</param>
        /// <param name="ActingUserID">User making the cancellation request</param>
        /// <returns>Boolean based on the success of creating the entry</returns>
        public bool CreateUserCancellationForUser(Guid TeenID, Guid ActingUserID)
        {
            UserCancellationEntity userCancellation;

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    userCancellation = new UserCancellationEntity();

                    userCancellation.CancellationDate = DateTime.UtcNow;
                    userCancellation.TeenId = TeenID;
                    userCancellation.CancelledById = ActingUserID;

                    if (!adapter.SaveEntity(userCancellation))
                        return false;

                    return true;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }

        }

        #endregion
    }
}
