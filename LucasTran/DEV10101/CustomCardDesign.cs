#region Copyright PAYjr Inc. 2005-2007
//
// All rights are reserved. Reproduction in whole or in part, in any 
// form or by any means, electronic, mechanical or otherwise, is    
// prohibited  without the prior written consent of the copyright owner.
//
#endregion

using System;
using System.Collections.Generic;
using System.Text;
using Payjr.Entity.EntityClasses;
using Payjr.Core.UserInfo.Interfaces;
using Payjr.Entity;
using Payjr.Core.Adapters;
using Payjr.Entity.DatabaseSpecific;
using SD.LLBLGen.Pro.ORMSupportClasses;
using Payjr.DataAdapters;
using Payjr.Core.Users;
using Payjr.Core.PreRegistration;
using Payjr.Core.Services;
using Payjr.Core.Jobs;
using Payjr.Entity.HelperClasses;
using System.Configuration;
using Payjr.Core.FinancialAccounts;
using Common.Business;
using Common.ServerSide.Provider;
using Common.Business.Validation.Rules;
using Payjr.Core.Providers;

namespace Payjr.Core.UserInfo
{
    /// <summary>
    /// Contains the custom card design of the child
    /// </summary>
    public class CustomCardDesign : BusinessEntityChild<CustomCardDesignEntity>, ICustomCardDesign
    {
        #region fields

        private CustomCardDesignEntity _cardDesign;

        private bool _sendApprovedNotifications;
        private bool _sendDeniedNotifications;
        private bool _updateCardCreate;
        private bool _enrollInCompetition;

        #endregion //fields

        #region Error Msgs
        #region Post Save Error Msgs
        #region Notification
        private const string ERROR_NOTIFICATION_FAILED = "Failed to send {0} notification for card design {1}";
        #endregion //Notification
        #region Enroll in Comp
        private const string ERROR_ENROLL_IN_COMP_FAILED = "Call to Serverside to enroll card id {0} in competition failed";
        #endregion //Enroll in Comp
        #endregion //Post Save Error Msgs
        #endregion //Error Msgs

        #region properties

        private INotificationService _notificationService;
        /// <summary>
        /// Use to set notification mock
        /// </summary>
        public INotificationService NotificationService
        {
            get
            {
                if (_notificationService == null)
                {
                    return ServiceFactory.NotificationService;
                }
                return _notificationService;
            }
            set
            {
                _notificationService = value;
            }

        }


        private ISSGProvider _ssgProvider;
        /// <summary>
        /// Use to set test mock
        /// </summary>
        public ISSGProvider SSGProvider
        {
            get
            {
                if (_ssgProvider == null)
                {
                    Guid CompetitionKey;
                    if (CustomCardEmails.Site != null && CustomCardEmails.Site.PrepaidModule != null && CustomCardEmails.Site.PrepaidModule.CompetitionKey.HasValue)
                    {
                        CompetitionKey = CustomCardEmails.Site.PrepaidModule.CompetitionKey.Value;
                    }
                    //If the prepaid module does not have the key get it from the config file
                    else
                    {
                        CompetitionKey = new Guid(ConfigurationManager.AppSettings["ServerSideCompetitionID"]);
                    }
                    _ssgProvider = new SSGProvider(ConfigurationManager.AppSettings["ServerSideCompBaseURL"], CompetitionKey.ToString()) as ISSGProvider;
                }
                return _ssgProvider;
            }
            set
            {
                _ssgProvider = value;
            }
        }

        private Picture _picture;
        /// <summary>
        /// The picture id of the card design.
        /// </summary>
        public Picture Picture
        {
            get
            {
                if (_cardDesign.PictureId.HasValue)
                {
                    _picture = new Picture(_cardDesign.PictureId.Value, BusinessParent, BusinessParentEventBag);
                }


                return _picture;
            }

        }

        /// <summary>
        /// Creates the a picture.
        /// </summary>
        /// <returns></returns>
        public Picture CreateNewPicture()
        {
            PictureEntity picture = new PictureEntity();
            this.Entity.Picture = picture;

            return _picture = new Picture(picture, BusinessParent, BusinessParentEventBag);
        }

        /// <summary>
        /// Gets a value indicating whether this object has been deleted
        /// </summary>
        /// <value>
        /// 	<c>true</c> if this instance is deleted; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>This will only return <c>true</c> if the object has in fact been deleted from persistence.  It does
        /// not return <c>true</c> if ONLY the <see cref="Delete"/> has been called</remarks>
        public override bool IsDeleted
        {
            get
            {
                return base.Entity.MakedForDeletion;
            }
        }

