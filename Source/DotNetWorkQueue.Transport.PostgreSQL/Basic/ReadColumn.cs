using System;
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <inheritdoc />
    public class ReadColumn : RelationalDatabase.Basic.ReadColumn
    {
        /// <inheritdoc />
        public override DateTime ReadAsDateTime(CommandStringTypes command, int column, IDataReader reader, DateTime noValue = default)
        {
            switch (command)
            {
                case CommandStringTypes.GetHeartBeatExpiredMessageIds:
                    return DateTime.FromBinary(reader.GetInt64(column));
                default:
                    return base.ReadAsDateTime(command, column, reader, noValue);
            }
        }
        /// <inheritdoc />
        public override DateTimeOffset ReadAsDateTimeOffset(CommandStringTypes command, int column, IDataReader reader, DateTimeOffset noValue = default)
        {
            switch (command)
            {
                case CommandStringTypes.DoesJobExist:
                case CommandStringTypes.GetJobLastKnownEvent:
                case CommandStringTypes.GetJobLastScheduleTime:
                    return new DateTimeOffset(new DateTime(reader.GetInt64(column), DateTimeKind.Utc));
                default:
                    return base.ReadAsDateTimeOffset(command, column, reader, noValue);
            }
        }
    }
}
