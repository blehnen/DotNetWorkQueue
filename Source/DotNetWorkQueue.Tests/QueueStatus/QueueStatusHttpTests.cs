using System;
using System.Net.Http;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.QueueStatus;
using DotNetWorkQueue.Serialization;


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
                using (var response = await client.GetAsync(ping).ConfigureAwait(false))
                {
                    using (var content = response.Content)
                    {
                        var result = await content.ReadAsStringAsync().ConfigureAwait(false);
                        Assert.Contains("pong", result);
                    }
                }

                using (var response = await client.GetAsync(invalid).ConfigureAwait(false))
                {
                    using (var content = response.Content)
                    {
                        var result = await content.ReadAsStringAsync().ConfigureAwait(false);
                        Assert.Contains("invalid request", result);
                    }
                }

                using (var response = await client.GetAsync(status).ConfigureAwait(false))
                {
                    using (var content = response.Content)
                    {
                        var result = await content.ReadAsStringAsync().ConfigureAwait(false);
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
