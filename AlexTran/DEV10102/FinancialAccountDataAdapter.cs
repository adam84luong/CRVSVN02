#region Copyright PAYjr Inc. 2005-2007
//
// All rights are reserved. Reproduction in whole or in part, in any 
// form or by any means, electronic, mechanical or otherwise, is    
// prohibited  without the prior written consent of the copyright owner.
//
#endregion

using System;
using System.Collections.Generic;
using System.Transactions;
using System.Data.SqlClient;
using System.Configuration;

using System.Data;
using Payjr.Entity.HelperClasses;
using Payjr.Entity.EntityClasses;
using Payjr.Entity.DatabaseSpecific;
using SD.LLBLGen.Pro.ORMSupportClasses;
using Payjr.Entity.FactoryClasses;
using Payjr.Entity;
using Payjr.Types;
using System.Runtime.InteropServices;
using Common.Types;


namespace Payjr.DataAdapters
{
    public class FinancialAccountDataAdapter
    {
        #region CustomAccounts

        /// <summary>
        /// Retrieves a custom account by the account ID.  Does not
        /// retrieve the custom fields associated.
        /// </summary>
        public CustomAccountFieldGroupEntity RetrieveCustomAccountFieldGroup(Guid customAccountFieldGroupID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                CustomAccountFieldGroupEntity ca = new CustomAccountFieldGroupEntity(customAccountFieldGroupID);
                adapter.FetchEntity(ca);
                return ca;
            }
        }

        /// <summary>
        /// Retrieves the custom account which is being used to process all the transactions
        /// of the user, using the user id.
        /// </summary>
        public CustomAccountFieldGroupEntity RetrieveInUseCustomFieldGroupByUserId(Guid UserID)
        {
            EntityCollection<CustomAccountFieldGroupEntity> fieldGroupCollection = new EntityCollection<CustomAccountFieldGroupEntity>(new CustomAccountFieldGroupEntityFactory());
            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                // First we will find the custom group which is active for this user.
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                bucket.Relations.Add(CustomAccountFieldGroupEntity.Relations.UserEntityUsingUserId);
                bucket.PredicateExpression.Add(CustomAccountFieldGroupFields.UserId == UserID);
                bucket.PredicateExpression.Add(CustomAccountFieldGroupFields.IsActive == true);
                adapter.FetchEntityCollection(fieldGroupCollection, bucket);

                if (fieldGroupCollection.Count == 0)
                {
                    // Because there were no groups active the user could have just created their account and we have not yet verified their account
                    // so we will get an unverified account that will be assocciated with the user. This will allow them to register new children without
                    // having to wait for their account to be verified.
                    EntityCollection<CustomAccountFieldGroupEntity> fieldGroupCollection2 = new EntityCollection<CustomAccountFieldGroupEntity>(new CustomAccountFieldGroupEntityFactory());
                    // Now check to see if they have an unverified account.
                    IRelationPredicateBucket newBucket = new RelationPredicateBucket();
                    //bucket.Relations.Add(CustomAccountFieldGroupEntity.Relations.UserEntityUsingUserId);
                    newBucket.PredicateExpression.Add(CustomAccountFieldGroupFields.UserId == UserID);
                    newBucket.PredicateExpression.Add(CustomAccountFieldGroupFields.Status == AccountStatus.Unverified);
                    adapter.FetchEntityCollection(fieldGroupCollection2, newBucket);

                    if (fieldGroupCollection2.Count > 0)
                    {
                        // return the group that is still unverified.
                        return fieldGroupCollection2[0];
                    }
                    else
                    {
                        // they have no good custom groups.
                        return null;
                    }
                }
                else if (fieldGroupCollection.Count == 1)
                {
                    // return the active group entity.
                    return fieldGroupCollection[0];
                }
                else
                {
                    // There is something wrong here if they have more than one active group.
                    //Log.WriteError("This user: " + UserID + " has more than one active custom account group", ModuleType.Savings);
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieve all of the savings accounts for the user
        /// </summary>
        /// <param name="UserID"></param>
        /// <returns></returns>
        public EntityCollection<CustomAccountFieldGroupEntity> RetrieveAllCustomGroupsByUserId(Guid UserID)
        {
            EntityCollection<CustomAccountFieldGroupEntity> fieldGroupCollection = new EntityCollection<CustomAccountFieldGroupEntity>(new CustomAccountFieldGroupEntityFactory());
            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                bucket.Relations.Add(CustomAccountFieldGroupEntity.Relations.UserEntityUsingUserId);
                bucket.PredicateExpression.Add(CustomAccountFieldGroupFields.UserId == UserID);

                PrefetchPath2 prefetchPath = new PrefetchPath2(EntityType.CustomAccountFieldGroupEntity);
                prefetchPath.Add(CustomAccountFieldGroupEntity.PrefetchPathCustomAccountFields);


                adapter.FetchEntityCollection(fieldGroupCollection, bucket, prefetchPath);

                return fieldGroupCollection;
            }
        }

        /// <summary>
        /// Retrieve the unverified custom account using the user id.
        /// </summary>
        public CustomAccountFieldGroupEntity RetrieveUnverifiedCustomFieldGroupByUserId(Guid UserID)
        {
            EntityCollection<CustomAccountFieldGroupEntity> fieldGroupCollection = new EntityCollection<CustomAccountFieldGroupEntity>(new CustomAccountFieldGroupEntityFactory());
            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                // First we will find the custom group which is active for this user.
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                bucket.Relations.Add(CustomAccountFieldGroupEntity.Relations.UserEntityUsingUserId);
                bucket.PredicateExpression.Add(CustomAccountFieldGroupFields.UserId == UserID);
                bucket.PredicateExpression.Add(CustomAccountFieldGroupFields.Status == AccountStatus.Unverified);
                adapter.FetchEntityCollection(fieldGroupCollection, bucket);

                if (fieldGroupCollection.Count > 1)
                {
                    // return the active group entity.
                    return fieldGroupCollection[0];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieve the Active custom account for the user using user id.
        /// </summary>
        public CustomAccountFieldGroupEntity RetrieveActiveCustomFieldGroupByUserId(Guid UserID)
        {
            EntityCollection<CustomAccountFieldGroupEntity> fieldGroupCollection = new EntityCollection<CustomAccountFieldGroupEntity>(new CustomAccountFieldGroupEntityFactory());
            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                // First we will find the custom group which is active for this user.
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                bucket.Relations.Add(CustomAccountFieldGroupEntity.Relations.UserEntityUsingUserId);
                bucket.PredicateExpression.Add(CustomAccountFieldGroupFields.UserId == UserID);
                bucket.PredicateExpression.Add(CustomAccountFieldGroupFields.IsActive == true);
                adapter.FetchEntityCollection(fieldGroupCollection, bucket);

                if (fieldGroupCollection.Count > 0)
                {
                    // return the active group entity.
                    return fieldGroupCollection[0];
                }
                else
                {
                    return null;
                }
            }
        }
        /// <summary>
        /// Mark all of the prepaid cards for the user as Active=false
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="journalDescription"></param>
        public void MarkAllPrepaidCardsForUserInActive(Guid userID, string journalDescription)
        {
            EntityCollection<PrepaidCardAccountEntity> prepaidCards = new EntityCollection<PrepaidCardAccountEntity>(new PrepaidCardAccountEntityFactory());
            IRelationPredicateBucket bucket = new RelationPredicateBucket();
            bucket.Relations.Add(PrepaidCardAccountEntity.Relations.PrepaidCardAccountUserEntityUsingPrepaidCardAccountId);
            bucket.PredicateExpression.Add(PrepaidCardAccountUserFields.UserId == userID);
            IPrefetchPath2 path = new PrefetchPath2((int)EntityType.PrepaidCardAccountEntity);

            using (TransactionScope scope = new TransactionScope())
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter(true))
                {
                    adapter.FetchEntityCollection(prepaidCards, bucket, path);

                    //Now that we have them let's write a journal and update the status
                    foreach (PrepaidCardAccountEntity prepaidCard in prepaidCards)
                    {
                        prepaidCard.Active = false;

                        if (!CreatePrepaidCardAccountJournal(adapter, userID, null, prepaidCard.PrepaidCardAccountId, "Moving Card to Not Active, " + journalDescription))
                        {
                            //TODO DON'T throw a base exception here.. create a data access layer exception
                            throw new Exception("Unable to write journal Entry for the card:" + prepaidCard.PrepaidCardAccountId);
                        }
                    }

                    //Send all of the cards back
                    adapter.SaveEntityCollection(prepaidCards);
                }

                scope.Complete();
            }
        }

        /// <summary>
        /// Create a prepaid card journal entry
        /// </summary>
        /// <param name="userID">User ID that owns the prepaid card</param>
        /// <param name="actingUserID">User ID that changed the prepaid card</param>
        /// <param name="prepaidCardID">Prepaid card ID</param>
        /// <param name="description">Description</param>
        /// <param name="error">Error output</param>
        /// <returns></returns>
        public bool CreatePrepaidCardAccountJournal(DataAccessAdapter adapter, Guid userID, Guid? actingUserID, Guid prepaidCardID, String description)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                PrepaidCardJournalEntity journal = new PrepaidCardJournalEntity();
                journal.PrepaidCardAccountId = prepaidCardID;
                journal.UserId = userID;
                journal.ActingUserId = actingUserID;
                journal.Description = description;
                journal.CreationDateTime = DateTime.UtcNow;

