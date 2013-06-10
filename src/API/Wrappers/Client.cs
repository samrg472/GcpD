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
using GcpD.Core;
using GcpD.API.References;

namespace GcpD.API.Wrappers {
    public class Client {

        private readonly ServerHandler Handler;
        private readonly GcpD.Core.ClientManagement.Client _Client;

        public Client(ServerHandler handler, GcpD.Core.ClientManagement.Client client) {
            Handler = handler;
            _Client = client;
        }

        public void SendMessage(string message) {
            _Client.Send(SendType.MSG, string.Format("Channel{1}false{0}Target{1}{2}{0}Message{1}{3}", SyntaxCode.PARAM_SPLITTER, SyntaxCode.VALUE_SPLITTER, _Client.NickName, message));
        }

        public void SendMessage(string channel, string message) {
            Handler.ChannelsManager.SendMessage(_Client.NickName, message, channel);
        }

        public static Client GetClient(ServerHandler handler, string nick) {
            if (!handler.ClientsManager.NickTaken(nick))
                return null;
            return new Client(handler, handler.ClientsManager.GetClient(nick));
        }
    }
}

