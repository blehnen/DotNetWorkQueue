using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using NSubstitute;


using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class CommitMessageTests
    {
        [Fact]
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
