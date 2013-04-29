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
using System.Transactions;

using Payjr.Configuration;

using Payjr.Core.FSV;
using Payjr.Core.Jobs;
using Payjr.Core.Modules;
using Payjr.Core.Providers;
using Payjr.Core.Users;

using Payjr.Entity.EntityClasses;

using Payjr.Types;
using Payjr.Core.Adapters;
using Payjr.Core.Services;
using Payjr.Entity;
using Payjr.Entity.HelperClasses;
using Payjr.Core.Providers.FNBO;


namespace Payjr.Core.BrandingSite
{
    public class Site
    {
        #region Private

        private readonly Guid _brandingID;
        private SiteTags _brandingTags;
        private readonly SiteConfiguration _siteConfig;

        private IPrepaidModule _prepaidModule;
        private TargetModule _targetModule;

        private ExperienceProvider _payjrProvider;

        private IProvider _savingProvider;

        #endregion

        #region Properties

        /// <summary>
        /// Get the Site Tags by branding.
        /// </summary>
        public SiteTags BrandingTags
        {
            get
            {
                if (_brandingTags == null)
                {
                    _brandingTags = new SiteTags(this);
                }
                return _brandingTags;
            }
        }

        /// <summary>
        /// The branding Id for the site
        /// </summary>
        public Guid BrandingID
        {
            get
            {
                return _brandingID;
            }
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name
        {
            get
            {
                return SiteConfig.Name;
            }
        }

        /// <summary>
        /// Url
        /// </summary>
        public string URL
        {
            get
            {
                return SiteConfig.URL;
            }
        }

        /// <summary>
        /// The branding Id for the site
        /// </summary>
        public BrandingEntity BrandingEntity
        {
            get
            {
                return AdapterFactory.BrandingAdapter.RetrieveBranding(_brandingID);
            }
        }

        /// <summary>
        /// The site configuration for this branding.
        /// </summary>
        public SiteConfiguration SiteConfig
        {
            get
            {
                return _siteConfig;
            }
        }

        #region Providers

        /// <summary>
        /// The PAYjr Experience Provider.
        /// </summary>
        public ExperienceProvider PayjrProvider
        {
            get
            {
                if (_payjrProvider == null)
                {
                    _payjrProvider = new ExperienceProvider();
                }
                return _payjrProvider;
            }
        }

        /// <summary>
        /// The Savings Provider
        /// </summary>
        public IProvider SavingProvider
        {
            get
            {
                if (_savingProvider == null)
                {
                    if (SiteConfig != null)
                    {
                        Guid pID = new Guid(SiteConfig.SavingsConfig.ProviderID);
                        _savingProvider = new SavingsProvider(ServiceFactory.BrandingService.RetrieveProviderByID(pID));
                    }
                }
                return _savingProvider;
            }
        }

        /// <summary>
        /// Gets the credit card provider.
        /// (Set this with mocks only)
        /// </summary>
        /// <value>The credit card provider.</value>
        public ICreditCardProvider PrepaidCreditProvider
        {
            get
            {
                if (PrepaidModule != null)
                {
                    return PrepaidModule.CreditProvider;
                }
                return null;
            }
            set
            {
                if(PrepaidModule != null)
                {
                    PrepaidModule.CreditProvider = value;
                }
            }
        }

        public ICreditCardProvider TargetCreditProvider
        {
            get
            {
                if (TargetModule != null)
                {
                    return TargetModule.CreditProvider;
                }
                return null;
            }
            set
            {
                if (TargetModule != null) { TargetModule.CreditProvider = value; }
            }
        }

        public IACHProvider PrepaidACHProvider
        {
            get
            {
                if (PrepaidModule != null)
                {
                    return PrepaidModule.ACHProvider;
                }
                return null;
            }
        }

        public IACHProvider TargetACHProvider
        {
            get
            {
                if (TargetModule != null)
                {
                    return TargetModule.ACHProvider;
                }
                return null;
            }
        }
        ///mimic creditcard provider. (Set this with mocks only)
        public ICardProvider PrepaidProvider
        {
            get
            {
                if (PrepaidModule != null)
                {
                    return PrepaidModule.PrepaidProvider;
                }
                return null;
            }
            set
            {
                if (PrepaidModule != null)
                {
                    PrepaidModule.PrepaidProvider = value;
                }
            }
        }

        /// <summary>
        /// Gets the Target GiftCard provider.
        /// </summary>
        /// <value>The target provider.</value>
        public ITargetGiftCardProvider TargetProvider
        {
            get
            {
                if (TargetModule != null)
                {
                    return TargetModule.TargetProvider;
                }
                return null;
            }
        }

        private string _cardProviderTypeString = null;
        /// <summary>
        /// String representation of the card provider type (for resources)
        /// </summary>
        public string CardProviderTypeString
        {
            get
            {
                if (_cardProviderTypeString == null)
                {
                    if (PrepaidProvider != null)
                    {
                        _cardProviderTypeString = PrepaidProvider.Type.ToString();
                    }
                }
                return _cardProviderTypeString;
            }
        }

        #endregion

        #region Modules

        /// <summary>
        /// The prepaid module
        /// </summary>
        public IPrepaidModule PrepaidModule
        {
            get
            {
                if (_prepaidModule == null)
                {
                    _prepaidModule = PrepaidModuleFactory.Instance.RetrievePrepaidModuleByBranding(_brandingID);
                }
                return _prepaidModule;
            }
            set
            {
                if (PrepaidModule != null) throw new InvalidOperationException("Site already has a PrepaidModule.");
                _prepaidModule = value;
            }
        }

        public TargetModule TargetModule
        {
            get
            {
                if (_targetModule == null)
                {
                    _targetModule = TargetModule.RetrieveTargetModuleByBranding(_brandingID);
                }
                return _targetModule;
            }
            set
            {
                if (TargetModule != null) throw new InvalidOperationException("Site already has a TargetModule.");
                _targetModule = value;
            }
        }

        #endregion

        #endregion //Properties

        #region Constructors

        /// <summary>
        /// Get the site specifics using the url
        /// </summary>
        /// <param name="url"></param>
        public Site(Uri url)
        {
            _siteConfig = SiteManager.GetSiteConfiguration(url);
            if (_siteConfig == null)
            {
                throw new ArgumentException("Site Configuration was not found for the supplied URL.  (" + url + ")");
            }

            _brandingID = new Guid(_siteConfig.ID);
        }

        /// <summary>
        /// Get the site specifics using the branding id.
        /// </summary>
        /// <param name="brandingID"></param>
        public Site(Guid brandingID)
        {
            if (brandingID == Guid.Empty) throw new ArgumentException("brandingID", "The brandingID should not be empty when creating the site object");

            _siteConfig = SiteManager.GetSiteConfiguration(brandingID);
            _brandingID = brandingID;

        }

        #endregion //Constructors

        #region Methods

        #region Modules

        #region Modules->Retrieve Methods

        /// <summary>
        /// Retrieves all the modules of the given branding
        /// </summary>
        /// <param name="brandingID">Branding ID</param>
        /// <returns></returns>
        public List<Module> RetrieveModules()
        {
            return Module.RetrieveModulesByBranding(_brandingID);
        }

        /// <summary>
        /// Retrieves the min transfer amount supported by this branding for this product.
        /// </summary>
        public Decimal RetrieveMinTransferAmount(Product product)
        {
            SiteConfiguration config = SiteManager.GetSiteConfiguration(_brandingID);
            return config.GetMinTransferAmount(product);
        }

        /// <summary>
        /// Retrieves the Max transfer amount supported by this branding for this product.
        /// </summary>
        public Decimal RetrieveMaxTransferAmount(Product product)
        {
            SiteConfiguration config = SiteManager.GetSiteConfiguration(_brandingID);
            return config.GetMaxTransferAmount(product);
        }

        /// <summary>
        /// Get the max initial load amount using the branding id
        /// </summary>
        /// <param name="brandingID">the branding id</param>
        /// <returns>Decimal</returns>
        public Decimal RetrieveMaxInitialLoadAmountbyBranding()
        {
            IPrepaidModule module = PrepaidModule;

            if (module != null)
            {
                return module.InitialLoadAmount;
            }
            else
            {
                return 0.00M;
            }

        }

        /// <summary>
        /// Return true if the branding included the Prepaid standard branding product
        /// </summary>
        /// <param name="brandingID"></param>
        /// <returns></returns>
        public bool IsBrandingPrepaid()
        {
            EntityCollection<ProductBrandingEntity> products = ServiceFactory.BrandingService.RetrieveProductsAssignedToBranding(_brandingID);

            foreach (ProductBrandingEntity product in products)
            {
                if (product.ProductNumber == Product.PPaid_Standard_Branding_I)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Return true if the branding included the Target GiftCard standard branding product
        /// </summary>
        /// <param name="brandingID"></param>
        /// <returns></returns>
        public bool IsBrandingTarget()
        {
            EntityCollection<ProductBrandingEntity> products = ServiceFactory.BrandingService.RetrieveProductsAssignedToBranding(_brandingID);

            foreach (ProductBrandingEntity product in products)
            {
                if (product.ProductNumber == Product.Target_Standard_Branding_I)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Determines whether a given branding/stie supports the savings programs
        /// </summary>
        public bool IsBrandingSavings()
        {
            SiteConfiguration config = SiteManager.GetSiteConfiguration(_brandingID);
            if (config != null)
            {
                return config.SupportsSavings;
            }

            return false;
        }

        #endregion//Modules->Retreive Methods

        #endregion //Modules

        /// <summary>
        /// Get the default theme fro the branding
        /// </summary>
        /// <param name="branding"></param>
        /// <returns></returns>
        public ThemeEntity GetDefaultTheme()
        {
            EntityCollection<BrandingThemeEntity> brandingThemes = ServiceFactory.BrandingService.RetrieveThemesAssignedToBranding(_brandingID);

            foreach (BrandingThemeEntity brandingTheme in brandingThemes)
            {
                if (brandingTheme.IsDefault)
                {
                    return ServiceFactory.BrandingService.RetrieveTheme(brandingTheme.ThemeId);
                }

            }

            return null;
        }

        /// <summary>
        /// Get the culture
        /// </summary>
        /// <returns></returns>
        public CultureEntity GetCulture()
        {
            return ServiceFactory.BrandingService.RetrieveAllCultures()[0];
        }

        public Payjr.Core.Providers.Provider GetCardProvider(User user)
        {
            if (PrepaidModule != null)
            {
                return FSVCardProvider.RetrieveFSVCardProvider(PrepaidModule.PrepaidProviderID, user);
            }
            return null;
        }

        /// <summary>
        /// Gets the IACHProvider configured for this branding
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public ACHProvider GetPrepaidACHProvider(User user)
        {
            if (PrepaidModule != null)
            {
                if (PrepaidModule.ACHProviderID.HasValue)
                {
                    return FNBOACHProvider.RetrieveFNBOACHProvider(PrepaidModule.ACHProviderID.Value, user);
                }
            }
            return null;
        }

        /// <summary>
        /// Gets the site document.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        public SiteDocument GetSiteDocument(SiteDocument.SiteDocumentType type)
        {
            if (BrandingEntity.SiteGroupId.HasValue)
            {
                return SiteDocument.GetSiteDocument(BrandingEntity.SiteGroupId.Value, type);
            }
            return null;
        }

        public static ProductDescription GetProductDescription(Product product)
        {
            System.Reflection.FieldInfo fieldInfo = product.GetType().GetField(product.ToString());
            ProductDescription[] attributes =
               (ProductDescription[])fieldInfo.GetCustomAttributes
               (typeof(ProductDescription), false);
            return (attributes.Length > 0) ? attributes[0] : null;
        }


        #endregion //Methods
    }
}