        /// <summary>
        /// Date the card was registered with us.
        /// </summary>
        public DateTime DateCreated
        {
            get
            {
                return _cardDesign.CreationDate;
            }
        }

        /// <summary>
        /// True - if the user has enrolled their design in
        /// any competition that is being held or was held.
        /// </summary>
        public bool InCompetition
        {
            get
            {
                return _cardDesign.InCompetition;
            }
        }

        /// <summary>
        /// True - if the card design has been approved by us.
        /// </summary>
        public bool IsApproved
        {
            get
            {
                return _cardDesign.IsApproved;
            }
        }

        /// <summary>
        /// The reason why the card design was not approved.
        /// If the card design is approved then this is empty.
        /// </summary>
        public string DenialReason
        {
            get
            {
                return _cardDesign.DenialReason;
            }
        }

        /// <summary>
        /// The nine character server side card design id.
        /// </summary>
        public string ServerSideDesignID
        {
            get
            {
                return _cardDesign.ServerSideCardDesignIdentifier.Trim();
            }
        }

        /// <summary>
        /// The card design id of the custom card.
        /// </summary>
        public Guid CustomCardDesignID
        {
            get
            {
                return _cardDesign.CustomCardDesignId;
            }
        }

        /// <summary>
        /// If the card has been processed and approved and the
        /// user has returned and registered with our service.
        /// </summary>
        public bool IsRetrieved
        {
            get
            {
                return _cardDesign.IsRetrieved;
            }
        }

        /// <summary>
        /// Role type of user that designed the card
        /// </summary>
        public RoleType? Creator
        {
            get
            {
                return _cardDesign.Creator;
            }
            set
            {
                _cardDesign.Creator = value;
            }
        }

        /// <summary>
        /// Set if the card has been designed
        /// </summary>
        public bool IsDesigned
        {
            get
            {
                if (_cardDesign.IsDesigned.HasValue)
                {
                    return _cardDesign.IsDesigned.Value;
                }
                return false;
            }
        }

        /// <summary>
        /// Set if a denial has been acknowledged
        /// </summary>
        public bool IsDenialACK
        {
            get
            {
                if (_cardDesign.IsDenialAck.HasValue)
                {
                    return _cardDesign.IsDenialAck.Value;
                }
                return false;
            }
        }

        /// <summary>
        /// Parent email address for this custom card design.
        /// </summary>
        public ParentTeenEmail CustomCardEmails
        {
            get
            {
                ParentTeenEmail email = null;
                if (_cardDesign.CustomCardDesignUsers.Count > 0)
                {
                    Teen teen = User.RetrieveUser(_cardDesign.CustomCardDesignUsers[0].UserId) as Teen;
                    if (teen != null)
                    {
                        email = new ParentTeenEmail(teen);
                    }
                }
                else
                {
                    UnRegisteredCustomCardDesign unReg = UnRegisteredCustomCardDesign.RetrieveUnregisteredFromCustomCardDesignID(CustomCardDesignID);
                    if (unReg != null)
                    {
                        email = new ParentTeenEmail(unReg);
                    }
                }
                return email;
            }
        }

        #endregion //properties

        #region constructors

        public CustomCardDesign(CustomCardDesignEntity cardDesign, BusinessBase parentObj, IParentEventBag parentEvents)
            : base(cardDesign, parentObj, parentEvents, new DataAccessAdapterFactory())
        {
            if (cardDesign == null)
            {
                throw new ArgumentNullException("cardDesignEntity", "The card design entity cannot be null when constructing the card design.");
            }

            _cardDesign = cardDesign;

            //Set defaults if this is a new entity
            if (IsNew)
            {
                _cardDesign.CreationDate = DateTime.UtcNow;
                _cardDesign.DenialReason = "";
                _cardDesign.IsApproved = false;
                _cardDesign.IsDenialAck = false;
                _cardDesign.IsDesigned = false;
                _cardDesign.IsRetrieved = false;
                _cardDesign.MakedForDeletion = false;
                _cardDesign.Price = 0.0M;
                _cardDesign.InCompetition = false;
            }
        }

        #endregion //constructors

        #region overridden calls

