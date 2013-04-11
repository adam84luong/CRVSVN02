using System;
using Common.Business;
using Common.Contracts.Prepaid.Records;
using Common.Contracts.Prepaid.Requests;
using Common.Contracts.Shared.Records;
using Common.Types;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Payjr.Core.FinancialAccounts;
using Payjr.Core.Test.Providers;
using Payjr.Core.Providers;
using Payjr.Core.Users;
using Payjr.Entity;
using Payjr.Entity.DatabaseSpecific;
using Payjr.Entity.EntityClasses;
using Payjr.Core.ServiceCommands.Prepaid;
using Payjr.Util.Test;
using Payjr.Core.FSV;
using Common.FSV.WebService;
using Payjr.Entity.HelperClasses;
using SD.LLBLGen.Pro.ORMSupportClasses;
using Payjr.Entity.FactoryClasses;


namespace Payjr.Core.Test.ServiceCommands.Prepaid
{
    [TestClass]
    public class GetCardDetailsServiceCommandTest : TestBase
    {
        private MockRepository _mocks;
        private TestProviderFactory _providers;
        private BrandingEntity _branding;

        [TestInitialize]
        public void InitializeTest()
        {
            _mocks = new MockRepository(MockBehavior.Default);
            _providers = new TestProviderFactory(_mocks);
        }

        [TestMethod]
        
