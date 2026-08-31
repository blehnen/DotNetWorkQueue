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
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Tests.Serialization
{
    [TestClass]
    public class SerializerResolverTests
    {
        [TestMethod]
        public void An_Unmarked_Message_Uses_The_Registered_Serializer()
        {
            //this is the case that matters most: every message written before the header existed
            //arrives here, and the behaviour has to match what happened before it existed
            var registered = Serializer("json.net");
            var sut = new SerializerResolver(registered);

            Assert.AreSame(registered, sut.Resolve(null));
            Assert.AreSame(registered, sut.Resolve(string.Empty));
            Assert.AreSame(registered, sut.Fallback);
        }

        [TestMethod]
        public void A_Marked_Message_Uses_The_Serializer_That_Wrote_It()
        {
            var registered = Serializer("json.net");
            var other = Serializer("system.text.json");
            var sut = new SerializerResolver(registered);
            sut.Add(other);

            Assert.AreSame(other, sut.Resolve("system.text.json"));
            Assert.AreSame(registered, sut.Resolve("json.net"));
        }

        [TestMethod]
        public void The_Fallback_Can_Be_Pointed_At_The_Serializer_That_Wrote_The_Backlog()
        {
            //the migration case: the queue now writes with one serializer but still holds messages
            //written by another, and those carry no header at all
            var newPrimary = Serializer("system.text.json");
            var legacy = Serializer("json.net");
            var sut = new SerializerResolver(newPrimary);

            sut.SetFallback(legacy);

            Assert.AreSame(legacy, sut.Resolve(null));
            Assert.AreSame(newPrimary, sut.Resolve("system.text.json"));
        }

        [TestMethod]
        public void An_Unknown_Serializer_Throws_Rather_Than_Guessing()
        {
            //reading a body with the wrong serializer does not reliably fail - it can hand back a
            //half populated object - so a loud failure beats a silent one
            var sut = new SerializerResolver(Serializer("json.net"));

            var ex = Assert.ThrowsExactly<DotNetWorkQueueException>(() => sut.Resolve("nobody.knows"));
            Assert.Contains("nobody.knows", ex.Message);
        }

        [TestMethod]
        public void Two_Serializers_Cannot_Share_An_Identifier()
        {
            //an ambiguous identifier would make the header meaningless
            var sut = new SerializerResolver(Serializer("json.net"));

            Assert.ThrowsExactly<DotNetWorkQueueException>(() => sut.Add(Serializer("json.net")));
        }

        [TestMethod]
        public void Registering_The_Same_Instance_Twice_Is_Harmless()
        {
            var registered = Serializer("json.net");
            var sut = new SerializerResolver(registered);

            sut.Add(registered);

            Assert.AreSame(registered, sut.Resolve("json.net"));
        }

        [TestMethod]
        public void The_Registered_Serializer_Is_Available_By_Its_Identifier()
        {
            var registered = Serializer("json.net");
            var sut = new SerializerResolver(registered);

            Assert.HasCount(1, sut.Registered);
            Assert.AreSame(registered, sut.Registered["json.net"]);
        }

        private static ISerializer Serializer(string id)
        {
            var serializer = Substitute.For<ISerializer>();
            serializer.SerializerId.Returns(id);
            return serializer;
        }
    }

    /// <summary>
    /// The round trip the whole feature exists for: a body written by one serializer is read back
    /// by that serializer even when the queue has since been pointed at a different one.
    /// </summary>
    [TestClass]
    public class SerializerMarkerRoundTripTests
    {
        [TestMethod]
        public void A_Body_Is_Read_Back_By_The_Serializer_That_Wrote_It()
        {
            var binder = new DenyListSerializationBinder();
            var newton = new JsonSerializer(binder);
            var stj = new SystemTextJsonSerializer(binder);

            //written by Newtonsoft
            var headers = new Dictionary<string, object>();
            var body = new MessageBody { Body = new Payload { Name = "written by newtonsoft" } };
            var bytes = newton.ConvertMessageToBytes(body, headers);
            headers["Queue-SerializerId"] = newton.SerializerId;

            //the queue now writes with System.Text.Json, but still has to read the old message
            var resolver = new SerializerResolver(stj);
            resolver.Add(newton);
            var root = new RootSerializer(null, stj, resolver);

            var back = root.BytesToMessage<MessageBody>(bytes, new MessageInterceptorsGraph(), headers);

            Assert.AreEqual("written by newtonsoft", (string)back.Body.Name);
        }

        [TestMethod]
        public void A_Body_With_No_Marker_Falls_Back()
        {
            var binder = new DenyListSerializationBinder();
            var newton = new JsonSerializer(binder);
            var stj = new SystemTextJsonSerializer(binder);

            //an old message: written by Newtonsoft, and carrying no serializer header at all
            var headers = new Dictionary<string, object>();
            var bytes = newton.ConvertMessageToBytes(
                new MessageBody { Body = new Payload { Name = "from before the header existed" } }, headers);

            var resolver = new SerializerResolver(stj);
            resolver.SetFallback(newton);
            var root = new RootSerializer(null, stj, resolver);

            var back = root.BytesToMessage<MessageBody>(bytes, new MessageInterceptorsGraph(), headers);

            Assert.AreEqual("from before the header existed", (string)back.Body.Name);
        }

        [TestMethod]
        public void A_Body_Naming_An_Unregistered_Serializer_Throws()
        {
            var binder = new DenyListSerializationBinder();
            var newton = new JsonSerializer(binder);
            var headers = new Dictionary<string, object> { { "Queue-SerializerId", "something.else" } };
            var bytes = newton.ConvertMessageToBytes(
                new MessageBody { Body = new Payload { Name = "x" } }, headers);

            var root = new RootSerializer(null, newton, new SerializerResolver(newton));

            //ASerializer wraps a failed read, which is what turns this into a poison message at the
            //transport rather than a silently mangled object
            var ex = Assert.ThrowsExactly<SerializationException>(() =>
                root.BytesToMessage<MessageBody>(bytes, new MessageInterceptorsGraph(), headers));

            Assert.IsInstanceOfType<DotNetWorkQueueException>(ex.InnerException);
            Assert.Contains("something.else", ex.InnerException.Message);
        }

        [TestMethod]
        public void A_Marker_That_Is_Not_A_String_Throws_Rather_Than_Falling_Back()
        {
            //a corrupt or forged header. Casting it away would quietly select the fallback and read
            //the body with the wrong serializer, which is exactly what the resolver exists to stop
            var binder = new DenyListSerializationBinder();
            var newton = new JsonSerializer(binder);
            var headers = new Dictionary<string, object> { { "Queue-SerializerId", 42 } };
            var bytes = newton.ConvertMessageToBytes(new MessageBody { Body = new Payload { Name = "x" } }, headers);

            var root = new RootSerializer(null, newton, new SerializerResolver(newton));

            var ex = Assert.ThrowsExactly<SerializationException>(() =>
                root.BytesToMessage<MessageBody>(bytes, new MessageInterceptorsGraph(), headers));

            Assert.IsInstanceOfType<DotNetWorkQueueException>(ex.InnerException);
            Assert.Contains("System.Int32", ex.InnerException.Message);
        }

        [TestMethod]
        public void A_Marker_Present_But_Null_Is_Treated_As_A_Legacy_Message()
        {
            //indistinguishable in intent from the header not being there at all
            var binder = new DenyListSerializationBinder();
            var newton = new JsonSerializer(binder);
            var headers = new Dictionary<string, object> { { "Queue-SerializerId", null } };
            var bytes = newton.ConvertMessageToBytes(
                new MessageBody { Body = new Payload { Name = "legacy" } }, headers);

            var root = new RootSerializer(null, newton, new SerializerResolver(newton));

            var back = root.BytesToMessage<MessageBody>(bytes, new MessageInterceptorsGraph(), headers);

            Assert.AreEqual("legacy", (string)back.Body.Name);
        }

        public class Payload { public string Name { get; set; } }
    }
}