        /// <summary>
        /// Is the object dirty.
        /// </summary>
        public override bool IsDirty
        {
            get
            {
                if (_picture != null && _picture.IsDirty)
                {
                    return true;
                }
                else
                {
                    return base.IsDirty;
                }
            }
        }

        /// <summary>
        /// Are the values valid.
        /// </summary>
        public override bool IsValid
        {
            get { return base.IsValid; }
        }

        /// <summary>
        /// Processes post save events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ParentEvents_UpdateDataSuccess(object sender, UpdateSuccessEventArgs e)
        {
            if (_sendApprovedNotifications)
            {
                if (!SendApprovedNotifications())
                {
                    e.CancelUpdate = true;
                }
            }

            if (_sendDeniedNotifications)
            {
                if (!SendDeniedNotifications())
                {
                    e.CancelUpdate = true;
                }
            }

            if (_updateCardCreate)
            {
                if (!UpdateCardCreateJobs())
                {
                    e.CancelUpdate = true;
                }
            }

            if (_enrollInCompetition)
            {
                if (!EnrollInCompetition())
                {
                    e.CancelUpdate = true;
                }
            }

            //reset all the bits
            _sendApprovedNotifications = false;
            _sendDeniedNotifications = false;
            _updateCardCreate = false;
            _enrollInCompetition = false;

            base.ParentEvents_UpdateDataSuccess(sender, e);
        }

        #endregion //overridden calls

        #region methods

        /// <summary>
        /// Sets the design of the card design
        /// </summary>
        /// <param name="serverSideID"></param>
        public void SetDesign(string serverSideID)
        {
            base.Entity.ServerSideCardDesignIdentifier = serverSideID;
            base.Entity.IsDesigned = true;
        }

        /// <summary>
        /// Acknowledges the denial
        /// </summary>
        public void AcknowledgeDenial()
        {
            base.Entity.IsDenialAck = true;
        }

        /// <summary>
        /// Sets the approval status
        /// </summary>
        /// <param name="isApproved"></param>
        /// <param name="denialReason"></param>
        public void SetApproval(bool isApproved, string denialReason)
        {
            base.Entity.IsApproved = isApproved;
            base.Entity.DenialReason = denialReason;
            base.Entity.IsRetrieved = true;

            if (isApproved)
            {
                _sendApprovedNotifications = true;
                _updateCardCreate = true;
            }
            else
            {
                _sendDeniedNotifications = true;
            }
        }

        /// <summary>
        /// Sets the competition status
        /// </summary>
        /// <param name="inCompetition"></param>
        public void SetCompetitionStatus(bool inCompetition)
        {
            base.Entity.InCompetition = inCompetition;

            if (inCompetition)
            {
                _enrollInCompetition = true;
            }
        }

        /// <summary>
        /// Determines whether a card design is retrieved for the specified server side ID.
        /// </summary>
        /// <param name="serverSideID">The server side ID.</param>
        public static bool IsCardDesignRetrieved(string serverSideID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                CustomCardDesignEntity cardDesign = AdapterFactory.CardDesignsDataAdapter.RetrieveCardDesignRegistration(adapter, serverSideID);
                return (cardDesign != null) ? cardDesign.IsRetrieved : false;
            }
        }


        #endregion //methods

        #region private calls

