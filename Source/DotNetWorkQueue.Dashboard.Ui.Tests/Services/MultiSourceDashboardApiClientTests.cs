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
using DotNetWorkQueue.Dashboard.Ui.Services;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Dashboard.Ui.Tests.Services
{
    [TestClass]
    public class MultiSourceDashboardApiClientTests
    {
        private static DashboardApiSourceConfig CreateSource(string name, string baseUrl = "https://example.com")
        {
            return new DashboardApiSourceConfig { Name = name, BaseUrl = baseUrl };
        }

        private static (IMultiSourceDashboardApiClient client, IHttpClientFactory factory) CreateSut(
            params DashboardApiSourceConfig[] sources)
        {
            var registry = new SourceRegistry(new List<DashboardApiSourceConfig>(sources));
            var factory = Substitute.For<IHttpClientFactory>();
            factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient());
            var client = new MultiSourceDashboardApiClient(registry, factory);
            return (client, factory);
        }

        [TestMethod]
        public void GetClientForSource_Returns_Client_For_Valid_Slug()
        {
            var (sut, _) = CreateSut(
                CreateSource("Local", "http://localhost:5000"),
                CreateSource("Production", "https://prod.example.com"));

            var result = sut.GetClientForSource("local");

            result.Should().NotBeNull();
            result.Should().BeAssignableTo<IDashboardApiClient>();
        }

        [TestMethod]
        public void GetClientForSource_Returns_Same_Instance_On_Second_Call()
        {
            var (sut, _) = CreateSut(CreateSource("Local", "http://localhost:5000"));

            var first = sut.GetClientForSource("local");
            var second = sut.GetClientForSource("local");

            first.Should().BeSameAs(second);
        }

        [TestMethod]
        public void GetClientForSource_Returns_Different_Clients_For_Different_Slugs()
        {
            var (sut, _) = CreateSut(
                CreateSource("Local", "http://localhost:5000"),
                CreateSource("Production", "https://prod.example.com"));

            var local = sut.GetClientForSource("local");
            var prod = sut.GetClientForSource("production");

            local.Should().NotBeSameAs(prod);
        }

        [TestMethod]
        public void GetClientForSource_Throws_KeyNotFoundException_For_Unknown_Slug()
        {
            var (sut, _) = CreateSut(CreateSource("Local", "http://localhost:5000"));

            var act = () => sut.GetClientForSource("nonexistent");

            act.Should().Throw<KeyNotFoundException>()
                .WithMessage("*nonexistent*");
        }

        [TestMethod]
        public void GetClientForSource_Throws_ArgumentNullException_For_Null_Slug()
        {
            var (sut, _) = CreateSut(CreateSource("Local", "http://localhost:5000"));

            var act = () => sut.GetClientForSource(null!);

            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void GetAllSources_Returns_All_Registry_Sources()
        {
            var (sut, _) = CreateSut(
                CreateSource("Local", "http://localhost:5000"),
                CreateSource("Production", "https://prod.example.com"));

            var result = sut.GetAllSources();

            result.Should().HaveCount(2);
            result[0].Name.Should().Be("Local");
            result[1].Name.Should().Be("Production");
        }

        [TestMethod]
        public void GetClientForSource_Calls_HttpClientFactory_With_Slug()
        {
            var (sut, factory) = CreateSut(CreateSource("Local", "http://localhost:5000"));

            sut.GetClientForSource("local");

            factory.Received(1).CreateClient("local");
        }
    }
}
