using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Decorator
{
    /// <summary>
    /// 
    /// </summary>
    public class SetStatusTableStatusCommandDecorator : ICommandHandler<DeleteStatusTableStatusCommand>
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly ICommandHandler<DeleteStatusTableStatusCommand> _decorated;
        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandDecorator" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="decorated">The decorated.</param>
        public SetStatusTableStatusCommandDecorator(IConnectionInformation connectionInformation,
            ICommandHandler<DeleteStatusTableStatusCommand> decorated)
        {
            Guard.NotNull(() => decorated, decorated);
            Guard.NotNull(() => connectionInformation, connectionInformation);

            _connectionInformation = connectionInformation;
            _decorated = decorated;
        }
        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        public void Handle(DeleteStatusTableStatusCommand command)
        {
            if (!DatabaseExists.Exists(_connectionInformation.ConnectionString))
            {
                return;
            }
            _decorated.Handle(command);
        }
    }
}
