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
using NUnit.Framework;
using GcpD.References;

namespace Test.Utilities {

    [TestFixture]
    public class Utils {
        [Test]
        public void Split() {
            string[] testArray = new string[] { "Channel\u0002#TestChannel", "Target\u0002Test" };

            string[] splitArray1 = GcpD.Utilities.Utils.Split(testArray, "Target", "Channel");
            string[] splitArray2 = GcpD.Utilities.Utils.Split(testArray, "Channel", "Target");
            string[] splitArray3 = GcpD.Utilities.Utils.Split(testArray, "Channel");
            string[] splitArray4 = GcpD.Utilities.Utils.Split(testArray, "Target");

            Assert.AreEqual(2, splitArray1.Length);
            Assert.AreEqual(2, splitArray2.Length);
            Assert.AreEqual(1, splitArray3.Length);
            Assert.AreEqual(1, splitArray4.Length);

            Assert.AreEqual("Test", splitArray1[0]);
            Assert.AreEqual("#TestChannel", splitArray1[1]);

            Assert.AreEqual("#TestChannel", splitArray2[0]);
            Assert.AreEqual("Test", splitArray2[1]);

            Assert.AreEqual("#TestChannel", splitArray3[0]);
            Assert.AreEqual("Test", splitArray4[0]);
        }

        [Test]
        public void ValueSplitter() {
            string testString = string.Format("PARAM{0}VALUE", SyntaxCode.VALUE_SPLITTER);
            string[] testArray = GcpD.Utilities.Utils.ValueSplitter(testString);

            Assert.AreEqual(2, testArray.Length);
            Assert.AreEqual("PARAM", testArray[0]);
            Assert.AreEqual("VALUE", testArray[1]);
        }
    }

}

