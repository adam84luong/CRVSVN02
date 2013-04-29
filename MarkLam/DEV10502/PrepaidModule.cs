using System;
using Payjr.Entity.EntityClasses;
using Payjr.Entity;
using Payjr.Core.Providers;
using Payjr.Core.Providers.FNBO;
using Payjr.Core.FSV;

namespace Payjr.Core.Modules
{
    /// <summary>
    /// The Prepaid Interface
    /// </summary>
    public interface IPrepaidModule
    {
        Guid? ACHProviderID { get; set; }
        IACHProvider ACHProvider { get; }
        Guid PrepaidProviderID { get; set; }
        /// <summary>
        /// (Set this with mocks only)
        /// </summary>
        ICardProvider PrepaidProvider { get; set; }
        Guid? CreditProviderID { get; set; }

        /// <summary>
        /// Gets or sets the credit provider.
        /// (Set this with mocks only)
        /// </summary>
        /// <value>The credit provider.</value>
        ICreditCardProvider CreditProvider { get; set; }

        short NumberACHTransactionsBeforeAccountChanges { get; set; }

        /// <summary>
        /// Gets/Sets the number of failures within 30 days before a credit card
        /// account gets set to BAD.
        /// </summary>
        short FailedCreditLimit { get; set; }

        Decimal InitialLoadAmount { get; set; }
        Decimal MinLoadAmount { get; set; }
        Decimal MaxLoadAmount { get; set; }
        Decimal MaxCardAmount { get; set; }

        short ACHLeadTimeInDays { get; // Might need adjustment to check account status values if they're actually used
            set; }

        AccountStatus ACHLeadTimeBankAccountStatus { get; set; }
        short MaxFreeUserACHPerMonth { get; set; }
        short? DowngradingWaitingPeriod { get; set; }
        Guid? HandoverKey { get; set; }
        Guid? CompetitionKey { get; set; }
        bool? ParticipatesInCompetition { get; set; }
        bool HasLimitedRouting { get; set; }
        string CardTemplateName { get; set; }
        string CardTemplateType { get; set; }
        bool SupportsEmergencyLoads { get; set; }
        decimal MaxEmergencyLoadAmount { get; set; }
        Guid ModuleID { get; }
        Guid BrandingID { get; }
        ModuleType Type { get; }
        bool IsEnabled { get; set; }
        void Save();
    }

    public class PrepaidModule : Module, IPrepaidModule
    {
        #region Fields

        private PrepaidModuleEntity _moduleEntity
        {
            get { return (PrepaidModuleEntity)ModuleEntity; }
        }

        private IACHProvider achProvider = null;
        private ICreditCardProvider creditProvider = null;
        private ICardProvider prepaidProvider = null;

        #endregion

        #region Properties

        public Guid? ACHProviderID
        {
            get { return _moduleEntity.FundingProviderId; }
            set { _moduleEntity.FundingProviderId = value; }
        }

        public IACHProvider ACHProvider
        {
            get
            {
                if (achProvider == null && _moduleEntity.FundingProviderId.HasValue)
                {
                    achProvider = FNBOACHProvider.RetrieveFNBOACHProvider(_moduleEntity.FundingProviderId.Value);
                }
                return achProvider;
            }
        }

        public Guid PrepaidProviderID
        {
            get { return _moduleEntity.DestinationProviderId; }
            set { _moduleEntity.DestinationProviderId = value; }
        }

        public ICardProvider PrepaidProvider
        {
            get
            {
                if (prepaidProvider == null)
                {
                    prepaidProvider = FSVCardProvider.RetrieveFSVCardProvider(_moduleEntity.DestinationProviderId);
                }
                return prepaidProvider;
            }
            set { prepaidProvider = value; }
        }

        public Guid? CreditProviderID
        {
            get { return _moduleEntity.CreditCardProviderId; }
            set { _moduleEntity.CreditCardProviderId = value; }
        }

        /// <summary>
        /// Gets or sets the credit provider.
        /// (Set this with mocks only)
        /// </summary>
        /// <value>The credit provider.</value>
        public ICreditCardProvider CreditProvider
        {
            get
            {
                if (creditProvider == null && _moduleEntity.CreditCardProviderId.HasValue)
                {
                    creditProvider = CreditCardProvider.RetrieveCreditProvider(_moduleEntity.CreditCardProviderId.Value);
                }
                return creditProvider;
            }
            set
            {
                creditProvider = value;
            }
        }

