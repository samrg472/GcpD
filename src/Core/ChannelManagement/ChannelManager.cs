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
using System.Collections.Generic;
using GcpD.Core.ClientManagement;
using GcpD.API.References;
using GcpD.Utilities;

namespace GcpD.Core.ChannelManagement {

    public class ChannelManager {

        public readonly ServerHandler Handler;

        private Dictionary<string, Channel> Channels = new Dictionary<string, Channel>();

        public ChannelManager(ServerHandler handler) {
            this.Handler = handler;
        }

        public bool UserInChannel(string user, string channel) {
            if (!Channels.ContainsKey(channel))
                return false;
            return Channels[channel].HasUser(user);
        }

        public void Join(string user, string channel) {
            if (!Channels.ContainsKey(channel))
                Create(channel);
            if (!Channels[channel].HasUser(user)) {
                foreach (string nick in Channels[channel].GetUsers()) {
                    Client client = Handler.ClientsManager.GetClient(nick);
                    client.Send(SendType.JOIN, string.Format("Channel{1}{2}{0}User{1}{3}", SyntaxCode.PARAM_SPLITTER, SyntaxCode.VALUE_SPLITTER, channel, user));
                }
                Console.WriteLine("User {0} joined {1}", user, channel);
            }
            Channels[channel].AddUser(user);
        }

        public void Leave(string user, string channel) {
            if (!Channels.ContainsKey(channel))
                return;
            Channels[channel].RemoveUser(user);
            foreach (string nick in Channels[channel].GetUsers()) {
                if (user == nick)
                    continue;
                Client client = Handler.ClientsManager.GetClient(nick);
                client.Send(SendType.LEAVE, string.Format("Channel{1}{2}{0}User{1}{3}", SyntaxCode.PARAM_SPLITTER, SyntaxCode.VALUE_SPLITTER, channel, user));
            }
            if (!Channels[channel].HasUsers())
                Destroy(channel);
        }

        public void Leave(string user) {
            string[] channels = new string[Channels.Count];
            Channels.Keys.CopyTo(channels, 0);
            for (int i = 0; i < channels.Length; i++)
                Leave(user, channels[i]);

        }

        public void SendMessage(string user, string channel, string message) {
            if (!Channels.ContainsKey(channel))
                return;
            if (!Channels[channel].HasUser(user))
                return;
            foreach (string nick in Channels[channel].GetUsers()) {
                if (nick == user)
                    continue;
                Client client = Handler.ClientsManager.GetClient(nick);
                client.Send(SendType.MSG, string.Format("Channel{1}true{0}Target{1}{2}{0}User{1}{4}{0}Message{1}{3}", SyntaxCode.PARAM_SPLITTER, SyntaxCode.VALUE_SPLITTER, channel, message, user));
            }
        }

        public void Create(string channel) {
            Channels[channel] = new Channel(channel);
        }

        public void Destroy(string channel) {
            Channels.Remove(channel);
        }

        public bool Registered(string channel) {
            return References.Database.Exists(InternalReferences.CHANNELS_TABLE, InternalReferences.CHANNELS_CHANNEL_COL, channel);
        }
    }

}

