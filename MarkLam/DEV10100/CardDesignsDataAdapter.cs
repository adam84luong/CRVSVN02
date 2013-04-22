using System;
using Payjr.Entity.DatabaseSpecific;
using Payjr.Entity.EntityClasses;
using Payjr.Entity.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using Payjr.Entity.FactoryClasses;
using Payjr.Entity;
using Payjr.Types;


namespace Payjr.DataAdapters.Users
{
    /// <summary>
    /// Card Designs
    /// </summary>
    public class CardDesignsDataAdapter
    {

        /// <summary>
        /// Retrieves a picture Entity with the given pictureID
        /// </summary>
        /// <param name="pictureID">Picture ID </param>
        /// <param name="pictureEntity">Entity output</param>
        /// <param name="error">Error output</param>
        /// <returns></returns>
        public PictureEntity RetrievePicture(DataAccessAdapter adapter,Guid pictureID)
        {

            if (pictureID == Guid.Empty)
            {
                return null;
            }

            try
            {
                PictureEntity pictureEntity = new PictureEntity(pictureID);

                    if (!adapter.FetchEntity(pictureEntity))
                    {
                        return null;
                    }
                    return pictureEntity;

            }
            catch (ORMException)
            {
                return null;
            }
        }


        #region Card Design Retrievals

        /// <summary>
        /// Retrieves Card Designs where the designs have not been approved
        /// </summary>
        /// <param name="adapter">The adapter.</param>
        /// <returns></returns>
        /// <exception cref="DataAccessAdapter">Thrown if there are problems retrieving designs</exception>
        public EntityCollection<CustomCardDesignEntity> RetrieveUnApprovedCardDesigns(DataAccessAdapter adapter)
        {
            try
            {
                EntityCollection<CustomCardDesignEntity> cardDesigns = new EntityCollection<CustomCardDesignEntity>(new CustomCardDesignEntityFactory());
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                // bucket.PredicateExpression.Add(new FieldCompareNullPredicate(UnRegisteredCardDesignFields.IsApproved, null));
                bucket.PredicateExpression.Add(CustomCardDesignFields.IsApproved == false);
                bucket.PredicateExpression.Add(CustomCardDesignFields.IsDesigned == true);
                bucket.PredicateExpression.Add(CustomCardDesignFields.IsRetrieved == false);
                adapter.FetchEntityCollection(cardDesigns, bucket);
                return cardDesigns;
            }
            catch (ORMException exceptionMessage)
            {
                throw new DataAccessException("Error when Retrieving UnApproved Card Designs", exceptionMessage);
            }


        }

        /// <summary>
        /// Retrieves Card Designs that have been approved
        /// </summary>
        /// <param name="adapter">The adapter.</param>
        /// <returns></returns>
        /// <exception cref="DataAccessAdapter">Thrown if there are problems retrieving desings</exception>
        public EntityCollection<CustomCardDesignEntity> RetrieveApprovedCardDesigns(DataAccessAdapter adapter)
        {
            try
            {
                EntityCollection<CustomCardDesignEntity> cardDesigns = new EntityCollection<CustomCardDesignEntity>(new CustomCardDesignEntityFactory());
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                bucket.PredicateExpression.Add(CustomCardDesignFields.IsApproved == true);
                adapter.FetchEntityCollection(cardDesigns, bucket);
                return cardDesigns;
            }
            catch (ORMException exceptionMessage)
            {
                throw new DataAccessException("Error when Retrieving Approved Card Designs", exceptionMessage);
            }


        }



