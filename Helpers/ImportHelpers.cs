using Microsoft.Extensions.Primitives;
using Sitecore.Commerce.Core;
using System.Collections.Generic;
using System.Linq;

namespace Plugin.Sample.MembershipPricing.Helpers
{
    public static class ImportHelpers
    {
        public const long MaxCommerceContextPerfomanceIteractions = 5;

        public static CommercePipelineExecutionContext OptimizeContextForImport(CommercePipelineExecutionContext context)
        {

            // for catalogimport, disable recommended blocks
            if (context.CommerceContext.Headers.ContainsKey(CoreConstants.Headers.PolicyKeys))
            {
                var keys = context.CommerceContext.Headers[CoreConstants.Headers.PolicyKeys].ToList();
                keys.Add("IndexDeletedSitecoreItemBlock");
                keys.Add("IndexUpdatedSitecoreItemBlock");
                keys.Add("AddEntityToIndexListBlock");
                keys.Add("IgnoreLocalizeEntity");

                var newkeys = new StringValues(keys.ToArray());

                context.CommerceContext.Headers[CoreConstants.Headers.PolicyKeys] = newkeys;
            }
            else
            {
                context.CommerceContext.Headers.Add(new KeyValuePair<string, StringValues>(CoreConstants.Headers.PolicyKeys, "IndexDeletedSitecoreItemBlock|IndexUpdatedSitecoreItemBlock|AddEntityToIndexListBlock|IgnoreLocalizeEntity"));
            }

            return context;
        }

        public static void ImproveCommercePerfomance(long increment, CommercePipelineExecutionContext context)
        {
            if (increment % MaxCommerceContextPerfomanceIteractions == 0)
            {
                context.CommerceContext = new CommerceContext(context.CommerceContext.Logger, context.CommerceContext.TelemetryClient)
                {
                    GlobalEnvironment = context.CommerceContext.GlobalEnvironment,
                    Environment = context.CommerceContext.Environment,
                    Headers = context.CommerceContext.Headers
                };
            }
        }
    }
}
