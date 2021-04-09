using DotNetWorkQueue.Transport.Memory.Basic;
using Xunit;

namespace DotNetWorkQueue.Tests.Transport.Memory.Basic
{
    public class JobTableCreationTests
    {
        [Fact()]
        public void CreateJobTable_Test()
        {
            var create = new JobTableCreation();
            var result = create.CreateJobTable();
            Assert.True(create.JobTableExists); //returns true even if not called, as the structure is created at startup of the queue
            Assert.Equal(QueueCreationStatus.Success, result.Status); //a no-op, but returns success as we already created it
            Assert.True(create.JobTableExists);
        }
    }
}