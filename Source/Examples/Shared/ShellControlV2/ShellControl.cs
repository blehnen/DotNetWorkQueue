// ---------------------------------------------------------------------
// Copyright (c) 2005 S. Senthil Kumar
// http://www.codeproject.com/Articles/9621/ShellControl-A-console-emulation-control
// ---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
namespace ShellControlV2
{
    public delegate void EventCommandEntered(object sender, CommandEnteredEventArgs e);

    public partial class ShellControl : UserControl
    {
        private ShellTextBox _shellTextBox;
        public event EventCommandEntered CommandEntered;

        public ShellControl()
        {
            InitializeComponent();
        }

        public void PrintLine()
        {
            _shellTextBox.PrintLine();
        }
        public void SendCommand(string command)
        {
            _shellTextBox.WriteCommand(command);
        }
        public void SetCommands(List<string> commands)
        {
            _shellTextBox.CommandCompletion.Commands = commands;
        }

        internal void FireCommandEntered(string command)
        {
            OnCommandEntered(command);
        }

        protected virtual void OnCommandEntered(string command)
        {
            CommandEntered?.Invoke(command, new CommandEnteredEventArgs(command));
        }

        public Color ShellTextForeColor
        {
            get => _shellTextBox?.ForeColor ?? Color.Green;
            set
            {
                if (_shellTextBox != null)
                    _shellTextBox.ForeColor = value;
            }
        }

        public Color ShellTextBackColor
        {
            get => _shellTextBox?.BackColor ?? Color.Black;
            set
            {
                if (_shellTextBox != null)
                    _shellTextBox.BackColor = value;
            }
        }

        public Font ShellTextFont
        {
            get => _shellTextBox?.Font ?? new Font("Tahoma", 8);
            set
            {
                if (_shellTextBox != null)
                    _shellTextBox.Font = value;
            }
        }

        public void Clear()
        {
            _shellTextBox.Clear();
        }

        public void WriteText(string text)
        {
            _shellTextBox.WriteText(text);
        }

        public string[] GetCommandHistory()
        {
            return _shellTextBox.GetCommandHistory();
        }

        public string Prompt
        {
            get => _shellTextBox.Prompt;
            set => _shellTextBox.Prompt = value;
        }
    }

    public class CommandEnteredEventArgs : EventArgs
    {
        public CommandEnteredEventArgs(string command)
        {
            Command = command;
        }
        public string Command { get; }
    }
}
