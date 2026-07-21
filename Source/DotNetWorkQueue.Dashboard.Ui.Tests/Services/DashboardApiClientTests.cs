// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------

#nullable enable

using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Ui.Models;
using DotNetWorkQueue.Dashboard.Ui.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Ui.Tests.Services
{
    [TestClass]
    public class DashboardApiClientTests
    {
        private const string Base = "api/v1/dashboard";

        private sealed class FakeHttpMessageHandler : HttpMessageHandler
        {
            public HttpRequestMessage? LastRequest { get; private set; }
            public HttpResponseMessage Response { get; set; } = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", Encoding.UTF8, "application/json")
            };

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                LastRequest = request;
                return Task.FromResult(Response);
            }
        }

        private static (DashboardApiClient client, FakeHttpMessageHandler handler) CreateSut()
        {
            var handler = new FakeHttpMessageHandler();
            var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var client = new DashboardApiClient(http);
            return (client, handler);
        }

        private static HttpResponseMessage JsonResponse(HttpStatusCode status, string json)
        {
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        }

        // ------------------------------------------------------------
        // Happy path
        // ------------------------------------------------------------

        [TestMethod]
        public async Task GetSettingsAsync_Returns_Deserialized_Value()
        {
            var (sut, handler) = CreateSut();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            var result = await sut.GetSettingsAsync();

            Assert.IsNotNull(result);
            Assert.AreEqual(HttpMethod.Get, handler.LastRequest!.Method);
            Assert.AreEqual($"http://localhost/{Base}/settings", handler.LastRequest.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetConnectionsAsync_Returns_Deserialized_List()
        {
            var (sut, handler) = CreateSut();
            handler.Response = JsonResponse(HttpStatusCode.OK, "[{},{}]");

            var result = await sut.GetConnectionsAsync();

            Assert.HasCount(2, result);
            Assert.AreEqual($"http://localhost/{Base}/connections", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetQueuesAsync_Builds_ConnectionId_Route()
        {
            var (sut, handler) = CreateSut();
            var connectionId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "[{}]");

            var result = await sut.GetQueuesAsync(connectionId);

            Assert.HasCount(1, result);
            Assert.AreEqual($"http://localhost/{Base}/connections/{connectionId}/queues", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetJobsAsync_Builds_ConnectionId_Route()
        {
            var (sut, handler) = CreateSut();
            var connectionId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "[{}]");

            var result = await sut.GetJobsAsync(connectionId);

            Assert.HasCount(1, result);
            Assert.AreEqual($"http://localhost/{Base}/connections/{connectionId}/jobs", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetQueueStatusAsync_Returns_Deserialized_Value()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            var result = await sut.GetQueueStatusAsync(queueId);

            Assert.IsNotNull(result);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/status", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetQueueFeaturesAsync_Returns_Deserialized_Value()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            var result = await sut.GetQueueFeaturesAsync(queueId);

            Assert.IsNotNull(result);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/features", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetQueueConfigurationAsync_Returns_Deserialized_Value()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            var result = await sut.GetQueueConfigurationAsync(queueId);

            Assert.IsNotNull(result);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/configuration", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetMessagesAsync_Returns_Deserialized_Value()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            var result = await sut.GetMessagesAsync(queueId);

            Assert.IsNotNull(result);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/messages?pageIndex=0&pageSize=25", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetMessageCountAsync_Returns_Deserialized_Value()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "42");

            var result = await sut.GetMessageCountAsync(queueId);

            Assert.AreEqual(42L, result);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/messages/count", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetMessageDetailAsync_Returns_Deserialized_Value()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            var result = await sut.GetMessageDetailAsync(queueId, "msg-1");

            Assert.IsNotNull(result);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/messages/msg-1", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetMessageBodyAsync_Returns_Deserialized_Value()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            var result = await sut.GetMessageBodyAsync(queueId, "msg-1");

            Assert.IsNotNull(result);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/messages/msg-1/body", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetMessageHeadersAsync_Returns_Deserialized_Value()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            var result = await sut.GetMessageHeadersAsync(queueId, "msg-1");

            Assert.IsNotNull(result);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/messages/msg-1/headers", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetMessageRetriesAsync_Returns_Deserialized_List()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "[{},{},{}]");

            var result = await sut.GetMessageRetriesAsync(queueId, "msg-1");

            Assert.HasCount(3, result);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/messages/msg-1/retries", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetStaleMessagesAsync_Returns_Deserialized_Value()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            var result = await sut.GetStaleMessagesAsync(queueId);

            Assert.IsNotNull(result);
            Assert.AreEqual(
                $"http://localhost/{Base}/queues/{queueId}/messages/stale?thresholdSeconds=60&pageIndex=0&pageSize=25",
                handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetErrorsAsync_Returns_Deserialized_Value()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            var result = await sut.GetErrorsAsync(queueId);

            Assert.IsNotNull(result);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/errors?pageIndex=0&pageSize=25", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task DeleteMessageAsync_Returns_True_On_Success()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = new HttpResponseMessage(HttpStatusCode.NoContent);

            var result = await sut.DeleteMessageAsync(queueId, "msg-1");

            Assert.IsTrue(result);
            Assert.AreEqual(HttpMethod.Delete, handler.LastRequest!.Method);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/messages/msg-1", handler.LastRequest.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task DeleteAllErrorsAsync_Returns_Deserialized_Value_On_Success()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            var result = await sut.DeleteAllErrorsAsync(queueId);

            Assert.IsNotNull(result);
            Assert.AreEqual(HttpMethod.Delete, handler.LastRequest!.Method);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/errors", handler.LastRequest.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task RequeueMessageAsync_Returns_True_On_Success()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = new HttpResponseMessage(HttpStatusCode.OK);

            var result = await sut.RequeueMessageAsync(queueId, "msg-1");

            Assert.IsTrue(result);
            Assert.AreEqual(HttpMethod.Post, handler.LastRequest!.Method);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/messages/msg-1/requeue", handler.LastRequest.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task ResetMessageAsync_Returns_True_On_Success()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = new HttpResponseMessage(HttpStatusCode.OK);

            var result = await sut.ResetMessageAsync(queueId, "msg-1");

            Assert.IsTrue(result);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/messages/msg-1/reset", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task UpdateMessageBodyAsync_Returns_True_On_Success()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = new HttpResponseMessage(HttpStatusCode.OK);

            var result = await sut.UpdateMessageBodyAsync(queueId, "msg-1", new EditMessageBodyRequest { Body = "new body" });

            Assert.IsTrue(result);
            Assert.AreEqual(HttpMethod.Put, handler.LastRequest!.Method);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/messages/msg-1/body", handler.LastRequest.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task RequeueAllErrorsAsync_Returns_Deserialized_Value_On_Success()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            var result = await sut.RequeueAllErrorsAsync(queueId);

            Assert.IsNotNull(result);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/errors/requeue-all", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task ResetAllStaleAsync_Returns_Deserialized_Value_On_Success()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            var result = await sut.ResetAllStaleAsync(queueId);

            Assert.IsNotNull(result);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/messages/reset-all", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetConsumersAsync_Returns_Deserialized_List()
        {
            var (sut, handler) = CreateSut();
            handler.Response = JsonResponse(HttpStatusCode.OK, "[{},{}]");

            var result = await sut.GetConsumersAsync();

            Assert.HasCount(2, result);
            Assert.AreEqual($"http://localhost/{Base}/consumers", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetConsumerCountsAsync_Returns_Deserialized_Dictionary()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, $"{{\"{queueId}\":5}}");

            var result = await sut.GetConsumerCountsAsync();

            Assert.HasCount(1, result);
            Assert.AreEqual(5, result[queueId]);
            Assert.AreEqual($"http://localhost/{Base}/consumers/count", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task CancelMessageAsync_Returns_True_On_Success()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = new HttpResponseMessage(HttpStatusCode.OK);

            var result = await sut.CancelMessageAsync(queueId, "msg-1");

            Assert.IsTrue(result);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/messages/msg-1/cancel", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetHistoryAsync_Returns_Deserialized_Value()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            var result = await sut.GetHistoryAsync(queueId);

            Assert.IsNotNull(result);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/history?pageIndex=0&pageSize=25", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetHistoryCountAsync_Returns_Deserialized_Value()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "7");

            var result = await sut.GetHistoryCountAsync(queueId);

            Assert.AreEqual(7L, result);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/history/count", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetHistoryByMessageIdAsync_Returns_Deserialized_Value()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            var result = await sut.GetHistoryByMessageIdAsync(queueId, "msg-1");

            Assert.IsNotNull(result);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/history/msg-1", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task PurgeHistoryAsync_Returns_Deserialized_Value_On_Success()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            var result = await sut.PurgeHistoryAsync(queueId);

            Assert.IsNotNull(result);
            Assert.AreEqual(HttpMethod.Delete, handler.LastRequest!.Method);
            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/history", handler.LastRequest.RequestUri!.ToString());
        }

        // ------------------------------------------------------------
        // Failure branches
        // ------------------------------------------------------------

        [TestMethod]
        public async Task GetSettingsAsync_Throws_On_NotFound()
        {
            var (sut, handler) = CreateSut();
            handler.Response = new HttpResponseMessage(HttpStatusCode.NotFound);

            await Assert.ThrowsExactlyAsync<HttpRequestException>(() => sut.GetSettingsAsync());
        }

        [TestMethod]
        public async Task GetConnectionsAsync_Throws_On_ServerError()
        {
            var (sut, handler) = CreateSut();
            handler.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            await Assert.ThrowsExactlyAsync<HttpRequestException>(() => sut.GetConnectionsAsync());
        }

        [TestMethod]
        public async Task GetConnectionsAsync_Returns_Empty_List_When_Body_Is_Null()
        {
            var (sut, _) = CreateSut();
            var handler = new FakeHttpMessageHandler { Response = JsonResponse(HttpStatusCode.OK, "null") };
            var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var client = new DashboardApiClient(http);

            var result = await client.GetConnectionsAsync();

            Assert.IsNotNull(result);
            Assert.IsEmpty(result);
        }

        [TestMethod]
        public async Task GetQueueStatusAsync_Returns_Null_When_Body_Is_Null()
        {
            var handler = new FakeHttpMessageHandler { Response = JsonResponse(HttpStatusCode.OK, "null") };
            var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
            var client = new DashboardApiClient(http);

            var result = await client.GetQueueStatusAsync(Guid.NewGuid());

            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task DeleteMessageAsync_Returns_False_On_NotFound()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = new HttpResponseMessage(HttpStatusCode.NotFound);

            var result = await sut.DeleteMessageAsync(queueId, "msg-1");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task RequeueMessageAsync_Returns_False_On_ServerError()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            var result = await sut.RequeueMessageAsync(queueId, "msg-1");

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task UpdateMessageBodyAsync_Returns_False_On_NotFound()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = new HttpResponseMessage(HttpStatusCode.NotFound);

            var result = await sut.UpdateMessageBodyAsync(queueId, "msg-1", new EditMessageBodyRequest { Body = "x" });

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task DeleteAllErrorsAsync_Throws_On_ServerError()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            await Assert.ThrowsExactlyAsync<HttpRequestException>(() => sut.DeleteAllErrorsAsync(queueId));
        }

        [TestMethod]
        public async Task RequeueAllErrorsAsync_Throws_On_ServerError()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            await Assert.ThrowsExactlyAsync<HttpRequestException>(() => sut.RequeueAllErrorsAsync(queueId));
        }

        [TestMethod]
        public async Task ResetAllStaleAsync_Throws_On_ServerError()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            await Assert.ThrowsExactlyAsync<HttpRequestException>(() => sut.ResetAllStaleAsync(queueId));
        }

        [TestMethod]
        public async Task PurgeHistoryAsync_Throws_On_ServerError()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = new HttpResponseMessage(HttpStatusCode.InternalServerError);

            await Assert.ThrowsExactlyAsync<HttpRequestException>(() => sut.PurgeHistoryAsync(queueId));
        }

        // ------------------------------------------------------------
        // URL / query building
        // ------------------------------------------------------------

        [TestMethod]
        public async Task GetMessagesAsync_Appends_Status_When_Provided()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            await sut.GetMessagesAsync(queueId, pageIndex: 2, pageSize: 10, status: 3);

            Assert.AreEqual(
                $"http://localhost/{Base}/queues/{queueId}/messages?pageIndex=2&pageSize=10&status=3",
                handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetMessageCountAsync_Appends_Status_When_Provided()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "1");

            await sut.GetMessageCountAsync(queueId, status: 4);

            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/messages/count?status=4", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetConsumersAsync_Appends_QueueId_When_Provided()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "[]");

            await sut.GetConsumersAsync(queueId);

            Assert.AreEqual($"http://localhost/{Base}/consumers?queueId={queueId}", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetConsumersAsync_Omits_QueueId_When_Not_Provided()
        {
            var (sut, handler) = CreateSut();
            handler.Response = JsonResponse(HttpStatusCode.OK, "[]");

            await sut.GetConsumersAsync();

            Assert.AreEqual($"http://localhost/{Base}/consumers", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetHistoryAsync_Appends_Status_When_Provided()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            await sut.GetHistoryAsync(queueId, pageIndex: 1, pageSize: 5, status: 2);

            Assert.AreEqual(
                $"http://localhost/{Base}/queues/{queueId}/history?pageIndex=1&pageSize=5&status=2",
                handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task GetHistoryCountAsync_Omits_Status_When_Not_Provided()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "0");

            await sut.GetHistoryCountAsync(queueId);

            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/history/count", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task PurgeHistoryAsync_Appends_OlderThanDays_When_Provided()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            await sut.PurgeHistoryAsync(queueId, olderThanDays: 30);

            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/history?olderThanDays=30", handler.LastRequest!.RequestUri!.ToString());
        }

        [TestMethod]
        public async Task PurgeHistoryAsync_Omits_OlderThanDays_When_Not_Provided()
        {
            var (sut, handler) = CreateSut();
            var queueId = Guid.NewGuid();
            handler.Response = JsonResponse(HttpStatusCode.OK, "{}");

            await sut.PurgeHistoryAsync(queueId);

            Assert.AreEqual($"http://localhost/{Base}/queues/{queueId}/history", handler.LastRequest!.RequestUri!.ToString());
        }
    }
}
