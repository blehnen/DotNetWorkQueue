// ---------------------------------------------------------------------
// Copyright (c) 2005 S. Senthil Kumar
// http://www.codeproject.com/Articles/9621/ShellControl-A-console-emulation-control
// ---------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
namespace ShellControlV2
{
    public class CommandCompletion
    {
        internal CommandCompletion()
        {
            Commands = new List<string>();
        }

        public string Filter { get; set; }
        public int CurrentPosition { get; set; }
        public List<string> Commands { get; set; }

        public List<string> GetCommands(string startsWith)
        {
            return Commands.Where(s => s.StartsWith(startsWith)).ToList();
        }

        public void Reset()
        {
            Filter = null;
            CurrentPosition = 0;
        }

        public string Complete(string input)
        {
            var completedCommand = "";

            // Performs command completion
            if (Filter == null)
                Filter = input;

            if (!Commands.Contains(Filter))
                Filter = string.Empty;

            var filteredList = GetCommands(Filter);
            if (filteredList.Count <= 0) return completedCommand;

            //Roll over to the beginning if the end is reached
            if (CurrentPosition >= filteredList.Count)
                CurrentPosition = 0;

            if (filteredList[CurrentPosition].Length > Filter.Length)
            {
                completedCommand = filteredList[CurrentPosition];
            }

            CurrentPosition++;

            return completedCommand;
        }
    }
}