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
using System.Linq;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

using JsonConfig;
using GcpD.API.References;

namespace GcpD.Core.ClientManagement {
    public partial class Client : IDisposable { // See Client.cs

        private static X509Certificate cert;

        public bool IsConnected {
            get { return RawClient != null && RawClient.Connected; }
        }

        public bool IsProperlyConnected {
            get { return _IsProperlyConnected; }
            internal set { _IsProperlyConnected = value; } 
        }

        public readonly ServerHandler Handler;

        private TcpClient RawClient;
        private StreamWriter Writer;
        private StreamReader Reader;
        private bool _Disposed = false;

        private Thread WorkerThread = null;
        private String HostName;
        private bool _IsProperlyConnected = false;


        public Client(TcpClient client, ServerHandler handler, bool ssl) {
            Handler = handler;
            RawClient = client;
            if (!ssl) {
                Writer = new StreamWriter(RawClient.GetStream());
                Reader = new StreamReader(RawClient.GetStream());
            } else {
                if (cert == null) {
                    var path = Path.GetFullPath(Path.Combine(References.GCPD_FOLDER, Config.User.Ssl_Cert_Path));
                    if (!(new FileInfo(path)).Exists)
                        throw new Exception("certificate does not exist at " + path);
                    cert = new X509Certificate2(path, Config.User.Ssl_Cert_Pass);
                }
                try {
                    SslStream stream = new SslStream(RawClient.GetStream(), false);
                    stream.AuthenticateAsServer(cert, false, System.Security.Authentication.SslProtocols.Tls, false);
                    Writer = new StreamWriter(stream);
                    Reader = new StreamReader(stream);
                } catch (IOException e) {
                    client.Close();
                    throw e;
                }
            }
            ThreadPool.QueueUserWorkItem((object o) => { AssignHostName(); });
        }

        #region Hostname methods
        private void AssignHostName() {
            var endPoint = (IPEndPoint) RawClient.Client.RemoteEndPoint;
            HostName = Dns.GetHostEntry(endPoint.Address).HostName;
        }

        public string GetHostName() {
            if (HostName == null)
                AssignHostName();
            return HostName;
        }
        #endregion

        #region Thread handlers
        protected void Work() {
            try {
                string line;
                while (IsConnected) {
                    line = Reader.ReadLine();
                    if (line == null) { // Disconnected or something bad happened; kill the listener
#if DEBUG
                        Console.WriteLine("Client {0} disconnected", GetHostName());
#endif
                        if (!_Disposed)
                            ThreadPool.QueueUserWorkItem((object o) => { Handler.ClientsManager.RemoveClient(this); });
                        break;
                    }
                    Parse(line);
                }
            } catch {
                ThreadPool.QueueUserWorkItem((object o) => {
                    StopHandling();
                    Handler.ClientsManager.RemoveClient(this); 
                });
            }
        }

        public void StartHandling() {
            try {
                try {
                    if (WorkerThread != null)
                        WorkerThread.Abort();
                } catch {}
                WorkerThread = null;
                WorkerThread = new Thread(new ThreadStart(Work));
                WorkerThread.Start();
            } catch {
                StopHandling();
            }
        }

        public void StopHandling() {
            try {
                if (WorkerThread != null)
                    WorkerThread.Abort();
            } catch {}
            WorkerThread = null;
        }
        #endregion

        #region Parser handlers
        protected void Parse(string line) {
            string[] splitData;
            if (!SyntaxCheck(line, out splitData)) {
                Send(SendType.SYNTAXERROR, string.Format("Error{1}0000{0}{2}", SyntaxCode.PARAM_SPLITTER, SyntaxCode.VALUE_SPLITTER, line));
                Console.WriteLine("Thrown syntax error for host {0}", GetHostName());
                return;
            }

            if (!Enum.GetNames(typeof(SendType)).Contains(splitData[0])) {
                SendError(SendType.ERROR_0001, splitData[0], line);
                return;
            }
            SendType type = (SendType) Enum.Parse(typeof(SendType), splitData[0]);

            if (type != SendType.CONNECT) {
                if (!IsProperlyConnected) {
                    SendError(SendType.ERROR_0007, type, line);
                    return;
                }
            }
            if (type != SendType.REGISTER && type != SendType.CONNECT)
                Console.WriteLine(line);

            switch (type) {
                case SendType.CONNECT:
                    ParserExecutor.Connect(this, line, splitData);
                    break;
                case SendType.QUIT:
                    Handler.ClientsManager.RemoveClient(this, Utilities.Utils.Split(splitData, "Reason")[0]);
                    break;
                case SendType.REGISTER:
                    ParserExecutor.Register(this, line, splitData);
                    break;
                case SendType.MSG:
                    ParserExecutor.Msg(this, line, splitData);
                    break;
                case SendType.JOIN:
                    ParserExecutor.Join(this, line, splitData);
                    break;
                case SendType.LEAVE:
                    ParserExecutor.Leave(this, line, splitData);
                    break;
                case SendType.PING:
                    ParserExecutor.Ping(this, line, splitData);
                    break;
                case SendType.PONG:
                    ParserExecutor.Pong(this, line, splitData);
                    break;
            }
        }

        protected bool SyntaxCheck(string line, out string[] splitData) {
            splitData = null;
            if (!line.Contains(SyntaxCode.PARAM_SPLITTER))
                return false;
            if (!line.Contains(SyntaxCode.VALUE_SPLITTER)) {
                splitData = new string[] { line.Substring(0, line.IndexOf('\u0001')) };
                return true;
            }
            splitData = line.Split(new[]{ SyntaxCode.PARAM_SPLITTER }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 1; i < splitData.Length; i++) {
                if (!splitData[i].Contains(SyntaxCode.VALUE_SPLITTER))
                    return false;
            }
            return true;

        }
        #endregion

        #region Deconstructor handlers
        ~Client() {
            Dispose(false);
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing) {
            if (_Disposed)
                return;
            _Disposed = true;

            if (disposing) {
                lock (_lock) {
                    try {
                        if (Writer != null)
                            Writer.Dispose();
                    } catch {}
                    Writer = null;

                    if (Reader != null)
                        Reader.Dispose();
                    Reader = null;

                    if (RawClient != null)
                        RawClient.Close();
                    RawClient = null;
                }
            }
        }
        #endregion

    }

}