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
using System.Reflection;
using System.IO;
using System.Data.Common;
using System.Security.Principal;
using Mono.Data.Sqlite;
using JsonConfig;
using GcpD.Core;
using GcpD.Utilities;
using GcpD.API.References;

namespace GcpD.Core.Entry {

    public sealed class Start {

        private static void Main(string[] args) {
            UserCheck();
            var di = new DirectoryInfo(References.GCPD_FOLDER);
            if (!di.Exists)
                di.Create();
            Directory.SetCurrentDirectory(References.GCPD_FOLDER);
            new Start().Run(args);
        }

        private static void UserCheck() {
#if BYPASS_SECURITY
            Console.WriteLine("WARNING:: STARTING IN INSECURE MODE");
            return;
#endif
            WindowsIdentity user = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(user);
            if (principal.IsInRole(WindowsBuiltInRole.Administrator))
                throw new InvalidOperationException("Cannot be ran as super-user");
        }

        private void Run(string[] args) {
            InitializeSettings();
            InitializeDatabase();

            Config.SetUserConfig(Config.ApplyJsonFromPath(Path.Combine(References.GCPD_FOLDER, "settings.conf")));
            Console.WriteLine("Binding to {0} on port {1}", string.IsNullOrEmpty(Config.User.BindAddress) ? "all interfaces" : Config.User.BindAddress, ushort.Parse(Config.User.Port));
            Console.WriteLine("Maximum number of connections {0}", Config.User.MaxConnections);
            InternalReferences.Handler = new ServerHandler(Config.User.BindAddress, 
                                                     ushort.Parse(Config.User.Port), 
                                                     uint.Parse(Config.User.MaxConnections));
            InternalReferences.Handler.Start();

            var loader = new Plugin.PluginLoader();
            loader.LoadPlugins();
        }

        private void InitializeDatabase() {
            // Populate sqlite database with default tables and columns
            References.Database.ExecuteNonQuery(string.Format("CREATE TABLE IF NOT EXISTS {0}({1} TEXT PRIMARY KEY NOT NULL, {2} TEXT NOT NULL)", 
                                                              InternalReferences.CHANNELS_TABLE, InternalReferences.CHANNELS_CHANNEL_COL, InternalReferences.CHANNELS_OWNER_COL));
            References.Database.ExecuteNonQuery(string.Format("CREATE TABLE IF NOT EXISTS {0}({1} TEXT PRIMARY KEY NOT NULL, {2} TEXT NOT NULL, {3} TEXT KEY NOT NULL)", 
                                                              InternalReferences.NICKS_TABLE, InternalReferences.NICKS_NICK_COL, InternalReferences.NICKS_PASS_COL, InternalReferences.NICKS_SALT_COL));
        }

        private void InitializeSettings() {
            if (new FileInfo(Path.Combine(References.GCPD_FOLDER, "settings.conf")).Exists)
                return;

            StreamReader reader = null;
            StreamWriter writer = null;
            try {
                reader = new StreamReader(Assembly.GetEntryAssembly().GetManifestResourceStream("default.conf"));
                writer = new StreamWriter(Path.Combine(References.GCPD_FOLDER, "settings.conf"));
                string writeOut = reader.ReadToEnd();
                writer.Write(writeOut);
                writer.Flush();
            } catch {
                Console.WriteLine("Some weird error writing the config file! :(");
            } finally {
                if (reader != null)
                    reader.Close();
                if (writer != null)
                    writer.Close();
                reader = null;
                writer = null;
            }
        }
    }
}

