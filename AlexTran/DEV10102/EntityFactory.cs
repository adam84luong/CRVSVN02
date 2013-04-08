using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.SqlClient;
using Aspose.iCalendar;
using Common.CreditGateway;
using Common.CreditGateway.CyberSource.Internals;
using Common.Types;
using Common.Util;
using Common.Util.Time;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Payjr.Configuration;
using Payjr.Core;
using Payjr.Core.Adapters;
using Payjr.Core.BrandingSite;
using Payjr.Core.FinancialAccounts;
using Payjr.Core.FinancialAccounts.Interfaces;
using Payjr.Core.Modules;
using Payjr.Core.Notifications;
using Payjr.Core.Providers;
using Payjr.Core.Providers.FNBO;
using Payjr.Core.Services;
using Payjr.Core.Strategies.Registration;
using Payjr.Core.UserInfo;
using Payjr.Core.Users;
using Payjr.Entity;
using Payjr.Entity.DatabaseSpecific;
using Payjr.Entity.EntityClasses;
using Payjr.Entity.FactoryClasses;
using Payjr.Entity.HelperClasses;
using Payjr.Types;
using RMock = Rhino.Mocks;
using SD.LLBLGen.Pro.ORMSupportClasses;

namespace Payjr.Util.Test
{
    public class FileAuditRecord
    {
        int _numberReceivedRecords;
        public int NumberReceivedRecords
        {
            get { return _numberReceivedRecords; }
            set { _numberReceivedRecords = value; }
        }
        int _numberProcessedRecords;
        public int NumberProcessedRecords
        {
            get { return _numberProcessedRecords; }
            set { _numberProcessedRecords = value; }
        }
        ProcessAuditStatus _status;
        public Payjr.Entity.ProcessAuditStatus Status
        {
            get { return _status; }
            set { _status = value; }
        }
        public FileAuditRecord(int numberReceivedRecords, int numberProcessedRecords, ProcessAuditStatus status)
        {
            _numberReceivedRecords = numberReceivedRecords;
            _numberProcessedRecords = numberProcessedRecords;
            _status = status;
        }

    };

    /// <summary>
    /// Class for creating entity
    /// </summary>
    public static class TestEntityFactory
    {
        const string CARD_NUMBER1 = "013156484984651";
        const string CARD_NUMBER2 = "056489484651312";
        const string CARD_NUMBER3 = "093720284738383";
        const string RA_NUMBER1 = "9483765892";
        const string RA_NUMBER2 = "3849202875";
        const long PO_NUMBER1 = 92843848429487;
        const long PO_NUMBER2 = 23957398517210;
        const decimal BALANCE_1 = 12.00M;
        const decimal BALANCE_2 = 29.00M;

        public const decimal INITIAL_MAXEMERGENCYLOAD = 73.75M;

        private const string DEFAULT_DOMAIN = "localhost:8080";
        public const string POLICY_VERSION = "1.0";

        public static string Connection = @"Provider=SQLOLEDB.1;Password=payjrpassword;Persist Security Info=True;User ID=sa;Initial Catalog=PayjrUnitTest;Data Source=(local);Current Language=us_english";
        public static string Catalog = "PayjrUnitTest";

        public delegate void BeforeEntitySave<T>(T entity);


        public static void OverridePrepaidModuleProvidersWithMocks(BrandingEntity branding, PrepaidModule prepaidModule)
        {
            Mock<IPrepaidModuleFactory> prepaidModuleFactoryMock;
            Mock<IPrepaidModule> prepaidModuleMock;


            prepaidModuleMock = new Mock<IPrepaidModule>();
            prepaidModuleFactoryMock = new Mock<IPrepaidModuleFactory>();
            prepaidModuleFactoryMock.Setup(x => x.RetrievePrepaidModuleByBranding(branding.BrandingId)).Returns(prepaidModuleMock.Object);

            var mockRepository = new RMock.MockRepository();
            ICreditCardProvider creditCardProvider = MockCreator.CreateMockICreditCardProviderWithModule(mockRepository, prepaidModule);
            ICardProvider cardProvider = MockCreator.CreateMockICardProviderWithPrepaidModule(mockRepository, prepaidModule);
            IACHProvider achProvider = MockCreator.CreateMockACHProvider(mockRepository, prepaidModule);


            prepaidModuleMock.Setup(x => x.CreditProvider).Returns(creditCardProvider);
            prepaidModuleMock.Setup(x => x.PrepaidProvider).Returns(cardProvider);
            prepaidModuleMock.Setup(x => x.ACHProvider).Returns(achProvider);

            mockRepository.ReplayAll();

            PrepaidModuleFactory.Instance = prepaidModuleFactoryMock.Object;

        }


        public static CultureEntity CreateCulture(string name)
        {
            using (DataAccessAdapter adapter = CreateAdapter())
            {
                CultureEntity entity = new CultureEntity();
                entity.Culture = name;
                entity.CultureDescription = "Description: ";
                entity.CultureId = Guid.NewGuid();

                adapter.SaveEntity(entity, true);

                return entity;
            }

        }

        public static CategoryEntity CreateCategory(string name)
        {
            using (DataAccessAdapter adapter = CreateAdapter())
            {
                CategoryEntity entity = new CategoryEntity();
                entity.Name = name;
                entity.FaqcategoryId = Guid.NewGuid();

                adapter.SaveEntity(entity, true);

                return entity;
            }

        }

        public static ThemeEntity CreateTheme(string name)
        {
            using (DataAccessAdapter adapter = CreateAdapter())
            {
                ThemeEntity entity = new ThemeEntity();
                entity.Name = name;
                entity.Description = "Description: " + name;
                entity.ThemeId = Guid.NewGuid();
                entity.MasterAdminMasterPageName = "foo";
                entity.AdminMasterPageName = "foo";
                entity.ParentMasterPageName = "parent";
                entity.TeenMasterPageName = "teen";
                entity.GiftGiverMasterPageName = "giftgiver";
                entity.ImagesDirectory = "images";

                adapter.SaveEntity(entity, true);

                return entity;
            }
        }

        public static BrandingEntity CreateBranding(string name)
        {
            return CreateBranding(name, null);
        }

        public static BrandingEntity CreateBrandingFromConfig(string domainName)
        {
            SiteManager.ReloadSites(false);

            SiteConfiguration site = SiteManager.GetSiteConfiguration(new Uri("http://" + domainName));

            if (site == null)
            {
                throw new Exception("Site config is not configured correctly for domain name:" + domainName);
            }


            using (DataAccessAdapter adapter = CreateAdapter())
            {
                BrandingEntity entity = new BrandingEntity();
                entity.BrandingId = new Guid(site.ID);
                entity.Name = site.Name;
                entity.DefaultDomain = domainName;
                entity.ReferralPrefix = "PAYj";
                adapter.SaveEntity(entity, true);

                return entity;

            }
        }

        public static BrandingEntity CreateBranding(string name, BeforeEntitySave<BrandingEntity> beforeSave)
        {


            using (DataAccessAdapter adapter = CreateAdapter())
            {
                BrandingEntity entity = new BrandingEntity();
                entity.BrandingId = new Guid("4CD4856E-4DBC-4D8B-A74C-B0F375CC1CFB");
                entity.Name = name;
                entity.DefaultDomain = GenerateUniqueName();
                entity.ReferralPrefix = "PAYj";

                if (beforeSave != null)
                {
                    beforeSave(entity);
                }

                adapter.SaveEntity(entity, true);

                return entity;

            }
        }

        public static BrandingCardDesignEntity CreateBrandingCardDesignEntity(Guid brandingID, string designCardNumber)
        {
            //CreateCardDesign
            byte[] data = new byte[1];
            data[0] = 1;
            PictureType pictureType = PictureType.Png;
            PictureEntity pictureEntity;
            Error error;
            StockCardDesignEntity cardDesignEntity;
            ServiceFactory.BrandingService.CreatePicture(data, pictureType, out pictureEntity, out error);
            ServiceFactory.BrandingService.CreateCardDesign(pictureEntity.PictureId, "shortName", "description", out cardDesignEntity, out error);

            //Create Branding Card Designs
            Guid cardDesignID = cardDesignEntity.CardDesignId;
            BrandingCardDesignEntity brandingCardDesignEntity;
            ServiceFactory.BrandingService.CreateBrandingCardDesign(cardDesignID, brandingID, designCardNumber, out brandingCardDesignEntity, out error);

            return brandingCardDesignEntity;
        }

        public static BrandingCardDesignEntity CreateBrandingCardDesignEntity(Guid brandingID)
        {
            return CreateBrandingCardDesignEntity(brandingID, "01");
        }

        public static CustomCardDesign CreateUserCustomCardDesign(Teen teen, string serversideID)
        {
            CustomCardDesign customCardDesign = teen.NewCustomCardDesign();
            customCardDesign.SetDesign(serversideID);
            customCardDesign.SetApproval(true, string.Empty);
            customCardDesign.CreateNewPicture();

            byte[] data = new byte[1];
            data[0] = 1;
            customCardDesign.Picture.Image = data;
            customCardDesign.Picture.PictureType = PictureType.Jpeg;

            Assert.IsTrue(teen.Save(null));
            return customCardDesign;
        }

        public static AlertEntity CreateNotificationPreference(EmailEntity email, NotificationType type, BeforeEntitySave<AlertEntity> beforeUpdate)
        {
            using (DataAccessAdapter adapter = CreateAdapter())
            {
                AlertEntity entity = new AlertEntity();

                entity.Email = email;
                entity.EmailNotificationPrefId = Guid.NewGuid();
                entity.NotificationType = type;
                entity.StartWindowTime = "00";
                entity.EndWindowTime = "00";

                if (beforeUpdate != null)
                {
                    beforeUpdate(entity);

                }
                adapter.SaveEntity(entity);

                return entity;
            }

        }

        public static RecurrencePattern CreateDailyPattern()
        {
            /*
                           Daily Forever:
                           
                  DTSTART;TZID=US-Eastern:19970902T090000
                  RRULE:FREQ=DAILY
                           
                  ==> (1997 9:00 AM EDT)September 2-30;October 1-25
                      (1997 9:00 AM EST)October 26-31;November 1-30;December 1-23
                           */
            string stringPattern = "DTSTART;TZID=US-Eastern:19970902T090000\n";
            stringPattern += "RRULE:FREQ=DAILY";

            RecurrencePattern pattern = new RecurrencePattern(stringPattern);
            return pattern;
        }

        public static UserCancellationEntity CreateUserCancellationEntry(Guid userID)
        {
            UserCancellationEntity userCancellation = new UserCancellationEntity();
            userCancellation.TeenId = userID;
            userCancellation.CancellationDate = DateTime.Now;
            userCancellation.CancelledById = userID;

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                adapter.SaveEntity(userCancellation, true);
            }

            return userCancellation;
        }

        public static void CheckFileAuditStatus(int numberReceivedRecords, int numberProcessedRecords, ProcessAuditStatus status)
        {
            List<FileAuditRecord> records = new List<FileAuditRecord>(1);
            records.Add(new FileAuditRecord(numberReceivedRecords, numberProcessedRecords, status));

            CheckFileAuditStatus(records);
        }

