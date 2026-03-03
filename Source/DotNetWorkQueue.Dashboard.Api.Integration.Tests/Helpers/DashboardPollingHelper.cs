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
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Models;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers
{
    public static class DashboardPollingHelper
    {
        public static async Task WaitForStatusAsync(
            HttpClient client,
            Guid queueId,
            Func<QueueStatusResponse, bool> predicate,
            int timeoutSeconds = 15)
        {
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < deadline)
            {
                var status = await client.GetFromJsonAsync<QueueStatusResponse>(
                    $"api/v1/dashboard/queues/{queueId}/status");
                if (status != null && predicate(status))
                    return;
                await Task.Delay(500);
            }
            throw new TimeoutException(
                $"Status predicate not met within {timeoutSeconds}s for queue {queueId}");
        }

        public static async Task WaitForStaleAsync(
            HttpClient client,
            Guid queueId,
            int thresholdSeconds = 1,
            int timeoutSeconds = 30)
        {
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < deadline)
            {
                var paged = await client.GetFromJsonAsync<PagedResponse<MessageResponse>>(
                    $"api/v1/dashboard/queues/{queueId}/messages/stale?thresholdSeconds={thresholdSeconds}");
                if (paged?.Items != null && paged.Items.Count > 0)
                    return;
                await Task.Delay(1000);
            }
            throw new TimeoutException(
                $"No stale messages detected within {timeoutSeconds}s for queue {queueId}");
        }

        public static async Task WaitForErrorsAsync(
            HttpClient client,
            Guid queueId,
            int expectedCount,
            int timeoutSeconds = 30)
        {
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (DateTime.UtcNow < deadline)
            {
                var status = await client.GetFromJsonAsync<QueueStatusResponse>(
                    $"api/v1/dashboard/queues/{queueId}/status");
                if (status != null && status.Error >= expectedCount)
                    return;
                await Task.Delay(500);
            }
            throw new TimeoutException(
                $"Expected {expectedCount} errors within {timeoutSeconds}s for queue {queueId}");
        }
    }
}
