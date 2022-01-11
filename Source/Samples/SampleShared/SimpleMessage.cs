using System.Text;
using DotNetWorkQueue;
using DotNetWorkQueue.Logging;
using Microsoft.Extensions.Logging;

namespace SampleShared
{
    public class SimpleMessage
    {
        public string Message { get; set; }
        public int ProcessingTime { get; set; }

        public ErrorTypes Error { get; set; }
    }

    public enum ErrorTypes
    {
        None = 0,
        Error = 1,
        RetryableError = 2,
        RetryableErrorFail = 3
    }

    public class TestClass
    {
        public void RunMe(IWorkerNotification workNotification, string input1, int input2, SomeInput moreInput)
        {
            var sb = new StringBuilder();
            sb.Append(input1);
            sb.Append(" ");
            sb.Append(input2);
            sb.Append(" ");
            sb.AppendLine(moreInput.Message);
            workNotification.Log.LogInformation(sb.ToString());
        }
    }

    public class SomeInput
    {
        public SomeInput()
        {
        }

        public SomeInput(string message)
        {
            Message = message;
        }

        public string Message { get; set; }
        public override string ToString()
        {
            return Message;
        }
    }
}
