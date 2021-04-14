using System;
using DotNetWorkQueue.Validation;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Basic
{
    /// <summary>
    /// Wraps a LiteDatabase. If direct/memory will leave the instance alone on dispose. If shared, database will be disposed.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class LiteDbConnection: IDisposable
    {
        private readonly bool _shared;

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDbConnection"/> class.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="shared">if set to <c>true</c> [shared].</param>
        public LiteDbConnection(LiteDatabase database, bool shared)
        {
            Guard.NotNull(() => database, database);
            Database = database;
            _shared = shared;
        }

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <value>
        /// The database.
        /// </value>
        public LiteDatabase Database { get; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if(_shared) //only dispose shared connections; manager handles direct
                Database?.Dispose();
        }
    }
}
