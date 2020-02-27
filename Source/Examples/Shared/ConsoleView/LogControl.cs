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
using System.Collections.Concurrent;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
namespace ConsoleView
{
    public partial class LogControl : UserControl
    {
        private readonly ConcurrentQueue<string> _messages = new ConcurrentQueue<string>();
        private readonly Timer _timer;
        private readonly ReaderWriterLockSlim _runningLocker;
        private volatile bool _running;

        public LogControl()
        {
            InitializeComponent();
            textBoxMessage.Font = new Font(FontFamily.GenericMonospace, textBoxMessage.Font.Size);
            _runningLocker = new ReaderWriterLockSlim();
            _timer = new Timer
            {
                Enabled = true,
                Interval = 1000
            };
            _timer.Tick += timer_Tick;
            _timer.Start();
        }

        private void timer_Tick(object sender, System.EventArgs e)
        {
            if (!Running && !_messages.IsEmpty)
            {
                Task.Run(() => UpdateMessages());
            }
        }

        public void Display(string value)
        {
            _messages.Enqueue(value);
        }

        private void UpdateMessages()
        {
            if (textBoxMessage.InvokeRequired)
            {
                MethodInvoker del = UpdateMessages;
                BeginInvoke(del);
                return;
            }

            //NOTE: this has flaws, but that's ok.
            if (!Running)
            {
                Running = true;
            }
            try
            {
                while (!_messages.IsEmpty)
                {
                    if (_messages.TryDequeue(out string message))
                    {
                        //don't use begin invoke here, or the data will be out of order
                        textBoxMessage.AppendText(message);
                    }
                }
            }
            finally
            {
                Running = false;
            }
        }
        private bool Running
        {
            get
            {
                _runningLocker.EnterReadLock();
                try
                {
                    return _running;
                }
                finally
                {
                    _runningLocker.ExitReadLock();
                }
            }
            set
            {
                _runningLocker.EnterWriteLock();
                try
                {
                    _running = value;
                }
                finally
                {
                    _runningLocker.ExitWriteLock();
                }
            }
        }
    }
}
