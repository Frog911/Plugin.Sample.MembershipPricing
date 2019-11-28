using Plugin.Sample.MembershipPricing.Models;
using System.Collections.Generic;

namespace Plugin.Sample.MembershipPricing.Pipelines.Arguments
{
    public class ImportMembershipPricesArgument
    {
        public ImportMembershipPricesArgument(string priceBookName, List<MembershipPriceModel> prices, string currencySetId)
        {
            PriceBookName = priceBookName;
            Prices = prices;
            CurrencySetId = currencySetId;
        }

        public string PriceBookName { get; set; }


        public List<MembershipPriceModel> Prices { get; set; }

        public string CurrencySetId { get; set; }
    }
}
