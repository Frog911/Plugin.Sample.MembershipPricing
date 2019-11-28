using Microsoft.AspNetCore.OData.Builder;
using Plugin.Sample.MembershipPricing.Models;
using Sitecore.Commerce.Core;
using System.Collections.Generic;

namespace Plugin.Sample.MembershipPricing.Components
{
    public class MembershipTiersComponent : Component
    {
        public MembershipTiersComponent()
        {
            Tiers = new List<CustomPriceTier>();
        }

        [Contained]
        public IList<CustomPriceTier> Tiers { get; set; }
    }
}
