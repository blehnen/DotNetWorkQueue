// ---------------------------------------------------------------------
// Copyright © 2015-2020 Brian Lehnen
// 
// All rights reserved.
// 
// MIT License
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// ---------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using ConsoleShared;
using ShellControlV2;
namespace ConsoleView
{
    public partial class FormMain : Form
    {
        private CommandList _commandLibraries;
        private long _asyncCount;
        private readonly Assembly _commandAssembly;

        public FormMain(Assembly commandAssembly)
        {
            InitializeComponent();
            _commandAssembly = commandAssembly;
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            Console.SetOut(new TextBoxStreamWriter(Output));
            try
            {
                _commandLibraries = ConsoleGetCommands.GetCommands(_commandAssembly);

                logControlCommand.Display("Usage is <namespace>.Command; use <namespace>.Help for command list" +
                                          Environment.NewLine);
                logControlCommand.Display("TAB will scroll through all commands in all namespaces" +
                                          Environment.NewLine);
                logControlCommand.Display("Optional parameters for commands are contained in []" +
                                         Environment.NewLine + Environment.NewLine);

                foreach (var command in _commandLibraries)
                {
                    logControlCommand.Display(command.Key.Instance.Info.Message + Environment.NewLine);
                }

                var list =
                    (from command in _commandLibraries
                     from child in command.Value
                     select command.Key.Namespace + "." + child.Key.Trim()).ToList();
                shellControl1.SetCommands(list);
                shellControl1.CommandEntered += ShellControl1OnCommandEntered;
            }
            catch (Exception error)
            {
                logControlCommand.Display(error.ToString());
            }
        }

        private void Output(string s)
        {
            if (InvokeRequired)
            {
                void Del()
                {
                    logControl1.Display(s);
                }

                BeginInvoke((MethodInvoker)Del);
                return;
            }

            logControl1.Display(s);
        }

        /// <summary>
        /// Shells the control1 on command entered.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="commandEnteredEventArgs">The <see cref="CommandEnteredEventArgs"/> instance containing the event data.</param>
        private async void ShellControl1OnCommandEntered(object sender, CommandEnteredEventArgs commandEnteredEventArgs)
        {
            if (string.IsNullOrWhiteSpace(commandEnteredEventArgs.Command)) return;
            var async = false;
            try
            {
                // Create a ConsoleCommand instance:
                var cmd = new ConsoleCommand(commandEnteredEventArgs.Command.Trim().Replace("\t", ""));

                // Execute the command:
                ConsoleExecuteResult result;
                if (ConsoleExecute.IsAsync(cmd, _commandLibraries))
                {
                    Interlocked.Increment(ref _asyncCount);
                    async = true;
                    result =
                        await
                            ConsoleExecute.ExecuteAsync(cmd, _commandLibraries).ConfigureAwait(false);
                }
                else
                {
                    result = ConsoleExecute.Execute(cmd, _commandLibraries);
                }

                // Write out the result:
                WriteToConsole(result.Message);

                if (result.Action != null && result.Action.Action == ConsoleExecuteActions.Exit)
                {
                    //If we exit here and async tasks are running, we won't ever finish them. That's because they are tied to the shell's command event...
                    if (Interlocked.Read(ref _asyncCount) == 0)
                    {
                        shellControl1.CommandEntered -= ShellControl1OnCommandEntered;
                        Close();
                    }
                    else
                    {
                        WriteToConsole($"Async tasks are still running {Interlocked.Read(ref _asyncCount)}");
                    }
                }
                else if (result.Action != null && result.Action.Action == ConsoleExecuteActions.StatusUri)
                {
                    queueStatusControl1.Display(result.Action.Target);
                }
                else if (result.Action != null && result.Action.Action == ConsoleExecuteActions.StartProcess)
                {
                    Process.Start(result.Action.Target);
                }
                else if (result.Action != null && result.Action.Action == ConsoleExecuteActions.StartMacro)
                {
                    ConsoleExecute.StartMacroCapture();
                }
                else if (result.Action != null && result.Action.Action == ConsoleExecuteActions.CancelMacro)
                {
                    ConsoleExecute.CancelMacroCapture();
                }
                else if (result.Action != null && result.Action.Action == ConsoleExecuteActions.SaveMacro)
                {
                    ConsoleExecute.SaveMacro(Path.Combine(Path.GetDirectoryName(_commandAssembly.Location) + @"\Macro\", result.Action.Target));
                }
                else if (result.Action != null && result.Action.Action == ConsoleExecuteActions.RunMacro)
                {
                    shellControl1.PrintLine();
                    foreach (ConsoleExecuteResult command in ConsoleExecute.RunMacro(Path.Combine(Path.GetDirectoryName(_commandAssembly.Location) + @"\Macro\", result.Action.Target)))
                    {
                        if (command.Action.Action == ConsoleExecuteActions.RunCommand)
                        {
                            shellControl1.SendCommand(command.Action.Target);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteToConsole(ex.ToString());
            }
            finally
            {
                if (async)
                {
                    Interlocked.Decrement(ref _asyncCount);
                }
            }
        }

        private void WriteToConsole(string message)
        {
            if (InvokeRequired)
            {
                void Del()
                {
                    WriteToConsole(message);
                }

                BeginInvoke((MethodInvoker)Del);
                return;
            }

            if (message.Length == 0) return;

            logControlCommand.Display(message + Environment.NewLine);
        }

        /// <summary>
        /// Raises the <see cref="E:Closing" /> event.
        /// </summary>
        /// <param name="e">The <see cref="System.ComponentModel.CancelEventArgs" /> instance containing the event data.</param>
        protected override void OnClosing(CancelEventArgs e)
        {
            //If we exit here and async tasks are running, we won't ever finish them. That's because they are tied to the shell's command event...
            if (Interlocked.Read(ref _asyncCount) > 0)
            {
                e.Cancel = true;
                WriteToConsole($"Async tasks are still running {Interlocked.Read(ref _asyncCount)}");
                return;
            }

            _commandLibraries?.Dispose();
            base.OnClosing(e);
        }
    }
    public class TextBoxStreamWriter : TextWriter
    {
        private readonly Action<string> _output;
        public TextBoxStreamWriter(Action<string> output)
        {
            _output = output;
        }

        public override void Write(char value)
        {
            _output.Invoke(value.ToString());
        }

        public override void Write(string value)
        {
            _output.Invoke(value);
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}