        /// <summary>
        /// Retrieves Card Designs that have been approved
        /// </summary>
        /// <param name="adapter">The adapter.</param>
        /// <returns></returns>
        /// <exception cref="DataAccessAdapter">Thrown if there are problems retrieving desings</exception>
        public EntityCollection<CustomCardDesignEntity> RetrieveLastCreatedCardDesigns(DataAccessAdapter adapter)
        {
            try
            {
                EntityCollection<CustomCardDesignEntity> cardDesigns = new EntityCollection<CustomCardDesignEntity>(new CustomCardDesignEntityFactory());
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                bucket.PredicateExpression.Add(CustomCardDesignFields.IsApproved == true);
                SortExpression sorter = new SortExpression(CustomCardDesignFields.CreationDate | SortOperator.Descending);

                adapter.FetchEntityCollection(cardDesigns, bucket,20,sorter);
                return cardDesigns;

            }
            catch (ORMException exceptionMessage)
            {
                throw new DataAccessException("Error when Retrieving Approved Card Designs", exceptionMessage);
            }


        }


        /// <summary>
        /// Retrieves Card Designs that have been approved
        /// </summary>
        /// <param name="adapter">The adapter.</param>
        /// <param name="cardID">The card ID.</param>
        /// <returns></returns>
        /// <exception cref="DataAccessAdapter">Thrown if there are problems retrieving desings</exception>
        public CustomCardDesignEntity RetrieveCardDesignRegistration(DataAccessAdapter adapter, string cardID)
        {

            try
            {
                EntityCollection<CustomCardDesignEntity> cardDesigns = new EntityCollection<CustomCardDesignEntity>(new CustomCardDesignEntityFactory());
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                bucket.PredicateExpression.Add(CustomCardDesignFields.ServerSideCardDesignIdentifier== cardID);

                adapter.FetchEntityCollection(cardDesigns, bucket);

                if (cardDesigns.Count > 0)
                {
                    return cardDesigns[0];
                }
                else
                {
                    return null;
                }

            }
            catch (ORMException exceptionMessage)
            {
                throw new DataAccessException("Error when Retrieving a single Card Design Registration", exceptionMessage);
            }

        }

        /// <summary>
        /// Retrieves all the card designs that a user has within our system.
        /// </summary>
        /// <param name="userID"></param>
        /// <returns></returns>
        public EntityCollection<CustomCardDesignEntity> RetrieveAllUserCardDesigns(Guid userID)
        {
            try
            {
                EntityCollection<CustomCardDesignEntity> cards = new EntityCollection<CustomCardDesignEntity>(new CustomCardDesignEntityFactory());
                using (DataAccessAdapter adapter = new DataAccessAdapter())
                {
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(CustomCardDesignEntity.Relations.CustomCardDesignUserEntityUsingCustomCardDesignId);
                    bucket.Relations.Add(UserEntity.Relations.CustomCardDesignUserEntityUsingUserId);
                    bucket.PredicateExpression.Add(CustomCardDesignUserFields.UserId == userID);
                    
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.CustomCardDesignEntity);
                    path.Add(CustomCardDesignEntity.PrefetchPathCustomCardDesignUsers);
                    
                    adapter.FetchEntityCollection(cards, bucket, path);
                }
                return cards;
            }
            catch (ORMException exception)
            {
                throw new DataAccessException("Unable to retrieve card designs for user:" + userID, exception);

            }
        }

        /// <summary>
        /// Get the card design for the user.
        /// </summary>
        /// <param name="cardDesignID"></param>
        /// <returns></returns>
        public CustomCardDesignUserEntity RetrieveUserCardDesignFromDesignID(Guid cardDesignID)
        {
            CustomCardDesignUserEntity card = null;
            try
            {
                EntityCollection<CustomCardDesignUserEntity> cards = new EntityCollection<CustomCardDesignUserEntity>(new CustomCardDesignUserEntityFactory());
                using (DataAccessAdapter adapter = new DataAccessAdapter())
                {
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(CustomCardDesignUserEntity.Relations.CustomCardDesignEntityUsingCustomCardDesignId);
                    bucket.PredicateExpression.Add(CustomCardDesignFields.CustomCardDesignId == cardDesignID);
                    adapter.FetchEntityCollection(cards, bucket);
                }
                if (cards.Count > 0)
                {
                    card = cards[0];
                }
                return card;
            }
            catch (ORMException exception)
            {
                throw new DataAccessException("Unable to retrieve card design for:" + cardDesignID, exception);
            }
        }

