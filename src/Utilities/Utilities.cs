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
using System.Security.Cryptography;
using GcpD.API.References;
using GcpD.Core;

namespace GcpD.Utilities {

    public static class Utils {

        public static ServerHandler GetServerHandler() {
            return InternalReferences.Handler;
        }

        /// <summary>
        /// Validates to make sure the 2 passwords are equal based on the salt. Salt must be a hexadecimal string.
        /// </summary>
        /// <returns><c>true</c>, if password is valid, <c>false</c> otherwise.</returns>
        /// <param name="key">Received password</param>
        /// <param name="hash">Original hash</param>
        /// <param name="salt">Hexadecimal salt</param>
        public static bool ValidPassword(string key, string hash, string salt) {
            if (key == null)
                return false;
            return Hash(key, salt) == hash;
        }

        /// <summary>
        /// Hashes the password based on the salt, the salt must be a hexadecimal string.
        /// </summary>
        /// <returns>Hashed password based on salt.</returns>
        /// <param name="password">password</param>
        /// <param name="salt">The hexadecimal string salt</param>
        public static string Hash(string password, string salt) {
            byte[] passcode = GetBytes(password);
            string hashString = "";
            using (HMACSHA512 sha = new HMACSHA512(FromHexString(salt))) {
                byte[] computed = sha.ComputeHash(passcode);
                hashString = ToHexString(computed);
            }

            return hashString;
        }

        /// <summary>
        /// Generates the salt.
        /// </summary>
        /// <returns>The salt in hex form.</returns>
        public static string GenerateSalt() {
            byte[] salt = new byte[2048];
            using (RNGCryptoServiceProvider crypto = new RNGCryptoServiceProvider()) {
                crypto.GetBytes(salt);
            }
            return ToHexString(salt);
        }

        public static byte[] GetBytes(string s) {
            return System.Text.Encoding.ASCII.GetBytes(s);
        }

        public static string ToHexString(byte[] bytes) {
            char[] c = new char[bytes.Length * 2];
            int b;
            for (int i = 0; i < bytes.Length; i++) {
                b = bytes[i] >> 4;
                c[i * 2] = (char) (55 + b + (((b - 10) >> 31) &- 7));
                b = bytes[i] & 0xF;
                c[i * 2 + 1] = (char) (55 + b + (((b - 10) >> 31) &- 7));
            }
            return new string(c);
        }

        public static byte[] FromHexString(string s) {
            int half = s.Length / 2;
            byte[] bytes = new byte[half];
            for (int i = 0; i < half; i++)
                bytes[i] = Convert.ToByte((char) s[i]);
            return bytes;
        }

        public static string[] Split(string[] data, params string[] parameters) {
            string[] filteredParams = new string[parameters.Length];
            for (int i = 0; i < data.Length; i++) {
                string[] subData = ValueSplitter(data[i]);
                if (subData == null)
                    continue;
                int index;
                if ((index = Array.IndexOf(parameters, subData[0])) > -1) {
                    filteredParams[index] = subData[1];
                }
            }
            return filteredParams;
        }

        public static string[] ValueSplitter(string data) {
            int index = data.IndexOf(SyntaxCode.VALUE_SPLITTER);
            if (index == -1)
                return null;
            return new string[] { data.Substring(0, index), data.Substring(index + 1) };
        }
    }

}

