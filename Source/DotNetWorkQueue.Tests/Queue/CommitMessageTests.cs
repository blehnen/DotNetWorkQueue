using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using NSubstitute;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class CommitMessageTests
    {
        [TestMethod]
        public void Test_Commit()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageContext = fixture.Create<IMessageContext>();
            var test = fixture.Create<CommitMessage>();
            test.Commit(messageContext);
            messageContext.Received(1).RaiseCommit();
        }
    }
}
