using System;

namespace Plugin.Sample.MembershipPricing.Models
{
    public class MembershipSnapshotPriceModel
    {
        public string MemershipLevel { get; set; }
        public int Qty { get; set; }
        public decimal Price { get; set; }
    }
}
