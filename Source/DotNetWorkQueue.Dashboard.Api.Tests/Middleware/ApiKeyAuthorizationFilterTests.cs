using System.Collections.Generic;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using DotNetWorkQueue.Dashboard.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Middleware
{
    [TestClass]
    public class ApiKeyAuthorizationFilterTests
    {
        [TestMethod]
        public void OnAuthorization_When_No_ApiKey_Configured_Allows_Request()
        {
            var options = new DashboardOptions { ApiKey = null };
            var filter = new ApiKeyAuthorizationFilter(options);
            var context = CreateContext();

            filter.OnAuthorization(context);

            context.Result.Should().BeNull();
        }

        [TestMethod]
        public void OnAuthorization_When_Valid_ApiKey_Allows_Request()
        {
            var options = new DashboardOptions { ApiKey = "secret-key-123" };
            var filter = new ApiKeyAuthorizationFilter(options);
            var context = CreateContext("secret-key-123");

            filter.OnAuthorization(context);

            context.Result.Should().BeNull();
        }

        [TestMethod]
        public void OnAuthorization_When_Invalid_ApiKey_Returns_Unauthorized()
        {
            var options = new DashboardOptions { ApiKey = "secret-key-123" };
            var filter = new ApiKeyAuthorizationFilter(options);
            var context = CreateContext("wrong-key");

            filter.OnAuthorization(context);

            context.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [TestMethod]
        public void OnAuthorization_When_Missing_Header_Returns_Unauthorized()
        {
            var options = new DashboardOptions { ApiKey = "secret-key-123" };
            var filter = new ApiKeyAuthorizationFilter(options);
            var context = CreateContext();

            filter.OnAuthorization(context);

            context.Result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        private static AuthorizationFilterContext CreateContext(string apiKeyHeaderValue = null)
        {
            var httpContext = new DefaultHttpContext();
            if (apiKeyHeaderValue != null)
            {
                httpContext.Request.Headers["X-Api-Key"] = apiKeyHeaderValue;
            }

            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            return new AuthorizationFilterContext(actionContext, new List<IFilterMetadata>());
        }
    }
}
