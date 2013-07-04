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
using GcpD.API.References;
using GcpD.Utilities;

namespace GcpD.Core.ClientManagement {
    internal static class ParserExecutor {

        public static void Connect(Client handler, string line, string[] splitData) {
            if (handler.IsProperlyConnected)
                return;

            string[] data;
            if (ParserHelper.Connect(splitData, out data)) {
                if (handler.Handler.ClientsManager.NickRegistered(data[0])) {
                    System.Data.Common.DbDataReader reader = References.Database.Read(string.Format("SELECT {0}, {1} FROM {2} WHERE {3}=@val", InternalReferences.NICKS_PASS_COL, InternalReferences.NICKS_SALT_COL, InternalReferences.NICKS_TABLE, InternalReferences.NICKS_NICK_COL),
                                             new Mono.Data.Sqlite.SqliteParameter("@val", data[0]));
                    string hash = "";
                    string salt = "";
                    while (reader.Read()) {
                        hash = reader.GetString(0);
                        salt = reader.GetString(1);
                    }
                    reader.Close();
                    reader = null;
                    if (!Utils.ValidPassword(data[1], hash, salt)) {
                        handler.SendError(SendType.ERROR_0002, SendType.CONNECT, line);
                        return;
                    }
                    handler.SetAuthenticated(true);
                }
                if (handler.SetNickName(data[0])) {
                    handler.Send(SendType.CONNECT, string.Format("Connected{0}true", SyntaxCode.VALUE_SPLITTER));
                    handler.IsProperlyConnected = true;
                    handler.SetRealName(data[2] ?? data[0]);
                } else {
                    handler.SendError(SendType.ERROR_0006, SendType.CONNECT, line);
                }
            } else
                handler.SendError(SendType.ERROR_0005, SendType.CONNECT, line);
        }

        public static void Register(Client handler, string line, string[] splitData) {
            string[] data;
            if (!ParserHelper.Register(splitData, out data)) {
                handler.SendError(SendType.ERROR_0005, SendType.REGISTER, line);
                return;
            }
            if (data[0] == "user") {
                if (handler.Handler.ClientsManager.NickRegistered(handler.NickName)) {
                    handler.SendError(SendType.ERROR_0006, SendType.REGISTER, line);
                    return;
                }
                string salt = Utils.GenerateSalt();
                References.Database.Insert(InternalReferences.NICKS_TABLE, InternalReferences.NICKS_NICK_COL, InternalReferences.NICKS_PASS_COL, InternalReferences.NICKS_SALT_COL,
                                           handler.NickName, Utils.Hash(data[1], salt), salt);
                handler.SetAuthenticated(true);
            } else if (data[0] == "channel") {
                if (handler.Handler.ChannelsManager.ChannelRegistered(data[2])) {
                    handler.SendError(SendType.ERROR_0006, SendType.REGISTER, line);
                    return;
                }
                string salt = Utils.GenerateSalt();
                References.Database.Insert(InternalReferences.CHANNELS_TABLE, InternalReferences.CHANNELS_CHANNEL_COL, InternalReferences.CHANNELS_OWNER_COL, InternalReferences.CHANNELS_PASS_COL, InternalReferences.CHANNELS_SALT_COL,
                                           data[2], handler.NickName, !string.IsNullOrEmpty(data[1]) ? Utils.Hash(data[1], salt) : null, !string.IsNullOrEmpty(data[1]) ? salt : null);
            } else {
                handler.SendError(SendType.ERROR_0005, SendType.REGISTER, line);
            }
        }

        public static void Join(Client handler, string line, string[] splitData) {
            string[] data;
            if (ParserHelper.Join(splitData, out data)) {
                if (handler.Handler.ChannelsManager.ChannelRegistered(data[0])) {
                    string hash = null;
                    string salt = null;
                    using (var reader = References.Database.Read(string.Format("SELECT {0}, {1} FROM {2} WHERE {3}=@val", InternalReferences.CHANNELS_PASS_COL, InternalReferences.CHANNELS_SALT_COL, InternalReferences.CHANNELS_TABLE, InternalReferences.CHANNELS_CHANNEL_COL),
                                                                 new Mono.Data.Sqlite.SqliteParameter("@val", data[0]))) {
                        while (reader.Read()) {
                            hash = reader.IsDBNull(0) ? null : reader.GetString(0);
                            salt = reader.IsDBNull(1) ? null : reader.GetString(1);
                        }
                    }
                    if (hash != null && salt != null) {
                        if (data[1] == null || !Utils.ValidPassword(data[1], hash, salt)) {
                            handler.SendError(SendType.ERROR_0002, SendType.CONNECT, line);
                            return;
                        }
                    }
                }
                handler.Handler.ChannelsManager.Join(handler.NickName, data[0]);
            } else
                handler.SendError(SendType.ERROR_0005, SendType.JOIN, line);
        }

