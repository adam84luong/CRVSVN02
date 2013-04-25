using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardLab.CMS.PayjrSites.DTO
{
    public class FundingCreditCard
    {
        public string AccountIdentifier { get; set; }
        public string CardType { get; set; }
        public string CardNumber { get; set; }
        public string Expiration { get; set; }
        public string Status { get; set; }

    }
}
