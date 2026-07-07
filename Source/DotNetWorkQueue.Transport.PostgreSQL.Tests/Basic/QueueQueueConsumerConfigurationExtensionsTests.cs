using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using NpgsqlTypes;
using Npgsql;
using DotNetWorkQueue.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic
{
    [TestClass]
    public class QueueQueueConsumerConfigurationExtensionsTests
    {
        [TestMethod]
        public void GetUserParameters_ReturnsNull_WhenNoSettingsExist()
        {
            var config = CreateConfig();
            var result = config.GetUserParameters();
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetUserClause_ReturnsNull_WhenNoSettingsExist()
        {
            var config = CreateConfig();
            var result = config.GetUserClause();
            Assert.IsNull(result);
        }

        [TestMethod]
        public void SetUserParameters_Then_GetUserParameters_ReturnsParameters()
        {
            var config = CreateConfig();
            var parameters = new List<NpgsqlParameter> { CreateParam("@p1") };
            config.SetUserParameters(parameters);

            var result = config.GetUserParameters();
            Assert.IsNotNull(result);
            Assert.HasCount(1, result);
            Assert.AreEqual("@p1", result[0].ParameterName);
        }

        [TestMethod]
        public void SetUserParameters_Overwrites_ExistingParameters()
        {
            var config = CreateConfig();
            var parameters1 = new List<NpgsqlParameter> { CreateParam("@p1") };
            config.SetUserParameters(parameters1);

            var parameters2 = new List<NpgsqlParameter> { CreateParam("@p2"), CreateParam("@p3") };
            config.SetUserParameters(parameters2);

            var result = config.GetUserParameters();
            Assert.HasCount(2, result);
        }

        [TestMethod]
        public void AddUserParameter_CreatesNewList_WhenNoneExists()
        {
            var config = CreateConfig();
            config.AddUserParameter(CreateParam("@p1"));

            var result = config.GetUserParameters();
            Assert.IsNotNull(result);
            Assert.HasCount(1, result);
        }

        [TestMethod]
        public void AddUserParameter_AppendsToExisting()
        {
            var config = CreateConfig();
            config.AddUserParameter(CreateParam("@p1"));
            config.AddUserParameter(CreateParam("@p2"));

            var result = config.GetUserParameters();
            Assert.HasCount(2, result);
        }

        [TestMethod]
        public void SetUserWhereClause_Then_GetUserClause_ReturnsClause()
        {
            var config = CreateConfig();
            config.SetUserWhereClause("AND Column1 = @p1");

            var result = config.GetUserClause();
            Assert.AreEqual("AND Column1 = @p1", result);
        }

        [TestMethod]
        public void SetUserParametersAndClause_Factory_Then_GetUserParameters_UsesFactory()
        {
            var config = CreateConfig();
            var parameters = new List<NpgsqlParameter> { CreateParam("@p1") };
            Func<List<NpgsqlParameter>> paramFactory = () => parameters;
            Func<string> clauseFactory = () => "AND Col = @p1";

            config.SetUserParametersAndClause(paramFactory, clauseFactory);

            var resultParams = config.GetUserParameters();
            Assert.IsNotNull(resultParams);
            Assert.HasCount(1, resultParams);

            var resultClause = config.GetUserClause();
            Assert.AreEqual("AND Col = @p1", resultClause);
        }

        [TestMethod]
        public void SetUserParametersAndClause_Factory_Overrides_StaticValues()
        {
            var config = CreateConfig();

            // Set static values first
            config.SetUserParameters(new List<NpgsqlParameter> { CreateParam("@static") });
            config.AdditionalSettings.Add("userdequeue", "AND Static = 1");

            // Now set factory
            var factoryParams = new List<NpgsqlParameter> { CreateParam("@factory") };
            config.SetUserParametersAndClause(() => factoryParams, () => "AND Factory = 1");

            // Factory should take precedence
            var resultParams = config.GetUserParameters();
            Assert.HasCount(1, resultParams);
            Assert.AreEqual("@factory", resultParams[0].ParameterName);

            var resultClause = config.GetUserClause();
            Assert.AreEqual("AND Factory = 1", resultClause);
        }

        [TestMethod]
        public void SetUserParametersAndClause_Factory_Overwrites_PreviousFactory()
        {
            var config = CreateConfig();

            config.SetUserParametersAndClause(
                () => new List<NpgsqlParameter> { CreateParam("@first") },
                () => "first");

            config.SetUserParametersAndClause(
                () => new List<NpgsqlParameter> { CreateParam("@second") },
                () => "second");

            var resultClause = config.GetUserClause();
            Assert.AreEqual("second", resultClause);
        }

        [TestMethod]
        public void SetUserWhereClause_Overwrites_WhenParamsExist()
        {
            var config = CreateConfig();
            config.AddUserParameter(CreateParam("@p1"));
            config.SetUserWhereClause("AND Col = @p1");
            config.SetUserWhereClause("AND Col2 = @p1");

            var result = config.GetUserClause();
            Assert.AreEqual("AND Col2 = @p1", result);
        }

        private static NpgsqlParameter CreateParam(string name)
        {
            return new NpgsqlParameter(name, NpgsqlDbType.Integer);
        }

        private QueueConsumerConfiguration CreateConfig()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueueConsumerConfiguration>();
        }
    }
}
