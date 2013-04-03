using System;
using System.Collections.Generic;
using System.Text;
using Payjr.Core.Adapters;
using Payjr.Entity.HelperClasses;
using Payjr.Entity.EntityClasses;
using Payjr.Entity.FactoryClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using Payjr.Entity.DatabaseSpecific;
using Payjr.Types;
using Payjr.Entity;

namespace Payjr.DataAdapters
{
    public class TransactionAdapter //: CacheAdapter // CacheAdapter not functioning
    {
        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="JournalAdapter"/> class.
        /// </summary>
        /// <param name="type">The type.</param>
        //public TransactionAdapter(CacheType type) : base(type) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="JournalAdapter"/> class.
        /// </summary>
        public TransactionAdapter() { }//: base(CacheType.ASPNetCacheProvider) { }
        #endregion


        public EntityCollection<TransactionAuditEntity> RetrieveTransactionAudits(Guid transactionJobID)
        {
            //string key = "Audit-TransactionID-" + transactionID;
            //if (this[key] == null)
            //{
            using (DataAccessAdapter adapter = new DataAccessAdapter(false))
            {
                EntityCollection<TransactionAuditEntity> audits = new EntityCollection<TransactionAuditEntity>(new TransactionAuditEntityFactory());
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                bucket.PredicateExpression.Add(TransactionAuditFields.TransactionJobId == transactionJobID);

                adapter.FetchEntityCollection(audits, bucket);

                //this[key] = audits;
                return audits;
            }
            //}
            //return (EntityCollection<TransactionAuditEntity>)this[key];
        }

        #region CardTransaction

