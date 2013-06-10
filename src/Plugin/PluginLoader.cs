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
using System.Reflection;
using System.Collections.Generic;
using GcpD.Utilities;

namespace GcpD.Plugin {

    public class PluginLoader {

        public static readonly string PluginsDirectory = Path.Combine(Utils.GCPD_FOLDER, "plugins");

        public void LoadPlugins() {
            string[] paths = Directory.GetFiles(PluginsDirectory, "*.dll");
            foreach (string path in paths)
                Load(path);
        }

        private void Load(string dll) {
            string DLLPath = Path.Combine(PluginsDirectory, dll);
            if (!(new FileInfo(DLLPath).Exists))
                return;

            var name = AssemblyName.GetAssemblyName(DLLPath);
            var assembly = Assembly.Load(name);

            foreach (Type t in assembly.GetTypes()) {
                if (t.GetInterface(typeof(API.IPlugin).Name) == null)
                    continue;

                API.IPlugin plugin = ((API.IPlugin)Activator.CreateInstance(t));
                plugin.OnLoad();
                plugin = null;
                return;
            }
        }

        private static Assembly AssemblyResolver(object sender, ResolveEventArgs args) {
            var assemblyname = new AssemblyName(args.Name).Name;
            var assemblyFileName = Path.Combine(PluginsDirectory, assemblyname + ".dll");
            return Assembly.LoadFrom(assemblyFileName);
        }

        static PluginLoader() {
            AppDomain.CurrentDomain.AssemblyResolve += AssemblyResolver;
            DirectoryInfo info = new DirectoryInfo(PluginsDirectory);
            if (!info.Exists)
                info.Create();
        }
    }
}

