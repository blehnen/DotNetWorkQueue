// ---------------------------------------------------------------------
// Copyright (c) 2005 S. Senthil Kumar
// http://www.codeproject.com/Articles/9621/ShellControl-A-console-emulation-control
// ---------------------------------------------------------------------
using System.Collections.Generic;
namespace ShellControlV2
{
    internal class CommandHistory
    {
        private int _currentPosition;
        private string _lastCommand;
        private readonly List<string> _commandHistory = new List<string>();

        internal void Add(string command)
        {
            if (command != _lastCommand)
            {
                _commandHistory.Add(command);
                _lastCommand = command;
                _currentPosition = _commandHistory.Count;
            }
            else
            {
                for (var i = 0; i < _commandHistory.Count; i++)
                {
                    if (command == _commandHistory[i])
                    {
                        _currentPosition = i + 1;
                        break;
                    }
                }
            }
        }

        internal bool DoesPreviousCommandExist()
        {
            return _currentPosition > -1;
        }

        internal bool DoesNextCommandExist()
        {
            return _currentPosition < _commandHistory.Count - 1;
        }

        internal string GetPreviousCommand()
        {
            if (_commandHistory.Count == 0) return string.Empty;
            if (_currentPosition == 0) return string.Empty;
            if (_currentPosition == -1) return string.Empty;

            _lastCommand = _commandHistory[--_currentPosition];
            return _lastCommand;
        }

        internal string GetNextCommand()
        {
            _lastCommand = _commandHistory[++_currentPosition];
            return LastCommand;
        }

        internal string LastCommand => _lastCommand;

        internal string[] GetCommandHistory()
        {
            return _commandHistory.ToArray();
        }
    }
}