        /// <summary>
        /// Saves the given card transaction entity.
        /// </summary>
        /// <param name="cardTransaction">The card transaction to save.</param>
        /// <exception cref="ArgumentNullException">Thrown if cardTransaction is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if cardTransaction is not new</exception>
        /// <exception cref="DataAccessException">Thrown if the save call fails</exception>
        /// <returns></returns>
        public bool SaveCardTransaction(CardTransactionEntity cardTransaction)
        {
            //Validate input
            if (cardTransaction == null) { throw new ArgumentNullException("cardTransaction"); }
            if (!cardTransaction.IsNew)
            {
                throw new InvalidOperationException(string.Format("Card Transactions cannot be updated.  ID: {0}",
                    cardTransaction.TransactionHistoryId.ToString()));
            }

            //Save
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    return adapter.SaveEntity(cardTransaction, true);
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(string.Format("Failed to save Card Transaction.  TranID: {0}",
                        cardTransaction.TranId), exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the card transaction by tran ID.
        /// </summary>
        /// <param name="tranID">The tran ID to look up with.</param>
        /// <returns></returns>
        /// 

        public CardTransactionEntity RetrieveCardTransactionByTranID(string tranID)
        {
            //Validate input
            if (string.IsNullOrEmpty(tranID)) { throw new ArgumentNullException("tranID"); }

            //Retrieve
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<CardTransactionEntity> collection = new EntityCollection<CardTransactionEntity>(new CardTransactionEntityFactory());

                    //Build filter
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(CardTransactionFields.TranId == tranID);

                    adapter.FetchEntityCollection(collection, bucket);

                    //If we got more than one entry throw an exception (we don't know which one is right and the data needs to be looked at)
                    if (collection.Count > 1)
                    {
                        throw new PayjrException("Found more than one transaction with the same TranID.  Could not resolve which one to retrieve.  TranID: " +
                            tranID);
                    }

                    //If we have one entry, return that
                    if (collection.Count > 0) { return collection[0]; }

                    return null;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message);
                    throw exception;
                }
            }
        }
        public EntityCollection<CardTransactionEntity> RetrieveCardTransactionsSearch(String cardIdentifier, DateTime startDate, DateTime endDate)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<CardTransactionEntity> cardTransactions = new EntityCollection<CardTransactionEntity>(new CardTransactionEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(PrepaidCardAccountFields.CardIdentifier == cardIdentifier);
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

        #endregion //CardTransaction

        #region Card Transaction Queue

        /// <summary>
        /// Saves the given Card Transaction Queue Entity (Does not refetch)
        /// </summary>
        /// <param name="cardTransactionQueueEntity">The card transaction queue entity to save.</param>
        /// <exception cref="ArugumentNullException">Thrown if cardTransactionQueueEntity is null</exception>
        /// <exception cref="DataAccessException">Thrown if the save call throws and exception</exception>
        /// <returns></returns>
        public bool SaveCardTransactionQueue(CardTransactionQueueEntity cardTransactionQueueEntity)
        {
            //Validate input
            if (cardTransactionQueueEntity == null) { throw new ArgumentNullException("cardTransactionQueueEntity"); }

            //Save
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    return adapter.SaveEntity(cardTransactionQueueEntity);
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves Card Transaction Queues that have not been processed
        /// </summary>
        /// <exception cref="DataAccessException">Thrown if the Fetch call throws an exception</exception>
        /// <returns></returns>
        public EntityCollection<CardTransactionQueueEntity> RetrieveUnprocessedCardTransactionQueues()
        {
            //Retrieve
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<CardTransactionQueueEntity> returnCol = new EntityCollection<CardTransactionQueueEntity>();

                    //Build Filter
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(CardTransactionQueueFields.Processed == false);

                    //Retrieve
                    adapter.FetchEntityCollection(returnCol, bucket);

                    return returnCol;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves Card Transaction Queues that have failed processing
        /// </summary>
        /// <exception cref="DataAccessException">Thrown if the Fetch call throws an exception</exception>
        /// <returns></returns>
        public int RetrieveFailedCardTransactionQueuesCount()
        {
            //Retrieve
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<CardTransactionQueueEntity> returnCol = new EntityCollection<CardTransactionQueueEntity>();

                    //Build Filter
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(CardTransactionQueueFields.Processed == true);
                    bucket.PredicateExpression.Add(CardTransactionQueueFields.Failure == true);

                    //Retrieve
                    return adapter.GetDbCount(returnCol, bucket);
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves Card Transaction Queues that have failed processing
        /// </summary>
        /// <returns></returns>
        public EntityCollection<CardTransactionQueueEntity> RetrieveFailedCardTransactionQueues()
        {
            return RetrieveFailedCardTransactionQueues(TransactionAdapter.FailedCardTransactionQueueSortColumn.None, SortOperator.Ascending, 0, 0);
        }

        /// <summary>
        /// Retrieves Card Transaction Queues that have failed processing
        /// </summary>
        /// <param name="sortColumn">The sort column.</param>
        /// <param name="sortOperator">The sort operator.</param>
        /// <param name="pageNumber">The page number.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <returns></returns>
        /// <exception cref="DataAccessException">Thrown if the Fetch call throws an exception</exception>
        public EntityCollection<CardTransactionQueueEntity> RetrieveFailedCardTransactionQueues(FailedCardTransactionQueueSortColumn sortColumn, SortOperator sortOperator, int pageNumber, int pageSize)
        {
            //Retrieve
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<CardTransactionQueueEntity> returnCol = new EntityCollection<CardTransactionQueueEntity>();

                    //Build Filter
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(CardTransactionQueueFields.Processed == true);
                    bucket.PredicateExpression.Add(CardTransactionQueueFields.Failure == true);

                    SortExpression sortExpression;
                    switch (sortColumn)
                    {
                        case FailedCardTransactionQueueSortColumn.EntryDate:
                            sortExpression = new SortExpression(CardTransactionQueueFields.TransactionEntryDate | sortOperator);
                            break;
                        case FailedCardTransactionQueueSortColumn.FailureDate:
                            sortExpression = new SortExpression(CardTransactionQueueFields.FailureDate | sortOperator);
                            break;
                        case FailedCardTransactionQueueSortColumn.FSVID:
                            sortExpression = new SortExpression(CardTransactionQueueFields.TransId | sortOperator);
                            break;
                        case FailedCardTransactionQueueSortColumn.Imported:
                            sortExpression = new SortExpression(CardTransactionQueueFields.Imported | sortOperator);
                            break;
                        case FailedCardTransactionQueueSortColumn.TransactionType:
                            sortExpression = new SortExpression(CardTransactionQueueFields.TransactionType | sortOperator);
                            break;
                        case FailedCardTransactionQueueSortColumn.None:
                        default:
                            sortExpression = null;
                            break;
                    }

                    //Retrieve
                    adapter.FetchEntityCollection(returnCol, bucket, pageSize > 0 ? pageSize : 50, sortExpression, pageNumber, pageSize);

                    return returnCol;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Deletes the card transaction queue with the given ID.
        /// </summary>
        /// <param name="cardTransactionQueueID">The card transaction queue ID of the queue entry to delete.</param>
        /// <exception cref="ArgumentException">Thrown if the cardTransactionQueueID is an empty Guid</exception>
        /// <exception cref="DataAccessException">Thrown if the Delete call throws an exception</exception>
        /// <returns></returns>
        public bool DeleteCardTransactionQueue(Guid cardTransactionQueueID)
        {
            //Validate Input
            if (cardTransactionQueueID == Guid.Empty) { throw new ArgumentException("cardTransactionQueueID must be set", "cardTransactionQueueID"); }

            //Delete
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    CardTransactionQueueEntity entityToDelete = new CardTransactionQueueEntity(cardTransactionQueueID);
                    return adapter.DeleteEntity(entityToDelete);
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException("An exception was thrown when trying to delete a cardTransactionQueue.  ID: " +
                        cardTransactionQueueID.ToString(), exceptionMessage);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the card transaction queue.
        /// </summary>
        /// <param name="QueueID">The queue ID.</param>
        /// <returns></returns>
        public CardTransactionQueueEntity RetrieveCardTransactionQueue(Guid queueID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<CardTransactionQueueEntity> entityCollection = new EntityCollection<CardTransactionQueueEntity>();

                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(CardTransactionQueueFields.CardTransactionQueueId == queueID);

                    adapter.FetchEntityCollection(entityCollection, bucket);

                    if (entityCollection.Count <= 0) return null;
                    if (entityCollection.Count >= 2) throw new DataAccessException("More than one query found with ID " + queueID);
                    return entityCollection[0];
                }
                catch (ORMException exceptionMessage)
                {
                    throw new DataAccessException(exceptionMessage.Message);
                }
            }
        }

        public enum FailedCardTransactionQueueSortColumn
        {
            None,
            EntryDate,
            FailureDate,
            FSVID,
            Imported,
            TransactionType
        }

        #endregion //Card Transaction Queue

        #region Card Transaction Process History

        /// <summary>
        /// Saves the given card transaction process history.  (Does not refetch)
        /// </summary>
        /// <param name="processHistory">The process history to save.</param>
        /// <exception cref="ArguementNullExcpeption">Thrown if processHistory is null</exception>
        /// <exception cref="InvalidOperationException">Thrown if processHistory is not new</exception>
        /// <exception cref="DataAccessException">Thrown if the Save call throws an exception</exception>
        /// <returns></returns>
        public bool SaveCardTransactionProcessHistory(CardTransactionProcessHistoryEntity processHistory)
        {
            //Validate input
            if (processHistory == null) { throw new ArgumentNullException("processHistory"); }
            if (!processHistory.IsNew) { throw new InvalidOperationException("Updates of Card Transaction Process History entries are not allowed"); }

            //Save
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    return adapter.SaveEntity(processHistory);
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message);
                    throw exception;
                }
            }
        }

        /// <summary>
        /// Retrieves the card transaction process history by trans ID.
        /// </summary>
        /// <param name="cardTransactionID">The card transaction ID to look up with.</param>
        /// <exception cref="ArguementException">Throw if cardTransactionID is an empty Guid</exception>
        /// <exception cref="DataAccessException">Thrown if the Retrieve call throws an exception</exception>
        /// <returns></returns>
        public EntityCollection<CardTransactionProcessHistoryEntity> RetrieveCardTransactionProcessHistoryByTransID(Guid cardTransactionID)
        {
            //Validate input
            if (cardTransactionID == Guid.Empty) { throw new ArgumentException("cardTransactionID must be set"); }

            //Retrieve
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<CardTransactionProcessHistoryEntity> collection = new EntityCollection<CardTransactionProcessHistoryEntity>(new CardTransactionProcessHistoryEntityFactory());

                    //Build filter
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(CardTransactionProcessHistoryFields.CardTransactionId == cardTransactionID);

                    adapter.FetchEntityCollection(collection, bucket);

                    return collection;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException("An exception occurred when try to retrieve process history for a transaction.  TransactionID:" +
                    cardTransactionID.ToString(), exceptionMessage);
                    throw exception;
                }
            }
        }

        #endregion //Card Transaction Process History

    }
}
