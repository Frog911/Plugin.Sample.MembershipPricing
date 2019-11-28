using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Plugin.Sample.MembershipPricing.Commands;
using Sitecore.Commerce.Core;
using System;
using System.Threading.Tasks;

namespace Plugin.Sample.MembershipPricing.Controllers
{
    [EnableQuery]
    [Route("api")]
    public class SellableItemPriceCardController : CommerceController
    {
        public SellableItemPriceCardController(
          IServiceProvider serviceProvider,
          CommerceEnvironment globalEnvironment)
          : base(serviceProvider, globalEnvironment)
        {
        }

        [HttpGet]
        [Route("GetSellableItemPriceCard(EntityId={entityId})")]
        public async Task<IActionResult> GetSellableItemPriceCard(string entityId)
        {
            if (!ModelState.IsValid || string.IsNullOrEmpty(entityId))
            {
                return NotFound();
            }

            var priceCard = await Command<GetSellableItemPriceCardCommand>().Process(CurrentContext, entityId, true);

            return priceCard != null ? new ObjectResult(priceCard) : (IActionResult)NotFound();
        }
    }
}
