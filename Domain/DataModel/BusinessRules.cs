using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DomainModels.DataModel
{
    //todo, can be save/read to/from setting file
    public static class BusinessRules
    {
        public const int RenewPeriodInDay = 14;

        public const double FinesPerWeek = 0.5;

        public const int MainUserId = 19;

        public const string MainUserBarcode = "M000001";

        public const int DefaultLimitPerUser = 2;
    }
}
