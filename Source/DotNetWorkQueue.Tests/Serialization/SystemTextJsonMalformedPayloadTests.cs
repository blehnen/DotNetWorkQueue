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
using System.Text;
using System.Text.Json;
using DotNetWorkQueue.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Serialization
{
    /// <summary>A top level holder, so its type name carries no escaped characters.</summary>
    public class TopLevelHolder
    {
        public object Payload { get; set; }
    }

    /// <summary>
    /// A corrupt or forged payload has to fail cleanly, because a clean failure becomes a poison
    /// message the operator can see. The alternative - a converter that reads past the damage and
    /// hands back a half-populated object - is the silent corruption this whole design is meant to
    /// avoid, so these are the paths worth pinning down.
    /// </summary>
    [TestClass]
    public class SystemTextJsonMalformedPayloadTests
    {
        private static readonly IReadOnlyDictionary<string, object> NoHeaders =
            new Dictionary<string, object>();

        private static SystemTextJsonSerializer Serializer() => new(new DenyListSerializationBinder());

        [TestMethod]
        [DataRow("[]", DisplayName = "body is not an object")]
        [DataRow("{\"notTheType\":\"x\"}", DisplayName = "body does not start with $type")]
        [DataRow("{\"$type\":\"\",\"$value\":{}}", DisplayName = "body carries an empty type name")]
        public void A_Malformed_Body_Throws(string json) => AssertThrowsJson(json);

        [TestMethod]
        public void A_Body_That_Ends_Where_The_Type_Was_Expected_Throws() => AssertThrowsJson("{}");

        [TestMethod]
        public void A_Body_That_Ends_Where_The_Value_Was_Expected_Throws()
        {
            //a resolvable type, so the binder is satisfied and the truncation is what fails
            AssertThrowsJson($"{{\"$type\":\"{Stamp(typeof(TopLevelProbe))}\"}}");
        }

        [TestMethod]
        public void A_Truncated_Polymorphic_Member_Throws()
        {
            AssertThrowsJson(Holding("{}"));
            AssertThrowsJson(Holding($"{{\"$type\":\"{Stamp(typeof(TopLevelProbe))}\"}}"));
        }

        [TestMethod]
        public void A_Binder_That_Omits_The_Assembly_Name_Round_Trips()
        {
            //ISerializationBinder may return a null assembly name, which makes the discriminator a
            //bare type name. Both halves have to agree about that, or the type is unresolvable.
            var serializer = new SystemTextJsonSerializer(new BareTypeNameBinder());
            var original = new TopLevelProbe { Name = "no assembly in the marker" };

            var bytes = serializer.ConvertMessageToBytes(new MessageBody { Body = original }, NoHeaders);

            Assert.DoesNotContain("|", Encoding.UTF8.GetString(bytes));
            object back = serializer.ConvertBytesToMessage<MessageBody>(bytes, NoHeaders).Body;
            Assert.IsInstanceOfType<TopLevelProbe>(back);
            Assert.AreEqual(original.Name, ((TopLevelProbe)back).Name);
        }

        /// <summary>Emits a bare type name, with no assembly, and resolves it again.</summary>
        private sealed class BareTypeNameBinder : Newtonsoft.Json.Serialization.ISerializationBinder
        {
            public Type BindToType(string assemblyName, string typeName) =>
                typeName == typeof(TopLevelProbe).FullName ? typeof(TopLevelProbe) : null;

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = null;
                typeName = serializedType.FullName;
            }
        }

        [TestMethod]
        public void A_Body_Missing_Its_Value_Throws()
            => AssertThrowsJson($"{{\"$type\":\"{Stamp(typeof(TopLevelHolder))}\",\"notTheValue\":{{}}}}");

        [TestMethod]
        public void A_Body_Naming_An_Unresolvable_Type_Is_Rejected_By_The_Binder()
        {
            //the default binder throws rather than returning null, so the rejection surfaces as its
            //own exception rather than reaching the converter's null check
            var serializer = Serializer();
            var json = "{\"$type\":\"No.Such.Type|No.Such.Assembly\",\"$value\":{}}";

            Assert.Throws<Newtonsoft.Json.JsonSerializationException>(() =>
                serializer.ConvertBytesToMessage<MessageBody>(Encoding.UTF8.GetBytes(json), NoHeaders));
        }

        [TestMethod]
        public void A_Binder_That_Returns_Null_Throws_Rather_Than_Producing_A_Null_Body()
        {
            //ISerializationBinder.BindToType may legally return null, and a third party binder can.
            //Letting that through would hand the caller a body that silently lost its type.
            var serializer = new SystemTextJsonSerializer(new NullReturningBinder());
            var json = $"{{\"$type\":\"{Stamp(typeof(TopLevelProbe))}\",\"$value\":{{}}}}";

            Assert.Throws<JsonException>(() =>
                serializer.ConvertBytesToMessage<MessageBody>(Encoding.UTF8.GetBytes(json), NoHeaders));
        }

        /// <summary>A binder that declines every type by returning null instead of throwing.</summary>
        private sealed class NullReturningBinder : Newtonsoft.Json.Serialization.ISerializationBinder
        {
            public Type BindToType(string assemblyName, string typeName) => null;

            public void BindToName(Type serializedType, out string assemblyName, out string typeName)
            {
                assemblyName = serializedType.Assembly.FullName;
                typeName = serializedType.FullName;
            }
        }

        [TestMethod]
        [DataRow("[]", DisplayName = "member is not an object")]
        [DataRow("{\"notTheType\":\"x\"}", DisplayName = "member does not start with $type")]
        public void A_Malformed_Polymorphic_Member_Throws(string memberJson)
            => AssertThrowsJson(Holding(memberJson));

        [TestMethod]
        public void A_Polymorphic_Member_Missing_Its_Value_Throws()
            => AssertThrowsJson(Holding($"{{\"$type\":\"{Stamp(typeof(TopLevelProbe))}\",\"notTheValue\":{{}}}}"));

        [TestMethod]
        public void A_Null_Polymorphic_Member_Round_Trips()
        {
            //the null branch on both sides of the polymorphic converter
            var serializer = Serializer();
            var bytes = serializer.ConvertMessageToBytes(
                new MessageBody { Body = new TopLevelHolder { Payload = null } }, NoHeaders);

            object back = serializer.ConvertBytesToMessage<MessageBody>(bytes, NoHeaders).Body;

            Assert.IsInstanceOfType<TopLevelHolder>(back);
            Assert.IsNull(((TopLevelHolder)back).Payload);
        }

        [TestMethod]
        public void A_Null_Body_Round_Trips()
        {
            var serializer = Serializer();
            var bytes = serializer.ConvertMessageToBytes(new MessageBody { Body = null }, NoHeaders);

            Assert.IsNull(serializer.ConvertBytesToMessage<MessageBody>(bytes, NoHeaders).Body);
        }

        /// <summary>Wraps a member payload in a well-formed holder, so only the member is damaged.</summary>
        private static string Holding(string memberJson) =>
            $"{{\"$type\":\"{Stamp(typeof(TopLevelHolder))}\",\"$value\":{{\"Payload\":{memberJson}}}}}";

        /// <summary>
        /// The discriminator the converters write, built the same way the binder does - type full
        /// name, a separator, then the assembly's full name.
        /// </summary>
        private static string Stamp(Type type) => $"{type.FullName}|{type.Assembly.FullName}";

        private static void AssertThrowsJson(string json)
        {
            var serializer = Serializer();
            Assert.Throws<JsonException>(() =>
                serializer.ConvertBytesToMessage<MessageBody>(Encoding.UTF8.GetBytes(json), NoHeaders));
        }
    }
}
