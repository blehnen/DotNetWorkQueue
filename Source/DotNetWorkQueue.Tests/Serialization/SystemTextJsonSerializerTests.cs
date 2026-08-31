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
using System.Text.Json.Serialization;
using DotNetWorkQueue.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NewtonsoftSerializer = DotNetWorkQueue.Serialization.JsonSerializer;

namespace DotNetWorkQueue.Tests.Serialization
{
    /// <summary>
    /// The point of these is not that System.Text.Json works - it is that it round trips the same
    /// shapes the Newtonsoft serializer does. Each case therefore runs through both, and the
    /// Newtonsoft result is asserted too, so a case that stops working for either is visible.
    /// </summary>
    /// <summary>A top level type, so its name appears in a payload without escape sequences.</summary>
    public class TopLevelProbe { public string Name { get; set; } }

    [TestClass]
    public class SystemTextJsonSerializerTests
    {
        private static readonly IReadOnlyDictionary<string, object> NoHeaders =
            new Dictionary<string, object>();

        private static SystemTextJsonSerializer Stj() => new(new DenyListSerializationBinder());
        private static NewtonsoftSerializer Newton() => new(new DenyListSerializationBinder());

        [TestMethod]
        [DynamicData(nameof(BodyCases))]
        public void Round_Trips_The_Same_Shapes_Newtonsoft_Does(string name, object body)
        {
            var expected = Describe(body);
            Assert.AreEqual(expected, RoundTrip(Newton(), body), $"Newtonsoft no longer round trips '{name}'");
            Assert.AreEqual(expected, RoundTrip(Stj(), body), $"System.Text.Json does not round trip '{name}'");
        }

        /// <summary>
        /// Compared by re-serializing with type names on rather than member by member: it is
        /// stricter, and it catches a type that came back as the wrong thing entirely.
        /// </summary>
        private static string RoundTrip(ISerializer serializer, object body)
        {
            var bytes = serializer.ConvertMessageToBytes(new MessageBody { Body = body }, NoHeaders);
            object back = serializer.ConvertBytesToMessage<MessageBody>(bytes, NoHeaders).Body;
            return Describe(back);
        }

        private static string Describe(object value) =>
            value == null ? "<null>" :
            JsonConvert.SerializeObject(value, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });

        public static IEnumerable<object[]> BodyCases()
        {
            yield return ["simple poco", new Simple { Name = "a", Count = 1 }];
            yield return ["nested poco", new Nested { Inner = new Simple { Name = "b", Count = 2 } }];
            yield return ["init-only properties", new InitOnly { Name = "d", Count = 4 }];
            yield return ["no parameterless ctor", new CtorOnly("e", 5)];
            yield return ["positional record", new Rec("f", 6)];
            yield return ["public fields", new Fields { Name = "g", Count = 7 }];
            yield return ["object-typed member", new HasObject { Payload = new Simple { Name = "j", Count = 9 } }];
            yield return ["interface-typed member", new HasInterface { Payload = new Simple { Name = "h", Count = 8 } }];
            yield return ["abstract-typed member", new HasAbstract { Payload = new Derived { Name = "i", Extra = "x" } }];
            yield return ["annotated concrete base", new HasAnnotatedBase { Payload = new DerivedAnnotated { Name = "r", Extra = "z" } }];
            yield return ["collections", new Collections { Items = { "k" }, Map = { { "l", 10 } } }];
            yield return ["scalars", new Scalars {
                Mode = Mode.Second, When = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc),
                Offset = new DateTimeOffset(2020, 1, 2, 3, 4, 5, TimeSpan.Zero),
                Id = Guid.Parse("11111111-2222-3333-4444-555555555555"),
                Money = 1.25m, Span = TimeSpan.FromMinutes(3) }];
            yield return ["nullables", new Nullables { Maybe = null, Also = 11 }];
            yield return ["struct body", new Point { X = 1, Y = 2 }];
            yield return ["List<T> body", new List<string> { "m", "n" }];
            yield return ["Dictionary body", new Dictionary<string, object> { { "o", "p" } }];
            yield return ["byte[] body", new byte[] { 1, 2, 3 }];
            yield return ["string body", "just a string"];
            yield return ["null body", null];
        }

        /// <summary>
        /// The one shape System.Text.Json does not carry on its own. Documented as a limitation of
        /// the opt-in serializer, and pinned here so it is a decision rather than a surprise.
        /// </summary>
        [TestMethod]
        public void Loses_A_Derived_Instance_Behind_An_Unannotated_Concrete_Base()
        {
            var body = new HasConcreteBase { Payload = new DerivedConcrete { Name = "q", Extra = "y" } };

            Assert.AreEqual(Describe(body), RoundTrip(Newton(), body), "Newtonsoft should still carry this");
            Assert.AreNotEqual(Describe(body), RoundTrip(Stj(), body),
                "if this starts passing, the documented [JsonDerivedType] limitation can be removed");
        }

        [TestMethod]
        public void Reports_A_Stable_Identifier()
        {
            //the id goes on the wire; changing it strands messages already in a queue
            Assert.AreEqual("system.text.json", Stj().SerializerId);
            Assert.AreEqual("json.net", Newton().SerializerId);
        }

        [TestMethod]
        public void Refuses_A_Type_The_Binder_Rejects()
        {
            //the deny list has to govern this serializer exactly as it governs Newtonsoft
            var binder = new DenyListSerializationBinder();
            var serializer = new SystemTextJsonSerializer(binder);
            //a top level type, so the payload carries its name unescaped - System.Text.Json writes
            //the '+' in a nested type name as \u002B, which would defeat the tamper below
            var bytes = serializer.ConvertMessageToBytes(
                new MessageBody { Body = new TopLevelProbe { Name = "a" } }, NoHeaders);

            var tampered = System.Text.Encoding.UTF8.GetString(bytes)
                .Replace(typeof(TopLevelProbe).FullName, "System.Diagnostics.Process");

            Assert.ThrowsExactly<JsonSerializationException>(() =>
                serializer.ConvertBytesToMessage<MessageBody>(
                    System.Text.Encoding.UTF8.GetBytes(tampered), NoHeaders));
        }

        public class Simple : IPayload { public string Name { get; set; } public int Count { get; set; } }
        public class Nested { public Simple Inner { get; set; } }
        public class InitOnly { public string Name { get; init; } public int Count { get; init; } }
        public class CtorOnly
        {
            public CtorOnly(string name, int count) { Name = name; Count = count; }
            public string Name { get; } public int Count { get; }
        }
        public record Rec(string Name, int Count);
        public class Fields { public string Name; public int Count; }
        public class HasObject { public object Payload { get; set; } }
        public interface IPayload { }
        public class HasInterface { public IPayload Payload { get; set; } }
        public abstract class Base { public string Name { get; set; } }
        public class Derived : Base { public string Extra { get; set; } }
        public class HasAbstract { public Base Payload { get; set; } }
        public class ConcreteBase { public string Name { get; set; } }
        public class DerivedConcrete : ConcreteBase { public string Extra { get; set; } }
        public class HasConcreteBase { public ConcreteBase Payload { get; set; } }
        [JsonDerivedType(typeof(AnnotatedBase), "base")]
        [JsonDerivedType(typeof(DerivedAnnotated), "derived")]
        public class AnnotatedBase { public string Name { get; set; } }
        public class DerivedAnnotated : AnnotatedBase { public string Extra { get; set; } }
        public class HasAnnotatedBase { public AnnotatedBase Payload { get; set; } }
        public class Collections
        {
            public List<string> Items { get; set; } = new();
            public Dictionary<string, int> Map { get; set; } = new();
        }
        public enum Mode { First, Second }
        public class Scalars
        {
            public Mode Mode { get; set; }
            public DateTime When { get; set; }
            public DateTimeOffset Offset { get; set; }
            public Guid Id { get; set; }
            public decimal Money { get; set; }
            public TimeSpan Span { get; set; }
        }
        public class Nullables { public int? Maybe { get; set; } public int? Also { get; set; } }
        public struct Point { public int X { get; set; } public int Y { get; set; } }
    }
}
