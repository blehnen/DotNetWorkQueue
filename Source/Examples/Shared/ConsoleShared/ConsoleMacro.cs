// ---------------------------------------------------------------------
// Copyright (c) 2014 John Atten
// http://www.codeproject.com/Articles/816301/Csharp-Building-a-Useful-Extensible-NET-Console-Ap
// ---------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ConsoleShared
{
    public class ConsoleMacro
    {
        private readonly Dictionary<int, string> _commands;
        private int _id;
        private int _lastId;
        public ConsoleMacro()
        {
            _commands = new Dictionary<int, string>();
            _lastId = -1;
        }

        public int Add(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return -1;
            if (Running) return -1;
            var id = Interlocked.Increment(ref _id);
            _commands.Add(id,
                value[value.Length - 1] == (char) 13 ? value.Substring(0, value.Length - 2) : value);
            _lastId = id;
            return id;
        }

        public List<string> Values
        {
            get
            {
                var values = new List<string>(_commands.Count);
                var list = _commands.Keys.ToList();
                list.Sort();

                // Loop through keys.
                values.AddRange(list.Select(key => _commands[key]));
                return values;
            }
        } 

        public int Count => _commands.Count;

        public void Remove(int key)
        {
            _commands.Remove(key);
        }

        public void RemoveLast()
        {
            if (_lastId > -1)
            {
                Remove(_lastId);
            }   
        }

        public bool Running { get; set; }

        public ConsoleExecuteResult Save(string name)
        {
            var path = Path.GetDirectoryName(name);
            if (string.IsNullOrEmpty(path)) return new ConsoleExecuteResult("invalid path");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var values = new List<string>(_commands.Count);
            var list = _commands.Keys.ToList();
            list.Sort();

            // Loop through keys.
            values.AddRange(list.Select(key => _commands[key]));

            File.WriteAllLines(name, values);
            return new ConsoleExecuteResult("Saved");
        }

        public ConsoleExecuteResult Load(string name)
        {
            if (!File.Exists(name)) return new ConsoleExecuteResult("Not found");
            var list = File.ReadAllLines(name).ToList();
            foreach (var command in list)
            {
                Add(command);
            }
            return new ConsoleExecuteResult("Loaded");
        }
    }
}