        public short NumberACHTransactionsBeforeAccountChanges
        {
            get { return _moduleEntity.AchnumberTransactionsBeforeAccountChange; }
            set { _moduleEntity.AchnumberTransactionsBeforeAccountChange = value; }
        }

        /// <summary>
        /// Gets/Sets the number of failures within 30 days before a credit card
        /// account gets set to BAD.
        /// </summary>
        public short FailedCreditLimit
        {
            get
            {
                return _moduleEntity.FailedCreditLimit;
            }
            set
            {
                _moduleEntity.FailedCreditLimit = value;
            }
        }

        public Decimal InitialLoadAmount
        {
            get { return _moduleEntity.InitialLoadAmount; }
            set { _moduleEntity.InitialLoadAmount = value; }
        }

        public Decimal MinLoadAmount
        {
            get { return _moduleEntity.MinLoadAmount; }
            set { _moduleEntity.MinLoadAmount = value; }
        }

        public Decimal MaxLoadAmount
        {
            get { return _moduleEntity.MaxLoadAmount; }
            set { _moduleEntity.MaxLoadAmount = value; }
        }

        public Decimal MaxCardAmount
        {
            get { return _moduleEntity.MaxCardAmount; }
            set { _moduleEntity.MaxCardAmount = value; }
        }

        private AchleadTimeEntity ACHLeadTime
        {
            get
            {
                if (_moduleEntity.AchleadTimes.Count == 0)
                {
                    //Create a new ACHLeadTimeEntity and add it
                    AchleadTimeEntity achLeadTime = new AchleadTimeEntity();
                    achLeadTime.PrepaidModuleId = _moduleEntity.PrepaidModuleId;
                    achLeadTime.BankAccountStatus = AccountStatus.AllowMoneyMovement;
                    achLeadTime.LeadTimeinDays = 0;
                    _moduleEntity.AchleadTimes.Add(achLeadTime);
                }
                return _moduleEntity.AchleadTimes[0];
            }
        }

        public short ACHLeadTimeInDays
        {
            get { return ACHLeadTime.LeadTimeinDays; } // Might need adjustment to check account status values if they're actually used
            set { ACHLeadTime.LeadTimeinDays = value; }
        }

        public AccountStatus ACHLeadTimeBankAccountStatus
        {
            get { return ACHLeadTime.BankAccountStatus; }
            set { ACHLeadTime.BankAccountStatus = value; }
        }

        public short MaxFreeUserACHPerMonth
        {
            get { return _moduleEntity.MaxFreeUserAchperMonth; }
            set { _moduleEntity.MaxFreeUserAchperMonth = value; }
        }

        public short? DowngradingWaitingPeriod
        {
            get { return _moduleEntity.DowngradingWaitingPeriod; }
            set { _moduleEntity.DowngradingWaitingPeriod = value; }
        }

        public Guid? HandoverKey
        {
            get { return _moduleEntity.ServerSideId; }
            set { _moduleEntity.ServerSideId = value; }
        }

        public Guid? CompetitionKey
        {
            get { return _moduleEntity.CompetitionKey; }
            set { _moduleEntity.CompetitionKey = value; }
        }

        public bool? ParticipatesInCompetition
        {
            get { return _moduleEntity.ParticipateInCompetition; }
            set { _moduleEntity.ParticipateInCompetition = value; }
        }

        public bool HasLimitedRouting
        {
            get { return _moduleEntity.HasLimitedRouting; }
            set { _moduleEntity.HasLimitedRouting = value; }
        }

