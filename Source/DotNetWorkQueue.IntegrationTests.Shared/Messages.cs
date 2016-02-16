// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Tynamix.ObjectFiller;

namespace DotNetWorkQueue.IntegrationTests.Shared
{
    public class FakeResponse
    {
        public string ResponseMessage { get; set; }
    }
    public class FakeMessage
    {
        public FakeMessage()
        {
            MoreInfo = new List<FakeSubClass>();
        }
        public string Name { get; set; }
        public DateTime BornOn { get; set; }
        public string HomePage { get; set; }
        public decimal Amount { get; set; }
        public bool Allowed { get; set; }
        public List<FakeSubClass> MoreInfo { get; set; }
    }

    public class FakeSubClass
    {
        public string MoreInfo { get; set; }
    }

    public class FakeMessageA
    {
        public string Name { get; set; }
        public DateTime BornOn { get; set; }
        public string HomePage { get; set; }
        public decimal Amount { get; set; }
        public bool Allowed { get; set; }
    }

    public class FakeMessageB
    {
        public string Name { get; set; }
        public bool Allowed { get; set; }
    }
    public static class GenerateMessage
    {
        public static TMessage Create<TMessage>()
            where TMessage: class
        {
            var pFiller = new Filler<TMessage>();
            return pFiller.Create();
        }
    }
}
