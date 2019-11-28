using Sitecore.Commerce.Core;

namespace Plugin.Sample.MembershipPricing.Models
{
    public class CustomPriceTier : Model
    {
        public CustomPriceTier(string currency, decimal quantity, decimal price, string membershipLevel)
        {
            Currency = currency;
            Quantity = quantity;
            Price = price;
            MembershipLevel = membershipLevel;
        }

        public string Id { get; set; }

        public string Currency { get; set; }

        public decimal Quantity { get; set; }

        public decimal Price { get; set; }

        public string MembershipLevel { get; set; }
    }
}
