using System;
using System.Collections.Generic;
using DotNetWorkQueue.Dashboard.Api.Middleware;
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

            Assert.IsTrue(context.ExceptionHandled);
            var result = context.Result as ObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(404, result.StatusCode);
            Assert.AreEqual("Queue not found", GetErrorMessage(result));
        }

        [TestMethod]
        public void InvalidOperationException_In_Production_Returns_Generic_Message()
        {
            var filter = CreateFilter("Production");
            var context = CreateExceptionContext(new InvalidOperationException("Queue not found"));

            filter.OnException(context);

            Assert.IsTrue(context.ExceptionHandled);
            var result = context.Result as ObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(404, result.StatusCode);
            Assert.AreEqual("An internal error occurred", GetErrorMessage(result));
        }

        [TestMethod]
        public void NotSupportedException_In_Development_Returns_Detailed_Message()
        {
            var filter = CreateFilter("Development");
            var context = CreateExceptionContext(new NotSupportedException("Feature not available"));

            filter.OnException(context);

            Assert.IsTrue(context.ExceptionHandled);
            var result = context.Result as ObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(501, result.StatusCode);
            Assert.AreEqual("Feature not available", GetErrorMessage(result));
        }

        [TestMethod]
        public void NotSupportedException_In_Production_Returns_Generic_Message()
        {
            var filter = CreateFilter("Production");
            var context = CreateExceptionContext(new NotSupportedException("Feature not available"));

            filter.OnException(context);

            Assert.IsTrue(context.ExceptionHandled);
            var result = context.Result as ObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(501, result.StatusCode);
            Assert.AreEqual("An internal error occurred", GetErrorMessage(result));
        }

        [TestMethod]
        public void ObjectDisposedException_Always_Returns_Service_Unavailable()
        {
            var filter = CreateFilter("Production");
            var context = CreateExceptionContext(new ObjectDisposedException("DashboardService"));

            filter.OnException(context);

            Assert.IsTrue(context.ExceptionHandled);
            var result = context.Result as ObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(503, result.StatusCode);
            Assert.AreEqual("Service unavailable", GetErrorMessage(result));
        }

        [TestMethod]
        public void ObjectDisposedException_In_Development_Also_Returns_Service_Unavailable()
        {
            var filter = CreateFilter("Development");
            var context = CreateExceptionContext(new ObjectDisposedException("DashboardService"));

            filter.OnException(context);

            Assert.IsTrue(context.ExceptionHandled);
            var result = context.Result as ObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(503, result.StatusCode);
            Assert.AreEqual("Service unavailable", GetErrorMessage(result));
        }

        [TestMethod]
        public void UnhandledException_In_Production_Returns_Generic_500()
        {
            var filter = CreateFilter("Production");
            var context = CreateExceptionContext(new ApplicationException("Something unexpected"));

            filter.OnException(context);

            Assert.IsTrue(context.ExceptionHandled);
            var result = context.Result as ObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(500, result.StatusCode);
            Assert.AreEqual("An internal error occurred", GetErrorMessage(result));
        }

        [TestMethod]
        public void UnhandledException_In_Development_Returns_Detailed_500()
        {
            var filter = CreateFilter("Development");
            var context = CreateExceptionContext(new ApplicationException("Something unexpected"));

            filter.OnException(context);

            Assert.IsTrue(context.ExceptionHandled);
            var result = context.Result as ObjectResult;
            Assert.IsNotNull(result);
            Assert.AreEqual(500, result.StatusCode);
            Assert.AreEqual("Something unexpected", GetErrorMessage(result));
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
