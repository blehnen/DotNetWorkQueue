using System;
using System.Collections.Generic;
using DotNetWorkQueue.Dashboard.Api.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Middleware
{
    [TestClass]
    public class DashboardExceptionFilterTests
    {
        [TestMethod]
        public void InvalidOperationException_In_Development_Returns_Detailed_Message()
        {
            var filter = CreateFilter("Development");
            var context = CreateExceptionContext(new InvalidOperationException("Queue not found"));

            filter.OnException(context);

            context.ExceptionHandled.Should().BeTrue();
            var result = context.Result as ObjectResult;
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(404);
            GetErrorMessage(result).Should().Be("Queue not found");
        }

        [TestMethod]
        public void InvalidOperationException_In_Production_Returns_Generic_Message()
        {
            var filter = CreateFilter("Production");
            var context = CreateExceptionContext(new InvalidOperationException("Queue not found"));

            filter.OnException(context);

            context.ExceptionHandled.Should().BeTrue();
            var result = context.Result as ObjectResult;
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(404);
            GetErrorMessage(result).Should().Be("An internal error occurred");
        }

        [TestMethod]
        public void NotSupportedException_In_Development_Returns_Detailed_Message()
        {
            var filter = CreateFilter("Development");
            var context = CreateExceptionContext(new NotSupportedException("Feature not available"));

            filter.OnException(context);

            context.ExceptionHandled.Should().BeTrue();
            var result = context.Result as ObjectResult;
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(501);
            GetErrorMessage(result).Should().Be("Feature not available");
        }

        [TestMethod]
        public void NotSupportedException_In_Production_Returns_Generic_Message()
        {
            var filter = CreateFilter("Production");
            var context = CreateExceptionContext(new NotSupportedException("Feature not available"));

            filter.OnException(context);

            context.ExceptionHandled.Should().BeTrue();
            var result = context.Result as ObjectResult;
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(501);
            GetErrorMessage(result).Should().Be("An internal error occurred");
        }

        [TestMethod]
        public void ObjectDisposedException_Always_Returns_Service_Unavailable()
        {
            var filter = CreateFilter("Production");
            var context = CreateExceptionContext(new ObjectDisposedException("DashboardService"));

            filter.OnException(context);

            context.ExceptionHandled.Should().BeTrue();
            var result = context.Result as ObjectResult;
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(503);
            GetErrorMessage(result).Should().Be("Service unavailable");
        }

        [TestMethod]
        public void ObjectDisposedException_In_Development_Also_Returns_Service_Unavailable()
        {
            var filter = CreateFilter("Development");
            var context = CreateExceptionContext(new ObjectDisposedException("DashboardService"));

            filter.OnException(context);

            context.ExceptionHandled.Should().BeTrue();
            var result = context.Result as ObjectResult;
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(503);
            GetErrorMessage(result).Should().Be("Service unavailable");
        }

        [TestMethod]
        public void UnhandledException_In_Production_Returns_Generic_500()
        {
            var filter = CreateFilter("Production");
            var context = CreateExceptionContext(new ApplicationException("Something unexpected"));

            filter.OnException(context);

            context.ExceptionHandled.Should().BeTrue();
            var result = context.Result as ObjectResult;
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(500);
            GetErrorMessage(result).Should().Be("An internal error occurred");
        }

        [TestMethod]
        public void UnhandledException_In_Development_Returns_Detailed_500()
        {
            var filter = CreateFilter("Development");
            var context = CreateExceptionContext(new ApplicationException("Something unexpected"));

            filter.OnException(context);

            context.ExceptionHandled.Should().BeTrue();
            var result = context.Result as ObjectResult;
            result.Should().NotBeNull();
            result.StatusCode.Should().Be(500);
            GetErrorMessage(result).Should().Be("Something unexpected");
        }

        private static DashboardExceptionFilter CreateFilter(string environmentName)
        {
            var logger = NullLoggerFactory.Instance.CreateLogger<DashboardExceptionFilter>();
            var environment = Substitute.For<IHostEnvironment>();
            environment.EnvironmentName.Returns(environmentName);
            return new DashboardExceptionFilter(logger, environment);
        }

        private static ExceptionContext CreateExceptionContext(Exception exception)
        {
            var httpContext = new DefaultHttpContext();
            var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
            return new ExceptionContext(actionContext, new List<IFilterMetadata>())
            {
                Exception = exception
            };
        }

        private static string GetErrorMessage(ObjectResult result)
        {
            // The anonymous object { error = "..." } is serialized via JSON round-trip to read the property
            var json = JsonConvert.SerializeObject(result.Value);
            var obj = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
            return obj["error"];
        }
    }
}
