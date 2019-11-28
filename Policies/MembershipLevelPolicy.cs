using Sitecore.Commerce.Core;
using System.Collections.Generic;

namespace Plugin.Sample.MembershipPricing.Policies
{
    public class MembershipLevelPolicy : Policy
    {
        public List<MembershipLevel> MembershipLevels { get; private set; } = new List<MembershipLevel>();
    }

    public class MembershipLevel : Policy
    {
        public string MemerbshipLevelName { get; set; }
    }
}