        /// <summary>
        /// Update any Card Create Jobs with an approval
        /// </summary>
        private bool UpdateCardCreateJobs()
        {
            //First let's check to see if we're a custom card for a teen
            if (CustomCardEmails.Teen != null)
            {
                //Ok this is a teen -- let's go see if there are any Create Card Jobs
                //Modify the Create Card Job to store the new ID
                PrepaidCardAccount card = CustomCardEmails.Teen.FinancialAccounts.GetPrepaidCardAccountByCustomCardDesignID(this.CustomCardDesignID);
                if (card != null)
                {
                    CreateCardJob createCreateJob = card.CardCreateJob;

                    if (createCreateJob.IsLocked.HasValue && createCreateJob.IsLocked.Value == true)
                    {
                        //See if this card create is attached to a locked Credit Card Job, if so unlock it
                        List<JobLink> jobLinks = JobLink.RetrievePrimaryJobLinks(createCreateJob.JobID);
                        foreach (JobLink jobLink in jobLinks)
                        {
                            if (jobLink.PrimaryJob.JobType == JobType.CreditCardTransactionJob)
                            {
                                    //unlock the credit Card transaction
                                jobLink.PrimaryJob.IsLocked = false;
                                jobLink.PrimaryJob.ScheduledStartTime = CreditCardTransactionJob.ScheduleCapture(DateTime.Now, CustomCardEmails.Teen.Site.PrepaidCreditProvider);
                                jobLink.PrimaryJob.SaveJob(jobLink.PrimaryJob.Status);
                            }
                        }

                        createCreateJob.IsLocked = false;
                        createCreateJob.SaveJob(createCreateJob.Status);
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// Sends the appropriate approval notifications to parents and children.
        /// </summary>
        private bool SendApprovedNotifications()
        {
            // If the user is not registered within our system.
            if (CustomCardEmails.IsUnregistered)
            {
                // If they enrolled the card in the competition.
                if (InCompetition)
                {
                    if (CustomCardEmails.InCompetitionOnly)
                    {
                        // If the teen has chosen to be in competition only and not given us a parent email.
                        if (!NotificationService.UnRegisteredCompetitionCardDesignApproved(CustomCardEmails, ServerSideDesignID))
                        {
                            base.WritePostSaveErrorToLog(this.ToString(), "SendApprovedNotifications",
                                string.Format(ERROR_NOTIFICATION_FAILED, "UnRegisteredCompetitionCardDesignApproved", ServerSideDesignID));
                            return false;
                        }
                        else { return true; }
                    }
                    // Send to child
                    if (!NotificationService.PreRegisteredCompetitionCardDesignApproved(CustomCardEmails, ServerSideDesignID))
                    {
                        base.WritePostSaveErrorToLog(this.ToString(), "SendApprovedNotifications",
                            string.Format(ERROR_NOTIFICATION_FAILED, "PreRegisteredCompetitionCardDesignApproved", ServerSideDesignID));
                        return false;
                    }
                    //Send to parent
                    if (!NotificationService.ParentPreRegisteredCompetitionCardDesignApproved(CustomCardEmails, ServerSideDesignID))
                    {
                        base.WritePostSaveErrorToLog(this.ToString(), "SendApprovedNotifications",
                            string.Format(ERROR_NOTIFICATION_FAILED, "ParentPreRegisteredCompetitionCardDesignApproved", ServerSideDesignID));
                        return false;
                    }
                }
                // If they did not enroll in the competition.
                else
                {
                    // Send to child
                    if (!NotificationService.PreRegisteredChildCardDesignApproved(CustomCardEmails, ServerSideDesignID))
                    {
                        base.WritePostSaveErrorToLog(this.ToString(), "SendApprovedNotifications",
                            string.Format(ERROR_NOTIFICATION_FAILED, "PreRegisteredChildCardDesignApproved", ServerSideDesignID));
                        return false;
                    }
                    // Send to parent
                    if (!NotificationService.ParentPreRegisteredChildCardDesignApproved(CustomCardEmails, ServerSideDesignID))
                    {
                        base.WritePostSaveErrorToLog(this.ToString(), "SendApprovedNotifications",
                            string.Format(ERROR_NOTIFICATION_FAILED, "ParentPreRegisteredChildCardDesignApproved", ServerSideDesignID));
                        return false;
                    }
                }
            }
            // If the user is registered within our system.
            else
            {
                // If they have enrolled in the competition.
                if (InCompetition)
                {
                    // TODO: this cannot be done with our current design. A registered user will not
                    // be able to enroll in the competition. will be added in future. MH.
                }
                // If they did not enroll in the competition.
                else
                {
                    if (CustomCardEmails.Teen.Email != null)
                    {
                        // Send to child
                        if (!NotificationService.ChildCardDesignApproved(CustomCardEmails.Parent, CustomCardEmails.Teen))
                        {
                            base.WritePostSaveErrorToLog(this.ToString(), "SendApprovedNotifications",
                                string.Format(ERROR_NOTIFICATION_FAILED, "ChildCardDesignApproved", ServerSideDesignID));
                            return false;
                        }
                    }
                    //Send to parent
                    if (!NotificationService.ParentCardDesignApproved(CustomCardEmails.Parent, CustomCardEmails.Teen))
                    {
                        base.WritePostSaveErrorToLog(this.ToString(), "SendApprovedNotifications",
                            string.Format(ERROR_NOTIFICATION_FAILED, "ParentCardDesignApproved", ServerSideDesignID));
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Sends appropriate denial notifications to parent and children.
        /// </summary>
        private bool SendDeniedNotifications()
        {
            // If the user is unregistered.
            if (CustomCardEmails.IsUnregistered)
            {
                // If the user has signed up for competition.
                if (InCompetition)
                {
                    if (CustomCardEmails.InCompetitionOnly)
                    {
                        // If the teen has chosen to be in competition only and not given us a parent email.
                        if (!NotificationService.UnRegisteredCompetitionCardDesignDenied(CustomCardEmails, ServerSideDesignID, DenialReason))
                        {
                            base.WritePostSaveErrorToLog(this.ToString(), "SendDeniedNotifications",
                                string.Format(ERROR_NOTIFICATION_FAILED, "UnRegisteredCompetitionCardDesignDenied", ServerSideDesignID));
                            return false;
                        }
                        else { return true; }
                    }
                    // Send to child
                    // We dont send a notification to the parent in this case.
                    if (!NotificationService.PreRegisteredCompetitionCardDesignDenied(CustomCardEmails, ServerSideDesignID, DenialReason))
                    {
                        base.WritePostSaveErrorToLog(this.ToString(), "SendDeniedNotifications",
                            string.Format(ERROR_NOTIFICATION_FAILED, "PreRegisteredCompetitionCardDesignDenied", ServerSideDesignID));
                        return false;
                    }
                }
                // If the user has not signed up for competition.
                else
                {
                    // Send to child
                    if (!NotificationService.PreRegisteredChildCardDesignDenied(CustomCardEmails, ServerSideDesignID, DenialReason))
                    {
                        base.WritePostSaveErrorToLog(this.ToString(), "SendDeniedNotifications",
                            string.Format(ERROR_NOTIFICATION_FAILED, "PreRegisteredChildCardDesignDenied", ServerSideDesignID));
                        return false;
                    }
                    // We dont want to send a notification to the parent if the card was denied.
                    // This can be implemented later if needed.
                    //if (!NotificationService.ParentPreRegisteredChildCardDesignDenied(CustomCardEmails, ServerSideDesignID, DenialReason))
                    //{
                    //    return false;
                    //}
                }
            }
            // If they are registered with our system.
            else
            {
                // If they signed up for the competition.
                if (InCompetition)
                {
                    // TODO: this cannot be done with our current design. A registered user will not
                    // be able to enroll in the competition. will be added in future. MH.
                }
                else
                {
                    if (CustomCardEmails.Teen.Email != null)
                    {
                        // Send to child
                        if (!NotificationService.ChildCardDesignDenied(CustomCardEmails.Parent, CustomCardEmails.Teen, DenialReason))
                        {
                            base.WritePostSaveErrorToLog(this.ToString(), "SendDeniedNotifications",
                               string.Format(ERROR_NOTIFICATION_FAILED, "ChildCardDesignDenied", ServerSideDesignID));
                            return false;
                        }
                    }
                    // Send to parent
                    if (!NotificationService.ParentCardDesignDenied(CustomCardEmails.Parent, CustomCardEmails.Teen, DenialReason))
                    {
                        base.WritePostSaveErrorToLog(this.ToString(), "SendDeniedNotifications",
                           string.Format(ERROR_NOTIFICATION_FAILED, "ParentCardDesignDenied", ServerSideDesignID));
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Enrolls the card into the competition
        /// </summary>
        /// <returns></returns>
        private bool EnrollInCompetition()
        {
            if (!SSGProvider.AddPublicDesign(ServerSideDesignID))
            {
                base.WritePostSaveErrorToLog(this.ToString(), "EnrollInCompetition", string.Format(ERROR_ENROLL_IN_COMP_FAILED, ServerSideDesignID));
                return false;
            }
            return true;
        }

        #endregion //private calls

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
            ValidationRules.AddRule<CustomCardDesign>(CheckServerSideIDIsNotDuplicate, "ServerSideDesignID");

            base.AddBusinessRules();
        }

        /// <summary>
        /// Rule for checking that the ServerSide ID does not already exist in the DB
        /// </summary>
        /// <param name="target"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool CheckServerSideIDIsNotDuplicate(CustomCardDesign target, RuleArgs e)
        {
            if (AdapterFactory.CardDesignsDataAdapter.DoesDBContainServerSideID(ServerSideDesignID, CustomCardDesignID))
            {
                e.Description = "Cannot add duplicate ServerSideID: " + ServerSideDesignID;
                return false;
            }
            return true;
        }
        #endregion //Rules
    }
}
