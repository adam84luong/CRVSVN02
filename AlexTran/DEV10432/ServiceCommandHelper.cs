using Payjr.Core.Users;
using Payjr.Core.UserInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Contracts.CreditCard.Records;

namespace Payjr.Core.ServiceCommands
{
    public class ServiceCommandHelper
    {
        public static UserDetailRecord ConvertUserToUserDetailRecord(User user)
      {
         var userDetailRecord = new UserDetailRecord
          {
              City = user.City,
              Country = user.Country,
              Email = user.EmailAddress,
              FirstName = user.FirstName,
              LastName = user.LastName,
              PhoneNumber = user.Phone.PhoneNumberString,
              PostalCode = user.PostalCode,
              State = user.Province,
              Street1 = user.Address1,
              Street2 = user.Address2
          };

          return userDetailRecord;
      }
    }
}
