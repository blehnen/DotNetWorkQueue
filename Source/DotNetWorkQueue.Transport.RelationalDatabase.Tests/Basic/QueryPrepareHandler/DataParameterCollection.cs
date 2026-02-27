using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.QueryPrepareHandler
{
    internal class DataParameterCollection : IDataParameterCollection
    {
        private readonly List<IDbDataParameter> _parameters = new List<IDbDataParameter>();

        public object this[string parameterName]
        {
            get => _parameters.FirstOrDefault(p => p.ParameterName == parameterName);
            set { }
        }

        public object this[int index]
        {
            get => _parameters[index];
            set => _parameters[index] = (IDbDataParameter)value;
        }

        public int Count => _parameters.Count;
        public bool IsFixedSize => false;
        public bool IsReadOnly => false;
        public bool IsSynchronized => false;
        public object SyncRoot => this;

        public int Add(object value)
        {
            _parameters.Add((IDbDataParameter)value);
            return _parameters.Count - 1;
        }

        public bool Any(System.Func<IDbDataParameter, bool> predicate) => _parameters.Any(predicate);

        public IDbDataParameter First(System.Func<IDbDataParameter, bool> predicate) => _parameters.First(predicate);

        public IDbDataParameter First() => _parameters.First();

        public void Clear() => _parameters.Clear();
        public bool Contains(string parameterName) => _parameters.Any(p => p.ParameterName == parameterName);
        public bool Contains(object value) => _parameters.Contains((IDbDataParameter)value);
        public void CopyTo(System.Array array, int index) { }
        public IEnumerator GetEnumerator() => _parameters.GetEnumerator();
        public int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);
        public int IndexOf(object value) => _parameters.IndexOf((IDbDataParameter)value);
        public void Insert(int index, object value) => _parameters.Insert(index, (IDbDataParameter)value);
        public void Remove(object value) => _parameters.Remove((IDbDataParameter)value);
        public void RemoveAt(string parameterName) => _parameters.RemoveAll(p => p.ParameterName == parameterName);
        public void RemoveAt(int index) => _parameters.RemoveAt(index);
    }
}
