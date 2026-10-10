using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace webapicore.Fillter
{
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Method)]
    public class ApiKeyAuthAttribute : Attribute, IAsyncActionFilter
    {
        private const string ApiKeyHeader = "APIKey";
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {

            if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeader, out var potentialApiKey))
            {
                context.Result = new UnauthorizedResult();
                return;

            }
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var apiKey = configuration.GetValue<string>(key: "APIKey");
            if (!apiKey.Equals(potentialApiKey))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            await next();

        }
    }
    [AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Method)]
    public class AdminApiKeyAuthAttribute : Attribute, IAsyncActionFilter
    {
        private const string ApiKeyHeader = "APIKeyAdmin";
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {

            if (!context.HttpContext.Request.Headers.TryGetValue(ApiKeyHeader, out var potentialApiKey))
            {
                context.Result = new UnauthorizedResult();
                return;

            }
            var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
            var apiKey = configuration.GetValue<string>(key: "APIKeyAdmin");
            if (!apiKey.Equals(potentialApiKey))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            await next();

        }
    }
}
