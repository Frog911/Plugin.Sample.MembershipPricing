using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using Plugin.Sample.MembershipPricing.Commands;
using Plugin.Sample.MembershipPricing.Components;
using Plugin.Sample.MembershipPricing.Models;
using Sitecore.Commerce.Core;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http.OData;

namespace Plugin.Sample.MembershipPricing.Controllers
{
    [Route("api")]
    public class CommandsController : CommerceController
    {
        public CommandsController(IServiceProvider serviceProvider
            , CommerceEnvironment globalEnvironment)
            : base(serviceProvider, globalEnvironment)
        {
        }

        [HttpPut]
        [Route("ImportMembershipPrices()")]
        public IActionResult ImportMembershipPrices([FromBody] ODataActionParameters value)
        {
            if (!ModelState.IsValid || value == null)
            {
                return new BadRequestObjectResult(ModelState);
            }
            
            if ( !((JArray)value["MembershipPrices"]).Any())
            {
                return new BadRequestObjectResult(value);
            }

            var membershipPrices = ((JArray)value["MembershipPrices"]).ToObject<MembershipPriceModel[]>();

            var command = Command<ImportMembershipPricesCommand>();
            var result = ExecuteLongRunningCommand(() => command.Process(CurrentContext, membershipPrices.ToList()));

            return new ObjectResult(result);
        }

        [HttpPut]
        [Route("SetMembershipLevelToCustomer()")]
        public async Task<IActionResult> SetMembershipLevelToCustomer([FromBody] ODataActionParameters value)
        {
            if (!ModelState.IsValid || value == null)
            {
                return new BadRequestObjectResult(ModelState);
            }

            var customerMembershipSubscription = ((JObject)value["CustomerMembershipSubscription"]).ToObject<CustomerMembershipSubscriptionModel>();

            var command = Command<AddEditCustomerMembershipSubscriptionCommand>();
            var membershipSubscription = new MembershipSubscriptionComponent
            {
                MemerbshipLevelName = customerMembershipSubscription.MemerbshipLevelName
            };


            var result = await command.Process(CurrentContext, customerMembershipSubscription.CustomerId, membershipSubscription);

            return new ObjectResult(command);
        }
    }
}
