namespace DotNetWorkQueue.Transport.Redis
{
    /// <summary>
    /// Runs a command
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <typeparam name="TOutput">The type of the output.</typeparam>
    public interface ICommandHandlerWithOutput<in TCommand, out TOutput>
    {
        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        TOutput Handle(TCommand command);
    }
    /// <summary>
    /// Runs a command
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    public interface ICommandHandler<in TCommand>
    {
        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        void Handle(TCommand command);
    }
}