        public string CardTemplateName
        {
            get
            {
                if (_moduleEntity.CardTemplate == null)
                {
                    return null;
                }
                return _moduleEntity.CardTemplate.Name;
            }
            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    if (_moduleEntity.CardTemplate == null)
                    {
                        _moduleEntity.CardTemplate = new CardTemplateEntity();
                    }
                    _moduleEntity.CardTemplate.Name = value;
                }
            }
        }

        public string CardTemplateType
        {
            get
            {
                if (_moduleEntity.CardTemplate == null)
                {
                    return null;
                }
                return _moduleEntity.CardTemplate.CardType;
            }
            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    if (_moduleEntity.CardTemplate == null)
                    {
                        _moduleEntity.CardTemplate = new CardTemplateEntity();
                    }
                    _moduleEntity.CardTemplate.CardType = value;
                }
            }
        }

        public bool SupportsEmergencyLoads
        {
            get
            {
                return _moduleEntity.SupportsEmergencyLoads;
            }
            set
            {
                _moduleEntity.SupportsEmergencyLoads = value;
            }
        }

        public decimal MaxEmergencyLoadAmount
        {
            get
            {
                return _moduleEntity.MaxEmergencyLoad;
            }
            set
            {
                _moduleEntity.MaxEmergencyLoad = value;
            }
        }

        #endregion

        #region Constructors

        public PrepaidModule
            (
            Guid brandingID,
            Guid achProviderID,
            Guid cardProviderID,
            Guid creditCardProviderID,
            short numberACHTransactionsBeforeAccountChanges,
            short FailedCreditLimit,
            Decimal initialLoadAmount,
            Decimal minLoadAmount,
            Decimal maxLoadAmount,
            Decimal maxCardAmount,
            short goodACHLeadTime,
            short maxFreeUserACHPerMonth,
            short downgradingWaitingPeriod,
            Guid? handoverKey,
            Guid? competitionKey,
            bool participatesInCompetition,
            bool hasLimitedRouting,
            string templateName,
            string templateType,
            bool supportsEmergencyLoads,
            decimal maxEmergencyLoadAmount
            ) : base(brandingID)
        {
            CardTemplateEntity cardTemplate = new CardTemplateEntity();

            _moduleEntity.DestinationProviderId = cardProviderID;
            _moduleEntity.FundingProviderId = achProviderID;
            _moduleEntity.AchnumberTransactionsBeforeAccountChange = numberACHTransactionsBeforeAccountChanges;
            _moduleEntity.FailedCreditLimit = FailedCreditLimit;
            _moduleEntity.InitialLoadAmount = initialLoadAmount;
            _moduleEntity.MinLoadAmount = minLoadAmount;
            _moduleEntity.MaxLoadAmount = maxLoadAmount;
            _moduleEntity.MaxCardAmount = maxCardAmount;
            _moduleEntity.MaxFreeUserAchperMonth = maxFreeUserACHPerMonth;
            _moduleEntity.DowngradingWaitingPeriod = downgradingWaitingPeriod;
            _moduleEntity.ParticipateInCompetition = participatesInCompetition;
            _moduleEntity.HasLimitedRouting = hasLimitedRouting;
            // Credit card specific entries.
            _moduleEntity.CreditCardProviderId = creditCardProviderID;
            _moduleEntity.SupportsEmergencyLoads = supportsEmergencyLoads;
            _moduleEntity.MaxEmergencyLoad = maxEmergencyLoadAmount;

            _moduleEntity.ServerSideId = handoverKey;
            _moduleEntity.CompetitionKey = competitionKey;

            // If they have specified a card template.
            if (!string.IsNullOrEmpty(templateName) && (!string.IsNullOrEmpty(templateType)))
            {
                // assign the template to the prepaid module.
                cardTemplate.Name = templateName;
                cardTemplate.CardType = templateType;
                _moduleEntity.CardTemplate = cardTemplate;
            }

            //Create a new ACHLeadTimeEntity and add it
            AchleadTimeEntity achLeadTime = new AchleadTimeEntity();
            achLeadTime.BankAccountStatus = AccountStatus.AllowMoneyMovement;
            achLeadTime.LeadTimeinDays = goodACHLeadTime;
            _moduleEntity.AchleadTimes.Add(achLeadTime);
        }

        public PrepaidModule(PrepaidModuleEntity moduleEntity) : base(moduleEntity) { }

        #endregion

        #region Methods

        protected override ModuleEntity CreateEntityForSave()
        {
            PrepaidModuleEntity entity = new PrepaidModuleEntity();
            entity.ModuleType = ModuleType.Prepaid;
            return entity;
        }

        #endregion
    }

}
