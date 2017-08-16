DotNetWorkQueue.Examples
=========

[**SQL Server**]
- Producer - Send Messages via SQL server transport
- Consumer - Process messages via SQL server transport
- ConsumerAsync - Process messages via SQL server transport using shared task scheduler
- RPCProducer - Send a message via SQL server transport and receive a response
- RPCConsumer - Process a messge via SQL server transport and send a response

[**Redis**]
- Producer - Send Messages via Redis transport
- Consumer - Process messages via Redis transport
- ConsumerAsync - Process messages via Redis transport using shared task scheduler
- RPCProducer - Send a message via Redis transport and receive a response
- RPCConsumer - Process a messge via Redis transport and send a response

[**SQLite**]
- Producer - Send Messages via SQLite transport
- Consumer - Process messages via SQLite transport
- ConsumerAsync - Process messages via SQLite transport using shared task scheduler
- RPCProducer - Send a message via SQLite transport and receive a response
- RPCConsumer - Process a messge via SQLite transport and send a response

[**PostGre**]
- Producer - Send Messages via SQL server transport
- Consumer - Process messages via SQL server transport
- ConsumerAsync - Process messages via SQL server transport using shared task scheduler
- RPCProducer - Send a message via SQL server transport and receive a response
- RPCConsumer - Process a messge via SQL server transport and send a response
- 
License
--------
Copyright (c) 2017 Brian Lehnen

All rights reserved.

MIT License

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.

3rd party Libraries
--------

This library uses multiple 3rd party libaries, listed below.

NOTE: Changes have been made to both libaries; they no longer exactly match the versions available on the web sites.

[ConsoleCommandLibrary ](http://www.codeproject.com/Articles/816301/Csharp-Building-a-Useful-Extensible-NET-Console-Ap)

Modifications:

    - Allow using instances instead of static libaries
    - Support async commands
    - Support command result actions - enum used, so actions are limited to hard coded list
    - Support params collection for string and timespan
    - Support nullable timespan as param
    - List default value for optional params
    - Add help/example syntax
    - Added macro support (capture/cancel/save/run)


[ShellControl ](http://www.codeproject.com/Articles/9621/ShellControl-A-console-emulation-control)

Modifications:

    - Changed code from dot net 1.1 to 4.0.
    - TAB scrolls through list of commmands.