        /// <summary>
        /// Gets a card design by the server side id
        /// </summary>
        /// <param name="serverSideID"></param>
        /// <returns></returns>
        public CustomCardDesignUserEntity RetrieveUserCardDesignFromServerSideID(string serverSideID)
        {
            CustomCardDesignUserEntity card = null;
            try
            {
                EntityCollection<CustomCardDesignUserEntity> cards = new EntityCollection<CustomCardDesignUserEntity>(new CustomCardDesignUserEntityFactory());
                using (DataAccessAdapter adapter = new DataAccessAdapter())
                {
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(CustomCardDesignUserEntity.Relations.CustomCardDesignEntityUsingCustomCardDesignId);
                    bucket.PredicateExpression.Add(CustomCardDesignFields.ServerSideCardDesignIdentifier == serverSideID);
                    adapter.FetchEntityCollection(cards, bucket);
                }
                if (cards.Count > 0)
                {
                    card = cards[0];
                }
                return card;
            }
            catch (ORMException exception)
            {
                throw new DataAccessException("Unable to retrieve card design for:" + serverSideID, exception);
            }
        }

        /// <summary>
        /// Get the card design for the user.
        /// </summary>
        /// <param name="cardDesignID"></param>
        /// <returns></returns>
        public CustomCardDesignEntity RetrieveUserCardDesign(Guid cardDesignID)
        {
            CustomCardDesignEntity card = null;
            try
            {
                EntityCollection<CustomCardDesignEntity> cards = new EntityCollection<CustomCardDesignEntity>(new CustomCardDesignEntityFactory());
                using (DataAccessAdapter adapter = new DataAccessAdapter())
                {
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(CustomCardDesignFields.CustomCardDesignId == cardDesignID);
                    adapter.FetchEntityCollection(cards, bucket);
                }
                if (cards.Count > 0)
                {
                    card = cards[0];
                }
                return card;
            }
            catch (ORMException exception)
            {
                throw new DataAccessException("Unable to retrieve card design for:" + cardDesignID, exception);
            }
        }