        public void ExecuteTestSucess()
        {
            TestEntityFactory.ClearAll();
            Parent parent;
            Teen teen;
            _branding = TestEntityFactory.CreateBranding("Giftcardlab");
            ThemeEntity theme = TestEntityFactory.CreateTheme("Buxx");
            CultureEntity culture = TestEntityFactory.CreateCulture("Culture");
            TestEntityFactory.CreateTeen(_branding, theme, culture, out parent, out teen, true);
            TestEntityFactory.CreatePrepaidModule(_branding.BrandingId);
            CreatePrepaidAccount(teen, true, PrepaidCardStatus.Good);

            CardHolderResponse cardHolderResponse = new CardHolderResponse("123", "Message",
                                                                           "<CardHolderInfo> <CardInfo Card= \"4315830000002628\" nbATTMID=\"2853186\" ReferredBy= \"2459883\" Company= \"2459883\" RegistrationDate= \"09/02/2005\" LastUpdate= \"09/16/2005\" LastName= \"Urban\" FirstName= \"John1\" MiddleName= \"\" Title= \"Coder\" LastName2= \"\" FirstName2= \"\" MiddleName2= \"\" AddrLine1= \"3410 W. Atlanta St.\" AddrLine2= \"\" City= \"Broken Arrow\" State= \"OK\" Country= \"US\" PostalCode= \"74012\" BirthDate= \"12/27/1967\" SSN= \"123121234\" MatriculaNumber= \"1234\" DriverLicense= \"01829304\" DLState= \"OK\" Email= \"jurban@gtplimited.com\" HomePhone= \"9183938393\" OfficePhone= \"\" MobilePhone= \"\" FaxPhone= \"\" ChallengeID= \"1000\" LicenseID= \"393709\" Subcompany= \"2459883\" Status= \"PA\" ExpirationDate=\"08/31/2008\" CardType= \"\" OtherCompanyName= \"\" PictureID= \"\" /></CardHolderInfo>");

            var fsvWebServiceMock = new Mock<IFsvWebService>();
            Balance balanceResponse = new Balance("1", @"Success", "100", "100","100","100");
            fsvWebServiceMock.Setup(x => x.GetCHInformation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(cardHolderResponse);
            fsvWebServiceMock.Setup(x => x.PrepaidCardBalanceInquiry(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(balanceResponse);
            
            IFsvWebService _interface = fsvWebServiceMock.Object;
            FSVBankFirstCardProvider provider = new FSVBankFirstCardProvider(GetProviderEntity(), teen, _interface);
            
            // create request
            var request = new PrepaidCardSearchRequest();
            request.Configuration = new RetrievalConfigurationRecord()
                                        {
                                            ApplicationKey = Guid.NewGuid()
                                        };
            var prepaidCardSearch = new PrepaidCardSearchCriteria();
            prepaidCardSearch.FirstName = teen.FirstName;
            prepaidCardSearch.LastName = teen.LastName;
            prepaidCardSearch.UserIdentifier = new Identifiers.UserIdentifier(teen.UserID).Identifier;
            prepaidCardSearch.AddressLine1 = teen.Address1;
            prepaidCardSearch.AddressLine2 = teen.Address2;

            request.Requests.Add(prepaidCardSearch);

            var target = new GetCardDetailsServiceCommand(_providers);
            target.CardProvider = provider;
            var result = target.Execute(request);

            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Records.Count);            
            Assert.AreEqual(100, result.Records[0].CardBalance);
            Assert.AreEqual(teen.FirstName, result.Records[0].CardHolder.FirstName);
            Assert.AreEqual(PrepaidCardStatus2.Activated, result.Records[0].CardStatus2);
        }

        [TestMethod]
        public void ExecuteTestFail_PrepaidCardNotFound()
        {
            TestEntityFactory.ClearAll();
            Parent parent;
            Teen teen;
            _branding = TestEntityFactory.CreateBranding("Giftcardlab");
            ThemeEntity theme = TestEntityFactory.CreateTheme("Buxx");
            CultureEntity culture = TestEntityFactory.CreateCulture("Culture");
            TestEntityFactory.CreateTeen(_branding, theme, culture, out parent, out teen, true);
            TestEntityFactory.CreatePrepaidModule(_branding.BrandingId);
            
            CardHolderResponse cardHolderResponse = new CardHolderResponse("123", "Message",
                                                                           "<CardHolderInfo> <CardInfo Card= \"4315830000002628\" nbATTMID=\"2853186\" ReferredBy= \"2459883\" Company= \"2459883\" RegistrationDate= \"09/02/2005\" LastUpdate= \"09/16/2005\" LastName= \"Urban\" FirstName= \"John1\" MiddleName= \"\" Title= \"Coder\" LastName2= \"\" FirstName2= \"\" MiddleName2= \"\" AddrLine1= \"3410 W. Atlanta St.\" AddrLine2= \"\" City= \"Broken Arrow\" State= \"OK\" Country= \"US\" PostalCode= \"74012\" BirthDate= \"12/27/1967\" SSN= \"123121234\" MatriculaNumber= \"1234\" DriverLicense= \"01829304\" DLState= \"OK\" Email= \"jurban@gtplimited.com\" HomePhone= \"9183938393\" OfficePhone= \"\" MobilePhone= \"\" FaxPhone= \"\" ChallengeID= \"1000\" LicenseID= \"393709\" Subcompany= \"2459883\" Status= \"PA\" ExpirationDate=\"08/31/2008\" CardType= \"\" OtherCompanyName= \"\" PictureID= \"\" /></CardHolderInfo>");

            var fsvWebServiceMock = new Mock<IFsvWebService>();
            Balance balanceResponse = new Balance("1", @"Success", "100", "100","100","100");
            fsvWebServiceMock.Setup(
                x =>
                x.GetCHInformation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                                   It.IsAny<string>())).Returns(cardHolderResponse);
            fsvWebServiceMock.Setup(
                x =>
                x.PrepaidCardBalanceInquiry(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                                   It.IsAny<string>())).Returns(balanceResponse);
            IFsvWebService _interface = fsvWebServiceMock.Object;
            FSVBankFirstCardProvider provider = new FSVBankFirstCardProvider(GetProviderEntity(), teen, _interface);
            
            // create request
            var request = new PrepaidCardSearchRequest();
            request.Configuration = new RetrievalConfigurationRecord()
                                        {
                                            ApplicationKey = Guid.NewGuid()
                                        };
            var prepaidCardSearch = new PrepaidCardSearchCriteria();
            prepaidCardSearch.FirstName = teen.FirstName;
            prepaidCardSearch.LastName = teen.LastName;
            prepaidCardSearch.UserIdentifier = new Identifiers.UserIdentifier(teen.UserID).Identifier;
            prepaidCardSearch.AddressLine1 = teen.Address1;
            prepaidCardSearch.AddressLine2 = teen.Address2;

            request.Requests.Add(prepaidCardSearch);
            var target = new GetCardDetailsServiceCommand(_providers);
            target.CardProvider = provider;
            var result = target.Execute(request);

            Assert.IsNotNull(result);           
            Assert.AreEqual(0, result.Records.Count);
        }

        [TestMethod]
        public void ExecuteTestFail_TeenNotFound()
        {
            TestEntityFactory.ClearAll();
            Parent parent;
            Teen teen;
            _branding = TestEntityFactory.CreateBranding("Giftcardlab");
            ThemeEntity theme = TestEntityFactory.CreateTheme("Buxx");
            CultureEntity culture = TestEntityFactory.CreateCulture("Culture");
            TestEntityFactory.CreateTeen(_branding, theme, culture, out parent, out teen, true);
            TestEntityFactory.CreatePrepaidModule(_branding.BrandingId);
            CreatePrepaidAccount(teen, true, PrepaidCardStatus.Good);

            CardHolderResponse cardHolderResponse = new CardHolderResponse("123", "Message",
                                                                           "<CardHolderInfo> <CardInfo Card= \"4315830000002628\" nbATTMID=\"2853186\" ReferredBy= \"2459883\" Company= \"2459883\" RegistrationDate= \"09/02/2005\" LastUpdate= \"09/16/2005\" LastName= \"Urban\" FirstName= \"John1\" MiddleName= \"\" Title= \"Coder\" LastName2= \"\" FirstName2= \"\" MiddleName2= \"\" AddrLine1= \"3410 W. Atlanta St.\" AddrLine2= \"\" City= \"Broken Arrow\" State= \"OK\" Country= \"US\" PostalCode= \"74012\" BirthDate= \"12/27/1967\" SSN= \"123121234\" MatriculaNumber= \"1234\" DriverLicense= \"01829304\" DLState= \"OK\" Email= \"jurban@gtplimited.com\" HomePhone= \"9183938393\" OfficePhone= \"\" MobilePhone= \"\" FaxPhone= \"\" ChallengeID= \"1000\" LicenseID= \"393709\" Subcompany= \"2459883\" Status= \"PA\" ExpirationDate=\"08/31/2008\" CardType= \"\" OtherCompanyName= \"\" PictureID= \"\" /></CardHolderInfo>");

            var fsvWebServiceMock = new Mock<IFsvWebService>();
            Balance balanceResponse = new Balance("1", @"Success", "100", "100", "100", "100");
            fsvWebServiceMock.Setup(x => x.GetCHInformation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(cardHolderResponse);
            fsvWebServiceMock.Setup(x => x.PrepaidCardBalanceInquiry(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(balanceResponse);

            IFsvWebService _interface = fsvWebServiceMock.Object;
            FSVBankFirstCardProvider provider = new FSVBankFirstCardProvider(GetProviderEntity(), teen, _interface);

            // create request
            var request = new PrepaidCardSearchRequest();
            request.Configuration = new RetrievalConfigurationRecord()
            {
                ApplicationKey = Guid.NewGuid()
            };
            var prepaidCardSearch = new PrepaidCardSearchCriteria();
            prepaidCardSearch.FirstName = teen.FirstName;
            prepaidCardSearch.LastName = teen.LastName;
            prepaidCardSearch.UserIdentifier = new Identifiers.UserIdentifier(parent.UserID).Identifier;
            prepaidCardSearch.AddressLine1 = teen.Address1;
            prepaidCardSearch.AddressLine2 = teen.Address2;

            request.Requests.Add(prepaidCardSearch);

            var target = new GetCardDetailsServiceCommand(_providers);
            target.CardProvider = provider;
            var result = target.Execute(request);

            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Records.Count);
        }

        #region Helper

        private FsvcardProviderEntity GetProviderEntity()
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter())
            {
                EntityCollection<FsvcardProviderEntity> provEntity = new EntityCollection<FsvcardProviderEntity>(new FsvcardProviderEntityFactory());

                IPrefetchPath2 path = new PrefetchPath2((int)EntityType.ProviderEntity);
                path.Add(FsvcardProviderEntity.PrefetchPathPrepaidModules);
                path.Add(FsvcardProviderEntity.PrefetchPathProviderNetworkConfig);

                IRelationPredicateBucket bucket = new RelationPredicateBucket();
                bucket.Relations.Add(FsvcardProviderEntity.Relations.PrepaidModuleEntityUsingDestinationProviderId);
                bucket.Relations.Add(FsvcardProviderEntity.Relations.ProviderNetworkConfigEntityUsingProviderNetworkConfigId);
                bucket.PredicateExpression.Add(PrepaidModuleFields.BrandingId == _branding.BrandingId);
                adapter.FetchEntityCollection(provEntity, bucket, path);
                if (provEntity.Count > 0)
                {
                    return provEntity[0];
                }
                else
                {
                    return null;
                }
            }
        }

        private void CreatePrepaidAccount(Teen user, bool isActive, PrepaidCardStatus status)
        {
            using (DataAccessAdapter adapter = new DataAccessAdapter(true))
            {
                PrepaidCardAccountEntity prepaidCard = new PrepaidCardAccountEntity();
                prepaidCard.ActivationMethod = PrepaidActivationMethod.unknown;
                prepaidCard.Active = isActive;
                prepaidCard.ActiveteDateTime = null;
                prepaidCard.BrandingCardDesignId = null;
                prepaidCard.CardIdentifier = null;
                prepaidCard.CardNumber = "213156484984651";
                prepaidCard.LostStolenDateTime = null;
                prepaidCard.MarkedForDeletion = false;
                prepaidCard.Status = status;
                prepaidCard.UserCardDesignId = null;

                PrepaidCardAccountUserEntity prepaidCardUser = new PrepaidCardAccountUserEntity();
                prepaidCardUser.UserId = user.UserID;
                prepaidCardUser.PrepaidCardAccount = prepaidCard;
                adapter.SaveEntity(prepaidCardUser);
            }
        }

        #endregion
    }
}
