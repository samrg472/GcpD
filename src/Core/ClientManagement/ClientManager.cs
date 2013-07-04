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
using GcpD.Utilities;
using GcpD.API.References;

namespace GcpD.Core.ClientManagement {
    public class ClientManager {

        public readonly ServerHandler Handler;

        private List<Client> UnnamedClients;
        private Dictionary<string, Client> Nicks;
        private readonly object _lock = new object();

        public ClientManager(ServerHandler handler, int maxUsers) {
            UnnamedClients = new List<Client>(maxUsers);
            Nicks = new Dictionary<string, Client>(maxUsers);
            Handler = handler;
        }

        public Client GetClient(string nick) {
            return Nicks[nick];
        }

        public bool NickTaken(string nick) {
            return Nicks.ContainsKey(nick);
        }

        public bool NickRegistered(string nick) {
            return References.Database.Exists(InternalReferences.NICKS_TABLE, InternalReferences.NICKS_NICK_COL, nick);
        }

        public void AddNick(string nick, Client client) {
            if (nick == null)
                throw new ArgumentNullException("nick");
            if (client == null)
                throw new ArgumentNullException("client");
            lock (_lock) {
                if (NickTaken(nick)) {
                    Console.WriteLine("!!! WARNING !!! Attempting to add a nick thats already taken!");
                    return;
                }
                if (UnnamedClients.Contains(client)) {
                    Console.WriteLine("Unnamed client has identified themself as " + nick);
                    UnnamedClients.Remove(client);
                }

                Nicks.Add(nick, client);
            }
        }

        internal void AddUnnamed(Client client) {
            if (UnnamedClients.Contains(client) || Nicks.ContainsValue(client))
                return;
            UnnamedClients.Add(client);
        }

        public void ChangeNick(string oldNick, string newNick) {
            Client client = Nicks[oldNick];
            AddNick(newNick, client);
            RemoveNick(oldNick, false);
        }

        private void RemoveNick(string nick, bool dispose) {
            if (dispose && Nicks.ContainsKey(nick))
                Nicks[nick].Dispose();
            Nicks[nick] = null;
            Nicks.Remove(nick);
            Handler.ChannelsManager.Leave(nick);
        }

        internal void RemoveClient(Client client) {
            Console.WriteLine("Erasing {0} from base", client.NickName ?? "unknown client"); // DEBUG
            if (client.NickName != null) {
                RemoveNick(client.NickName, true);
                return;
            }
            UnnamedClients.Remove(client);
            client.Dispose();
        }

        public void Clear() {
            foreach (Client c in Nicks.Values)
                c.Dispose();
            foreach (Client c in UnnamedClients)
                c.Dispose();
            Nicks.Clear();
            UnnamedClients.Clear();
        }
    }
}

