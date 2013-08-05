//
//  Author:
//    samrg472 samrg472@gmail.com
//
//  Copyright (c) 2013
//
//  All rights reserved.
//
//  Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//
//     * Redistributions in modified binary form must provide the modified source code.
//     * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//     * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in
//       the documentation and/or other materials provided with the distribution.
//     * Neither the name of the [ORGANIZATION] nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
//  THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
//  "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
//  LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
//  A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
//  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
//  EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
//  PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
//  PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
//  LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
//  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
//  SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using GcpD.Core;
using GcpD.Core.ClientManagement;
using GcpD.Core.ChannelManagement;
using GcpD.Utilities;

namespace GcpD.Core {

    public class ServerHandler {

        public bool IsListening {
            get { return ServerListener.Server.Active && SslServerListener.Server.Active; }
        }

        public readonly int MaxConnections;
        public readonly ChannelManager ChannelsManager;
        public readonly ClientManager ClientsManager;

        private readonly Listener ServerListener;
        private readonly Listener SslServerListener;

        #region Constructors/Destructors
        public ServerHandler(string bindAddress, ushort port, ushort sslPort, uint maxConnections) {
            MaxConnections = (int) maxConnections;
            ChannelsManager = new ChannelManager(this);
            ClientsManager = new ClientManager(this);

            var Server = new TcpListenerWrapper(string.IsNullOrEmpty(bindAddress) ? IPAddress.Any : IPAddress.Parse(bindAddress), port);
            ServerListener = new Listener(this, Server, false);

            var SslServer = new TcpListenerWrapper(string.IsNullOrEmpty(bindAddress) ? IPAddress.Any : IPAddress.Parse(bindAddress), sslPort);
            SslServerListener = new Listener(this, SslServer, true);
        }

        ~ServerHandler() {
            Stop();
        }
        #endregion

        #region Controllers
        public void Start() {
            if (IsListening)
                return;

            ClientsManager.Clear();
            ServerListener.Run(true);
            SslServerListener.Run(true);
        }

        public void Stop() {
            if (!IsListening)
                return;
            ServerListener.Run(false);
            SslServerListener.Run(false);

            ClientsManager.Clear();
        }
        #endregion

        private class Listener {

            public readonly TcpListenerWrapper Server;

            private Thread thread;
            private readonly ServerHandler Handler;
            private readonly bool Ssl;

            public Listener(ServerHandler handler, TcpListenerWrapper server, bool ssl) {
                Handler = handler;
                Server = server;
                Ssl = ssl;
            }

            public void Run(bool run) {
                try {
                    if (run) {
                        Server.Start(Handler.MaxConnections);
                        thread = new Thread(new ThreadStart(ClientListener));
                        thread.Start();
                    } else {
                        Server.Stop();
                        thread.Abort();
                    }
                } catch (Exception e) {
                    Console.WriteLine("Error performing task to start/stop server\n", e);
                }
            }

            public void ClientListener() {
                try {
                    while (Server.Active) {
                        TcpClient client = Server.AcceptTcpClient();
                        if (Handler.ClientsManager.getTotalUsersCount() >= Handler.MaxConnections) {
                            client.Close();
                            continue;
                        }
                        try {
                            Client cHandler = new Client(client, Handler, Ssl);
                            cHandler.StartHandling();
                            Handler.ClientsManager.AddUnnamed(cHandler);
                        } catch (Exception e) {
                            Console.WriteLine("Error handling user:\n" + e);
                        }
                    }
                } catch {}
            }
        }
    }
}