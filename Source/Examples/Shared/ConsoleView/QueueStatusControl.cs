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
using System.Net;
using System.Windows.Forms;

namespace ConsoleView
{
    public partial class QueueStatusControl : UserControl
    {
        private string _location;

        public QueueStatusControl()
        {
            InitializeComponent();
        }

        public void Display(string location)
        {
            _location = location;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                if (!Uri.IsWellFormedUriString(_location, UriKind.Absolute)) return;
                using (var client = new WebClient())
                {
                    var result = client.DownloadString(new Uri(_location));
                    var json = Newtonsoft.Json.JsonSerializer.Create(new Newtonsoft.Json.JsonSerializerSettings());
                    var o = json.Deserialize(new Newtonsoft.Json.JsonTextReader(new System.IO.StringReader(result)));
                    richTextBoxJson.Text = o.ToString();
                }
            }
            catch (Exception error)
            {
                richTextBoxJson.Text = error.ToString();
            }
        }

        private void toolStripButtonRefresh_Click(object sender, EventArgs e)
        {
            LoadData();
        }
    }
}
