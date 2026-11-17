using System.Collections.Generic;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using DotNetWorkQueue.Dashboard.Api.Middleware;
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

            Assert.IsNull(context.Result);
        }

        [TestMethod]
        public void OnAuthorization_When_Valid_ApiKey_Allows_Request()
        {
            var options = new DashboardOptions { ApiKey = "secret-key-123" };
            var filter = new ApiKeyAuthorizationFilter(options);
            var context = CreateContext("secret-key-123");

            filter.OnAuthorization(context);

            Assert.IsNull(context.Result);
        }

        [TestMethod]
        public void OnAuthorization_When_Invalid_ApiKey_Returns_Unauthorized()
        {
            var options = new DashboardOptions { ApiKey = "secret-key-123" };
            var filter = new ApiKeyAuthorizationFilter(options);
            var context = CreateContext("wrong-key");

            filter.OnAuthorization(context);

            Assert.IsInstanceOfType<UnauthorizedObjectResult>(context.Result);
        }

        [TestMethod]
        public void OnAuthorization_When_Missing_Header_Returns_Unauthorized()
        {
            var options = new DashboardOptions { ApiKey = "secret-key-123" };
            var filter = new ApiKeyAuthorizationFilter(options);
            var context = CreateContext();

            filter.OnAuthorization(context);

            Assert.IsInstanceOfType<UnauthorizedObjectResult>(context.Result);
        }

        [TestMethod]
        [DataRow("secret-key-12")]
        [DataRow("secret-key-1234")]
        [DataRow("")]
        public void OnAuthorization_When_ApiKey_Has_A_Different_Length_Returns_Unauthorized(string provided)
        {
            var options = new DashboardOptions { ApiKey = "secret-key-123" };
            var filter = new ApiKeyAuthorizationFilter(options);
            var context = CreateContext(provided);

            filter.OnAuthorization(context);

            Assert.IsInstanceOfType<UnauthorizedObjectResult>(context.Result);
        }

        [TestMethod]
        [DataRow("Secret-key-123")]
        [DataRow("secret-key-124")]
        public void OnAuthorization_When_ApiKey_Differs_In_One_Character_Returns_Unauthorized(string provided)
        {
            var options = new DashboardOptions { ApiKey = "secret-key-123" };
            var filter = new ApiKeyAuthorizationFilter(options);
            var context = CreateContext(provided);

            filter.OnAuthorization(context);

            Assert.IsInstanceOfType<UnauthorizedObjectResult>(context.Result);
        }

        [TestMethod]
        public void OnAuthorization_When_Valid_NonAscii_ApiKey_Allows_Request()
        {
            var options = new DashboardOptions { ApiKey = "clé-secrète-ü" };
            var filter = new ApiKeyAuthorizationFilter(options);
            var context = CreateContext("clé-secrète-ü");

            filter.OnAuthorization(context);

            Assert.IsNull(context.Result);
        }

        [TestMethod]
        public void OnAuthorization_When_ApiKey_Differs_Only_In_A_NonAscii_Character_Returns_Unauthorized()
        {
            var options = new DashboardOptions { ApiKey = "clé-secrète" };
            var filter = new ApiKeyAuthorizationFilter(options);
            var context = CreateContext("clè-secrète");

            filter.OnAuthorization(context);

            Assert.IsInstanceOfType<UnauthorizedObjectResult>(context.Result);
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
