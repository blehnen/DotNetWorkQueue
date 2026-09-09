using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryPrepareHandler
{
    /// <summary>
    /// An in-memory parameter collection for prepare-handler tests.
    /// </summary>
    /// <remarks>
    /// Derives from <see cref="DbParameterCollection"/> rather than implementing
    /// <c>IDataParameterCollection</c>, because <see cref="DbCommand.Parameters"/> is typed as the
    /// abstract class. The convenience members below (<c>Any</c>, <c>First</c>) are what the tests
    /// actually assert against and are kept unchanged.
    /// </remarks>
    internal class DataParameterCollection : DbParameterCollection
    {
        private readonly List<DbParameter> _parameters = new List<DbParameter>();

        public override int Count => _parameters.Count;
        public override object SyncRoot => this;

        public bool Any(Func<DbParameter, bool> predicate) => _parameters.Any(predicate);

        public DbParameter First(Func<DbParameter, bool> predicate) => _parameters.First(predicate);

        public DbParameter First() => _parameters.First();

        public override int Add(object value)
        {
            _parameters.Add((DbParameter)value);
            return _parameters.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var value in values)
                _parameters.Add((DbParameter)value);
        }

        public override void Clear() => _parameters.Clear();

        public override bool Contains(string value) => _parameters.Any(p => p.ParameterName == value);

        public override bool Contains(object value) => _parameters.Contains((DbParameter)value);

        public override void CopyTo(Array array, int index) { }

        public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();

        public override int IndexOf(string parameterName) =>
            _parameters.FindIndex(p => p.ParameterName == parameterName);

        public override int IndexOf(object value) => _parameters.IndexOf((DbParameter)value);

        public override void Insert(int index, object value) =>
            _parameters.Insert(index, (DbParameter)value);

        public override void Remove(object value) => _parameters.Remove((DbParameter)value);

        public override void RemoveAt(string parameterName) =>
            _parameters.RemoveAll(p => p.ParameterName == parameterName);

        public override void RemoveAt(int index) => _parameters.RemoveAt(index);

        protected override DbParameter GetParameter(int index) => _parameters[index];

        protected override DbParameter GetParameter(string parameterName) =>
            _parameters.FirstOrDefault(p => p.ParameterName == parameterName);

        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var index = IndexOf(parameterName);
            if (index >= 0)
                _parameters[index] = value;
            else
                _parameters.Add(value);
        }
    }
}
