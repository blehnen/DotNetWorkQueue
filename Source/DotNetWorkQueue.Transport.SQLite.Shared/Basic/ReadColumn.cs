using System;
using System.Data;
using System.Globalization;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.IReadColumn" />
    public class ReadColumn : RelationalDatabase.Basic.ReadColumn
    {
        /// <summary>
        /// Reads as string.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="column">The column, if known. -1 if the caller has no idea.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="noValue"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override string ReadAsString(CommandStringTypes command, int column, IDataReader reader, string noValue = null)
        {
            switch (command)
            {
                case CommandStringTypes.GetColumnNamesFromTable:
                    return reader.GetString(1); //sqlite puts column name in column 1, not 0
                default:
                    return base.ReadAsString(command, column, reader, noValue);
            }
        }

        /// <summary>
        /// Reads as date time.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="column">The column, if known. -1 if the caller has no idea.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="noValue"></param>
        /// <returns></returns>
        public override DateTime ReadAsDateTime(CommandStringTypes command, int column, IDataReader reader, DateTime noValue = default(DateTime))
        {
            switch (command)
            {
                case CommandStringTypes.GetHeartBeatExpiredMessageIds:
                    return DateTime.FromBinary(reader.GetInt64(column));
                default:
                    return base.ReadAsDateTime(command, column, reader, noValue);
            }
        }

        /// <summary>
        /// Reads as a date time offset
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="column">The column, if known. -1 if the caller has no idea.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="noValue"></param>
        /// <returns></returns>
        public override DateTimeOffset ReadAsDateTimeOffset(CommandStringTypes command, int column, IDataReader reader, DateTimeOffset noValue = default(DateTimeOffset))
        {
            switch (command)
            {
                case CommandStringTypes.DoesJobExist:
                case CommandStringTypes.GetJobLastKnownEvent:
                case CommandStringTypes.GetJobLastScheduleTime:
                    return DateTimeOffset.Parse(reader.GetString(column),
                        CultureInfo.InvariantCulture);
                default:
                    return base.ReadAsDateTimeOffset(command, column, reader, noValue);
            }
        }
    }
}