        /// <summary>
        /// Retrieves an unregistered card design by ID
        /// </summary>
        /// <param name="cardDesignID"></param>
        /// <returns></returns>
        public UnregisteredCustomCardDesignEntity RetrieveUnregisteredCardDesign(Guid cardDesignID)
        {
            UnregisteredCustomCardDesignEntity unregCardDesign = new UnregisteredCustomCardDesignEntity(cardDesignID);
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    if (!adapter.FetchEntity(unregCardDesign))
                    {
                        return null;
                    }
                    return unregCardDesign;
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        public UnregisteredCustomCardDesignEntity RetrieveUnregisteredCardDesignByServerSideID(string serverSideID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<UnregisteredCustomCardDesignEntity> unRegCardDesigns = new EntityCollection<UnregisteredCustomCardDesignEntity>(new UnregisteredCustomCardDesignEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.Relations.Add(UnregisteredCustomCardDesignEntity.Relations.CustomCardDesignEntityUsingCustomCardDesignUnregisteredId);
                    bucket.PredicateExpression.Add(CustomCardDesignFields.ServerSideCardDesignIdentifier == serverSideID);
                    adapter.FetchEntityCollection(unRegCardDesigns, bucket);

                    if(unRegCardDesigns.Count > 0)
                    {
                        return unRegCardDesigns[0];
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

        public bool DoesDBContainServerSideID(string serverSideID, Guid? customCardID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    EntityCollection<CustomCardDesignEntity> cardDesigns = new EntityCollection<CustomCardDesignEntity>(new CustomCardDesignEntityFactory());
                    IRelationPredicateBucket bucket = new RelationPredicateBucket();
                    bucket.PredicateExpression.Add(CustomCardDesignFields.ServerSideCardDesignIdentifier == serverSideID);
                    if(customCardID.HasValue)
                    {
                        bucket.PredicateExpression.Add(CustomCardDesignFields.CustomCardDesignId != customCardID.Value);
                    }
                    IPrefetchPath2 path = new PrefetchPath2((int)EntityType.CustomCardDesignEntity);

                    adapter.FetchEntityCollection(cardDesigns, bucket, path);

                    return (cardDesigns.Count > 0);
                }
                catch (ORMException exceptionMessage)
                {
                    DataAccessException exception = new DataAccessException(exceptionMessage.Message, exceptionMessage);
                    throw exception;
                }
            }
        }

        #region Registered Card Designs

        /// <summary>
        /// Retrieves a card design from the database if it has not been designed.
        /// </summary>
        /// <param name="adapter">data access adapter</param>
        /// <param name="userID">user id</param>
        /// <returns>Null - If the card already has a design.</returns>
        public CustomCardDesignEntity RetrieveCardNeedingDesignbyChild(DataAccessAdapter adapter, Guid userID)
        {
            try
            {
                EntityCollection<CustomCardDesignEntity> cardDesigns = new EntityCollection<CustomCardDesignEntity>(new CustomCardDesignEntityFactory());
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                //bucket.PredicateExpression.Add(UserCardDesignFields.UserId == userID);

                adapter.FetchEntityCollection(cardDesigns, bucket);
                if (cardDesigns.Count == 1)
                {
                    if (cardDesigns[0].IsRetrieved == false)
                    {
                        return cardDesigns[0];
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
            catch (ORMException exceptionMessage)
            {
                throw new DataAccessException("Error when retrieving user card design", exceptionMessage);
            }
        }

        #endregion Registered Card Designs

        #endregion //Card Design Retrievals

        #region Card Design Creation

        /// <summary>
        /// Create a new Custom Card Design Entity.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public CustomCardDesignEntity CreateNewCardDesign(UserEntity user)
        {
            CustomCardDesignEntity cardDesign = new CustomCardDesignEntity();
            cardDesign.CustomCardDesignId = Guid.NewGuid();

            CustomCardDesignUserEntity cardDesignUser = new CustomCardDesignUserEntity();

            cardDesignUser.User = user;
            cardDesignUser.CustomCardDesign = cardDesign;

            user.CustomCardDesignUsers.Add(cardDesignUser);

            return cardDesign;
        }

        public CustomCardDesignEntity CreateNewUnregisteredCustomCardDesign(UnregisteredCustomCardDesignEntity entity)
        {
            CustomCardDesignEntity cardDesign = new CustomCardDesignEntity();

            cardDesign.CustomCardDesignId = Guid.NewGuid();

            entity.CustomCardDesignUnregisteredId = cardDesign.CustomCardDesignId;
            entity.CustomCardDesign = cardDesign;

            return cardDesign;
        }


        #endregion //Card Design Creation

        #region Card Updates

        /// <summary>
        /// Update all of the card designs
        /// </summary>
        /// <param name="adapter">The adapter.</param>
        /// <param name="cardDesigns">The card designs.</param>
        /// <returns></returns>
        /// <exception cref="DataAccessAdapter">Thrown if updating the card designs</exception>
        public int UpdateCardDesigns(DataAccessAdapter adapter, EntityCollection<CustomCardDesignEntity> cardDesigns)
        {
            try
            {
                return adapter.SaveEntityCollection(cardDesigns);
            }
            catch (ORMException exceptionMessage)
            {
                throw new DataAccessException("Error when saving the collection of UnRegistered Card Designs", exceptionMessage);
            }
        }

        /// <summary>
        /// Assigns an existing card design to a user.
        /// </summary>
        /// <param name="user">child that the card is being assigned to</param>
        /// <param name="customCardDesignID">custom card id.</param>
        /// <returns></returns>
        public CustomCardDesignUserEntity AssignCustomCardDesignToUser(UserEntity user, Guid customCardDesignID)
        {
            CustomCardDesignUserEntity cardUser = new CustomCardDesignUserEntity();
            cardUser.User = user;
            cardUser.CustomCardDesignId = customCardDesignID;

            user.CustomCardDesignUsers.Add(cardUser);

            return cardUser;
        }

        #endregion //Card Updates
    }
}
