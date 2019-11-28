using Sitecore.Commerce.Core;

namespace Plugin.Sample.MembershipPricing.Policies
{
    public class ImportMembershipPricesPolicy : Policy
    {

        public ImportMembershipPricesPolicy()
        {
        }
        public string CurrencySetId { get; set; }

        public string PriceBookName { get; set; }
    }
}