        public static void CheckFileAuditStatus(List<FileAuditRecord> records)
        {
            List<FileAuditRecord> foundRecords = new List<FileAuditRecord>();

            //Check file audit
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                EntityCollection<FileProcessingAuditEntity> fileAudits = new EntityCollection<FileProcessingAuditEntity>(new FileProcessingAuditEntityFactory());
                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                IPrefetchPath2 path = new PrefetchPath2((int)EntityType.FileProcessingAuditEntity);

                adapter.FetchEntityCollection(fileAudits, bucket, path);

                if (records.Count != fileAudits.Count)
                {
                    Assert.Fail("The number of Files expected was " + records.Count + " but received was " + fileAudits.Count);
                }

                foreach (FileProcessingAuditEntity fileProcessed in fileAudits)
                {
                    FileAuditRecord foundRecord = null;

                    foreach (FileAuditRecord record in records)
                    {
                        if (record.NumberProcessedRecords == fileProcessed.AppliedRecords &&
                            record.NumberReceivedRecords == fileProcessed.ReceivedRecords &&
                            record.Status == fileProcessed.FileStatus)
                        {
                            foundRecords.Add(record);
                            foundRecord = record;
                            break;
                        }
                    }
                    if (foundRecord != null)
                    {
                        records.Remove(foundRecord);
                    }
                }

                if (records.Count != 0)
                {
                    Assert.Fail("Failed to create the correct number of files");
                }


            }
        }

        public static void CreateExpereienceProvider()
        {
            //this is bad I know, but you can't just save a provider entity
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.AppSettings["ConnectionString"]))
            {
                connection.Open();
                string queryString = @"INSERT INTO [dbo].[Provider]([ProviderID], [Name], [Comment], [Type])
                    Values('" + Guid.NewGuid() + @"', 'Experience Provider', 'Added as part of 2.8.1 DB migration', 0)";

                SqlCommand command = new SqlCommand(queryString, connection);
                command.ExecuteNonQuery();
            }
        }

        public static void CreateZeroAmountProvider()
        {
            //this is bad I know, but you can't just save a provider entity
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.AppSettings["ConnectionString"]))
            {
                connection.Open();
                string queryString = @"INSERT INTO [dbo].[Provider]([ProviderID], [Name], [Comment], [Type])
                    Values('" + Guid.NewGuid() + @"', 'Zero Amount Provider', 'Added as part of 2.8.1 DB migration', 9)";

                SqlCommand command = new SqlCommand(queryString, connection);
                command.ExecuteNonQuery();
            }
        }

        public static void CreateSavingsProvider()
        {
            //this is bad I know, but you can't just save a provider entity
            using (SqlConnection connection = new SqlConnection(ConfigurationManager.AppSettings["ConnectionString"]))
            {
                connection.Open();
                //Provider ID comes from config file
                string queryString = @"INSERT INTO [dbo].[Provider]([ProviderID], [Name], [Comment], [Type])
                    Values('291138b4-44b1-4f31-b028-e6c579ba08ae', 'Savings Provider', 'Added as part of 2.8.1 DB migration', 5)";

                SqlCommand command = new SqlCommand(queryString, connection);
                command.ExecuteNonQuery();
            }
        }

        public static PrepaidModule CreatePrepaidModule(Guid brandingID)
        {
          return  CreatePrepaidModule(brandingID, ProviderType.FSVMetaBankProvider);
        }

        public static PrepaidModule CreatePrepaidModule(Guid brandingID, ProviderType type)
        {
            Site site = new Site(brandingID);

            Error error = null;
            Guid billingBankAccount;
            ServiceFactory.BrandingService.CreateProviderBankAccount("Billing", "ShortName", "1234", "111924392", BankAccountType.Checking, out billingBankAccount, out error);

            Guid sweepBankAccount;
            ServiceFactory.BrandingService.CreateProviderBankAccount("Sweep", "ShortName", "1234", "111924392", BankAccountType.Checking, out sweepBankAccount, out error);

            Guid challengeBankAccount;
            ServiceFactory.BrandingService.CreateProviderBankAccount("challenge", "ShortName", "1234", "111924392", BankAccountType.Checking, out challengeBankAccount, out error);

            Guid finalBankAccount;
            ServiceFactory.BrandingService.CreateProviderBankAccount("final", "ShortName", "1234", "111924392", BankAccountType.Checking, out finalBankAccount, out error);

            FNBOACHProvider achProvider = new FNBOACHProvider(
                "FNBOACHProvider" + Guid.NewGuid().ToString().Substring(0, 11),
                "Comment",
                "userName",
                "password",
                "webservice",
                "companyId",
                "origin",
                "origFinan",
                "123",
                "123",
                billingBankAccount,
                sweepBankAccount,
                challengeBankAccount,
                finalBankAccount,
                "LoadIncomfingFundsTaxID",
                "FinalTaxID",
                "BillingTaxID",
                "SweepTaxID",
                "ChallengeTaxID"
                );
            achProvider.Save();

            if (type == ProviderType.FSVMetaBankProvider)
            {
                ServiceFactory.BrandingService.CreateFSVCardProvider("FSVCardProvider" + Guid.NewGuid().ToString().Substring(0, 11), "comment", "payjrdev", "jr06dev", "https://www.fsvsecurecard.com/fsv/fsvremote/fsvremote.WSDL", "2342",
                    "7050894", "1100", 12, "4464820000005910", "9070", "4464820000005910", "9070", "4464820000005910", "9070", ProviderType.FSVMetaBankProvider, true, 100, 1000, 1000, 5000, Guid.NewGuid(), Guid.NewGuid(), out error);
            }

            else if (type == ProviderType.FSVBankFirstProvider)
            {
                ServiceFactory.BrandingService.CreateFSVCardProvider("FSVBankFirstCardProvider", "comment", "payjrdev", "jr06dev", "https://www.fsvsecurecard.com/fsv/fsvremote/fsvremote.WSDL", "2342",
                    "8318203", "1100", 12, "4464820000000390", "9070", "4464820000000390", "9070", "4464820000000390", "9070", ProviderType.FSVBankFirstProvider, true, 100, 1000, 1000, 5000, Guid.NewGuid(), Guid.NewGuid(), out error);
            }

            ServiceFactory.BrandingService.CreateCreditCardProvider("CreditCardProvider", "comment", "payjunior",
                "W0+d3+qEvGSfA9uClMTwSVg67gjzoChW7XjWBsbkUKhQ5kboScyjZZLlWlwiuHnTEbwQmjqsD+xKqFYnsX2u36ZZiMBLGklHYhvFAi1FaV5wuk9ulfRwKLb8ryELHAJWxkefDetbnzEh3OkkWbjs5j8W8joSDQY8/39/DKe3XThUZ+aexqfsjCqQtOA8PztJWDruCPOgKFbteNYGxuRQqFDmRuhJzKNlkuVaXCK4edMRvBCaOqwP7EqoViexfa7fplmIwEsaSUdiG8UCLUVpXnC6T26V9HAotvyvIQscAlbGR58N61ufMSHc6SRZuOzmPxbyOhINBjz/f38Mp7ddOA==",
                "payjunior", 1000, 3, DateTime.Now.Date,
                3, DateTime.Now.Date.AddHours(11), 3, DateTime.Now.Date.AddHours(11), 3, DateTime.Now.Date, DateTime.Today.AddHours(17));

            EntityCollection<ProviderEntity> providers = ServiceFactory.BrandingService.RetrieveAllProviders();
            Guid ACHProviderId = Guid.Empty;
            Guid CardProviderId = Guid.Empty;
            Guid CreditCardProviderId = Guid.Empty;
            foreach (ProviderEntity provider in providers)
            {
                if (provider.Type == ProviderType.FNBOACHProvider)
                    ACHProviderId = provider.ProviderId;
                else if (provider.Type == ProviderType.CreditCardProvider)
                    CreditCardProviderId = provider.ProviderId;
                else if (provider.Type == type)
                    CardProviderId = provider.ProviderId;
            }
            PrepaidModule module = new PrepaidModule(
                brandingID,
                ACHProviderId,
                CardProviderId,
                CreditCardProviderId,
                15,
                2,
                20.0M,
                10.0M,
                100.0M,
                1000.0M,
                3,
                1,
                10,
                Guid.NewGuid(),
                Guid.NewGuid(),
                true,
                false,
                "TemplateName",
                "21",
                true,
                INITIAL_MAXEMERGENCYLOAD
                );
            module.Save();

            return module;
        }

        public static void CreateTargetModule(Guid brandingID)
        {
            Site site = new Site(brandingID);

            Error error = null;
            Guid billingBankAccount;
            ServiceFactory.BrandingService.CreateProviderBankAccount("Billing", "ShortName", "1234", "111924392", BankAccountType.Checking, out billingBankAccount, out error);

            Guid sweepBankAccount;
            ServiceFactory.BrandingService.CreateProviderBankAccount("Sweep", "ShortName", "1234", "111924392", BankAccountType.Checking, out sweepBankAccount, out error);

            Guid challengeBankAccount;
            ServiceFactory.BrandingService.CreateProviderBankAccount("challenge", "ShortName", "1234", "111924392", BankAccountType.Checking, out challengeBankAccount, out error);

            Guid finalBankAccount;
            ServiceFactory.BrandingService.CreateProviderBankAccount("final", "ShortName", "1234", "111924392", BankAccountType.Checking, out finalBankAccount, out error);

            FNBOACHProvider achProvider = new FNBOACHProvider(
                "FNBOACHProvider" + Guid.NewGuid().ToString().Substring(0, 11),
                "Comment",
                "userName",
                "password",
                "webservice",
                "companyId",
                "origin",
                "origFinan",
                "123",
                "123",
                billingBankAccount,
                sweepBankAccount,
                challengeBankAccount,
                finalBankAccount,
                "LoadIncomfingFundsTaxID",
                "FinalTaxID",
                "BillingTaxID",
                "SweepTaxID",
                "ChallengeTaxID"
                );
            SaveProviderReturnStatus providerSaveStatus = achProvider.Save();
            Assert.AreEqual<SaveProviderReturnStatus>(SaveProviderReturnStatus.SavedSuccessfully, providerSaveStatus, "FNBOACHProvider save failed");

            providerSaveStatus = ServiceFactory.BrandingService.CreateCreditCardProvider(
                "CreditCardProvider" + Guid.NewGuid().ToString().Substring(0, 11),
                "comment",
                "payjunior",
                "W0+d3+qEvGSfA9uClMTwSVg67gjzoChW7XjWBsbkUKhQ5kboScyjZZLlWlwiuHnTEbwQmjqsD+xKqFYnsX2u36ZZiMBLGklHYhvFAi1FaV5wuk9ulfRwKLb8ryELHAJWxkefDetbnzEh3OkkWbjs5j8W8joSDQY8/39/DKe3XThUZ+aexqfsjCqQtOA8PztJWDruCPOgKFbteNYGxuRQqFDmRuhJzKNlkuVaXCK4edMRvBCaOqwP7EqoViexfa7fplmIwEsaSUdiG8UCLUVpXnC6T26V9HAotvyvIQscAlbGR58N61ufMSHc6SRZuOzmPxbyOhINBjz/f38Mp7ddOA==",
                "payjunior",
                1000,
                0,
                DateTime.Now.Date,
                4,
                DateTime.Now.Date.AddHours(11),
                1,
                DateTime.Now.Date.AddHours(11),
                0,
                DateTime.Now.Date, DateTime.Now.AddHours(17)
                );
            Assert.AreEqual<SaveProviderReturnStatus>(SaveProviderReturnStatus.SavedSuccessfully, providerSaveStatus, "CreditCardProvider save failed");

            DateTime targetTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 7, 0, 0);
            providerSaveStatus = ServiceFactory.BrandingService.CreateTargetGiftCardProvider(
                "TargetGiftCardProvider" + Guid.NewGuid().ToString().Substring(0, 11),
                "GiftCard",
                5M,
                5M,
                5M,
                150M,
                2000M,
                targetTime
                );
            Assert.AreEqual<SaveProviderReturnStatus>(SaveProviderReturnStatus.SavedSuccessfully, providerSaveStatus, "TargetGiftCardProvider save failed");

            EntityCollection<ProviderEntity> providers = ServiceFactory.BrandingService.RetrieveAllProviders();
            Guid ACHProviderId = Guid.Empty;
            Guid CreditCardProviderId = Guid.Empty;
            Guid TargetProviderId = Guid.Empty;

            foreach (ProviderEntity provider in providers)
            {
                if (provider.Type == ProviderType.FNBOACHProvider)
                    ACHProviderId = provider.ProviderId;
                else if (provider.Type == ProviderType.CreditCardProvider)
                    CreditCardProviderId = provider.ProviderId;
                else if (provider.Type == ProviderType.TargetProvider)
                    TargetProviderId = provider.ProviderId;
            }
            TargetModule module = new TargetModule(
                brandingID,
                ACHProviderId,
                CreditCardProviderId,
                TargetProviderId
                );
            module.Save();
        }

        public static void CreatePolicyDocuments(BrandingEntity branding)
        {
            //Create the Group
            Guid groupName = Guid.NewGuid();
            SiteDocumentGroup.Create(groupName.ToString());

            SiteDocumentGroup group = null;
            foreach (SiteDocumentGroup siteGroup in SiteDocumentGroup.GetAllSiteDocumentGroups())
            {
                if (siteGroup.Name == groupName.ToString())
                {
                    group = siteGroup;
                    break;
                }
            }

            //Create the documents
            SiteDocument.Create(group.ID, SiteDocument.SiteDocumentType.CardholderAgreement, DateTime.Now, "description",
                Guid.NewGuid().ToByteArray(), ".html", POLICY_VERSION);
            SiteDocument.Create(group.ID, SiteDocument.SiteDocumentType.PrivacyPolicy, DateTime.Now, "description",
                Guid.NewGuid().ToByteArray(), ".html", POLICY_VERSION);
            SiteDocument.Create(group.ID, SiteDocument.SiteDocumentType.SiteTermsAndConditions, DateTime.Now, "description",
                Guid.NewGuid().ToByteArray(), ".html", POLICY_VERSION);
            SiteDocument.Create(group.ID, SiteDocument.SiteDocumentType.TargetGiftCardConditions, DateTime.Now, "description",
                Guid.NewGuid().ToByteArray(), ".html", POLICY_VERSION);

            //Set the group id on the branding
            branding.SiteGroupId = group.ID;
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                adapter.SaveEntity(branding);
            }
        }

        public static NotificationEntity CreateNotification(NotificationType type, string subjectLine, string notificationText, CultureEntity culture)
        {
            return CreateNotification(type, subjectLine, notificationText, culture, null);

        }

        /// <summary>
        /// Create an entity
        /// </summary>
        /// <param name="type"></param>
        /// <param name="notificationText"></param>
        /// <returns></returns>
        public static NotificationEntity CreateNotification(
            NotificationType type,
            string subjectLine,
            string notificationText,
            CultureEntity culture,
            BeforeEntitySave<NotificationEntity> beforeUpdate)
        {
            using (DataAccessAdapter adapter = CreateAdapter())
            {
                GlobalNotificationEntity entity = new GlobalNotificationEntity();
                entity.NotificationType = type;
                entity.NotificationText = notificationText;
                entity.NotificationTitle = subjectLine;
                entity.Culture = culture;
                entity.NotificationId = Guid.NewGuid();
                entity.EmailType = EmailType.Html;

                if (beforeUpdate != null)
                {
                    beforeUpdate(entity);
                }

                adapter.SaveEntity(entity);
                return entity;
            }
        }

        public static BrandingNotificationEntity CreateCustomNotification(
            BrandingEntity brand,
            NotificationType type,
            string subjectLine,
            string notificationText,
            CultureEntity culture)
        {
            return CreateCustomNotification(brand, type, subjectLine, notificationText, culture, null);
        }

        public static BrandingNotificationEntity CreateCustomNotification(
            BrandingEntity brand,
            NotificationType type,
            string subjectLine,
            string notificationText,
            CultureEntity culture,
            BeforeEntitySave<BrandingNotificationEntity> beforeUpdate)
        {
            using (DataAccessAdapter adapter = CreateAdapter())
            {
                BrandingNotificationEntity entity = new BrandingNotificationEntity();
                entity.NotificationType = type;
                entity.NotificationText = notificationText;
                entity.NotificationTitle = subjectLine;
                entity.Culture = culture;
                entity.NotificationId = Guid.NewGuid();
                entity.Branding = brand;
                entity.EmailType = EmailType.Html;

                if (beforeUpdate != null)
                {
                    beforeUpdate(entity);
                }

                adapter.SaveEntity(entity);
                return entity;
            }
        }

        public static Parent CreateParent(BeforeEntitySave<ParentEntity> beforeSave)
        {

            using (DataAccessAdapter adapter = CreateAdapter())
            {
                Parent parent = Parent.NewParent();
                parent.DOB = DateTime.Now;
                parent.LastName = "back";
                parent.ReceivePaperStatement = false;
                parent.RoleType = RoleType.Parent;
                parent.LastIPAddress = "192.168.1.1";

                return parent;
            }
        }

        public static void CreateAlert(User user, Teen teen, NotificationType type)
        {
            Alert alert = user.NewAlert(user.Email);
            alert.NotificationType = type;
            alert.AppliesToUser = teen;
            user.Save(null);

        }

        public static void ClearEmails()
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                IRelationPredicateBucket filter = new RelationPredicateBucket();

                adapter.DeleteEntitiesDirectly("EmailEntity", filter);
            }
        }

        public static Teen CreateTeen(Parent parent, BeforeEntitySave<TeenEntity> beforeSave)
        {

            using (DataAccessAdapter adapter = CreateAdapter())
            {
                Teen teen = Teen.NewTeen();
                teen.DOB = DateTime.Now;
                teen.ParentID = parent.UserID;
                teen.RoleType = RoleType.RegisteredTeen;
                (teen.UserEntity as RegisteredTeenEntity).PaymentThreshold = 12.0M;

                ReferralCode code = new ReferralCode(ReferralCode.GenerateCardProgram(), "r");
                code.GenerateReferral();

                teen.ReferralCode = code.ToString();

                if (beforeSave != null)
                {
                    beforeSave(teen.UserEntity as TeenEntity);
                }

                return teen;
            }
        }

        /// <summary>
        /// Generate
        /// </summary>
        /// <returns></returns>
        private static string GenerateUniqueName()
        {
            //Not really unique -- but enough
            return Guid.NewGuid().ToString().Substring(20, 12).Replace("-", "");
        }

        public static User CreateUser(User user,
            ThemeEntity theme, BrandingEntity branding, CultureEntity culture,
            BeforeEntitySave<UserEntity> beforeSave, bool createRelatedTables)
        {
            string username = GenerateUniqueName();
            string salt = GenerateUniqueName();
            string password = AdapterFactory.UserAdapter.CreateSecurePassword(salt, "clearpassword");

            user.CreationDate = DateTime.Now;
            user.UserEntity.CommunicationPhrase = GenerateUniqueName();
            user.FailedPasswordAnswerAttemptWindowsStart = DateTime.Now;
            user.FailedPasswordAttemptAttemptCount = 0;
            user.FailedPasswordAttemptCount = 0;
            user.FailedPasswordAttemptWindowStart = DateTime.Now;
            user.IsLockedOut = false;
            user.IsOnLine = false;
            user.LastActivityDate = DateTime.Now;
            user.LastLockedOutDate = DateTime.Now;
            user.LastLoginDate = DateTime.Now;
            user.LastPasswordChangeDate = DateTime.Now;
            user.Password = password;
            user.PasswordAnswer = "Answer";
            user.PasswordQuestion = "question";
            user.PasswordSalt = salt;
            user.RecieveMarketingEmail = false;
            user.UserName = username;
            user.TimeZone = GetCentralTime();
            user.UserEntity.Culture = culture;
            user.UserEntity.Branding = branding;
            user.UserEntity.Theme = theme;
            user.LastIPAddress = "192.168.1.1";
            user.LastHostName = "foobar.com";
            user.FirstName = GenerateUniqueName();
            user.MiddleName = GenerateUniqueName();
            user.LastName = GenerateUniqueName();

            if (createRelatedTables == true)
            {
                user.NewUserIdentifier();
                user.SSN.Type = IdentifierType.SSN;
                user.SSN.Identifier = Common.Util.Utils.TryTrim("123112312");

                user.NewAddress();
                user.UserAddress.Address1 = "12309 WhiteHall Lane";
                user.UserAddress.City = "Bowie";
                user.UserAddress.State = "MD";
                user.UserAddress.ZipCode = "21037-1234"; //Keep this with a dash(need them for the tests)
                user.UserAddress.Country = "USA";

                user.NewPhone();
                user.Phone.Number = "1232342333"; //keep the dashes (need them for the tests)
                user.Phone.Type = PhoneType.Home;

                //Create an email
                user.NewEmail();
                user.Email.Address = Guid.NewGuid().ToString().Replace("-", "") + "@payjr.com";
                user.Email.Type = EmailType.Html;
                user.Email.RequiresValidation = false;

            }

            if (beforeSave != null)
            {
                beforeSave(user.UserEntity);
            }
            if (!user.Save(null))
            {
                throw new Exception("Something bad happened when creating a test user");
            }
            if (user.RoleType == RoleType.RegisteredTeen)
            {
                ServiceFactory.UserConfiguration.AssignProductToUser(Product.Experience_IC, user as Teen);
            }
            return user;


        }

        /// <summary>
        /// Create a Security Info
        /// </summary>
        /// <param name="adapter"></param>
        public static Parent CreateParent(BrandingEntity branding, ThemeEntity theme, CultureEntity culture, bool createRelated)
        {
            Parent parent = CreateParent(null);

            User user = CreateUser(parent, theme, branding, culture, null, createRelated);

            return parent;
        }

        /// <summary>
        /// Create a Security Info
        /// </summary>
        /// <param name="adapter"></param>
        public static MasterAdminEntity CreateMA(BrandingEntity branding, ThemeEntity theme, CultureEntity culture)
        {

            using (DataAccessAdapter adapter = CreateAdapter())
            {
                MasterAdminEntity masterAdmin = new MasterAdminEntity();

                string username = GenerateUniqueName();
                string salt = GenerateUniqueName();
                string password = AdapterFactory.UserAdapter.CreateSecurePassword(salt, username);

                masterAdmin.IsSupervisor = true;
                masterAdmin.RoleType = RoleType.MasterAdmin;
                masterAdmin.CreationDate = DateTime.Now;
                masterAdmin.CommunicationPhrase = GenerateUniqueName();
                masterAdmin.FailedPasswordAnswerAttemptWindowsStart = DateTime.Now;
                masterAdmin.FailedPasswordAttemptAttemptCount = 0;
                masterAdmin.FailedPasswordAttemptCount = 0;
                masterAdmin.FailedPasswordAttemptWindowStart = DateTime.Now;
                masterAdmin.IsLockedOut = false;
                masterAdmin.IsOnLine = false;
                masterAdmin.LastActivityDate = DateTime.Now;
                masterAdmin.LastLockedOutDate = DateTime.Now;
                masterAdmin.LastLoginDate = DateTime.Now;
                masterAdmin.LastPasswordChangedDate = DateTime.Now;
                masterAdmin.Password = password;
                masterAdmin.PasswordAnswer = "Answer";
                masterAdmin.PasswordQuestion = "question";
                masterAdmin.PasswordSalt = salt;
                masterAdmin.RecieveMarketingEmail = false;
                masterAdmin.UserName = username;
                masterAdmin.TimeZone = GetCentralTime().ToString();
                masterAdmin.Culture = culture;
                masterAdmin.Branding = branding;
                masterAdmin.Theme = theme;
                masterAdmin.LastIpaddress = "192.168.1.1";
                masterAdmin.LastHostName = "foobar.com";
                masterAdmin.FirstName = GenerateUniqueName();
                masterAdmin.MiddleName = GenerateUniqueName();
                masterAdmin.LastName = GenerateUniqueName();

                adapter.SaveEntity(masterAdmin, true);

                return masterAdmin;

            }
        }

        /// <summary>
        /// Create a branding admin.
        /// </summary>
        /// <param name="branding"></param>
        /// <param name="theme"></param>
        /// <param name="culture"></param>
        /// <returns></returns>
        public static AdminEntity CreateBrandingAdmin(out BrandingEntity branding, out ThemeEntity theme, out CultureEntity culture)
        {
            culture = TestEntityFactory.CreateCulture("culture");
            theme = TestEntityFactory.CreateTheme("theme");
            BrandingAdapter brandingAdapter = AdapterFactory.BrandingAdapter;
            branding = brandingAdapter.RetrieveBrandingbyDomainName(DEFAULT_DOMAIN);

            if (branding == null)
            {
                branding = TestEntityFactory.CreateBrandingFromConfig(DEFAULT_DOMAIN);
            }

            using (DataAccessAdapter adapter = CreateAdapter())
            {
                AdminEntity brandingAdmin = new AdminEntity();

                string username = GenerateUniqueName();
                string salt = GenerateUniqueName();
                string password = AdapterFactory.UserAdapter.CreateSecurePassword(salt, username);

                brandingAdmin.IsSupervisor = true;
                brandingAdmin.RoleType = RoleType.Admin;
                brandingAdmin.CreationDate = DateTime.Now;
                brandingAdmin.CommunicationPhrase = GenerateUniqueName();
                brandingAdmin.FailedPasswordAnswerAttemptWindowsStart = DateTime.Now;
                brandingAdmin.FailedPasswordAttemptAttemptCount = 0;
                brandingAdmin.FailedPasswordAttemptCount = 0;
                brandingAdmin.FailedPasswordAttemptWindowStart = DateTime.Now;
                brandingAdmin.IsLockedOut = false;
                brandingAdmin.IsOnLine = false;
                brandingAdmin.LastActivityDate = DateTime.Now;
                brandingAdmin.LastLockedOutDate = DateTime.Now;
                brandingAdmin.LastLoginDate = DateTime.Now;
                brandingAdmin.LastPasswordChangedDate = DateTime.Now;
                brandingAdmin.Password = password;
                brandingAdmin.PasswordAnswer = "Answer";
                brandingAdmin.PasswordQuestion = "question";
                brandingAdmin.PasswordSalt = salt;
                brandingAdmin.RecieveMarketingEmail = false;
                brandingAdmin.UserName = username;
                brandingAdmin.TimeZone = GetCentralTime().ToString();
                brandingAdmin.Culture = culture;
                brandingAdmin.Branding = branding;
                brandingAdmin.Theme = theme;
                brandingAdmin.LastIpaddress = "192.168.1.1";
                brandingAdmin.LastHostName = "foobar.com";
                brandingAdmin.FirstName = GenerateUniqueName();
                brandingAdmin.MiddleName = GenerateUniqueName();
                brandingAdmin.LastName = GenerateUniqueName();

                adapter.SaveEntity(brandingAdmin, true);

                return brandingAdmin;
            }
        }

        /// <summary>
        /// Create a Security Info
        /// </summary>
        /// <param name="adapter"></param>
        public static User CreateUser(ThemeEntity theme, BrandingEntity branding, CultureEntity culture)
        {
            Parent parent = CreateParent(null);

            return CreateUser(parent, theme, branding, culture, null, true);
        }

        /// <summary>
        /// Create a Security Info
        /// </summary>
        /// <param name="adapter"></param>
        public static User CreateUser(ThemeEntity theme, BrandingEntity branding, CultureEntity culture, BeforeEntitySave<UserEntity> beforeSave)
        {
            Parent parent = CreateParent(null);

            return CreateUser(parent, theme, branding, culture, beforeSave, true);
        }

        public static void CreateTeen(out BrandingEntity branding, out ThemeEntity theme, out Parent parent, out Teen teen)
        {
            CreateTeen(out branding, out theme, out parent, out teen, true);
        }

        public static void CreateTeen(out BrandingEntity branding, out ThemeEntity theme, out Parent parent, out Teen teen, bool createRelatedTables)
        {
            CultureEntity culture;
            CreateTeen(out branding, out theme, out culture, out parent, out teen, createRelatedTables);

        }

        public static void CreateTeen(out BrandingEntity branding, out ThemeEntity theme, out CultureEntity culture, out Parent parent, out Teen teen, bool createRelatedTables)
        {
            culture = TestEntityFactory.CreateCulture("culture");
            theme = TestEntityFactory.CreateTheme("theme");
            BrandingAdapter brandingAdapter = AdapterFactory.BrandingAdapter;
            branding = brandingAdapter.RetrieveBrandingbyDomainName(DEFAULT_DOMAIN);

            if (branding == null)
            {
                branding = TestEntityFactory.CreateBrandingFromConfig(DEFAULT_DOMAIN);

            }

            CreateTeen(branding, theme, culture, out parent, out teen, createRelatedTables);
        }

        public static void CreateTeen(BrandingEntity branding, ThemeEntity theme, CultureEntity culture, out Parent parent, out Teen teen, bool createRelatedTables)
        {
            parent = CreateParent(null);

            User _parentSecInfo = TestEntityFactory.CreateUser(parent, theme, branding, culture, null, createRelatedTables);

            teen = TestEntityFactory.CreateTeen(parent, null);
            User _teenSecInfo = TestEntityFactory.CreateUser(teen, theme, branding, culture, null, createRelatedTables);
        }

        public static void CreateTeen(BrandingEntity branding, ThemeEntity theme, CultureEntity culture, Parent parent, out Teen teen)
        {
            teen = TestEntityFactory.CreateTeen(parent, null);
            User _teenSecInfo = TestEntityFactory.CreateUser(teen, theme, branding, culture, null, true);
        }

        /// <summary>
        /// Create a savings and prepaid account for the parent
        /// </summary>
        /// <param name="parent"></param>
        public static void CreateSavingsAndBankAccount(User user)
        {
            CreateBankAccount(user, true, AccountStatus.Unverified);
            CreateSavingsAccount(user, Payjr.Entity.AccountType.Checking, user.UserID);
        }

        /// <summary>
        /// Create an account in the savings program of the type indicated.
        /// </summary>
        /// <param name="parent"></param>
        public static void CreateSavingsAccount(User user, Payjr.Entity.AccountType type, Guid familyID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                CustomAccountFieldGroupEntity savings = new CustomAccountFieldGroupEntity();
                savings.AccountType = type;
                savings.IsActive = true;
                savings.Status = AccountStatus.Unverified;
                savings.UserId = user.UserID;
                savings.CustomAccountFields.AddRange(TestEntityFactory.CreateCustomFields(user.BrandingId));

                adapter.SaveEntity(savings, true);
            }
        }

        /// <summary>
        /// Create a savings and prepaid account for the parent
        /// </summary>
        /// <param name="parent"></param>
        public static void CreateSavingsAccount(User user, Guid familyID)
        {
            CreateSavingsAccount(user, Payjr.Entity.AccountType.Savings, familyID);
        }

        public static void CreateSavingsAccount(User user, bool isActive, AccountStatus status, Guid brandingID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                CustomAccountFieldGroupEntity savings = new CustomAccountFieldGroupEntity();
                savings.AccountType = Payjr.Entity.AccountType.Savings;
                savings.IsActive = isActive;
                savings.Status = status;
                savings.UserId = user.UserID;
                savings.CustomAccountFields.AddRange(TestEntityFactory.CreateCustomFields(brandingID));

                adapter.SaveEntity(savings);
            }
        }

        public static void CreatePrepaidAccount(User user, bool isActive, PrepaidCardStatus status)
        {
            ServiceFactory.UserConfiguration.AssignProductToUser(Product.PPaid_IC, user as Teen);
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                PrepaidCardAccountEntity prepaidCard = new PrepaidCardAccountEntity();
                prepaidCard.ActivationMethod = PrepaidActivationMethod.unknown;
                prepaidCard.Active = isActive;
                prepaidCard.ActiveteDateTime = null;
                prepaidCard.BrandingCardDesignId = null;
                prepaidCard.CardIdentifier = null;
                prepaidCard.CardNumber = "5150620000973224";
                prepaidCard.LostStolenDateTime = null;
                prepaidCard.MarkedForDeletion = false;
                prepaidCard.Status = status;
                prepaidCard.UserCardDesignId = null;

                PrepaidCardAccountUserEntity prepaidCardUser = new PrepaidCardAccountUserEntity();
                prepaidCardUser.UserId = user.UserID;
                prepaidCardUser.PrepaidCardAccount = prepaidCard;

                adapter.SaveEntity(prepaidCardUser);

                CreditCardCreateJobEntity prepaidCardCreateJob = new CreditCardCreateJobEntity();
                prepaidCardCreateJob.CreateTime = DateTime.UtcNow;
                prepaidCardCreateJob.ScheduledStartTime = DateTime.UtcNow;
                prepaidCardCreateJob.UserId = user.UserID;
                prepaidCardCreateJob.Status = JobStatus.Waiting;
                prepaidCardCreateJob.JobType = JobType.CreateCardJob;
                prepaidCardCreateJob.PrepaidCardAccountId = prepaidCard.PrepaidCardAccountId;

                adapter.SaveEntity(prepaidCardCreateJob);
            }
        }

        public static void CreatePrepaidAccount(Teen teen, string cardNumber, bool isActive, PrepaidCardStatus status, Guid brandingCardDesignID)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                PrepaidCardAccountEntity prepaidCard = new PrepaidCardAccountEntity();
                prepaidCard.ActivationMethod = PrepaidActivationMethod.unknown;
                prepaidCard.Active = isActive;
                prepaidCard.ActiveteDateTime = null;
                prepaidCard.BrandingCardDesignId = brandingCardDesignID;
                prepaidCard.CardIdentifier = null;
                prepaidCard.CardNumber = cardNumber;
                prepaidCard.LostStolenDateTime = null;
                prepaidCard.MarkedForDeletion = false;
                prepaidCard.Status = status;
                prepaidCard.UserCardDesignId = null;

                PrepaidCardAccountUserEntity prepaidCardUser = new PrepaidCardAccountUserEntity();
                prepaidCardUser.UserId = teen.UserID;
                prepaidCardUser.PrepaidCardAccount = prepaidCard;

                adapter.SaveEntity(prepaidCardUser);
            }
        }

        public static Guid CreateBankAccount(User user, bool IsActive, AccountStatus status)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                BankAccountEntity bank = new BankAccountEntity();
                bank.AccountIdentifier = string.Empty;
                bank.AccountNumber = "123456231564561";
                bank.RoutingNumber = "104000016";
                bank.ShortName = "ShortName";
                bank.Status = status;
                bank.Active = IsActive;
                bank.MarkedForDeletion = false;
                bank.Name = "Bank Name";
                bank.Type = BankAccountType.Checking;
                bank.WaitingDays = 3;

                BankAccountUserEntity bankUser = new BankAccountUserEntity();
                bankUser.UserId = user.UserID;
                bankUser.BankAccount = bank;

                adapter.SaveEntity(bankUser, true);

                return bank.BankAccountId;
            }
        }

        public static void CreateBankAccount(User user, bool IsActive, AccountStatus status, string accountNumber, string routingNumber, BankAccountType type)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                BankAccountEntity bank = new BankAccountEntity();
                bank.AccountIdentifier = string.Empty;
                bank.AccountNumber = accountNumber;
                bank.RoutingNumber = routingNumber;
                bank.ShortName = "ShortName";
                bank.Status = status;
                bank.Active = IsActive;
                bank.MarkedForDeletion = false;
                bank.Name = "Bank Name";
                bank.Type = type;
                bank.WaitingDays = 3;

                BankAccountUserEntity bankUser = new BankAccountUserEntity();
                bankUser.UserId = user.UserID;
                bankUser.BankAccount = bank;

                adapter.SaveEntity(bankUser);
            }
        }

        public static Guid CreateCreditCardAccount(User user, bool isActive, AccountStatus status)
        {
            CreditCardAccountEntity cardAccount = new CreditCardAccountEntity();
            cardAccount.CardIdentifier = null;
            cardAccount.CardNumber = "4012001021000613";
            cardAccount.CreatedBy = string.Empty;
            cardAccount.Status = status;
            cardAccount.CreatedTime = DateTime.Now;
            cardAccount.CreditCardAccountId = Guid.NewGuid();
            cardAccount.ExpirationMonth = DateTime.Now.ToString("MM");
            cardAccount.ExpirationYear = DateTime.Now.ToString("yyyy");
            cardAccount.IsDefault = isActive;
            cardAccount.MarkedForDeletion = false;
            cardAccount.ModifiedBy = string.Empty;
            cardAccount.ModifiedDate = DateTime.Now;
            cardAccount.NickName = "Nick Name";
            cardAccount.Type = CreditCardType.VISA;
            cardAccount.UserId = user.UserID;
            cardAccount.Status = status;

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                adapter.SaveEntity(cardAccount, true);
            }
            return cardAccount.CreditCardAccountId;
        }

        public static void CreateTargetAccount(User teen, AccountStatus status)
        {
            CreateTargetAccount(teen, status, true);
        }

        /// <summary>
        /// Creates a target account.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="status">The status.</param>
        public static void CreateTargetAccount(User teen, AccountStatus status, bool createInitialLoad)
        {
            ServiceFactory.UserConfiguration.AssignProductToUser(Product.Target_IC, teen as Teen);
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    // Create a new Target Account
                    TargetAccountEntity targetGiftCard = new TargetAccountEntity();
                    targetGiftCard.UserId = teen.UserID;
                    targetGiftCard.CardNumber = CARD_NUMBER1;
                    targetGiftCard.Ranumber = RA_NUMBER1;

                    targetGiftCard.DateSent = DateTime.UtcNow.AddDays(2);

                    targetGiftCard.MarkedForDeletion = false;
                    targetGiftCard.Status = status;
                    if (createInitialLoad)
                    {
                        targetGiftCard.Balance = BALANCE_1;
                        targetGiftCard.LastBalance = DateTime.UtcNow.AddDays(-1);
                    }

                    adapter.SaveEntity(targetGiftCard, true);

                    if (createInitialLoad)
                    {
                        // Create an initial Transfer Job for the new Target Account
                        TargetTransferJobEntity initJob = new TargetTransferJobEntity();
                        initJob.Amount = 15.00M;
                        initJob.CreateTime = DateTime.Now;
                        initJob.Description = "Initial Target GiftCard Load";
                        initJob.JobType = JobType.TargetInitialLoadJob;
                        initJob.OcurrenceType = TransactionOccurrenceType.Manual;
                        initJob.PostDate = DateTime.Now.AddDays(2);
                        initJob.Status = JobStatus.Waiting_for_generation;
                        initJob.ScheduledStartTime = DateTime.Now;
                        initJob.TargetAccountId = targetGiftCard.TargetAccountId;
                        initJob.TransactionDirection = TransactionDirection.Credit;
                        initJob.TransactionType = TransactionType.LOAD;
                        initJob.UserId = teen.UserID;
                        adapter.SaveEntity(initJob, true);
                    }
                }
                catch (ORMException) { return; }
            }
        }

        /// <summary>
        /// Creates a target account that has not been sent for fulfillment yet.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="status">The status.</param>
        public static void CreateUnsentTargetAccount(User teen, AccountStatus status, bool createInitialLoad)
        {
            ServiceFactory.UserConfiguration.AssignProductToUser(Product.Target_IC, teen as Teen);
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                try
                {
                    // Create a new Target Account
                    TargetAccountEntity targetGiftCard = new TargetAccountEntity();
                    targetGiftCard.UserId = teen.UserID;
                    //targetGiftCard.CardNumber = CARD_NUMBER1;
                    //targetGiftCard.Ranumber = RA_NUMBER1;

                    //targetGiftCard.DateSent = DateTime.UtcNow.AddDays(2);

                    targetGiftCard.MarkedForDeletion = false;
                    targetGiftCard.Status = status;
                    if (createInitialLoad)
                    {
                        targetGiftCard.Balance = BALANCE_1;
                        targetGiftCard.LastBalance = DateTime.UtcNow.AddDays(-1);
                    }
                    adapter.SaveEntity(targetGiftCard, true);

                    if (createInitialLoad)
                    {
                        // Create an initial Transfer Job for the new Target Account
                        TargetTransferJobEntity initJob = new TargetTransferJobEntity();
                        initJob.Amount = 15.00M;
                        initJob.CreateTime = DateTime.Now;
                        initJob.Description = "Initial Target GiftCard Load";
                        initJob.JobType = JobType.TargetInitialLoadJob;
                        initJob.OcurrenceType = TransactionOccurrenceType.Manual;
                        initJob.PostDate = DateTime.Now.AddDays(2);
                        initJob.Status = JobStatus.Waiting_for_generation;
                        initJob.ScheduledStartTime = DateTime.Now;
                        initJob.TargetAccountId = targetGiftCard.TargetAccountId;
                        initJob.TransactionDirection = TransactionDirection.Credit;
                        initJob.TransactionType = TransactionType.LOAD;
                        initJob.UserId = teen.UserID;
                        adapter.SaveEntity(initJob, true);
                    }
                }
                catch (ORMException e) { return; }
            }
        }

        /// <summary>
        /// Is the Job Type in the list
        /// </summary>
        /// <param name="jobs"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsJobTypeInList(EntityCollection<JobEntity> jobs, JobType type)
        {

            foreach (JobEntity job in jobs)
            {
                if (job.JobType == type)
                {
                    return true;
                }
            }
            return false;

        }

        /// <summary>
        /// Create a savings and prepaid account for the parent
        /// </summary>
        /// <param name="parent"></param>
        public static void MakeTeenSavingsTeen(Guid userId)
        {
            Teen teen = User.RetrieveUser(userId) as Teen;
            ServiceFactory.UserConfiguration.AssignProductToUser(Product.Savings_IC, teen);
            CreateSavingsAccount(teen, true, AccountStatus.AllowMoneyMovement, teen.BrandingId);
        }

        /// <summary>
        /// Create a savings and prepaid account for the parent
        /// </summary>
        /// <param name="parent"></param>
        public static void MakeTeenPrepaidTeen(Guid userId)
        {
            ServiceFactory.UserConfiguration.AssignProductToUser(Product.PPaid_IC, User.RetrieveUser(userId) as Teen);
        }

        /// <summary>
        /// Makes the teen a target program user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        public static void MakeTeenTargetTeen(Guid userId)
        {
            ServiceFactory.UserConfiguration.AssignProductToUser(Product.Target_IC, User.RetrieveUser(userId) as Teen);
        }

        /// <summary>
        /// Creates the adapter.
        /// </summary>
        /// <returns></returns>
        public static DataAccessAdapter CreateAdapter()
        {
            return new DataAccessAdapter(true);
        }

        /// <summary>
        /// Gets the job entity.
        /// </summary>
        /// <param name="jobID">The job ID.</param>
        /// <returns></returns>
        public static JobEntity GetJobEntity(Guid jobID)
        {
            //Check against database
            using (DataAccessAdapter adapter = CreateAdapter())
            {
                JobEntity entity = new JobEntity(jobID);
                IPrefetchPath2 path = new PrefetchPath2((int)EntityType.JobEntity);
                path.Add(JobEntity.PrefetchPathJobData);

                if (adapter.FetchEntity(entity, path))
                    return entity;
                else
                    return null;
            }
        }

        /// <summary>
        /// Get the branding entity
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public static BrandingEntity GetBrandingEntity(Guid ID)
        {
            //Check against database
            using (DataAccessAdapter adapter = CreateAdapter())
            {
                BrandingEntity entity = new BrandingEntity(ID);
                //IPrefetchPath2 path = new PrefetchPath2((int)EntityType.JobEntity);
                //  path.Add(JobEntity.PrefetchPathJobData);

                if (adapter.FetchEntity(entity))
                    return entity;
                else
                    return null;
            }
        }

        /// <summary>
        /// The a specific prepaid card for the user and test how many meet that criteria
        /// </summary>
        /// <param name="userID"></param>
        /// <param name="active"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public static PrepaidCardAccountEntity GetPrepaidCard(Guid userID, bool active, int count)
        {
            EntityCollection<PrepaidCardAccountEntity> prepaidCards;
            Error error;
            int countMatches = 0;
            PrepaidCardAccountEntity foundAccount = null;

            foreach (PrepaidCardAccountEntity account in RetrievePrepaidCardAccountsByUser(userID))
            {
                if (account.Active == active)
                {
                    countMatches++;
                    foundAccount = account;
                }

            }
            Assert.AreEqual<int>(count, countMatches, "The number of cards in the DB does not match what we expect");
            return foundAccount;
        }

        /// <summary>
        /// retrieves Pre-Paid Card Account by user
        /// </summary>
        /// <param name="UserId">the user id.</param>
        /// <returns>collection of pre-paid cards</returns>
        public static EntityCollection<PrepaidCardAccountEntity> RetrievePrepaidCardAccountsByUser(Guid UserId)
        {
            EntityCollection<PrepaidCardAccountEntity> prepaidCards = new EntityCollection<PrepaidCardAccountEntity>(new PrepaidCardAccountEntityFactory());

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {

                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                bucket.Relations.Add(PrepaidCardAccountEntity.Relations.PrepaidCardAccountUserEntityUsingPrepaidCardAccountId);
                bucket.PredicateExpression.Add(PrepaidCardAccountUserFields.UserId == UserId);

                adapter.FetchEntityCollection(prepaidCards, bucket);
                return prepaidCards;


            }
        }

        /// <summary>
        /// Get the branding entity
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public static ThemeEntity GetThemeEntity(Guid ID)
        {
            //Check against database
            using (DataAccessAdapter adapter = CreateAdapter())
            {
                ThemeEntity entity = new ThemeEntity(ID);

                if (adapter.FetchEntity(entity))
                    return entity;
                else
                    return null;
            }
        }

        /// <summary>
        /// Creates the transaction lookup.
        /// </summary>
        /// <param name="tranCode">The tran code.</param>
        /// <param name="shortDesc">The short desc.</param>
        /// <param name="longDesc">The long desc.</param>
        /// <param name="isFinancial">if set to <c>true</c> [is financial].</param>
        /// <param name="reporting">The reporting.</param>
        /// <param name="userVisible">if set to <c>true</c> [user visible].</param>
        public static void CreateTransactionLookup(string tranCode, string shortDesc, string longDesc, bool isFinancial, short reporting, bool userVisible)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                TransactionLookupEntity lookup = new TransactionLookupEntity();
                lookup.TranCode = tranCode;
                lookup.ShortDesc = shortDesc;
                lookup.LongDescription = longDesc;
                lookup.IsFinancial = isFinancial;
                lookup.ReportingGroup = reporting;
                lookup.UserVisible = userVisible;
                lookup.IsNew = true;
                lookup.TransactionLookUpId = Guid.NewGuid();

                adapter.SaveEntity(lookup);
            }
        }

        /// <summary>
        /// Creates the custom fields.
        /// </summary>
        /// <param name="brandingID">The branding ID.</param>
        /// <returns></returns>
        public static List<CustomAccountFieldEntity> CreateCustomFields(Guid brandingID)
        {
            EntityCollection<CustomAccountFieldEntity> testTwo = new EntityCollection<CustomAccountFieldEntity>(new CustomAccountFieldEntityFactory());
            SiteConfiguration site = SiteManager.GetSiteConfiguration(brandingID);

            foreach (AccountField field in site.SavingsConfig.AccountFieldConfig.AccountField)
            {
                testTwo.Add(field.CreateEntity("foobar"));

            }

            List<CustomAccountFieldEntity> toReturn = new List<CustomAccountFieldEntity>(testTwo);
            return toReturn;
        }

        public static List<SavingsAccount.SavingsAccountField> CreateSavingsFields(Guid brandingID)
        {
            List<CustomAccountFieldEntity> entities = CreateCustomFields(brandingID);

            List<SavingsAccount.SavingsAccountField> fields = new List<SavingsAccount.SavingsAccountField>();
            foreach (CustomAccountFieldEntity entity in entities)
            {
                fields.Add(new SavingsAccount.SavingsAccountField(entity));
            }

            return fields;
        }


        /// <summary>
        /// Get the branding entity
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public static EmailEntity GetEmailEntity(Guid ID)
        {
            //Check against database
            using (DataAccessAdapter adapter = CreateAdapter())
            {
                EmailEntity entity = new EmailEntity(ID);

                if (adapter.FetchEntity(entity))
                    return entity;
                else
                    return null;
            }
        }

        /// <summary>
        /// Clears all entries from the UnitTest database.
        /// </summary>
        public static void ClearAll()
        {
            IRelationPredicateBucket filter = new RelationPredicateBucket();

            CacheFactory.GetCache(CacheType.AppMockCacheProvider).Flush();


            using (DataAccessAdapter adapter = CreateAdapter())
            {

                adapter.DeleteEntitiesDirectly("BlackListedAccountEntity", filter);
                adapter.DeleteEntitiesDirectly("CorporateLoadEntryEntity", filter);
                adapter.DeleteEntitiesDirectly("CorporateLoadEntity", filter);
                adapter.DeleteEntitiesDirectly("CompanyEntity", filter);

                adapter.DeleteEntitiesDirectly("AccountBalanceEntity", filter);

                adapter.DeleteEntitiesDirectly("PolicyEntity", filter);

                adapter.DeleteEntitiesDirectly("UnregisteredCustomCardDesignEntity", filter);
                adapter.DeleteEntitiesDirectly("CustomCardDesignUserEntity", filter);
                adapter.DeleteEntitiesDirectly("CustomCardDesignEntity", filter);

                adapter.DeleteEntitiesDirectly("AlertEntity", filter);
                adapter.DeleteEntitiesDirectly("TransactionLookupEntity", filter);
                adapter.DeleteEntitiesDirectly("FedAchDirectoryEntity", filter);
                adapter.DeleteEntitiesDirectly("UserErrorEntity", filter);
                adapter.DeleteEntitiesDirectly("UserDataEntity", filter);

                adapter.DeleteEntitiesDirectly("BrandingAdminIpEntity", filter);
                adapter.DeleteEntitiesDirectly("MasterAdminEntity", filter);
                adapter.DeleteEntitiesDirectly("BrandingFaqEntryEntity", filter);
                adapter.DeleteEntitiesDirectly("FaqEntryEntity", filter);
                adapter.DeleteEntitiesDirectly("KnowledgeBaseEntryEntity", filter);
                adapter.DeleteEntitiesDirectly("CategoryEntity", filter);
                adapter.DeleteEntitiesDirectly("CardTransactionQueueEntity", filter);
                adapter.DeleteEntitiesDirectly("CardTransactionProcessHistoryEntity", filter);
                adapter.DeleteEntitiesDirectly("CardTransactionEntity", filter);
                adapter.DeleteEntitiesDirectly("TransactionAuditEntity", filter);
                adapter.DeleteEntitiesDirectly("FileProcessingAuditEntity", filter);

                adapter.DeleteEntitiesDirectly("SavingsAccountJournalEntity", filter);

                adapter.DeleteEntitiesDirectly("SavingsTransferJobEntity", filter);
                adapter.DeleteEntitiesDirectly("VerificationJobEntity", filter);
                adapter.DeleteEntitiesDirectly("SavingsTransferJobEntity", filter);
                adapter.DeleteEntitiesDirectly("CustomAccountFieldEntity", filter);
                adapter.DeleteEntitiesDirectly("CustomAccountFieldGroupEntity", filter);

                adapter.DeleteEntitiesDirectly("UserJournalEntity", filter);
                adapter.DeleteEntitiesDirectly("TargetAccountJournalEntity", filter);
                adapter.DeleteEntitiesDirectly("PrepaidCardJournalEntity", filter);
                adapter.DeleteEntitiesDirectly("PdscardTransactionJobEntity", filter);

                adapter.DeleteEntitiesDirectly("BillingAmountEntity", filter);

                adapter.DeleteEntitiesDirectly("UserPurseEntity", filter);
                adapter.DeleteEntitiesDirectly("PurseEntity", filter);

                adapter.DeleteEntitiesDirectly("FsvcardTransactionJobEntity", filter);
                adapter.DeleteEntitiesDirectly("FnboachtransactionJobEntity", filter);
                adapter.DeleteEntitiesDirectly("FnboachproviderEntity", filter);
                adapter.DeleteEntitiesDirectly("FsvcardProviderEntity", filter);

                adapter.DeleteEntitiesDirectly("PdsachtransactionJobEntity", filter);
                adapter.DeleteEntitiesDirectly("AchtransactionJobEntity", filter);
                adapter.DeleteEntitiesDirectly("CardTransactionJobEntity", filter);
                adapter.DeleteEntitiesDirectly("UserErrorEntity", filter);

                adapter.DeleteEntitiesDirectly("PdsysachproviderEntity", filter);
                adapter.DeleteEntitiesDirectly("PdsyscardProviderEntity", filter);

                adapter.DeleteEntitiesDirectly("AchleadTimeEntity", filter);

                adapter.DeleteEntitiesDirectly("LimitedRoutingNumberEntity", filter);
                adapter.DeleteEntitiesDirectly("PrepaidModuleEntity", filter);
                adapter.DeleteEntitiesDirectly("CardProviderEntity", filter);

                adapter.DeleteEntitiesDirectly("LimitedRoutingNumberEntity", filter);

                adapter.DeleteEntitiesDirectly("MarketingRegistrationInfoEntity", filter);
                adapter.DeleteEntitiesDirectly("MarketingInformationEntity", filter);
                adapter.DeleteEntitiesDirectly("UnregisteredCreditCardTransactionEntity", filter);
                adapter.DeleteEntitiesDirectly("CreditCardCreditEntity", filter);
                adapter.DeleteEntitiesDirectly("CreditCardAccountJournalEntity", filter);
                adapter.DeleteEntitiesDirectly("CreditCardTransactionJobEntity", filter);
                adapter.DeleteEntitiesDirectly("CreditCardAuthReversalEntity", filter);
                adapter.DeleteEntitiesDirectly("CreditCardPayerAuthenticationEnrollmentEntity", filter);
                adapter.DeleteEntitiesDirectly("CreditCardPayerAuthenticationValidateEntity", filter);
                adapter.DeleteEntitiesDirectly("CreditCardAuthorizationEntity", filter);
                adapter.DeleteEntitiesDirectly("CreditCardCaptureEntity", filter);
                adapter.DeleteEntitiesDirectly("CreditCardAccountEntity", filter);

                adapter.DeleteEntitiesDirectly("CustomAccountFieldEntity", filter);
                adapter.DeleteEntitiesDirectly("CustomAccountFieldGroupEntity", filter);
                adapter.DeleteEntitiesDirectly("BrandingCardDesignEntity", filter);
                adapter.DeleteEntitiesDirectly("StockCardDesignEntity", filter);
                adapter.DeleteEntitiesDirectly("PictureEntity", filter);
                adapter.DeleteEntitiesDirectly("ErrorDescriptionEntity", filter);
                adapter.DeleteEntitiesDirectly("TransactionAuditEntity", filter);
                adapter.DeleteEntitiesDirectly("ChallengeDepositJobEntity", filter);
                adapter.DeleteEntitiesDirectly("LinkedJobEntity", filter);
                adapter.DeleteEntitiesDirectly("BankAccountJournalEntity", filter);
                adapter.DeleteEntitiesDirectly("UserJournalEntity", filter);
                adapter.DeleteEntitiesDirectly("JournalEntity", filter);
                adapter.DeleteEntitiesDirectly("LinkedJobEntity", filter);
                adapter.DeleteEntitiesDirectly("UserIdentifierEntity", filter);

                adapter.DeleteEntitiesDirectly("CreditCardCreateJobEntity", filter);
                adapter.DeleteEntitiesDirectly("PrepaidCardAccountUserEntity", filter);
                adapter.DeleteEntitiesDirectly("PrepaidCardAccountEntity", filter);
                adapter.DeleteEntitiesDirectly("ReferralModuleEntity", filter);
                adapter.DeleteEntitiesDirectly("TargetModuleEntity", filter);
                adapter.DeleteEntitiesDirectly("ModuleEntity", filter);
                adapter.DeleteEntitiesDirectly("AchproviderEntity", filter);
                adapter.DeleteEntitiesDirectly("TargetProviderEntity", filter);
                adapter.DeleteEntitiesDirectly("CreditCardProviderEntity", filter);
                adapter.DeleteEntitiesDirectly("ProviderNetworkConfigEntity", filter);
                adapter.DeleteEntitiesDirectly("ProductUserEntity", filter);
                adapter.DeleteEntitiesDirectly("ZoneSettingEntity", filter);
                adapter.DeleteEntitiesDirectly("PhoneUserEntity", filter);
                adapter.DeleteEntitiesDirectly("PhoneEntity", filter);
                adapter.DeleteEntitiesDirectly("AddressUserEntity", filter);
                adapter.DeleteEntitiesDirectly("AddressEntity", filter);
                adapter.DeleteEntitiesDirectly("BankAccountUserEntity", filter);
                adapter.DeleteEntitiesDirectly("BankAccountEntity", filter);
                adapter.DeleteEntitiesDirectly("ReferralBonusWindowEntity", filter);
                adapter.DeleteEntitiesDirectly("ReferralModuleEntity", filter);
                adapter.DeleteEntitiesDirectly("ModuleEntity", filter);
                adapter.DeleteEntitiesDirectly("BrandingAnnouncementEntryEntity", filter);
                adapter.DeleteEntitiesDirectly("AnnouncementEntryEntity", filter);
                adapter.DeleteEntitiesDirectly("AlertEntity", filter);
                adapter.DeleteEntitiesDirectly("ChoreMasterEntity", filter);
                adapter.DeleteEntitiesDirectly("ChoreJobEntity", filter);
                adapter.DeleteEntitiesDirectly("ChoreEntity", filter);
                adapter.DeleteEntitiesDirectly("AllowanceJobEntity", filter);
                adapter.DeleteEntitiesDirectly("EmailEntity", filter);
                adapter.DeleteEntitiesDirectly("NotificationJobEntity", filter);
                adapter.DeleteEntitiesDirectly("JobDataEntity", filter);
                adapter.DeleteEntitiesDirectly("TargetTransferJobEntity", filter);
                adapter.DeleteEntitiesDirectly("TransactionJobEntity", filter);
                adapter.DeleteEntitiesDirectly("JobEntity", filter);
                adapter.DeleteEntitiesDirectly("ProviderEntity", filter);
                adapter.DeleteEntitiesDirectly("ScheduledItemEntity", filter);

                // Target GiftCard related entities.
                adapter.DeleteEntitiesDirectly("TargetAccountEntity", filter);

                adapter.DeleteEntitiesDirectly("TeenTransactionHistoryEntity", filter);
                adapter.DeleteEntitiesDirectly("ReferralEntity", filter);
                adapter.DeleteEntitiesDirectly("PreRegisteredTeenEntity", filter);
                adapter.DeleteEntitiesDirectly("RegisteredTeenEntity", filter);
                adapter.DeleteEntitiesDirectly("TeenEntity", filter);
                adapter.DeleteEntitiesDirectly("AdminEntity", filter);
                adapter.DeleteEntitiesDirectly("ParentEntity", filter);
                adapter.DeleteEntitiesDirectly("PromotionalPricingEntity", filter);
                adapter.DeleteEntitiesDirectly("UserProductPricingEntity", filter);
                adapter.DeleteEntitiesDirectly("BrandingProductPricingEntity", filter);
                adapter.DeleteEntitiesDirectly("SystemProductPricingEntity", filter);
                adapter.DeleteEntitiesDirectly("ProductPricingEntity", filter);
                adapter.DeleteEntitiesDirectly("UserCancellationEntity", filter);
                adapter.DeleteEntitiesDirectly("DelinquentAccountEntity", filter);
                adapter.DeleteEntitiesDirectly("UserEntity", filter);
                adapter.DeleteEntitiesDirectly("BrandingNotificationEntity", filter);
                adapter.DeleteEntitiesDirectly("GlobalNotificationEntity", filter);
                adapter.DeleteEntitiesDirectly("NotificationEntity", filter);
                adapter.DeleteEntitiesDirectly("ProductBrandingEntity", filter);
                adapter.DeleteEntitiesDirectly("BrandingThemeEntity", filter);
                adapter.DeleteEntitiesDirectly("BrandingEntity", filter);
                adapter.DeleteEntitiesDirectly("SiteDocumentEntity", filter);
                adapter.DeleteEntitiesDirectly("SiteDocumentGroupEntity", filter);
                adapter.DeleteEntitiesDirectly("ThemeEntity", filter);
                adapter.DeleteEntitiesDirectly("CultureEntity", filter);
                adapter.DeleteEntitiesDirectly("JournalEntity", filter);
            }

        }

        public static TimeZoneInformation GetCentralTime()
        {

            foreach (TimeZoneInformation zone in TimeZoneInformation.USTimeZones)
            {
                if (zone.DisplayName.Contains("Central Time"))
                    return zone;
            }
            return null;
        }

        public class RegData : IRegistrationData
        {

            #region IRegistrationData Members

            public Payjr.Core.UserInfo.IParentInfo parentInfo
            {
                get { return _parent as IParentInfo; }
            }

            public Payjr.Core.UserInfo.IAddress parentAddress
            {
                get { return _parent.UserAddress as IAddress; }
            }

            public Payjr.Core.UserInfo.IPhone parentPhone
            {
                get { return _parent.Phone as IPhone; }
            }

            public Payjr.Core.UserInfo.IEmail parentEmail
            {
                get { return _parent.Email as IEmail; }
            }

            public Payjr.Core.UserInfo.IUserIdentifier parentIdentifier
            {
                get { return _parent.SSN as IUserIdentifier; }
            }

            public Payjr.Core.UserInfo.IChildInfo childInfo
            {
                get { return _teen as IChildInfo; }
            }

            public Payjr.Core.UserInfo.IAddress childAddress
            {
                get { return _teen.UserAddress as IAddress; }
            }

            public Payjr.Core.UserInfo.IPhone childPhone
            {
                get { return _teen.Phone as IPhone; }
            }

            public Payjr.Core.UserInfo.IEmail childEmail
            {
                get { return _teen.Email as IEmail; }
            }

            public Payjr.Core.UserInfo.IUserIdentifier childIdentifier
            {
                get { return _teen.SSN as IUserIdentifier; }
            }

            LoadInfo _loadInfo;
            public Payjr.Core.UserInfo.IInitialLoadInfo initialLoadInfo
            {
                get
                {
                    if (_loadInfo == null)
                    {
                        _loadInfo = LoadInfo.GetLoadInfo();
                    }
                    return _loadInfo as IInitialLoadInfo;
                }
            }

            public Payjr.Core.FinancialAccounts.ICreditCardInfo creditCardInfo
            {
                get
                {
                    if (_useCredit)
                        return _parent.FinancialAccounts.ActiveCreditCardAccount as ICreditCardInfo;
                    return new EmptyCreditInfo();
                }
            }

            CardInfo _cardInfo;
            public Payjr.Core.FinancialAccounts.ICardInfo cardInfo
            {
                get
                {
                    if (_cardInfo == null)
                    {
                        _cardInfo = CardInfo.GetCardInfo();
                    }
                    return _cardInfo as ICardInfo;
                }
            }

            public Payjr.Core.FinancialAccounts.ISavingsAccountInfo parentSavingsAccount
            {
                get { return _parent.FinancialAccounts.ActiveSavingsAccount as ISavingsAccountInfo; }
            }

            public Payjr.Core.FinancialAccounts.ISavingsAccountInfo childSavingsAccount
            {
                get { return _teen.FinancialAccounts.ActiveSavingsAccount as ISavingsAccountInfo; }
            }

            public decimal initialLoad
            {
                get { return 10.0M; }
            }

            public string ParentPassword
            {
                get { return "Password"; }
            }

            public string ChildPassword
            {
                get { return "Password"; }
            }

            #endregion

            private Teen _teen;
            private Parent _parent;
            private bool _useCredit;
            public RegData(Teen teen, Parent parent, bool useCredit)
            {
                _teen = teen;
                _parent = parent;
                _useCredit = useCredit;
            }


            private class EmptyCreditInfo : Payjr.Core.FinancialAccounts.ICreditCardInfo
            {

                #region ICreditCardInfo Members

                public string CardNumber
                {
                    get
                    {
                        return "";
                    }
                    set
                    {
                        return;
                    }
                }

                public string CardIdentifier
                {
                    get
                    {
                        return "";
                    }
                    set
                    {
                        return;
                    }
                }

                public string NickName
                {
                    get
                    {
                        return "VISA";
                    }
                    set
                    {
                        return;
                    }
                }

                public bool IsDefault
                {
                    get
                    {
                        return true;
                    }
                    set
                    {
                        return;
                    }
                }

                public CreditCardType Type
                {
                    get
                    {
                        return CreditCardType.VISA;
                    }
                    set
                    {
                        return;
                    }
                }

                public DateTime ExpirationDate
                {
                    get
                    {
                        return DateTime.Now.AddYears(1);
                    }
                    set
                    {
                        return;
                    }
                }

                public string CID
                {
                    get
                    {
                        return "";
                    }
                    set
                    {
                        return;
                    }
                }

                public string PAR
                {
                    get
                    {
                        return String.Empty;
                    }
                    set
                    {
                        return;
                    }
                }

                #endregion
            }
        }

        public class CardInfo : ICardInfo
        {

            #region ICardInfo Members

            private string _stockCardIdentifier;
            public string StockCardIdentifier
            {
                get
                {
                    return _stockCardIdentifier;
                }
                set
                {
                    _stockCardIdentifier = value;
                }
            }

            private string _stockCardImageSrc;
            public string StockCardImageSrc
            {
                get
                {
                    return _stockCardImageSrc;
                }
                set
                {
                    _stockCardImageSrc = value;
                }
            }

            private bool _isCardDesignedByChild;
            public bool IsCardDesignedByChild
            {
                get
                {
                    return _isCardDesignedByChild;
                }
                set
                {
                    _isCardDesignedByChild = value;
                }
            }

            private string _customCardID;
            public string CustomCardID
            {
                get
                {
                    return _customCardID;
                }
                set
                {
                    _customCardID = value;
                }
            }

            public string CustomCardSrc
            {
                get
                {
                    return "";
                }
            }

            #endregion

            public static CardInfo GetCardInfo()
            {
                CardInfo cardInfo = new CardInfo();
                cardInfo.IsCardDesignedByChild = true;
                cardInfo.StockCardIdentifier = "01";
                cardInfo.StockCardImageSrc = "";
                cardInfo.CustomCardID = "";

                return cardInfo;
            }
        }

        public class LoadInfo : IInitialLoadInfo
        {
            #region IInitialLoadInfo Members

            private decimal _loadAmount;
            public decimal LoadAmount
            {
                get { return _loadAmount; }
                set { _loadAmount = value; }
            }

            private RecurrencePattern _pattern;
            public RecurrencePattern Pattern
            {
                get { return _pattern; }
                set { _pattern = value; }
            }

            #endregion //IInitialLoadInfo

            public static LoadInfo GetLoadInfo()
            {
                LoadInfo loadInfo = new LoadInfo();
                loadInfo.LoadAmount = 12.00M;
                loadInfo.Pattern = CreateDailyPattern();
                return loadInfo;
            }

        }

        public class TestGatewayReply : IGatewayReply
        {
            #region Fields

            private bool _wasSuccesful;
            private int _reasonCode;
            private string _decision;
            private string _decisionMessage;
            private string _requestToken;
            private string _requestID;
            private object _sourceGateWayReply;
            private string _acsUrl;
            private string _paReq;
            private string _authenticateData;
            private HybridDictionary _innerReplyFields;
            private bool _isDecisionManagerRecommended;

            #endregion //Fields

            #region IGatewayReply Members

            public bool WasSuccesful
            {
                get { return _wasSuccesful; }
                set { _wasSuccesful = value; }
            }

            public int ReasonCode
            {
                get { return _reasonCode; }
                set { _reasonCode = value; }
            }

            public string[] MissingFields
            {
                get { throw new Exception("The method or operation is not implemented."); }
            }

            public string[] InvalidFields
            {
                get { throw new Exception("The method or operation is not implemented."); }
            }

            public string Decision
            {
                get { return _decision; }
                set { _decision = value; }
            }

            public string DecisionMessage
            {
                get { return _decisionMessage; }
                set { _decisionMessage = value; }
            }

            public string RequestToken
            {
                get { return _requestToken; }
                set { _requestToken = value; }
            }

            public string RequestID
            {
                get { return _requestID; }
                set { _requestID = value; }
            }

            public object SourceGateWayReply
            {
                get { return _sourceGateWayReply; }
                set { _sourceGateWayReply = value; }
            }

            public Exception ProcessException
            {
                get { throw new Exception("The method or operation is not implemented."); }
            }

            public string ACSUrl
            {
                get
                {
                    return _acsUrl;
                }
                set
                {
                    _acsUrl = value;
                }
            }

            public string PaReq
            {
                get
                {
                    return _paReq;
                }
                set
                {
                    _paReq = value;
                }
            }

            public string AuthenticationData
            {
                get { return _authenticateData; }
                set { _authenticateData = value; }
            }

            public HybridDictionary InnerReplyFields
            {
                get { return _innerReplyFields; }
                set { _innerReplyFields = value; }
            }

            public bool IsDecisionManagerRecommended
            {
                get { return _isDecisionManagerRecommended; }
                set { _isDecisionManagerRecommended = value; }
            }

            #endregion
        }

        public static TestGatewayReply CreateAuthorizationReply()
        {
            TestEntityFactory.TestGatewayReply reply = new TestEntityFactory.TestGatewayReply();
            reply.ACSUrl = "ACSUrl";
            reply.Decision = "Decision";
            reply.DecisionMessage = "DecisionMessage";
            reply.PaReq = "PaReq";
            reply.ReasonCode = 3;
            reply.RequestID = "RequestID";
            reply.RequestToken = "RequestToken";
            reply.WasSuccesful = true;
            reply.AuthenticationData = "AuthenticationData";

            CCAuthReply authReply = new CCAuthReply();
            authReply.accountBalance = "0.0";
            authReply.amount = "0.0";
            authReply.authenticationXID = "authenticationXID";
            authReply.authFactorCode = "authFactorCode";
            authReply.authorizationCode = "authorizationCode";
            authReply.authorizationXID = "authorizationXID";
            authReply.authorizedDateTime = DateTime.Now.ToShortDateString();
            authReply.authRecord = "authRecord";
            authReply.avsCode = "avsCode";
            authReply.avsCodeRaw = "avsCodeRaw";
            authReply.bmlAccountNumber = "bmlAccountNumber";
            authReply.cavvResponseCode = "cavvResponseCode";
            authReply.cavvResponseCodeRaw = "cavvResponseCodeRaw";
            authReply.cvCode = "cvCode";
            authReply.cvCodeRaw = "cvCodeRaw";
            authReply.enhancedDataEnabled = "enhanceddataEnabled";
            authReply.forwardCode = "forwardCode";
            authReply.fundingTotals = null;
            authReply.fxQuoteExpirationDateTime = DateTime.Now.ToShortDateString();
            authReply.fxQuoteID = "fxQuoteID";
            authReply.fxQuoteRate = "fxQuoteRate";
            authReply.fxQuoteType = "fxQuoteType";
            authReply.merchantAdviceCode = "merchantAdviceCode";
            authReply.merchantAdviceCodeRaw = "merchantAdviceCodeRaw";
            authReply.personalIDCode = "personalIDCode";
            authReply.processorCardType = "processorCardType";
            authReply.processorResponse = "processorResponse";
            authReply.reasonCode = "5";
            authReply.reconciliationID = "reconciliationID";
            authReply.referralResponseNumber = "2";

            ReplyMessage replyMessage = new ReplyMessage();
            replyMessage.ccAuthReply = authReply;

            reply.SourceGateWayReply = replyMessage;

            return reply;
        }

        public static TestGatewayReply CreateCheckEnrollmentReply()
        {
            TestEntityFactory.TestGatewayReply reply = new TestEntityFactory.TestGatewayReply();
            reply.ACSUrl = "ACSUrl";
            reply.Decision = "Decision";
            reply.DecisionMessage = "DecisionMessage";
            reply.PaReq = "PaReq";
            reply.ReasonCode = 3;
            reply.RequestID = "RequestID";
            reply.RequestToken = "RequestToken";
            reply.WasSuccesful = true;

            PayerAuthEnrollReply enrollReply = new PayerAuthEnrollReply();
            enrollReply.acsURL = "www.payjr.com";
            enrollReply.commerceIndicator = "commerceIndicator";
            enrollReply.paReq = "paReq";
            enrollReply.proofXML = "proofXML";
            enrollReply.proxyPAN = "proxyPAN";
            enrollReply.reasonCode = "4";
            enrollReply.ucafCollectionIndicator = "ucafCollectionIndicator";
            enrollReply.xid = "xid";

            ReplyMessage replyMessage = new ReplyMessage();
            replyMessage.payerAuthEnrollReply = enrollReply;

            reply.SourceGateWayReply = replyMessage;
            return reply;
        }

        public static TestGatewayReply CreateCheckValidationReply()
        {
            TestEntityFactory.TestGatewayReply reply = new TestEntityFactory.TestGatewayReply();
            reply.ACSUrl = "ACSUrl";
            reply.Decision = "Decision";
            reply.DecisionMessage = "DecisionMessage";
            reply.PaReq = "PaReq";
            reply.ReasonCode = 3;
            reply.RequestID = "RequestID";
            reply.RequestToken = "RequestToken";
            reply.WasSuccesful = true;

            PayerAuthValidateReply validateReply = new PayerAuthValidateReply();
            validateReply.authenticationResult = "authResult";
            validateReply.authenticationStatusMessage = "authStatusMessage";
            validateReply.cavv = "cavv";
            validateReply.commerceIndicator = "05";
            validateReply.eci = "eci";
            validateReply.eciRaw = "eciRaw";
            validateReply.reasonCode = "0";
            validateReply.ucafAuthenticationData = "ucafAuthData";
            validateReply.ucafCollectionIndicator = "ucafColectionIndicator";
            validateReply.xid = "xid";

            ReplyMessage replyMessage = new ReplyMessage();
            replyMessage.payerAuthValidateReply = validateReply;

            reply.SourceGateWayReply = replyMessage;
            return reply;
        }

        public static TestGatewayReply CreateCaptureReply()
        {
            TestEntityFactory.TestGatewayReply reply = new TestEntityFactory.TestGatewayReply();
            reply.ACSUrl = "ACSUrl";
            reply.Decision = "Decision";
            reply.DecisionMessage = "DecisionMessage";
            reply.PaReq = "PaReq";
            reply.ReasonCode = 3;
            reply.RequestID = "RequestID";
            reply.RequestToken = "RequestToken";
            reply.WasSuccesful = true;

            CCCaptureReply captureReply = new CCCaptureReply();
            captureReply.amount = "5";
            captureReply.enhancedDataEnabled = "enhanceDataEnabled";
            captureReply.fundingTotals = null;
            captureReply.fxQuoteExpirationDateTime = DateTime.Now.ToShortDateString();
            captureReply.fxQuoteID = "fxQuoteID";
            captureReply.fxQuoteRate = "fxQuoteRate";
            captureReply.fxQuoteType = "fxQuoteType";
            captureReply.purchasingLevel3Enabled = "false";
            captureReply.reasonCode = "3";
            captureReply.reconciliationID = "rconciliationID";
            captureReply.requestDateTime = DateTime.Now.ToShortDateString();

            ReplyMessage replyMessage = new ReplyMessage();
            replyMessage.ccCaptureReply = captureReply;

            reply.SourceGateWayReply = replyMessage;
            return reply;
        }

        public static TestGatewayReply CreateReverseAuthReply()
        {
            TestEntityFactory.TestGatewayReply reply = new TestEntityFactory.TestGatewayReply();
            reply.ACSUrl = "ACSUrl";
            reply.Decision = "Decision";
            reply.DecisionMessage = "DecisionMessage";
            reply.PaReq = "PaReq";
            reply.ReasonCode = 3;
            reply.RequestID = "RequestID";
            reply.RequestToken = "RequestToken";
            reply.WasSuccesful = true;

            CCAuthReversalReply authRevReply = new CCAuthReversalReply();
            authRevReply.amount = "10";
            authRevReply.authorizationCode = "authCode";
            authRevReply.forwardCode = "forwardCode";
            authRevReply.processorResponse = "processorResponse";
            authRevReply.reasonCode = "7";
            authRevReply.requestDateTime = DateTime.Now.ToShortDateString();

            ReplyMessage replyMessage = new ReplyMessage();
            replyMessage.ccAuthReversalReply = authRevReply;

            reply.SourceGateWayReply = replyMessage;
            return reply;
        }

        public static TestGatewayReply CreateCreditReply(String Amount)
        {
            TestEntityFactory.TestGatewayReply reply = new TestEntityFactory.TestGatewayReply();
            reply.ACSUrl = "ACSUrl";
            reply.Decision = "Decision";
            reply.DecisionMessage = "DecisionMessage";
            reply.PaReq = "PaReq";
            reply.ReasonCode = 3;
            reply.RequestID = "RequestID";
            reply.RequestToken = "RequestToken";
            reply.WasSuccesful = true;

            CCCreditReply creditReply = new CCCreditReply();
            creditReply.amount = Amount;
            creditReply.authorizationXID = "authXID";
            creditReply.enhancedDataEnabled = "enhancedDataEnabled";
            creditReply.forwardCode = "forwardCode";
            creditReply.purchasingLevel3Enabled = "false";
            creditReply.reasonCode = "6";
            creditReply.reconciliationID = "reconciliationID";
            creditReply.requestDateTime = DateTime.Now.ToShortDateString();

            ReplyMessage replyMessage = new ReplyMessage();
            replyMessage.ccCreditReply = creditReply;

            reply.SourceGateWayReply = replyMessage;
            return reply;
        }

        public class TestMonthlyFeeForm : IMonthlyFeeForm
        {
            private SqlDataReader _reader;
            public string _message;

            #region IMonthlyFeeForm Members

            public bool CanGenerate
            {
                get
                {
                    return true;
                }
                set
                {

                }
            }

            public System.Windows.Forms.ProgressBar GenerationProgress
            {
                get
                {
                    return new System.Windows.Forms.ProgressBar();
                }
                set
                {

                }
            }

            public SqlDataReader Reader
            {
                get
                {
                    return _reader;
                }
                set
                {
                    _reader = value;
                }
            }

            public void AddMessage(Exception exception)
            {
                _message += exception.ToString();
            }

            public void AddMessage(string formatMessage, params string[] message)
            {
                _message += string.Format(formatMessage, message) + "\r\n";
            }

            #endregion

            public TestMonthlyFeeForm(SqlDataReader reader)
            {
                _reader = reader;
                _message = string.Empty;
            }
        }

        public static CardTransactionQueueEntity CreateCardTransactionQueueEntity(string cardNumber) {
            CardTransactionQueueEntity entity = new CardTransactionQueueEntity();

            FileProcessingAuditEntity audit = new FileProcessingAuditEntity();

            audit.ActingUserId = null;
            audit.AppliedRecords = 0;
            audit.ComputerName = Environment.MachineName;
            audit.Description = "Something";
            audit.Direction = 0;
            audit.Filename = "something.txt";
            audit.FileStatus = ProcessAuditStatus.Processing;
            audit.ReceivedRecords = 1;
            audit.StartDatetime = DateTime.UtcNow;
            audit.EndDatetime = DateTime.UtcNow;
            audit.FileType = ProcessAuditType.FSVCardSettlement;

            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                adapter.SaveEntity(audit);
            }

            entity.Amount = 10;
            entity.ApplyLogic = true;
            entity.Fee = 1;
            entity.Failure = false;
            entity.FileProcessingAudit = audit;
            entity.PrepaidCardNumber = cardNumber;
            entity.PrepaidCardNumberLastFour = cardNumber.Substring(12, 4);
            entity.Imported = false;
            entity.MerchantId = "MerchantId";
            entity.MerchantNameAddress = "MerchantNameAddress";
            entity.MerchantRef = "MerchantRef";
            entity.Mmc = "Mmc";
            entity.Processed = false;
            entity.Ref1 = "Ref1";
            entity.Ref2 = "Ref2";
            entity.RunningBalance = 20;
            entity.TerminalId = "TerminalId";
            entity.TransId = "1234";
            entity.TransactionDate = DateTime.UtcNow;
            entity.TransactionEntryDate = DateTime.UtcNow;
            entity.TransactionType = "1234";

            AdapterFactory.TransactionAdapter.SaveCardTransactionQueue(entity);

            return entity;
        }
        public static void CreateCardTransaction(PrepaidCardAccount account, string transactionType, string ref1, string merchantRef, DateTime transactionDate)
        {

            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                CardTransactionEntity cardTrans = new CardTransactionEntity();

                //Go get the purse
                cardTrans.PurseId = null;
                cardTrans.PrepaidAccountId = account.AccountID;
                cardTrans.Amount = 2.00M;
                cardTrans.Fee = 0.00M;
                cardTrans.TransactionType = transactionType;
                cardTrans.TransactionDate = transactionDate;
                cardTrans.TransactionEntryDate = transactionDate;
                cardTrans.RunningBalance = 0.00M;
                cardTrans.TranId = "123";
                cardTrans.Ref1 = ref1;
                cardTrans.Ref2 = "";
                cardTrans.Mmc = "1234";
                cardTrans.MerchantId = "ID";
                cardTrans.TerminalId = "1234";
                cardTrans.MerchantRef = merchantRef;
                cardTrans.MerchantNameAddress = "NAMEADDRESS";
                cardTrans.PrepaidCardNumber = "1234567891234";
                cardTrans.PrepaidCardNumber = account.CardNumber;
                cardTrans.PrepaidCardNumberLastFour = account.CardNumber.Substring(account.CardNumber.Length - 4, 4);
                // vcReference;
                //FiservID;
                if (!adapter.SaveEntity(cardTrans))
                {
                   
                }
            }
        }
    }
}