        public static void Leave(Client handler, string line, string[] splitData) {
            string[] data;
            if (ParserHelper.Leave(splitData, out data)) {
                if (handler.Handler.ChannelsManager.UserInChannel(handler.NickName, data[0])) {
                    handler.SendError(SendType.ERROR_0003, SendType.LEAVE, line);
                    return;
                }
                handler.Handler.ChannelsManager.Leave(handler.NickName, data[0]);
            } else
                handler.SendError(SendType.ERROR_0005, SendType.LEAVE, line);
        }

        public static void Msg(Client handler, string line, string[] splitData) {
            string[] data;
            if (!ParserHelper.Msg(splitData, out data)) {
                handler.SendError(SendType.ERROR_0005, SendType.MSG, line);
                return;
            }
            if (string.IsNullOrEmpty(data[1])) {
                handler.SendError(SendType.ERROR_0004, SendType.MSG, line);
                return;
            }
            if (data[0] == "true") {
                handler.Handler.ChannelsManager.SendMessage(handler.NickName, data[1], data[2]);
                EventHandlers.PostMessageEvent(new API.Events.MessageEvent(data[1], handler.NickName, data[2]));
            } else if (data[0] == "false") {
                if (!handler.Handler.ClientsManager.NickTaken(data[1])) {
                        handler.SendError(SendType.ERROR_0004, SendType.MSG, line);
                    return;
                    }
                Client client = handler.Handler.ClientsManager.GetClient(data[1]);
                client.Send(SendType.MSG, string.Format("Channel{1}false{0}Target{1}{2}{0}Message{1}{3}", SyntaxCode.PARAM_SPLITTER, SyntaxCode.VALUE_SPLITTER, data[1], data[2]));
                EventHandlers.PostMessageEvent(new API.Events.MessageEvent(data[1], data[2]));
            } else {
                handler.SendError(SendType.ERROR_0005, SendType.MSG, line);
            }
        
        }

        public static void Ping(Client handler, string line, string[] splitData) {
            string[] data;
            ParserHelper.Ping(splitData, out data);

            if (data[0] == null) { // Client wants to ping the server
                handler.Send(SendType.PONG);
            } else { // Client wants to ping a client
                if (!handler.Handler.ClientsManager.NickTaken(data[0])) {
                    handler.SendError(SendType.ERROR_0004, SendType.PING, line);
                    return;
                }
                Client client = handler.Handler.ClientsManager.GetClient(data[0]);
                client.Send(SendType.PING, string.Format("User{0}{1}", SyntaxCode.VALUE_SPLITTER, data[0]));
            }
        }

        public static void Pong(Client handler, string line, string[] splitData) {
            string[] data;
            if (!ParserHelper.Pong(splitData, out data)) {
                handler.SendError(SendType.ERROR_0005, SendType.PONG, line);
                return;
            }
            if (!handler.Handler.ClientsManager.NickTaken(data[0]))
                return;
            Client client = handler.Handler.ClientsManager.GetClient(data[0]);
            client.Send(SendType.PONG, string.Format("User{0}{1}", SyntaxCode.VALUE_SPLITTER, data[0]));
        }
    }

    internal static class ParserHelper {

        public static bool Connect(string[] data, out string[] parameters) {
            parameters = Utils.Split(data, "Nick", "Pass", "RealName");
            if (parameters[0] == null)
                return false;
            return true;
        }

        public static bool Register(string[] data, out string[] parameters) {
            parameters = Utils.Split(data, "Type", "Pass", "Channel");
            if ((parameters[0] == null) || ((parameters[1] == null) && (parameters[2] == null)))
                return false;
            return true;
        }

        public static bool Join(string[] data, out string[] parameters) {
            parameters = Utils.Split(data, "Channel", "Pass");
            if (parameters[0] == null)
                return false;
            return true;
        }

        public static bool Leave(string[] data, out string[] parameters) {
            parameters = Utils.Split(data, "Channel");
            if (parameters[0] == null)
                return false;
            return true;
        }

        public static bool Msg(string[] data, out string[] parameters) {
            parameters = Utils.Split(data, "Channel", "Target", "Message");
            for (int i = 0; i < parameters.Length; i++)
                if (parameters[i] == null)
                    return false;
            return true;
        }

        public static bool Ping(string[] data, out string[] parameters) {
            parameters = Utils.Split(data, "User");
            return true;
        }

        public static bool Pong(string[] data, out string[] parameters) {
            parameters = Utils.Split(data, "User");
            if (parameters[0] == null)
                return false;
            return true;
        }
    }
}

