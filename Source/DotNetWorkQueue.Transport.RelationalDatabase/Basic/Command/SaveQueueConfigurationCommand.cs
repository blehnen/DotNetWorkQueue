namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command
{
    public class SaveQueueConfigurationCommand
    {
        public SaveQueueConfigurationCommand(byte[] configuration)
        {
            Configuration = configuration;
        }
        public byte[] Configuration { get; }
    }
}
