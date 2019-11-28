using System.Collections.Generic;

namespace Plugin.Sample.MembershipPricing.Models
{
    public class MembershipPriceModel
    {
        public string XCProductId { get; set; }
        public List<MembershipSnapshotModel> Snapshots { get; set; }
    }
}
