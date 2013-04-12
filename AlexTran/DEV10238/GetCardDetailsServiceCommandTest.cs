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
        private PrepaidCardSearchRequest _request;      
        private MockRepository _mocks;
        private TestProviderFactory _providers;
        private BrandingEntity _branding;
        private Parent _parent;
        private Teen _teen;
        private ThemeEntity _theme;
        private CultureEntity _culture;

        [TestInitialize]
        public void InitializeTest()
        {
            TestEntityFactory.ClearAll();          
            _branding = TestEntityFactory.CreateBranding("Giftcardlab");
            _theme = TestEntityFactory.CreateTheme("Buxx");
            _culture = TestEntityFactory.CreateCulture("Culture");
            TestEntityFactory.CreateTeen(_branding, _theme, _culture, out _parent, out _teen, true);
            TestEntityFactory.CreatePrepaidModule(_branding.BrandingId);
            _mocks = new MockRepository(MockBehavior.Default);
            _providers = new TestProviderFactory(_mocks);
            _request = CreatePrepaidCardSearchRequest(true);   
        }

        [TestMethod]        
        public void ExecuteTestSucess_SearchByUserIdentifier()
        {    
            TestEntityFactory.CreatePrepaidAccount(_teen, true, PrepaidCardStatus.Good);
            CardHolderResponse cardHolderResponse = new CardHolderResponse("123", "Message",
                                                                           "<CardHolderInfo> <CardInfo Card= \"4315830000002628\" nbATTMID=\"2853186\" ReferredBy= \"2459883\" Company= \"2459883\" RegistrationDate= \"09/02/2005\" LastUpdate= \"09/16/2005\" LastName= \"Urban\" FirstName= \"John1\" MiddleName= \"\" Title= \"Coder\" LastName2= \"\" FirstName2= \"\" MiddleName2= \"\" AddrLine1= \"3410 W. Atlanta St.\" AddrLine2= \"\" City= \"Broken Arrow\" State= \"OK\" Country= \"US\" PostalCode= \"74012\" BirthDate= \"12/27/1967\" SSN= \"123121234\" MatriculaNumber= \"1234\" DriverLicense= \"01829304\" DLState= \"OK\" Email= \"jurban@gtplimited.com\" HomePhone= \"9183938393\" OfficePhone= \"\" MobilePhone= \"\" FaxPhone= \"\" ChallengeID= \"1000\" LicenseID= \"393709\" Subcompany= \"2459883\" Status= \"PA\" ExpirationDate=\"08/31/2008\" CardType= \"\" OtherCompanyName= \"\" PictureID= \"\" /></CardHolderInfo>");
            var fsvWebServiceMock = new Mock<IFsvWebService>();
            Balance balanceResponse = new Balance("1", @"Success", "100", "100","100","100");
            fsvWebServiceMock.Setup(x => x.GetCHInformation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(cardHolderResponse);
            fsvWebServiceMock.Setup(x => x.PrepaidCardBalanceInquiry(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(balanceResponse);
            
            IFsvWebService _interface = fsvWebServiceMock.Object;
            FSVBankFirstCardProvider provider = new FSVBankFirstCardProvider(GetProviderEntity(), _teen, _interface);

            _request.Requests[0].UserIdentifier = new Identifiers.UserIdentifier(_teen.UserID).Identifier;        
            var target = new GetCardDetailsServiceCommand(_providers);
            target.CardProvider = provider;
            var result = target.Execute(_request);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Records.Count);            
            Assert.AreEqual(100, result.Records[0].CardBalance);
            Assert.AreEqual(_teen.FirstName, result.Records[0].CardHolder.FirstName);
            Assert.AreEqual(PrepaidCardStatus2.Activated, result.Records[0].CardStatus2);
        }

        [TestMethod]
        public void ExecuteTestSucess_SearchByUserIdentifierAndCardNumber()
        {
            TestEntityFactory.CreatePrepaidAccount(_teen, true, PrepaidCardStatus.Good);
            CardHolderResponse cardHolderResponse = new CardHolderResponse("123", "Message",
                                                                           "<CardHolderInfo> <CardInfo Card= \"4315830000002628\" nbATTMID=\"2853186\" ReferredBy= \"2459883\" Company= \"2459883\" RegistrationDate= \"09/02/2005\" LastUpdate= \"09/16/2005\" LastName= \"Urban\" FirstName= \"John1\" MiddleName= \"\" Title= \"Coder\" LastName2= \"\" FirstName2= \"\" MiddleName2= \"\" AddrLine1= \"3410 W. Atlanta St.\" AddrLine2= \"\" City= \"Broken Arrow\" State= \"OK\" Country= \"US\" PostalCode= \"74012\" BirthDate= \"12/27/1967\" SSN= \"123121234\" MatriculaNumber= \"1234\" DriverLicense= \"01829304\" DLState= \"OK\" Email= \"jurban@gtplimited.com\" HomePhone= \"9183938393\" OfficePhone= \"\" MobilePhone= \"\" FaxPhone= \"\" ChallengeID= \"1000\" LicenseID= \"393709\" Subcompany= \"2459883\" Status= \"PA\" ExpirationDate=\"08/31/2008\" CardType= \"\" OtherCompanyName= \"\" PictureID= \"\" /></CardHolderInfo>");
            var fsvWebServiceMock = new Mock<IFsvWebService>();
            Balance balanceResponse = new Balance("1", @"Success", "100", "100", "100", "100");
            fsvWebServiceMock.Setup(x => x.GetCHInformation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(cardHolderResponse);
            fsvWebServiceMock.Setup(x => x.PrepaidCardBalanceInquiry(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(balanceResponse);

            IFsvWebService _interface = fsvWebServiceMock.Object;
            FSVBankFirstCardProvider provider = new FSVBankFirstCardProvider(GetProviderEntity(), _teen, _interface);

            _request.Requests[0].UserIdentifier = new Identifiers.UserIdentifier(_teen.UserID).Identifier;
            _request.Requests[0].CardNumberFull = "5150620000973224";
            var target = new GetCardDetailsServiceCommand(_providers);
            target.CardProvider = provider;
            var result = target.Execute(_request);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Records.Count);
            Assert.AreEqual(100, result.Records[0].CardBalance);
            Assert.AreEqual(_teen.FirstName, result.Records[0].CardHolder.FirstName);
            Assert.AreEqual(PrepaidCardStatus2.Activated, result.Records[0].CardStatus2);
        }
        [TestMethod]
        public void ExecuteTestSucess_SearchByPrepaidCardIdentifier()
        {
            TestEntityFactory.CreatePrepaidAccount(_teen, true, PrepaidCardStatus.Good);
            CardHolderResponse cardHolderResponse = new CardHolderResponse("123", "Message",
                                                                           "<CardHolderInfo> <CardInfo Card= \"4315830000002628\" nbATTMID=\"2853186\" ReferredBy= \"2459883\" Company= \"2459883\" RegistrationDate= \"09/02/2005\" LastUpdate= \"09/16/2005\" LastName= \"Urban\" FirstName= \"John1\" MiddleName= \"\" Title= \"Coder\" LastName2= \"\" FirstName2= \"\" MiddleName2= \"\" AddrLine1= \"3410 W. Atlanta St.\" AddrLine2= \"\" City= \"Broken Arrow\" State= \"OK\" Country= \"US\" PostalCode= \"74012\" BirthDate= \"12/27/1967\" SSN= \"123121234\" MatriculaNumber= \"1234\" DriverLicense= \"01829304\" DLState= \"OK\" Email= \"jurban@gtplimited.com\" HomePhone= \"9183938393\" OfficePhone= \"\" MobilePhone= \"\" FaxPhone= \"\" ChallengeID= \"1000\" LicenseID= \"393709\" Subcompany= \"2459883\" Status= \"PA\" ExpirationDate=\"08/31/2008\" CardType= \"\" OtherCompanyName= \"\" PictureID= \"\" /></CardHolderInfo>");
            var fsvWebServiceMock = new Mock<IFsvWebService>();
            Balance balanceResponse = new Balance("1", @"Success", "100", "100", "100", "100");
            fsvWebServiceMock.Setup(x => x.GetCHInformation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(cardHolderResponse);
            fsvWebServiceMock.Setup(x => x.PrepaidCardBalanceInquiry(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(balanceResponse);
            IFsvWebService _interface = fsvWebServiceMock.Object;
            FSVBankFirstCardProvider provider = new FSVBankFirstCardProvider(GetProviderEntity(), _teen, _interface);

            _request.Requests[0].PrepaidCardIdentifier = "PJRPCA:" + _teen.FinancialAccounts.ActivePrepaidCardAccount.AccountID.ToString().ToUpper();
            var target = new GetCardDetailsServiceCommand(_providers);
            target.CardProvider = provider;
            var result = target.Execute(_request);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Records.Count);
            Assert.AreEqual(100, result.Records[0].CardBalance);
            Assert.AreEqual(_teen.FirstName, result.Records[0].CardHolder.FirstName);
            Assert.AreEqual(PrepaidCardStatus2.Activated, result.Records[0].CardStatus2);
        }
        [TestMethod]
        public void ExecuteTestSucess_SearchByCardNumber()
        {
           TestEntityFactory.CreatePrepaidAccount(_teen, true, PrepaidCardStatus.Good);
            CardHolderResponse cardHolderResponse = new CardHolderResponse("123", "Message",
                                                                           "<CardHolderInfo> <CardInfo Card= \"4315830000002628\" nbATTMID=\"2853186\" ReferredBy= \"2459883\" Company= \"2459883\" RegistrationDate= \"09/02/2005\" LastUpdate= \"09/16/2005\" LastName= \"Urban\" FirstName= \"John1\" MiddleName= \"\" Title= \"Coder\" LastName2= \"\" FirstName2= \"\" MiddleName2= \"\" AddrLine1= \"3410 W. Atlanta St.\" AddrLine2= \"\" City= \"Broken Arrow\" State= \"OK\" Country= \"US\" PostalCode= \"74012\" BirthDate= \"12/27/1967\" SSN= \"123121234\" MatriculaNumber= \"1234\" DriverLicense= \"01829304\" DLState= \"OK\" Email= \"jurban@gtplimited.com\" HomePhone= \"9183938393\" OfficePhone= \"\" MobilePhone= \"\" FaxPhone= \"\" ChallengeID= \"1000\" LicenseID= \"393709\" Subcompany= \"2459883\" Status= \"PA\" ExpirationDate=\"08/31/2008\" CardType= \"\" OtherCompanyName= \"\" PictureID= \"\" /></CardHolderInfo>");
            var fsvWebServiceMock = new Mock<IFsvWebService>();
            Balance balanceResponse = new Balance("1", @"Success", "100", "100", "100", "100");
            fsvWebServiceMock.Setup(x => x.GetCHInformation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(cardHolderResponse);
            fsvWebServiceMock.Setup(x => x.PrepaidCardBalanceInquiry(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(balanceResponse);
            IFsvWebService _interface = fsvWebServiceMock.Object;
            FSVBankFirstCardProvider provider = new FSVBankFirstCardProvider(GetProviderEntity(), _teen, _interface);
      
            _request.Requests[0].CardNumberFull = "5150620000973224";
            var target = new GetCardDetailsServiceCommand(_providers);
            target.CardProvider = provider;
            var result = target.Execute(_request);
            Assert.IsNotNull(result);
            Assert.AreEqual(1, result.Records.Count);
            Assert.AreEqual(100, result.Records[0].CardBalance);
            Assert.AreEqual(_teen.FirstName, result.Records[0].CardHolder.FirstName);
            Assert.AreEqual(PrepaidCardStatus2.Activated, result.Records[0].CardStatus2);
        }
        [TestMethod]
        public void Execute_Failure_PrepaidCardNotFound()
        {
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
            FSVBankFirstCardProvider provider = new FSVBankFirstCardProvider(GetProviderEntity(), _teen, _interface);

            _request.Requests[0].UserIdentifier = new Identifiers.UserIdentifier(_teen.UserID).Identifier;
            var target = new GetCardDetailsServiceCommand(_providers);
            target.CardProvider = provider;
            var result = target.Execute(_request);
            Assert.IsNotNull(result);           
            Assert.AreEqual(0, result.Records.Count);
        }

        [TestMethod]
        public void Execute_Failure_TeenNotFound()
        {
            TestEntityFactory.CreatePrepaidAccount(_teen, true, PrepaidCardStatus.Good);
            CardHolderResponse cardHolderResponse = new CardHolderResponse("123", "Message",
                                                                           "<CardHolderInfo> <CardInfo Card= \"4315830000002628\" nbATTMID=\"2853186\" ReferredBy= \"2459883\" Company= \"2459883\" RegistrationDate= \"09/02/2005\" LastUpdate= \"09/16/2005\" LastName= \"Urban\" FirstName= \"John1\" MiddleName= \"\" Title= \"Coder\" LastName2= \"\" FirstName2= \"\" MiddleName2= \"\" AddrLine1= \"3410 W. Atlanta St.\" AddrLine2= \"\" City= \"Broken Arrow\" State= \"OK\" Country= \"US\" PostalCode= \"74012\" BirthDate= \"12/27/1967\" SSN= \"123121234\" MatriculaNumber= \"1234\" DriverLicense= \"01829304\" DLState= \"OK\" Email= \"jurban@gtplimited.com\" HomePhone= \"9183938393\" OfficePhone= \"\" MobilePhone= \"\" FaxPhone= \"\" ChallengeID= \"1000\" LicenseID= \"393709\" Subcompany= \"2459883\" Status= \"PA\" ExpirationDate=\"08/31/2008\" CardType= \"\" OtherCompanyName= \"\" PictureID= \"\" /></CardHolderInfo>");
            var fsvWebServiceMock = new Mock<IFsvWebService>();
            Balance balanceResponse = new Balance("1", @"Success", "100", "100", "100", "100");
            fsvWebServiceMock.Setup(x => x.GetCHInformation(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(cardHolderResponse);
            fsvWebServiceMock.Setup(x => x.PrepaidCardBalanceInquiry(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(balanceResponse);
            IFsvWebService _interface = fsvWebServiceMock.Object;
            FSVBankFirstCardProvider provider = new FSVBankFirstCardProvider(GetProviderEntity(), _teen, _interface);
          
            _request.Requests[0].UserIdentifier=new Identifiers.UserIdentifier(_parent.UserID).Identifier;  
            var target = new GetCardDetailsServiceCommand(_providers);
            target.CardProvider = provider;
            var result = target.Execute(_request);
            Assert.IsNotNull(result);
            Assert.AreEqual(0, result.Records.Count);
        }
        [TestMethod]
        public void Execute_Failure_RequestIsNull()
        {
            _request = null;
            var target = new GetCardDetailsServiceCommand(ProviderFactory);
            var result = target.Execute(_request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format("request must be set{0}Parameter name: request", Environment.NewLine),
                result.Status.ErrorMessage);
        }


        [TestMethod]
        public void Execute_Failure_RequestRecordIsEmpty()
        {
            PrepaidCardSearchRequest request = new PrepaidCardSearchRequest();
            var target = new GetCardDetailsServiceCommand(ProviderFactory);
            var result = target.Execute(request);
            Assert.IsNotNull(result.Status);
            Assert.IsFalse(result.Status.IsSuccessful);
            Assert.AreEqual(
                string.Format(
                    "request.Requests must have item{0}Parameter name: request",
                    Environment.NewLine),
                result.Status.ErrorMessage);
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

        private PrepaidCardSearchRequest CreatePrepaidCardSearchRequest(bool initPrepaidCardSearchRequest)
        {           
            var result = new PrepaidCardSearchRequest
            {
                Configuration = new RetrievalConfigurationRecord
                {
                    ApplicationKey = Guid.NewGuid()
                },
                Header = new RequestHeaderRecord
                {
                    CallerName = "GetCardDetailsServiceCommandTest"
                }
            };

            if (initPrepaidCardSearchRequest)
            {
                var prepaidCardSearch = new PrepaidCardSearchCriteria();
                prepaidCardSearch.FirstName = _teen.FirstName;
                prepaidCardSearch.LastName = _teen.LastName;           
                prepaidCardSearch.AddressLine1 = _teen.Address1;
                prepaidCardSearch.AddressLine2 = _teen.Address2;
                result.Requests.Add(prepaidCardSearch);
            }
            return result;
        }         
    

        #endregion
    }
}
