using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using DotNetWorkQueue.Trace;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Trace
{
    [TestClass]
    public class TraceExtensionsTests
    {
        private static readonly ActivitySource TestSource = new ActivitySource("DotNetWorkQueue.Tests.Trace");

        [TestMethod]
        public void Inject_Does_Not_Throw_With_Empty_Headers()
        {
            var message = Substitute.For<IMessage>();
            message.Headers.Returns(new Dictionary<string, object>());
            var headers = Substitute.For<IStandardHeaders>();

            message.Inject(TestSource, default(ActivityContext), headers);
        }

        [TestMethod]
        public void Extract_IReceivedMessageInternal_Returns_Default_When_No_Headers()
        {
            var message = Substitute.For<IReceivedMessageInternal>();
            message.Headers.Returns(new ReadOnlyDictionary<string, object>(new Dictionary<string, object>()));
            var headers = Substitute.For<IStandardHeaders>();

            var result = message.Extract(TestSource, headers);

            Assert.AreEqual(default(ActivityContext), result);
        }

        [TestMethod]
        public void Extract_IMessage_Returns_Default_When_No_Headers()
        {
            var message = Substitute.For<IMessage>();
            message.Headers.Returns(new Dictionary<string, object>());
            var headers = Substitute.For<IStandardHeaders>();

            var result = message.Extract(TestSource, headers);

            Assert.AreEqual(default(ActivityContext), result);
        }

        [TestMethod]
        public void Extract_IDictionary_Returns_Default_When_No_Headers()
        {
            IDictionary<string, object> inputHeaders = new Dictionary<string, object>();
            var headers = Substitute.For<IStandardHeaders>();

            var result = inputHeaders.Extract(TestSource, headers);

            Assert.AreEqual(default(ActivityContext), result);
        }

        [TestMethod]
        public void Extract_IReadOnlyDictionary_Returns_Default_When_No_Headers()
        {
            IReadOnlyDictionary<string, object> inputHeaders =
                new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());
            var headers = Substitute.For<IStandardHeaders>();

            var result = inputHeaders.Extract(TestSource, headers);

            Assert.AreEqual(default(ActivityContext), result);
        }

        [TestMethod]
        public void Extract_IMessageContext_Returns_Default_When_No_Headers()
        {
            var context = Substitute.For<IMessageContext>();
            context.Headers.Returns(new ReadOnlyDictionary<string, object>(new Dictionary<string, object>()));
            var headers = Substitute.For<IStandardHeaders>();

            var result = context.Extract(TestSource, headers);

            Assert.AreEqual(default(ActivityContext), result);
        }

        [TestMethod]
        public void AddCommonTags_Sets_Server_And_Queue_Tags()
        {
            using (var activity = new Activity("test"))
            {
                activity.Start();

                var data = Substitute.For<IAdditionalMessageData>();
                var correlationId = Substitute.For<ICorrelationId>();
                data.CorrelationId.Returns(correlationId);
                data.Route.Returns((string)null);
                data.TraceTags.Returns(new Dictionary<string, string>());

                var connectionInfo = Substitute.For<IConnectionInformation>();
                connectionInfo.Server.Returns("TestServer");
                connectionInfo.QueueName.Returns("TestQueue");

                activity.AddCommonTags(data, connectionInfo);

                Assert.AreEqual("TestServer", activity.GetTagItem("Server"));
                Assert.AreEqual("TestQueue", activity.GetTagItem("Queue"));
                Assert.IsNotNull(activity.GetTagItem("CorrelationId"));
            }
        }

        [TestMethod]
        public void AddCommonTags_Sets_Route_When_Present()
        {
            using (var activity = new Activity("test"))
            {
                activity.Start();

                var data = Substitute.For<IAdditionalMessageData>();
                var correlationId = Substitute.For<ICorrelationId>();
                data.CorrelationId.Returns(correlationId);
                data.Route.Returns("my-route");
                data.TraceTags.Returns(new Dictionary<string, string>());

                var connectionInfo = Substitute.For<IConnectionInformation>();

                activity.AddCommonTags(data, connectionInfo);

                Assert.AreEqual("my-route", activity.GetTagItem("Route"));
            }
        }

        [TestMethod]
        public void AddCommonTags_Does_Not_Set_Route_When_Empty()
        {
            using (var activity = new Activity("test"))
            {
                activity.Start();

                var data = Substitute.For<IAdditionalMessageData>();
                var correlationId = Substitute.For<ICorrelationId>();
                data.CorrelationId.Returns(correlationId);
                data.Route.Returns(string.Empty);
                data.TraceTags.Returns(new Dictionary<string, string>());

                var connectionInfo = Substitute.For<IConnectionInformation>();

                activity.AddCommonTags(data, connectionInfo);

                Assert.IsNull(activity.GetTagItem("Route"));
            }
        }

        [TestMethod]
        public void AddCommonTags_Sets_UserTags()
        {
            using (var activity = new Activity("test"))
            {
                activity.Start();

                var data = Substitute.For<IAdditionalMessageData>();
                var correlationId = Substitute.For<ICorrelationId>();
                data.CorrelationId.Returns(correlationId);
                data.Route.Returns((string)null);
                data.TraceTags.Returns(new Dictionary<string, string>
                {
                    { "CustomTag1", "Value1" },
                    { "CustomTag2", "Value2" }
                });

                var connectionInfo = Substitute.For<IConnectionInformation>();

                activity.AddCommonTags(data, connectionInfo);

                Assert.AreEqual("Value1", activity.GetTagItem("CustomTag1"));
                Assert.AreEqual("Value2", activity.GetTagItem("CustomTag2"));
            }
        }

        [TestMethod]
        public void AddMessageIdTag_IQueueOutputMessage_Sets_Tag_When_HasValue()
        {
            using (var activity = new Activity("test"))
            {
                activity.Start();

                var messageId = Substitute.For<IMessageId>();
                messageId.HasValue.Returns(true);
                var setting = Substitute.For<ISetting>();
                setting.Value.Returns(Guid.NewGuid());
                messageId.Id.Returns(setting);

                var sentMessage = Substitute.For<ISentMessage>();
                sentMessage.MessageId.Returns(messageId);

                var outputMessage = Substitute.For<IQueueOutputMessage>();
                outputMessage.SentMessage.Returns(sentMessage);

                activity.AddMessageIdTag(outputMessage);

                Assert.IsNotNull(activity.GetTagItem("MessageId"));
            }
        }

        [TestMethod]
        public void AddMessageIdTag_IQueueOutputMessage_Does_Not_Set_Tag_When_No_Value()
        {
            using (var activity = new Activity("test"))
            {
                activity.Start();

                var messageId = Substitute.For<IMessageId>();
                messageId.HasValue.Returns(false);

                var sentMessage = Substitute.For<ISentMessage>();
                sentMessage.MessageId.Returns(messageId);

                var outputMessage = Substitute.For<IQueueOutputMessage>();
                outputMessage.SentMessage.Returns(sentMessage);

                activity.AddMessageIdTag(outputMessage);

                Assert.IsNull(activity.GetTagItem("MessageId"));
            }
        }

        [TestMethod]
        public void AddMessageIdTag_IReceivedMessageInternal_Sets_Tag_When_HasValue()
        {
            using (var activity = new Activity("test"))
            {
                activity.Start();

                var messageId = Substitute.For<IMessageId>();
                messageId.HasValue.Returns(true);
                var setting = Substitute.For<ISetting>();
                setting.Value.Returns(Guid.NewGuid());
                messageId.Id.Returns(setting);

                var message = Substitute.For<IReceivedMessageInternal>();
                message.MessageId.Returns(messageId);

                activity.AddMessageIdTag(message);

                Assert.IsNotNull(activity.GetTagItem("MessageId"));
            }
        }

        [TestMethod]
        public void AddMessageIdTag_IReceivedMessageInternal_Does_Not_Set_Tag_When_No_Value()
        {
            using (var activity = new Activity("test"))
            {
                activity.Start();

                var messageId = Substitute.For<IMessageId>();
                messageId.HasValue.Returns(false);

                var message = Substitute.For<IReceivedMessageInternal>();
                message.MessageId.Returns(messageId);

                activity.AddMessageIdTag(message);

                Assert.IsNull(activity.GetTagItem("MessageId"));
            }
        }

        [TestMethod]
        public void AddMessageIdTag_IMessageContext_Sets_Tag_When_HasValue()
        {
            using (var activity = new Activity("test"))
            {
                activity.Start();

                var messageId = Substitute.For<IMessageId>();
                messageId.HasValue.Returns(true);
                var setting = Substitute.For<ISetting>();
                setting.Value.Returns(Guid.NewGuid());
                messageId.Id.Returns(setting);

                var context = Substitute.For<IMessageContext>();
                context.MessageId.Returns(messageId);

                activity.AddMessageIdTag(context);

                Assert.IsNotNull(activity.GetTagItem("MessageId"));
            }
        }

        [TestMethod]
        public void AddMessageIdTag_IMessageContext_Does_Not_Set_Tag_When_No_Value()
        {
            using (var activity = new Activity("test"))
            {
                activity.Start();

                var messageId = Substitute.For<IMessageId>();
                messageId.HasValue.Returns(false);

                var context = Substitute.For<IMessageContext>();
                context.MessageId.Returns(messageId);

                activity.AddMessageIdTag(context);

                Assert.IsNull(activity.GetTagItem("MessageId"));
            }
        }

        [TestMethod]
        public void AddMessageIdTag_IMessageId_Sets_Tag_When_HasValue()
        {
            using (var activity = new Activity("test"))
            {
                activity.Start();

                var messageId = Substitute.For<IMessageId>();
                messageId.HasValue.Returns(true);
                var setting = Substitute.For<ISetting>();
                setting.Value.Returns(Guid.NewGuid());
                messageId.Id.Returns(setting);

                activity.AddMessageIdTag(messageId);

                Assert.IsNotNull(activity.GetTagItem("MessageId"));
            }
        }

        [TestMethod]
        public void AddMessageIdTag_IMessageId_Does_Not_Set_Tag_When_Null()
        {
            using (var activity = new Activity("test"))
            {
                activity.Start();

                activity.AddMessageIdTag((IMessageId)null);

                Assert.IsNull(activity.GetTagItem("MessageId"));
            }
        }

        [TestMethod]
        public void AddMessageIdTag_IMessageId_Does_Not_Set_Tag_When_No_Value()
        {
            using (var activity = new Activity("test"))
            {
                activity.Start();

                var messageId = Substitute.For<IMessageId>();
                messageId.HasValue.Returns(false);

                activity.AddMessageIdTag(messageId);

                Assert.IsNull(activity.GetTagItem("MessageId"));
            }
        }

        [TestMethod]
        public void AddMessageIdTag_String_Sets_Tag_When_Not_Empty()
        {
            using (var activity = new Activity("test"))
            {
                activity.Start();

                activity.AddMessageIdTag("my-id-123");

                Assert.AreEqual("my-id-123", activity.GetTagItem("MessageId"));
            }
        }

        [TestMethod]
        public void AddMessageIdTag_String_Does_Not_Set_Tag_When_Null()
        {
            using (var activity = new Activity("test"))
            {
                activity.Start();

                activity.AddMessageIdTag((string)null);

                Assert.IsNull(activity.GetTagItem("MessageId"));
            }
        }

        [TestMethod]
        public void AddMessageIdTag_String_Does_Not_Set_Tag_When_Empty()
        {
            using (var activity = new Activity("test"))
            {
                activity.Start();

                activity.AddMessageIdTag(string.Empty);

                Assert.IsNull(activity.GetTagItem("MessageId"));
            }
        }
    }
}
