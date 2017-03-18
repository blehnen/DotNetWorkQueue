// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Net.Http;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.QueueStatus;
using DotNetWorkQueue.Serialization;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.QueueStatus
{
    public class QueueStatusHttpTests
    {
        [Fact]
        public async void Create_Default()
        {
            var test = Create();
            Assert.NotNull(test.Configuration);
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var provider = fixture.Create<QueueStatusProviderNoOp>();
            test.AddStatusProvider(provider);

            Assert.NotNull(test.Options());

            test.Start();

            var ping = "http://localhost:9898/ping";
            var status = "http://localhost:9898/status";
            var invalid = "http://localhost:9898/invalid";

            using (var client = new HttpClient())
            {
                using (var response = await client.GetAsync(ping))
                {
                    using (var content = response.Content)
                    {
                        var result = await content.ReadAsStringAsync();
                        Assert.Contains("pong", result);
                    }
                }

                using (var response = await client.GetAsync(invalid))
                {
                    using (var content = response.Content)
                    {
                        var result = await content.ReadAsStringAsync();
                        Assert.Contains("invalid request", result);
                    }
                }

                using (var response = await client.GetAsync(status))
                {
                    using (var content = response.Content)
                    {
                        var result = await content.ReadAsStringAsync();
                        Assert.Contains("Queues", result);
                    }
                }
            }

            test.Dispose();
        }
        private IQueueStatus Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            IInternalSerializer serializer = fixture.Create<JsonSerializerInternal>();
            fixture.Inject(serializer);
            var configuration = fixture.Create<QueueStatusHttpConfiguration>();
            configuration.ListenerAddress = new Uri("http://localhost:9898");
            fixture.Inject(configuration);

            IConfiguration additional = fixture.Create<AdditionalConfiguration>();
            additional.SetSetting("QueueStatusHttpConfiguration", configuration);
            fixture.Inject(additional);

            return fixture.Create<QueueStatusHttp>();
        }
    }
}