                //Now let's save it
                if (!adapter.SaveEntity(journal))
                {
                    return false;
                }

                scope.Complete();
                return true;
            }
        }

        /// <summary>
        /// Create a prepaid card journal entry
        /// </summary>
        /// <param name="adapter">The adapter.</param>
        /// <param name="prepaidCardAccountID">The prepaid card account ID.</param>
        /// <param name="status">The status.</param>
        /// <param name="active">if set to <c>true</c> [active].</param>
        /// <returns></returns>
        public bool ChangePrepaidCardStatus(DataAccessAdapter adapter, Guid prepaidCardAccountID, PrepaidCardStatus status, bool active)
        {

            IRelationPredicateBucket bucket = new RelationPredicateBucket();
            bucket.PredicateExpression.Add(PrepaidCardAccountFields.PrepaidCardAccountId == prepaidCardAccountID);
            PrepaidCardAccountEntity prepaidCard = new PrepaidCardAccountEntity();
            prepaidCard.Active = active;
            prepaidCard.Status = status;


            using (TransactionScope scope = new TransactionScope())
            {

                if (adapter.UpdateEntitiesDirectly(prepaidCard, bucket) != 1)
                {
                    return false;
                }
                scope.Complete();
                return true;
            }
        }



        /// <summary>
        /// Retrieve the list of custom fields using the user id.
        /// </summary>
        public List<CustomAccountFieldEntity> RetrieveCustomFieldsbyUserId(Guid UserID)
        {
            try
            {
                List<CustomAccountFieldEntity> fields = new List<CustomAccountFieldEntity>();
                EntityCollection<CustomAccountFieldEntity> fieldCollection = new EntityCollection<CustomAccountFieldEntity>(new CustomAccountFieldEntityFactory());
                // Get the correct group of values.
                CustomAccountFieldGroupEntity fieldGroup = RetrieveInUseCustomFieldGroupByUserId(UserID);
                using (DataAccessAdapter adapter = new DataAccessAdapter())
                {
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(CustomAccountFieldFields.CustomAccountFieldGroupId == fieldGroup.CustomAccountFieldGroupId);
                    adapter.FetchEntityCollection(fieldCollection, bucket);

                    foreach (CustomAccountFieldEntity field in fieldCollection)
                    {
                        // add each entity to the list.
                        fields.Add(field);
                    }
                }
                return fields;
            }
            catch (ORMException)
            {
                //Log.WriteException(exception);
                return null;
            }
        }

        /// <summary>
        /// Retrieve the list of custom fields using the account field group id.
        /// </summary>
        /// <param name="groupID"></param>
        /// <returns></returns>
        public EntityCollection<CustomAccountFieldEntity> RetrieveCustomFieldsbyGroupID(Guid groupID)
        {
            EntityCollection<CustomAccountFieldEntity> fieldCollection = new EntityCollection<CustomAccountFieldEntity>(new CustomAccountFieldEntityFactory());
            // Get the correct group of values.
            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                bucket.PredicateExpression.Add(CustomAccountFieldFields.CustomAccountFieldGroupId == groupID);
                adapter.FetchEntityCollection(fieldCollection, bucket);
            }
            return fieldCollection;

        }

        public List<CustomAccountFieldGroupEntity> RetrieveCurrentCustomAccountFieldGroups(Guid siteID)
        {
            EntityCollection<CustomAccountFieldGroupEntity> childAccounts =
                new EntityCollection<CustomAccountFieldGroupEntity>(new CustomAccountFieldGroupEntityFactory());
            IRelationPredicateBucket bucket = new RelationPredicateBucket();
            bucket.Relations.Add(CustomAccountFieldGroupEntity.Relations.UserEntityUsingUserId);
            bucket.PredicateExpression.Add(UserFields.RoleType == RoleType.RegisteredTeen);
            bucket.PredicateExpression.Add(UserFields.BrandingId == siteID);
            bucket.PredicateExpression.Add(CustomAccountFieldGroupFields.IsActive == true);
            // child accounts can be in status FailedVerification, Good, Closed, Locked.
            // this method retrieves accounts that need to request current account information -- i.e. child accounts in good standing.
            bucket.PredicateExpression.Add(
                (CustomAccountFieldGroupFields.Status == AccountStatus.AllowMoneyMovement));

            IPrefetchPath2 path = new PrefetchPath2((int)EntityType.CustomAccountFieldGroupEntity);
            path.Add(CustomAccountFieldGroupEntity.PrefetchPathCustomAccountFields);

            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                adapter.FetchEntityCollection(childAccounts, bucket, path);
            }

            return new List<CustomAccountFieldGroupEntity>(childAccounts);
        }

        /// <summary>
        /// Retrieve the current balance for the account
        /// </summary>
        /// <param name="groupID"></param>
        /// <returns></returns>
        public AccountBalanceEntity RetrieveCurrentBalance(Guid groupID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<AccountBalanceEntity> balances = new EntityCollection<AccountBalanceEntity>(new AccountBalanceEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(AccountBalanceFields.CustomAccountFieldGroupId == groupID);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.AccountBalanceEntity);
                    SortExpression sorter = new SortExpression(AccountBalanceFields.BalanceDate | SortOperator.Descending);

                    adapter.FetchEntityCollection(balances, bucket, 1, sorter);

                    if (balances.Count > 0)
                    {
                        return balances[0];
                    }
                    else
                    {
                        return null;
                    }
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException("Error when Retriving the Current Balance", exceptionMessage);
                    throw exception;
                }
            }

        }

        /// <summary>
        /// Retrieves the user entity who owns the custom account field group specified.
        /// </summary>
        public UserEntity RetrieveUserForCustomAccountFieldGroup(Guid customAccountFieldGroupID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                bucket.Relations.Add(UserEntity.Relations.CustomAccountFieldGroupEntityUsingUserId);
                bucket.PredicateExpression.Add(CustomAccountFieldGroupFields.CustomAccountFieldGroupId ==
                                               customAccountFieldGroupID);
                EntityCollection<UserEntity> users = new EntityCollection<UserEntity>(new UserEntityFactory());
                adapter.FetchEntityCollection(users, bucket);

                if (users.Count > 0)
                    return users[0];
                else
                    return null;
            }
        }

        /// <summary>
        /// Retrieves the savings transfers.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <param name="role">The role.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="statuses">The statuses.</param>
        /// <returns></returns>
        public EntityCollection<SavingsTransferJobEntity> RetrieveSavingsTransfers(Guid accountID, RoleType role, DateTime startDate, DateTime endDate, params JobStatus[] statuses)
        {
            EntityCollection<SavingsTransferJobEntity> transactions
                    = new EntityCollection<SavingsTransferJobEntity>(new SavingsTransferJobEntityFactory());

            using (DataAccessAdapter adapter = new DataAccessAdapter(false))
            {
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                if (role == RoleType.Parent)
                {
                    bucket.PredicateExpression.Add(SavingsTransferJobFields.CustomAccountFieldGroupId == accountID);
                }
                else // role == RoleType.RegisteredTeen
                {
                    bucket.PredicateExpression.Add(SavingsTransferJobFields.DestinationAccounFieldGroupId == accountID);
                }
                // ensure job type
                bucket.PredicateExpression.Add(SavingsTransferJobFields.JobType == JobType.SavingsTransferJob);
                // ensure start & end dates
                if (startDate > DateTime.MinValue)
                {
                    bucket.PredicateExpression.Add(SavingsTransferJobFields.ScheduledStartTime >= startDate);
                }
                if (endDate <= DateTime.MaxValue)
                {
                    bucket.PredicateExpression.Add(SavingsTransferJobFields.ScheduledStartTime <= endDate);
                }
                // Check to see if we have any statuses passed in with the function.
                if (statuses != null && statuses.Length > 0)
                {
                    // Add to the predicate expression with an OR so all the statuses
                    // are within an OR.
                    IPredicateExpression andStatus = new PredicateExpression();
                    foreach (JobStatus status in statuses)
                    {
                        andStatus.AddWithOr(SavingsTransferJobFields.Status == status);
                    }
                    // add the OR expression to our existing predicate expression with
                    // an AND.
                    bucket.PredicateExpression.Add(andStatus);
                }

                IPrefetchPath2 path = new PrefetchPath2((int)EntityType.SavingsTransferJobEntity);
                path.Add(SavingsTransferJobEntity.PrefetchPathBillingAmounts);

                ISortExpression sorter = new SortExpression(SortClauseFactory.Create(TransactionJobFieldIndex.CreateTime, SortOperator.Descending));

                adapter.FetchEntityCollection(transactions, bucket, 100000, sorter, path);

                return transactions;
            }
        }

        /// <summary>
        /// Retrieve all of the pending savings jobs
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        /// <param name="role"></param>
        [Obsolete]
        public EntityCollection<SavingsTransferJobEntity> RetrieveSavingsTransactionJobsNotSentWithBilling(Guid userID, RoleType role)
        {
            EntityCollection<SavingsTransferJobEntity> transactions
                    = new EntityCollection<SavingsTransferJobEntity>(new SavingsTransferJobEntityFactory());

            using (DataAccessAdapter adapter = new DataAccessAdapter(false))
            {
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                if (role == RoleType.Parent)
                {
                    bucket.PredicateExpression.Add(SavingsTransferJobFields.UserId == userID);
                }
                else // role == RoleType.RegisteredTeen
                {
                    bucket.Relations.Add(
                        SavingsTransferJobEntity.Relations.
                            CustomAccountFieldGroupEntityUsingDestinationAccounFieldGroupId);
                    bucket.PredicateExpression.Add(CustomAccountFieldGroupFields.UserId == userID);
                }
                bucket.PredicateExpression.Add(SavingsTransferJobFields.JobType == JobType.SavingsTransferJob);

                IPredicateExpression andStatus = new PredicateExpression();
                andStatus.Add(SavingsTransferJobFields.Status == JobStatus.Waiting_for_generation);
                andStatus.AddWithOr(SavingsTransferJobFields.Status == JobStatus.Paused);
                bucket.PredicateExpression.Add(andStatus);

                IPrefetchPath2 path = new PrefetchPath2((int)EntityType.JobEntity);
                path.Add(SavingsTransferJobEntity.PrefetchPathBillingAmounts);

                ISortExpression sorter = new SortExpression(SortClauseFactory.Create(TransactionJobFieldIndex.CreateTime, SortOperator.Descending));

                adapter.FetchEntityCollection(transactions, bucket, 100000, sorter, path);

                return transactions;
            }
        }

        /// <summary>
        /// Creates the new custom account field group.
        /// </summary>
        /// <param name="userID">The user id.</param>
        /// <param name="customFields">The custom fields.</param>
        /// <param name="accType">Type of the account.</param>
        /// <param name="customAccountFieldGroupID">The custom account group id.</param>
        /// <returns></returns>
        public bool CreateNewCustomAccountFieldGroup(Guid userID, List<CustomAccountFieldEntity> customFields, Payjr.Entity.AccountType accType, out Guid customAccountFieldGroupID)
        {
            if (customFields == null)
                throw new ArgumentNullException("The custom fields are null");

            using (TransactionScope scope = new TransactionScope())
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter())
                {
                    // Create a new entry for the custom group.
                    CustomAccountFieldGroupEntity group = new CustomAccountFieldGroupEntity();
                    group.Status = AccountStatus.Unverified;
                    group.UserId = userID;
                    // Find out if we have an existing account for the user.
                    // If we do we will set the isActive bit to false.
                    // Otherwise we will set the isActive bit to true to indicate that this is their
                    // first account.
                    CustomAccountFieldGroupEntity oldGroup = RetrieveActiveCustomFieldGroupByUserId(userID);
                    if (oldGroup != null)
                    {
                        group.IsActive = false;
                    }
                    else
                    {
                        group.IsActive = true;
                    }
                    group.AccountType = accType;

                    if (!adapter.SaveEntity(group, true))
                    {
                        customAccountFieldGroupID = Guid.Empty;
                        return false;
                    }
                    customAccountFieldGroupID = group.CustomAccountFieldGroupId;

                    // Create a new entry in the database for each of the custom fields.
                    foreach (CustomAccountFieldEntity field in customFields)
                    {
                        field.CustomAccountFieldGroupId = customAccountFieldGroupID;
                        if (!adapter.SaveEntity(field))
                        {
                            return false;
                        }
                    }
                    scope.Complete();
                    return true;
                }
            }
        }

        /// <summary>
        /// Creates the account balance history.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <param name="availableBalance">The available balance.</param>
        /// <param name="currentBalance">The current balance.</param>
        /// <param name="balanceDate">The balance date.</param>
        /// <returns></returns>
        public bool CreateAccountBalanceHistory(Guid accountID, decimal? availableBalance, decimal? currentBalance, DateTime balanceDate)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter(true))
                {
                    try
                    {
                        AccountBalanceEntity history = new AccountBalanceEntity();
                        history.CustomAccountFieldGroupId = accountID;
                        history.AvailableBalance = availableBalance;
                        history.CurrentBalance = currentBalance;
                        history.BalanceDate = balanceDate;

                        if (!adapter.SaveEntity(history))
                        {
                            return false;
                        }
                    }
                    catch (ORMException exceptionMessage)
                    {
                        DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                        throw exception;
                    }
                }
                scope.Complete();
                return true;
            }
        }

        /// <summary>
        /// Creates the savings account journal entry.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <param name="userID">The user ID.</param>
        /// <param name="actingUserID">The acting user ID.</param>
        /// <param name="description">The description.</param>
        /// <returns></returns>
        public bool CreateSavingsAccountJournalEntry(Guid accountID, Guid userID, Guid? actingUserID, string description)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter(true))
                {
                    try
                    {
                        SavingsAccountJournalEntity journal = new SavingsAccountJournalEntity();
                        journal.ActingUserId = actingUserID;
                        journal.CreationDateTime = DateTime.Now;
                        journal.CustomAccountFieldGroupId = accountID;
                        journal.Description = description;
                        journal.UserId = userID;

                        if (!adapter.SaveEntity(journal))
                        {
                            return false;
                        }
                    }
                    catch (ORMException exceptionMessage)
                    {
                        DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                        throw exception;
                    }
                }
                scope.Complete();
                return true;
            }
        }

        /// <summary>
        /// Retrieves the savings account journal entries.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public EntityCollection<JournalEntity> RetrieveSavingsAccountJournalEntries(Guid accountID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<JournalEntity> journalEntries = new EntityCollection<JournalEntity>(new JournalEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(SavingsAccountJournalFields.CustomAccountFieldGroupId == accountID);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.JournalEntity);

                    adapter.FetchEntityCollection(journalEntries, bucket, path);
                    return journalEntries;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        #endregion

        #region BankAccounts

        /// <summary>
        /// Creates a bank account.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns></returns>
        public BankAccountEntity CreateBankAccount(UserEntity user)
        {
            BankAccountEntity bank = new BankAccountEntity();
            BankAccountUserEntity bankUser = new BankAccountUserEntity();
            bankUser.BankAccount = bank;
            bankUser.User = user;

            user.BankAccountUsers.Add(bankUser);

            return bank;
        }

        /// <summary>
        /// Retrieves the bank accounts by accountID
        /// </summary>
        /// <returns></returns>
        public BankAccountEntity RetrieveBankAccountByID(Guid bankAccountID)
        {

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                BankAccountEntity bank = new BankAccountEntity(bankAccountID);
                IPrefetchPath2 path = new PrefetchPath2((int)EntityType.BankAccountEntity);
                path.Add(BankAccountEntity.PrefetchPathBankAccountUsers);
                if (adapter.FetchEntity(bank, path))
                {

                    return bank;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Retrieves the bank accounts
        /// </summary>
        /// <returns></returns>
        public EntityCollection<BankAccountEntity> RetrieveBankAccounts(Guid UserId)
        {
            EntityCollection<BankAccountEntity> banks = new EntityCollection<BankAccountEntity>(new BankAccountEntityFactory());

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                bucket.Relations.Add(BankAccountEntity.Relations.BankAccountUserEntityUsingBankAccountId);
                bucket.PredicateExpression.Add(BankAccountUserFields.UserId == UserId);
                adapter.FetchEntityCollection(banks, bucket);
            }
            return banks;
        }

        /// <summary>
        /// Retrieve all bank accounts with specified account number and routing number
        /// </summary>
        /// <param name="accountNumber">Account number of desired account</param>
        /// <param name="routingNumber">Routing number of desired account</param>
        /// <returns>List of accounts matching the supplied account and routing numbers</returns>
        public EntityCollection<BankAccountEntity> RetrieveBankAccounts(string accountNumber, string routingNumber)
        {
            AesCryptoString.AesCryptoString encryptedAccountNumber = new AesCryptoString.AesCryptoString();
            encryptedAccountNumber = accountNumber;

            AesCryptoString.AesCryptoString encryptedRoutingNumber = new AesCryptoString.AesCryptoString();
            encryptedRoutingNumber = routingNumber;

            EntityCollection<BankAccountEntity> banks = new EntityCollection<BankAccountEntity>(new BankAccountEntityFactory());

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                bucket.PredicateExpression.Add(BankAccountFields.AccountNumber == encryptedAccountNumber);
                bucket.PredicateExpression.AddWithAnd(BankAccountFields.RoutingNumber == encryptedRoutingNumber);
                adapter.FetchEntityCollection(banks, bucket);
            }
            return banks;
        }

        /// <summary>
        /// Retrieves Transactions
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns></returns>
        public EntityCollection<FnboachtransactionJobEntity> RetrieveACHTransactionsForAccount(Guid accountID, DateTime startDate, DateTime endDate)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    //Create the Query
                    EntityCollection<FnboachtransactionJobEntity> jobs = new EntityCollection<FnboachtransactionJobEntity>(new FnboachtransactionJobEntityFactory());

                    IRelationPredicateBucket bucket = CreateTransactionQuery(startDate, endDate, accountID);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.FnboachtransactionJobEntity);
                    path.Add(FnboachtransactionJobEntity.PrefetchPathBillingAmounts);


                    adapter.FetchEntityCollection(jobs, bucket, path);

                    return jobs;

                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }

            }
        }

        /// <summary>
        /// Retrieves the pending ACH transactions for an account.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public EntityCollection<FnboachtransactionJobEntity> RetrievePendingACHTransactionsForAccount(Guid accountID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<FnboachtransactionJobEntity> achJobs = new EntityCollection<FnboachtransactionJobEntity>(new FnboachtransactionJobEntityFactory());
                    IRelationPredicateBucket statusBucket = new RelationPredicateBucket();
                    statusBucket.PredicateExpression.AddWithOr(FnboachtransactionJobFields.Status == JobStatus.Paused);
                    statusBucket.PredicateExpression.AddWithOr(FnboachtransactionJobFields.Status == JobStatus.Waiting_for_generation);
                    statusBucket.PredicateExpression.AddWithOr(FnboachtransactionJobFields.Status == JobStatus.Waiting_for_user);

                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(FnboachtransactionJobFields.UserBankAccountId == accountID);
                    bucket.PredicateExpression.Add(statusBucket.PredicateExpression);

                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.FnboachtransactionJobEntity);
                    path.Add(FnboachtransactionJobEntity.PrefetchPathSecondaryLinkedJobs);
                    adapter.FetchEntityCollection(achJobs, bucket, path);
                    return achJobs;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Creates the transaction query.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        private IRelationPredicateBucket CreateTransactionQuery(DateTime startDate, DateTime endDate, Guid accountID)
        {
            //This creates the following query
            //(ACH.UserID==UserID)&&(((JobStatus==Pending || JobStatus== W_f_G)&&(CreateTime <= endDate && CreateTime >= startDate))||
            //    ((JobStatus==Completed || JobStatus==Running || JobStatus==Waiting)&&(CompleteTime<=endDate && CompleteTime>=startDate)||
            //    ((JobStatus==Cancelled)&&(CancelledDate<=endDate&&CancelledDtae>=startDate)))&&(JobID!=doNotRetrieveIDs)

            IRelationPredicateBucket bucket = new RelationPredicateBucket();
            bucket.PredicateExpression.Add(FnboachtransactionJobFields.UserBankAccountId == accountID);

            //Pending Clauses
            IPredicateExpression pendingStatusOrClause = new PredicateExpression();
            pendingStatusOrClause.Add(FnboachtransactionJobFields.Status == JobStatus.Paused);
            pendingStatusOrClause.AddWithOr(FnboachtransactionJobFields.Status == JobStatus.Waiting_for_generation);

            IPredicateExpression pendingTimeOrClause = new PredicateExpression();
            pendingTimeOrClause.Add(FnboachtransactionJobFields.CreateTime <= endDate);
            pendingTimeOrClause.Add(FnboachtransactionJobFields.CreateTime > startDate);

            IPredicateExpression pendingOrClause = new PredicateExpression();
            pendingOrClause.Add(pendingStatusOrClause);
            pendingOrClause.Add(pendingTimeOrClause);

            //Completed Clauses
            IPredicateExpression completeStatusOrClause = new PredicateExpression();
            completeStatusOrClause.Add(FnboachtransactionJobFields.Status == JobStatus.Completed);
            completeStatusOrClause.AddWithOr(FnboachtransactionJobFields.Status == JobStatus.Running);
            completeStatusOrClause.AddWithOr(FnboachtransactionJobFields.Status == JobStatus.Waiting);

            IPredicateExpression completeTimeOrClause = new PredicateExpression();
            completeTimeOrClause.Add(FnboachtransactionJobFields.CompletedTime <= endDate);
            completeTimeOrClause.Add(FnboachtransactionJobFields.CompletedTime >= startDate);

            IPredicateExpression completeOrClause = new PredicateExpression();
            completeOrClause.Add(completeStatusOrClause);
            completeOrClause.Add(completeTimeOrClause);

            //Canceled Clauses
            IPredicateExpression cancelledStatusOrClause = new PredicateExpression();
            cancelledStatusOrClause.Add(FnboachtransactionJobFields.Status == JobStatus.Cancelled);

            IPredicateExpression cancelledTimeOrClause = new PredicateExpression();
            //This needs to be the cancelled date
            cancelledTimeOrClause.Add(FnboachtransactionJobFields.CreateTime <= endDate);
            cancelledTimeOrClause.Add(FnboachtransactionJobFields.CreateTime >= startDate);

            IPredicateExpression cancelledOrClause = new PredicateExpression();
            cancelledOrClause.Add(cancelledStatusOrClause);
            cancelledOrClause.Add(cancelledTimeOrClause);

            IPredicateExpression orClause = new PredicateExpression();
            orClause.Add(cancelledOrClause);
            orClause.AddWithOr(completeOrClause);
            orClause.AddWithOr(pendingOrClause);

            bucket.PredicateExpression.Add(orClause);

            return bucket;
        }

        /// <summary>
        /// Retrieves the pending challenge job for the given account.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public ChallengeDepositJobEntity RetrievePendingChallengeJobForAccount(Guid accountID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<ChallengeDepositJobEntity> challengeJobs = new EntityCollection<ChallengeDepositJobEntity>(new ChallengeDepositJobEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(ChallengeDepositJobFields.ParentAccountId == accountID);
                    bucket.PredicateExpression.Add(ChallengeDepositJobFields.Status == JobStatus.Waiting_for_user);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.ChallengeDepositJobEntity);

                    adapter.FetchEntityCollection(challengeJobs, bucket, path);

                    if (challengeJobs.Count > 0)
                    {
                        return challengeJobs[0];
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
        /// Saves the challenge job.
        /// </summary>
        /// <param name="challengeJob">The challenge job.</param>
        /// <returns></returns>
        public bool SaveChallengeJob(ChallengeDepositJobEntity challengeJob)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    return adapter.SaveEntity(challengeJob, true);
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }

        }

        /// <summary>
        /// Creates a bank account journal entry.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <param name="userID">The user ID.</param>
        /// <param name="actingUserID">The acting user ID.</param>
        /// <param name="description">The description.</param>
        /// <returns></returns>
        public bool CreateBankAccountJournalEntry(Guid accountID, Guid userID, Guid? actingUserID, string description)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter(true))
                {
                    try
                    {
                        BankAccountJournalEntity journalEntry = new BankAccountJournalEntity();
                        journalEntry.BankAccountId = accountID;
                        journalEntry.Description = description;
                        journalEntry.CreationDateTime = DateTime.Now;
                        journalEntry.UserId = userID;
                        journalEntry.ActingUserId = actingUserID;

                        if (!adapter.SaveEntity(journalEntry))
                        {
                            return false;
                        }
                    }
                    catch (ORMException exceptionMessage)
                    {
                        DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                        throw exception;
                    }
                }
                scope.Complete();
                return true;
            }
        }

        /// <summary>
        /// Retrieves bank account journal entries.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public EntityCollection<JournalEntity> RetrieveBankAccountJournalEntries(Guid accountID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<JournalEntity> journalEntries = new EntityCollection<JournalEntity>(new JournalEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(BankAccountJournalFields.BankAccountId == accountID);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.JournalEntity);

                    adapter.FetchEntityCollection(journalEntries, bucket, path);
                    return journalEntries;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        #endregion

        #region Credit Card Accounts

        /// <summary>
        /// Retrieves the credit card account count by user for the last 3 months
        /// </summary>
        /// <param name="userID">The user ID.</param>
        /// <returns>Count of all credit cards in the last 3 months for a user</returns>
        public int RetrieveCountCreditCardAccountsByUserLast3Months(Guid userID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();

                    bucket.PredicateExpression.Add(CreditCardAccountFields.UserId == userID);
                    bucket.PredicateExpression.Add(CreditCardAccountFields.CreatedTime > DateTime.Now.AddMonths(-3));

                    ResultsetFields fields = new ResultsetFields(3);
                    fields[0] = CreditCardAccountFields.CardNumber;
                    fields[1] = CreditCardAccountFields.ExpirationMonth;
                    fields[2] = CreditCardAccountFields.ExpirationYear;

                    // Only return distinct 
                    return adapter.GetDbCount(fields, bucket, null, false);
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the credit card accounts by user.
        /// </summary>
        /// <param name="userID">The user ID.</param>
        /// <returns></returns>
        public EntityCollection<CreditCardAccountEntity> RetrieveCreditCardAccountsByUser(Guid userID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<CreditCardAccountEntity> creditCards = new EntityCollection<CreditCardAccountEntity>(new CreditCardAccountEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(CreditCardAccountFields.UserId == userID);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.CreditCardAccountEntity);

                    adapter.FetchEntityCollection(creditCards, bucket, path);
                    return creditCards;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the credit card account by account ID.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public CreditCardAccountEntity RetrieveCreditCardAccountByAccountID(Guid accountID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    CreditCardAccountEntity creditCard = new CreditCardAccountEntity(accountID);
                    adapter.FetchEntity(creditCard);
                    return creditCard;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the credit card accounts by card number.
        /// </summary>
        /// <param name="cardNumber">The card number.</param>
        /// <returns></returns>
        public EntityCollection<CreditCardAccountEntity> RetrieveCreditCardAccountsByCardNumber(string cardNumber)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    AesCryptoString.AesCryptoString encryptedString = cardNumber;
                    EntityCollection<CreditCardAccountEntity> cards = new EntityCollection<CreditCardAccountEntity>(new CreditCardAccountEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(CreditCardAccountFields.CardNumber == encryptedString);

                    adapter.FetchEntityCollection(cards, bucket);

                    return cards;

                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Creates the credit card journal entry.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <param name="userID">The user ID.</param>
        /// <param name="actingUserID">The acting user ID.</param>
        /// <param name="description">The description.</param>
        /// <returns></returns>
        public bool CreateCreditCardJournalEntry(Guid accountID, Guid userID, Guid? actingUserID, string description)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    CreditCardAccountJournalEntity journalEntry = new CreditCardAccountJournalEntity();
                    journalEntry.ActingUserId = actingUserID;
                    journalEntry.CreationDateTime = DateTime.Now;
                    journalEntry.CreditCardAccountId = accountID;
                    journalEntry.CreditCardAccountJournalId = Guid.NewGuid();
                    journalEntry.Description = description;
                    journalEntry.UserId = userID;

                    return adapter.SaveEntity(journalEntry);
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the credit card journal entries.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public EntityCollection<JournalEntity> RetrieveCreditCardJournalEntries(Guid accountID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<JournalEntity> journalEntries = new EntityCollection<JournalEntity>(new JournalEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(CreditCardAccountJournalFields.CreditCardAccountId == accountID);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.JournalEntity);

                    adapter.FetchEntityCollection(journalEntries, bucket, path);
                    return journalEntries;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the verification job for account.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public CreditCardTransactionJobEntity RetrieveVerificationJobForAccount(Guid accountID, TransactionType transType)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<CreditCardTransactionJobEntity> creditCardTransactionJob = new EntityCollection<CreditCardTransactionJobEntity>(new CreditCardTransactionJobEntityFactory());

                    IRelationPredicateBucket statusBucket = new RelationPredicateBucket();
                    statusBucket.PredicateExpression.Add(CreditCardTransactionJobFields.TransactionType == transType);

                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(CreditCardTransactionJobFields.CreditCardAccountId == accountID);
                    bucket.PredicateExpression.Add(statusBucket.PredicateExpression);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.CreditCardTransactionJobEntity);

                    SortExpression sorter = new SortExpression();
                    sorter.Add(SortClauseFactory.Create(CreditCardTransactionJobFieldIndex.CreateTime, SortOperator.Descending));

                    adapter.FetchEntityCollection(creditCardTransactionJob, bucket, 0, sorter, path);
                    if (creditCardTransactionJob.Count > 0)
                    {
                        return creditCardTransactionJob[0];
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
        /// Retrieves the pending credit card transactions for account.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public EntityCollection<CreditCardTransactionJobEntity> RetrievePendingCreditCardTransactionsForAccount(Guid accountID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<CreditCardTransactionJobEntity> cardJobs = new EntityCollection<CreditCardTransactionJobEntity>(new CreditCardTransactionJobEntityFactory());
                    IRelationPredicateBucket statusBucket = new RelationPredicateBucket();
                    statusBucket.PredicateExpression.AddWithOr(CreditCardTransactionJobFields.Status == JobStatus.Paused);
                    statusBucket.PredicateExpression.AddWithOr(CreditCardTransactionJobFields.Status == JobStatus.Waiting_for_generation);
                    statusBucket.PredicateExpression.AddWithOr(CreditCardTransactionJobFields.Status == JobStatus.Waiting_for_user);
                    statusBucket.PredicateExpression.AddWithOr(CreditCardTransactionJobFields.Status == JobStatus.Waiting);

                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(CreditCardTransactionJobFields.CreditCardAccountId == accountID);
                    bucket.PredicateExpression.Add(statusBucket.PredicateExpression);

                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.CreditCardTransactionJobEntity);
                    path.Add(CreditCardTransactionJobEntity.PrefetchPathSecondaryLinkedJobs);
                    adapter.FetchEntityCollection(cardJobs, bucket, path);
                    return cardJobs;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves Transactions
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns></returns>
        public EntityCollection<CreditCardTransactionJobEntity> RetrieveCreditCardTransactionsForAccount(Guid accountID, DateTime startDate, DateTime endDate)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    //Create the Query
                    EntityCollection<CreditCardTransactionJobEntity> jobs = new EntityCollection<CreditCardTransactionJobEntity>(new CreditCardTransactionJobEntityFactory());

                    IRelationPredicateBucket bucket = CreateCreditCardTransactionQuery(startDate, endDate, accountID);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.CreditCardTransactionJobEntity);
                    path.Add(FnboachtransactionJobEntity.PrefetchPathBillingAmounts);


                    adapter.FetchEntityCollection(jobs, bucket, path);

                    return jobs;

                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }

            }
        }

        /// <summary>
        /// Retrieves Transactions
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns></returns>
        public EntityCollection<CreditCardCreditEntity> RetrieveCreditCardCreditsForAccount(Guid accountID, DateTime startDate, DateTime endDate)
        {
            IRelationPredicateBucket bucket;
            EntityCollection<CreditCardCreditEntity> credits;

            bucket = new RelationPredicateBucket();
            credits = new EntityCollection<CreditCardCreditEntity>(new CreditCardCreditEntityFactory());

            bucket.Relations.Add(CreditCardCreditEntity.Relations.CreditCardTransactionJobEntityUsingCreditCardTransactionId);

            bucket.PredicateExpression.Add(CreditCardTransactionJobFields.CreditCardAccountId == accountID);

            bucket.PredicateExpression.Add(CreditCardCreditFields.ReplyCreditDatetime <= endDate);
            bucket.PredicateExpression.Add(CreditCardCreditFields.ReplyCreditDatetime >= startDate);

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    adapter.FetchEntityCollection(credits, bucket);
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }

            return credits;

        }

        /// <summary>
        /// Retrieves Credit Transaction by using the credit id
        /// </summary>
        /// <param name="creditID">The credit ID.</param>
        /// <returns></returns>
        public CreditCardCreditEntity RetrieveCreditCardCreditByCreditID(Guid creditID)
        {
            IRelationPredicateBucket bucket;
            EntityCollection<CreditCardCreditEntity> credits;

            bucket = new RelationPredicateBucket();
            credits = new EntityCollection<CreditCardCreditEntity>(new CreditCardCreditEntityFactory());

            bucket.PredicateExpression.Add(CreditCardCreditFields.CreditCardCreditId == creditID);

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    adapter.FetchEntityCollection(credits, bucket);
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }

            if (credits.Count > 0)
                return credits[0];
            else
                return null;
        }


        /// <summary>
        /// Creates the transaction query.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        private IRelationPredicateBucket CreateCreditCardTransactionQuery(DateTime startDate, DateTime endDate, Guid accountID)
        {
            IRelationPredicateBucket bucket = new RelationPredicateBucket();
            bucket.PredicateExpression.Add(CreditCardTransactionJobFields.CreditCardAccountId == accountID);

            //Pending Clauses
            IPredicateExpression pendingStatusOrClause = new PredicateExpression();
            pendingStatusOrClause.Add(CreditCardTransactionJobFields.Status == JobStatus.Paused);
            pendingStatusOrClause.AddWithOr(CreditCardTransactionJobFields.Status == JobStatus.Waiting);
            pendingStatusOrClause.AddWithOr(CreditCardTransactionJobFields.Status == JobStatus.Waiting_for_generation);

            IPredicateExpression pendingTimeOrClause = new PredicateExpression();
            pendingTimeOrClause.Add(CreditCardTransactionJobFields.CreateTime <= endDate);
            pendingTimeOrClause.Add(CreditCardTransactionJobFields.CreateTime > startDate);

            IPredicateExpression pendingOrClause = new PredicateExpression();
            pendingOrClause.Add(pendingStatusOrClause);
            pendingOrClause.Add(pendingTimeOrClause);

            //Completed Clauses
            IPredicateExpression completeStatusOrClause = new PredicateExpression();
            completeStatusOrClause.Add(CreditCardTransactionJobFields.Status == JobStatus.Completed);
            completeStatusOrClause.AddWithOr(CreditCardTransactionJobFields.Status == JobStatus.Running);
            completeStatusOrClause.AddWithOr(CreditCardTransactionJobFields.Status == JobStatus.Error);

            IPredicateExpression completeTimeOrClause = new PredicateExpression();
            completeTimeOrClause.Add(CreditCardTransactionJobFields.CompletedTime <= endDate);
            completeTimeOrClause.Add(CreditCardTransactionJobFields.CompletedTime >= startDate);

            IPredicateExpression completeOrClause = new PredicateExpression();
            completeOrClause.Add(completeStatusOrClause);
            completeOrClause.Add(completeTimeOrClause);

            //Canceled Clauses
            IPredicateExpression cancelledStatusOrClause = new PredicateExpression();
            cancelledStatusOrClause.Add(CreditCardTransactionJobFields.Status == JobStatus.Cancelled);

            IPredicateExpression cancelledTimeOrClause = new PredicateExpression();
            //This needs to be the cancelled date
            cancelledTimeOrClause.Add(CreditCardTransactionJobFields.CreateTime <= endDate);
            cancelledTimeOrClause.Add(CreditCardTransactionJobFields.CreateTime >= startDate);

            IPredicateExpression cancelledOrClause = new PredicateExpression();
            cancelledOrClause.Add(cancelledStatusOrClause);
            cancelledOrClause.Add(cancelledTimeOrClause);

            IPredicateExpression orClause = new PredicateExpression();
            orClause.Add(cancelledOrClause);
            orClause.AddWithOr(completeOrClause);
            orClause.AddWithOr(pendingOrClause);

            bucket.PredicateExpression.Add(orClause);

            return bucket;
        }
        #endregion //Credit Card Accounts

        #region Prepaid Account

        /// <summary>
        /// Creates a prepaid card account.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns></returns>
        public PrepaidCardAccountEntity CreatePrepaidCardAccount(UserEntity user)
        {
            PrepaidCardAccountEntity card = new PrepaidCardAccountEntity();
            PrepaidCardAccountUserEntity cardUser = new PrepaidCardAccountUserEntity();
            cardUser.PrepaidCardAccount = card;
            cardUser.User = user;

            user.PrepaidCardAccountUsers.Add(cardUser);

            return card;
        }

        /// <summary>
        /// Retrieves the card create job for the given account.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public JobEntity RetrieveCardCreateJobForAccount(Guid accountID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<CreditCardCreateJobEntity> jobs = new EntityCollection<CreditCardCreateJobEntity>(new CreditCardCreateJobEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(CreditCardCreateJobFields.PrepaidCardAccountId == accountID);
                    adapter.FetchEntityCollection(jobs, bucket);

                    if (jobs.Count > 0)
                    {
                        return jobs[0];
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
        /// Retrieves the card transactions by account.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns></returns>
        public EntityCollection<CardTransactionEntity> RetrieveCardTransactionsByAccount(Guid accountID, DateTime startDate, DateTime endDate)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<CardTransactionEntity> cardTransactions = new EntityCollection<CardTransactionEntity>(new CardTransactionEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(CardTransactionFields.PrepaidAccountId == accountID);
                    bucket.PredicateExpression.Add(CardTransactionFields.TransactionDate <= endDate);
                    bucket.PredicateExpression.Add(CardTransactionFields.TransactionDate >= startDate);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.CardTransactionEntity);

                    adapter.FetchEntityCollection(cardTransactions, bucket, path);
                    return cardTransactions;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        public EntityCollection<CardTransactionEntity> RetrieveCardTransactionsByAccount(Guid accountID, DateTime startDate, DateTime endDate, int pageNumber, int pageSize)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<CardTransactionEntity> cardTransactions = new EntityCollection<CardTransactionEntity>(new CardTransactionEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(CardTransactionFields.PrepaidAccountId == accountID);
                    bucket.PredicateExpression.Add(CardTransactionFields.TransactionDate <= endDate);
                    bucket.PredicateExpression.Add(CardTransactionFields.TransactionDate >= startDate);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.CardTransactionEntity);
                    SortExpression sortexp = new SortExpression();
                    sortexp.Add(CardTransactionFields.TransactionDate | SortOperator.Descending);
                    adapter.FetchEntityCollection(cardTransactions, bucket, 0, sortexp,path, pageNumber, pageSize);
                   return cardTransactions;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }
      

        /// <summary>
        /// Creates a prepaid card account journal entry.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <param name="userID">The user ID.</param>
        /// <param name="actingUserID">The acting user ID.</param>
        /// <param name="description">The description.</param>
        /// <returns></returns>
        public bool CreatePrepaidCardAccountJournalEntry(Guid accountID, Guid userID, Guid? actingUserID, string description)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter(true))
                {
                    try
                    {
                        PrepaidCardJournalEntity journal = new PrepaidCardJournalEntity();
                        journal.ActingUserId = actingUserID;
                        journal.CreationDateTime = DateTime.Now;
                        journal.Description = description;
                        journal.PrepaidCardAccountId = accountID;
                        journal.UserId = userID;

                        if (!adapter.SaveEntity(journal))
                        {
                            return false;
                        }
                    }
                    catch (ORMException exceptionMessage)
                    {
                        DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                        throw exception;
                    }
                }
                scope.Complete();
                return true;
            }
        }

        /// <summary>
        /// Retireves the prepaid card account journal entries.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public EntityCollection<JournalEntity> RetirevePrepaidCardAccountJournalEntries(Guid accountID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<JournalEntity> journalEntries = new EntityCollection<JournalEntity>(new JournalEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(PrepaidCardJournalFields.PrepaidCardAccountId == accountID);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.JournalEntity);

                    adapter.FetchEntityCollection(journalEntries, bucket, path);
                    return journalEntries;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the scheduled FSV card transactions for a user.
        /// </summary>
        /// <param name="userID">The user ID.</param>
        /// <returns></returns>
        public EntityCollection<FsvcardTransactionJobEntity> RetrieveScheduledFSVCardTransactions(Guid userID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<FsvcardTransactionJobEntity> cardLoads = new EntityCollection<FsvcardTransactionJobEntity>(new FsvcardTransactionJobEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(FsvcardTransactionJobFields.UserId == userID);
                    bucket.PredicateExpression.Add(FsvcardTransactionJobFields.Status == JobStatus.Waiting);
                    bucket.PredicateExpression.Add(FsvcardTransactionJobFields.TransactionType != TransactionType.Reversal);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.FsvcardTransactionJobEntity);

                    adapter.FetchEntityCollection(cardLoads, bucket, path);
                    return cardLoads;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves all prepaid accounts that are stored in our system.
        /// </summary>
        /// <returns></returns>
        public EntityCollection<PrepaidCardAccountEntity> RetrieveAllPrepaidAccounts()
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                try
                {
                    EntityCollection<PrepaidCardAccountEntity> prepaidAccounts = new EntityCollection<PrepaidCardAccountEntity>(new PrepaidCardAccountEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(PrepaidCardAccountFields.MarkedForDeletion == false);
                    bucket.PredicateExpression.Add(PrepaidCardAccountFields.CardNumber != DBNull.Value);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.PrepaidCardAccountEntity);
                    path.Add(PrepaidCardAccountEntity.PrefetchPathPrepaidCardAccountUsers);
                    adapter.FetchEntityCollection(prepaidAccounts, bucket, path);
                    return prepaidAccounts;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the open prepaid accounts. These are accounts which are not in
        /// the CLOSED or REPLACED state.
        /// </summary>
        /// <returns></returns>
        public EntityCollection<PrepaidCardAccountEntity> RetrieveOpenPrepaidAccounts()
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                try
                {
                    EntityCollection<PrepaidCardAccountEntity> prepaidAccounts = new EntityCollection<PrepaidCardAccountEntity>(new PrepaidCardAccountEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(PrepaidCardAccountFields.Status != PrepaidCardStatus.Closed);
                    bucket.PredicateExpression.Add(PrepaidCardAccountFields.Status != PrepaidCardStatus.Replaced);
                    bucket.PredicateExpression.Add(PrepaidCardAccountFields.MarkedForDeletion == false);
                    bucket.PredicateExpression.Add(PrepaidCardAccountFields.CardNumber != DBNull.Value);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.PrepaidCardAccountEntity);
                    path.Add(PrepaidCardAccountEntity.PrefetchPathPrepaidCardAccountUsers);
                    adapter.FetchEntityCollection(prepaidAccounts, bucket, path);
                    return prepaidAccounts;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the unfulfilled prepaid accounts
        /// </summary>
        /// <returns></returns>
        public EntityCollection<PrepaidCardAccountEntity> RetrieveUnfulfilledPrepaidAccounts()
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<PrepaidCardAccountEntity> prepaidAccounts = new EntityCollection<PrepaidCardAccountEntity>(new PrepaidCardAccountEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(PrepaidCardAccountFields.FulfillmentDateSent == DBNull.Value);
                    bucket.PredicateExpression.Add(PrepaidCardAccountFields.CardNumber != DBNull.Value);
                    adapter.FetchEntityCollection(prepaidAccounts, bucket);
                    return prepaidAccounts;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the unissued prepaid accounts
        /// </summary>
        /// <returns></returns>
        public EntityCollection<PrepaidCardAccountEntity> RetrieveUnissuedPrepaidAccounts()
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<PrepaidCardAccountEntity> prepaidAccounts = new EntityCollection<PrepaidCardAccountEntity>(new PrepaidCardAccountEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(PrepaidCardAccountFields.FulfillmentDateSent != DBNull.Value);
                    bucket.PredicateExpression.Add(PrepaidCardAccountFields.IssueDate == DBNull.Value);
                    bucket.PredicateExpression.Add(PrepaidCardAccountFields.CardNumber != DBNull.Value);
                    adapter.FetchEntityCollection(prepaidAccounts, bucket);
                    return prepaidAccounts;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// retrieves Pre-Paid Card Account by user
        /// </summary>
        /// <param name="UserId">the user id.</param>
        /// <returns>collection of pre-paid cards</returns>
        public bool RetrievePrepaidCardAccountsByUser(Guid userID, out EntityCollection<PrepaidCardAccountEntity> prepaidCards)
        {
            prepaidCards = new EntityCollection<PrepaidCardAccountEntity>(new PrepaidCardAccountEntityFactory());

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(PrepaidCardAccountEntity.Relations.PrepaidCardAccountUserEntityUsingPrepaidCardAccountId);
                    bucket.PredicateExpression.Add(PrepaidCardAccountUserFields.UserId == userID);

                    adapter.FetchEntityCollection(prepaidCards, bucket);
                    return true;

                }
                catch (ORMException exception)
                {
                    throw new DataAccessException("Error retrieving prepaid accounts", exception);
                }
            }
        }

        /// <summary>
        /// This function return true if the ach oustanding balance plus the amount passed is less than 250
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="amount"></param>
        /// <returns></returns>
        public bool CanTransferMoney(Guid userID, Decimal amount)
        {
            using (SqlConnection connection = new SqlConnection())
            {

                try
                {
                    connection.ConnectionString = ConfigurationManager.AppSettings["ConnectionString"];

                    SqlCommand command = new SqlCommand("UP_CheckPendingACHAmount", connection);
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.Add(new SqlParameter("@UserId", userID));
                    command.Parameters.Add(new SqlParameter("@Amount", amount));

                    connection.Open();
                    SqlDataReader reader = command.ExecuteReader();
                    if (reader.Read())
                    {
                        if ((int)reader[0] == 1)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }

                    connection.Close();
                }
                catch (Exception)
                {

                    return false;
                }
            }
            return true;
        }

        public int RetrieveActivePrepaidAccountTotal()
        {
            int count = 0;
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    PredicateExpression predicate = new PredicateExpression();
                    predicate.Add(PrepaidCardAccountFields.Active == 1);
                    PredicateExpression statusPredicate = new PredicateExpression();
                    statusPredicate.Add(PrepaidCardAccountFields.Status == PrepaidCardStatus.Good);
                    statusPredicate.AddWithOr(PrepaidCardAccountFields.Status == PrepaidCardStatus.Suspended);
                    predicate.AddWithAnd(statusPredicate);

                    count = (int)adapter.GetScalar(
                        PrepaidCardAccountFields.PrepaidCardAccountId,
                        null,
                        AggregateFunction.Count,
                        predicate
                    );
                }
                catch (ORMException e)
                {
                    throw new DataAccessException(e.Message);
                }
            }
            return count;
        }

        public int RetrieveActiveCreditAccountTotal()
        {
            int count = 0;
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    PredicateExpression predicate = new PredicateExpression();
                    predicate.Add(CreditCardAccountFields.Status == AccountStatus.AllowMoneyMovement);

                    count = (int)adapter.GetScalar(
                        CreditCardAccountFields.CreditCardAccountId,
                        null,
                        AggregateFunction.Count,
                        predicate
                    );
                }
                catch (ORMException e)
                {
                    throw new DataAccessException(e.Message);
                }
            }
            return count;
        }

        public int RetrieveActiveACHAccountTotal()
        {
            int count = 0;
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    PredicateExpression predicate = new PredicateExpression();
                    predicate.Add(BankAccountFields.Status == AccountStatus.AllowMoneyMovement);

                    count = (int)adapter.GetScalar(
                        BankAccountFields.BankAccountId,
                        null,
                        AggregateFunction.Count,
                        predicate
                    );
                }
                catch (ORMException e)
                {
                    throw new DataAccessException(e.Message);
                }
            }
            return count;
        }

        public int RetrieveActiveSavingsAccountTotal()
        {
            int count = 0;
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    PredicateExpression predicate = new PredicateExpression();
                    predicate.Add(CustomAccountFieldGroupFields.Status == AccountStatus.AllowMoneyMovement);

                    count = (int)adapter.GetScalar(
                        CustomAccountFieldGroupFields.CustomAccountFieldGroupId,
                        null,
                        AggregateFunction.Count,
                        predicate
                    );
                }
                catch (ORMException e)
                {
                    throw new DataAccessException(e.Message);
                }
            }
            return count;
        }


        #endregion //Prepaid Account

        #region Manual Payment Account

        /// <summary>
        /// Retrieves the manual payment transactions.
        /// </summary>
        /// <param name="parentID">The parent ID.</param>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns></returns>
        public EntityCollection<TransactionJobEntity> RetrieveManualPaymentTransactions(Guid parentID, DateTime startDate, DateTime endDate)
        {
            try
            {
                EntityCollection<TransactionJobEntity> jobs = new EntityCollection<TransactionJobEntity>(new JobEntityFactory());

                using (DataAccessAdapter adapter = new DataAccessAdapter(true))
                {
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(TransactionJobFields.UserId == parentID);
                    bucket.PredicateExpression.Add(TransactionJobFields.JobType == JobType.ManualLoadJob);
                    bucket.PredicateExpression.Add(TransactionJobFields.Status == JobStatus.Completed);
                    bucket.PredicateExpression.Add(TransactionJobFields.CompletedTime <= endDate);
                    bucket.PredicateExpression.Add(TransactionJobFields.CompletedTime >= startDate);
                    adapter.FetchEntityCollection(jobs, bucket);
                    return jobs;
                }
            }
            catch (ORMException exceptionMessage)
            {
                DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                throw exception;
            }
        }

        #endregion //Manual Payment Account

        /// <summary>
        /// Retrieves the allowances using the given account for a funding job.
        /// </summary>
        /// <param name="accountID">The account ID.</param>
        /// <returns></returns>
        public EntityCollection<ScheduledItemEntity> RetrieveAllowancesUsingAccountForFundingAccount(Guid accountID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<ScheduledItemEntity> allowances = new EntityCollection<ScheduledItemEntity>(new ScheduledItemEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(ScheduledItemEntity.Relations.TransactionAccountInfoEntityUsingTransactionAccountInfoId);
                    bucket.PredicateExpression.Add(ScheduledItemFields.IsEnabled == true);
                    bucket.PredicateExpression.Add(TransactionAccountInfoFields.FundingAccountId == accountID);
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.ScheduledItemEntity);

                    adapter.FetchEntityCollection(allowances, bucket, path);
                    return allowances;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        #region ACH Processing

        /// <summary>
        /// Retrieve the pending ACH items from the DB 
        /// </summary>
        /// <returns></returns>
        public EntityCollection<AchItemEntity> RetrievePendingACH()
        {
            try
            {
                EntityCollection<AchItemEntity> pendingAch = new EntityCollection<AchItemEntity>(new AchItemEntityFactory());

                using (DataAccessAdapter adapter = new DataAccessAdapter(true))
                {
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(AchItemFields.Processed == false);
                    adapter.FetchEntityCollection(pendingAch, bucket);
                    return pendingAch;
                }
            }
            catch (ORMException exceptionMessage)
            {
                DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                throw exception;
            }
        }

        /// <summary>
        /// Return the number of pending ACH items from the DB 
        /// </summary>
        /// <returns></returns>
        public int CountPendingACH()
        {
            try
            {
                EntityCollection<AchItemEntity> pendingAch = new EntityCollection<AchItemEntity>(new AchItemEntityFactory());

                using (DataAccessAdapter adapter = new DataAccessAdapter(true))
                {
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(AchItemFields.Processed == false);
                    adapter.FetchEntityCollection(pendingAch, bucket);
                    Console.WriteLine("Number of pending ACH records: {0}", pendingAch.Count);
                    return pendingAch.Count;
                }
            }
            catch (ORMException exceptionMessage)
            {
                DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                throw exception;
            }
        }

        /// <summary>
        /// Insert a new ACH Item into the database
        /// </summary>
        /// <param name="achItem"></param>
        /// <returns></returns>
        public bool InsertACHItem(AchItemEntity achItem)
        {
            using (TransactionScope scope = new TransactionScope())
            {


                using (DataAccessAdapter adapter = new DataAccessAdapter())
                {
                    adapter.SaveEntity(achItem);
                }
                scope.Complete();
            }
            return true;
        }

        /// <summary>
        /// inserts into the database a collection of ACHItems
        /// </summary>
        /// <param name="achItems"></param>
        /// <returns></returns>
        public bool InsertACHItem(EntityCollection<AchItemEntity> achItems)
        {
            UnitOfWork2 work = new UnitOfWork2();
            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                foreach (AchItemEntity item in achItems)
                {
                    work.AddForSave(item);
                }
                work.Commit(adapter, true);

            }
            return true;
        }

        /// <summary>
        /// Update the ACHitem into the database
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool UpdateACHItem(AchItemEntity item)
        {
            using (TransactionScope scope = new TransactionScope())
            {
                using (DataAccessAdapter adapter = new DataAccessAdapter())
                {
                    adapter.SaveEntity(item);
                }
                scope.Complete();
            }
            return true;
        }


        #endregion

        #region Emergency Loads

        /// <summary>
        /// Retrieves emergency loads for the given user in the given Time span.
        /// </summary>
        /// <param name="user">The user to to retrieve the loads for. If this value is null, the loads for the entire system are returned</param>
        /// <param name="startTime">The earliest date of the time span. (set this equal to end Time to get all transactions)</param>
        /// <param name="endTime">The most recent date of the time span. (set this equal to start time to get all transactions)</param>
        /// <param name="pageSize">Size of the page. If you don't want paging set this value to 0</param>
        /// <param name="pageNumber">The page number. If you don't want paging set this value to 0</param>
        /// <returns></returns>
        public EntityCollection<CreditCardTransactionJobEntity> RetrieveEmergencyLoads(UserEntity user, DateTime startTime, DateTime endTime, int pageSize, int pageNumber)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<CreditCardTransactionJobEntity> jobs = new EntityCollection<CreditCardTransactionJobEntity>(new CreditCardTransactionJobEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(CreditCardTransactionJobEntity.Relations.LinkedJobEntityUsingSecondaryJobId);
                    bucket.PredicateExpression.Add(CreditCardTransactionJobFields.TransactionType == TransactionType.EmergencyLoad);

                    //If the user is set, add the id to the filter
                    if (user != null)
                    {
                        bucket.PredicateExpression.Add(CreditCardTransactionJobFields.UserId == user.UserId);
                    }
                    //If the if the span dates are equal don't filter by the time span
                    if (!startTime.Equals(endTime))
                    {
                        bucket.PredicateExpression.Add(CreditCardTransactionJobFields.CreateTime > startTime);
                        bucket.PredicateExpression.Add(CreditCardTransactionJobFields.CreateTime < endTime);
                    }

                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.CreditCardTransactionJobEntity);
                    IPrefetchPathElement2 linkedJobPath = CreditCardTransactionJobEntity.PrefetchPathSecondaryLinkedJobs;
                    linkedJobPath.SubPath.Add(LinkedJobEntity.PrefetchPathJob);
                    path.Add(linkedJobPath);

                    adapter.FetchEntityCollection(jobs, bucket, 0, null, path, pageNumber, pageSize);

                    return jobs;

                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        #endregion //Emergency Loads

        #region Black Listing

        /// <summary>
        /// Retrieves the active black listing for a given account.
        /// If the account does not have a current blacklisting the method returns null
        /// </summary>
        /// <param name="accountNumber">The account number to lookup.</param>
        /// <returns></returns>
        public BlackListedAccountEntity RetrieveActiveBlackListingForAccount(string accountNumber)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    AesCryptoString.AesCryptoString encryptedString = accountNumber;
                    EntityCollection<BlackListedAccountEntity> blackLists = new EntityCollection<BlackListedAccountEntity>(new BlackListedAccountEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(BlackListedAccountFields.AccountNumber == encryptedString);
                    bucket.PredicateExpression.Add(BlackListedAccountFields.IsDeleted == false);

                    adapter.FetchEntityCollection(blackLists, bucket);

                    if (blackLists.Count > 0)
                    {
                        return blackLists[0];
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
        /// Retrieves the black listings for the given account.
        /// </summary>
        /// <param name="accountNumber">The account number to retrieve the black lists for.</param>
        /// <returns></returns>
        public EntityCollection<BlackListedAccountEntity> RetrieveBlackListingsForAccount(string accountNumber)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    AesCryptoString.AesCryptoString encryptedString = accountNumber;
                    EntityCollection<BlackListedAccountEntity> blackLists = new EntityCollection<BlackListedAccountEntity>(new BlackListedAccountEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();

                    bucket.PredicateExpression.Add(BlackListedAccountFields.AccountNumber == encryptedString);

                    adapter.FetchEntityCollection(blackLists, bucket);

                    return blackLists;

                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the active black listings for the entire system.
        /// </summary>
        /// <returns></returns>
        public EntityCollection<BlackListedAccountEntity> RetrieveActiveBlackListingsForSystem()
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<BlackListedAccountEntity> blackLists = new EntityCollection<BlackListedAccountEntity>(new BlackListedAccountEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();

                    bucket.PredicateExpression.Add(BlackListedAccountFields.IsDeleted == false);

                    adapter.FetchEntityCollection(blackLists, bucket);

                    return blackLists;

                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the all black listings for the entire system.
        /// </summary>
        /// <returns></returns>
        public EntityCollection<BlackListedAccountEntity> RetrieveBlackListingsForSystem()
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<BlackListedAccountEntity> blackLists = new EntityCollection<BlackListedAccountEntity>(new BlackListedAccountEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();

                    adapter.FetchEntityCollection(blackLists, bucket);

                    return blackLists;

                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }
        #endregion //Black Listing

        #region Delinquency

        /// <summary>
        /// Retrieves all delinquencies.
        /// </summary>
        /// <returns></returns>
        public EntityCollection<DelinquentAccountEntity> RetrieveDelinquencies()
        {
            return RetrieveDelinquencies(SortOperator.Ascending);
        }

        /// <summary>
        /// Retrieves all delinquencies.
        /// </summary>
        /// <returns></returns>
        public EntityCollection<DelinquentAccountEntity> RetrieveDelinquencies(SortOperator sortOperator)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<DelinquentAccountEntity> delinquencies = new EntityCollection<DelinquentAccountEntity>(new DelinquentAccountEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();

                    // Sort by the delinquency date.
                    SortExpression sortexp = new SortExpression();
                    sortexp.Add(DelinquentAccountFields.DelinquencyDate | sortOperator);

                    adapter.FetchEntityCollection(delinquencies, bucket, 0, sortexp);

                    return delinquencies;

                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves all unresolved delinquencies.
        /// </summary>
        /// <param name="pageNumber">The page number.  Setting this to 0 returns all results.</param>
        /// <param name="pageSize">Size of the page. Setting this to 0 returns all results</param>
        /// <returns></returns>
        public EntityCollection<DelinquentAccountEntity> RetrieveUnresolvedDelinquencies(int pageNumber, int pageSize)
        {
            return RetrieveUnresolvedDelinquencies(pageNumber, pageSize, SortOperator.Ascending);
        }

        /// <summary>
        /// Retrieves all unresolved delinquencies.
        /// </summary>
        /// <param name="pageNumber">The page number.  Setting this to 0 returns all results.</param>
        /// <param name="pageSize">Size of the page. Setting this to 0 returns all results</param>
        /// <returns></returns>
        public EntityCollection<DelinquentAccountEntity> RetrieveUnresolvedDelinquencies(int pageNumber, int pageSize, SortOperator sortOperator)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<DelinquentAccountEntity> delinquencies = new EntityCollection<DelinquentAccountEntity>(new DelinquentAccountEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(DelinquentAccountFields.ResolvedDate == DBNull.Value);

                    // Sort by the delinquency date.
                    SortExpression sortexp = new SortExpression();
                    sortexp.Add(DelinquentAccountFields.DelinquencyDate | sortOperator);

                    adapter.FetchEntityCollection(delinquencies, bucket, 0, sortexp, pageNumber, pageSize);

                    return delinquencies;

                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the number of unresolved delinquencies currently exist
        /// </summary>
        /// <returns></returns>
        public int RetrieveUnresolvedDelinquenciesCount()
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<DelinquentAccountEntity> delinquencies = new EntityCollection<DelinquentAccountEntity>(new DelinquentAccountEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(DelinquentAccountFields.ResolvedDate == DBNull.Value);

                    return adapter.GetDbCount(delinquencies, bucket);
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the delinquencies for user.
        /// </summary>
        /// <param name="userID">The user ID.</param>
        /// <returns></returns>
        public EntityCollection<DelinquentAccountEntity> RetrieveDelinquenciesForUser(Guid userID)
        {
            return RetrieveDelinquenciesForUser(userID, SortOperator.Ascending);
        }

        /// <summary>
        /// Retrieves the delinquencies for user.
        /// </summary>
        /// <param name="userID">The user ID.</param>
        /// <returns></returns>
        public EntityCollection<DelinquentAccountEntity> RetrieveDelinquenciesForUser(Guid userID, SortOperator sortOperator)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<DelinquentAccountEntity> delinquencies = new EntityCollection<DelinquentAccountEntity>(new DelinquentAccountEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(DelinquentAccountFields.TeenId == userID);

                    // Sort by the delinquency date.
                    SortExpression sortexp = new SortExpression();
                    sortexp.Add(DelinquentAccountFields.DelinquencyDate | sortOperator);

                    adapter.FetchEntityCollection(delinquencies, bucket, 0, sortexp);

                    return delinquencies;

                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the delinquencies by user ID.
        /// </summary>
        /// <param name="userID">The user ID.</param>
        /// <returns></returns>
        public EntityCollection<DelinquentAccountEntity> RetrieveDelinquenciesByUserID(Guid userID)
        {
            return RetrieveDelinquenciesByUserID(userID, SortOperator.Ascending);
        }

        /// <summary>
        /// Retrieves the delinquencies by user ID.
        /// </summary>
        /// <param name="userID">The user ID.</param>
        /// <returns></returns>
        public EntityCollection<DelinquentAccountEntity> RetrieveDelinquenciesByUserID(Guid userID, SortOperator sortOperator)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<DelinquentAccountEntity> delinquencies = new EntityCollection<DelinquentAccountEntity>(new DelinquentAccountEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(DelinquentAccountFields.TeenId == userID);

                    // Sort by the delinquency date.
                    SortExpression sortexp = new SortExpression();
                    sortexp.Add(DelinquentAccountFields.DelinquencyDate | sortOperator);

                    adapter.FetchEntityCollection(delinquencies, bucket, 0, sortexp);

                    return delinquencies;

                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the unresolved delinquencies by user ID.
        /// </summary>
        /// <param name="userID">The user ID.</param>
        /// <returns></returns>
        public EntityCollection<DelinquentAccountEntity> RetrieveUnresolvedDelinquenciesByUserID(Guid userID)
        {
            return RetrieveUnresolvedDelinquenciesByUserID(userID, SortOperator.Ascending);
        }

        /// <summary>
        /// Retrieves the unresolved delinquencies by user ID.
        /// </summary>
        /// <param name="userID">The user ID.</param>
        /// <returns></returns>
        public EntityCollection<DelinquentAccountEntity> RetrieveUnresolvedDelinquenciesByUserID(Guid userID, SortOperator sortOperator)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<DelinquentAccountEntity> delinquencies = new EntityCollection<DelinquentAccountEntity>(new DelinquentAccountEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(DelinquentAccountFields.ResolvedDate == DBNull.Value);
                    bucket.PredicateExpression.Add(DelinquentAccountFields.TeenId == userID);

                    // Sort by the delinquency date.
                    SortExpression sortexp = new SortExpression();
                    sortexp.Add(DelinquentAccountFields.DelinquencyDate | sortOperator);

                    adapter.FetchEntityCollection(delinquencies, bucket, 0, sortexp);

                    return delinquencies;

                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Saves the delinquency.
        /// </summary>
        /// <param name="entity">The entity.</param>
        public void SaveDelinquency(DelinquentAccountEntity entity)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                if (!adapter.SaveEntity(entity))
                {
                    throw new DataAccessException("Unable to save DelinquentAccountEntity");
                }
            }
        }

        /// <summary>
        /// Retrieves the delinquency.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        public DelinquentAccountEntity RetrieveDelinquency(Guid id)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                try
                {
                    EntityCollection<DelinquentAccountEntity> delinquencies = new EntityCollection<DelinquentAccountEntity>(new DelinquentAccountEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(DelinquentAccountFields.TeenId == id);

                    adapter.FetchEntityCollection(delinquencies, bucket);

                    if (delinquencies.Count == 1)
                    {
                        return delinquencies[0];
                    }
                    throw new DataAccessException("Unable to retrieve Delinquency with provided ID: " + id);
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
