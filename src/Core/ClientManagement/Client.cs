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
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using GcpD.API.References;

namespace GcpD.Core.ClientManagement {
    public partial class Client : IDisposable { // See ClientHandler.cs

        public string NickName {
            get { return _NickName; }
        }
        private string _NickName = null;

        public string RealName {
            get { return _RealName; }
        }
        private string _RealName = null;

        public void Send(SendType type) {
            Send(type, "");
        }

        public void Send(SendType type, string msg) {
            Writer.WriteLine(type.ToString() + SyntaxCode.PARAM_SPLITTER + msg);
            Writer.Flush();
        }

        public void SendError(SendType error, SendType type, string data) {
            SendError(error, type.ToString(), data);
        }

        internal void SendError(SendType error, string type, string data) {
            string errorString = error.ToString();
            string numericalError = errorString.Substring(errorString.IndexOf('_') + 1);
            Send(SendType.ERROR, string.Format("Error{1}{2}{0}Type{1}{3}{0}{4}",
                                               SyntaxCode.PARAM_SPLITTER, SyntaxCode.VALUE_SPLITTER, numericalError,
                                               type,
                                               data.Substring(data.IndexOf(SyntaxCode.PARAM_SPLITTER) + 1)));
        }

        /// <summary>
        /// Sets the name of the nick. Make sure to handle unsuccessful nick changes due to the nick being taken.
        /// </summary>
        /// <returns><c>true</c>, if nick name was successfully set, <c>false</c> otherwise.</returns>
        public bool SetNickName(string nick) {
            if (Handler.ClientsManager.NickTaken(nick))
                return false;
            if (NickName != null)
                Handler.ClientsManager.ChangeNick(NickName, nick);
            else
                Handler.ClientsManager.AddNick(nick, this);
            _NickName = nick;
            return true;
        }

        public void SetRealName(string realName) {
            if (_RealName != null)
                return;
            _RealName = realName;
        }
    }
}

