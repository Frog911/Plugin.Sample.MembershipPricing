using System;
using System.Collections.Generic;

namespace Plugin.Sample.MembershipPricing.Models
{
    public class MembershipSnapshotModel
    {
        public DateTimeOffset EffectiveDate { get; set; }
        public List<MembershipSnapshotPriceModel> Prices { get; set; }
    }
}
