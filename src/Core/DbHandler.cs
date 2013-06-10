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
using System.Data.Common;
using Mono.Data.Sqlite;

namespace GcpD.Core {
    public class DbHandler {

        public const string CONNECTION_STRING = "URI=file:Data.db";

        private readonly SqliteConnection connection;

        public DbHandler() : this(null) {
        }

        public DbHandler(SqliteConnection connection) {
            this.connection = connection ?? new SqliteConnection(CONNECTION_STRING);
            this.connection.Open();
        }

        ~DbHandler() {
            connection.Dispose();
        }

        /// <summary>
        /// Determines whether the value exists for the specified table and column.
        /// </summary>
        /// <example>
        /// The following example shows the usage.
        /// </example>
        /// <code>
        /// exists("Data", "MyColumn", "MyValue") // -> false or true if the value exists under the column in the table
        /// </code>
        /// <param name="table">Table.</param>
        /// <param name="column">Column.</param>
        /// <param name="value">Value.</param>
        public bool Exists(string table, string column, string value) {
            string query = "SELECT * FROM " + table + " WHERE " + column + "=@value";
            DbDataReader reader = Read(query, new SqliteParameter("@value", value));
            return reader.HasRows;
        }

        /// <summary>
        /// Inserts values into columns in the table
        /// </summary>
        /// <example>
        /// The following code is example usage. In the parameter s, half should be columns and half should be values.
        /// In this format: insert("TestTable", [Columns to insert values into], [Values to insert into columns in the same order])
        /// </example>
        /// <code>
        /// insert("Data", "Test", "TestValue")
        /// </code>
        /// <param name="table">Table.</param>
        /// <param name="s">Columns and values.</param>
        public void Insert(string table, params string[] s) {
            SqliteParameter[] parameters = new SqliteParameter[s.Length / 2];
            string query = "INSERT INTO " + table + " (";
            for (int i = 0; i < s.Length / 2; i++) {
                query += s[i] + (i == s.Length / 2 - 1 ? "" : ", ");
            }
            query += ") VALUES (";
            for (int i = s.Length / 2; i < s.Length; i++) {
                query += "@val" + i.ToString() + (i == s.Length - 1 ? "" : ", ");
                parameters[i - (s.Length / 2)] = new SqliteParameter("@val" + i.ToString(), s[i]);
            }

            query += ")";
            ExecuteNonQuery(query, parameters);
        }

        /// <summary>
        /// Deletes the specified index from the column in the table. Best used on the primary key column.
        /// </summary>
        /// <param name="table">Table.</param>
        /// <param name="column">Column.</param>
        /// <param name="index">Index.</param>
        public void Delete(string table, string column, string index) {
            string query = "DELETE FROM " + table + " WHERE " + column + "=@index";
            ExecuteNonQuery(query, new SqliteParameter("@index", index));
        }

        /// <summary>
        /// Read the specified query. Optionally providing parameters for the query.
        /// The DbDataReader object must be manually closed and set null.
        /// </summary>
        /// <example>
        /// This sample demonstrates the usage with parameters.
        /// </example>
        /// <code>
        /// read("SELECT MyTable FROM DATA WHERE MyColumn=@value", new SqliteParameter("@value", "MyValue"))
        /// </code>
        /// <param name="query">Query.</param>
        /// <param name="parameters">Parameters.</param>
        public DbDataReader Read(string query, params SqliteParameter[] parameters) {
            DbCommand command = connection.CreateCommand();
            command.CommandText = query;
            foreach (SqliteParameter param in parameters)
                command.Parameters.Add(param);
            return command.ExecuteReader();
        }

        /// <summary>
        /// Execute the specified query. Optionally providing parameters for the query.
        /// </summary>
        /// <example>
        /// This sample demonstrates the usage with parameters.
        /// </example>
        /// <code>
        /// ExecuteNonQuery("INSERT INTO MyTable (MyColumn) VALUES (@value)", new SqliteParameter("@value", "MyValue"))
        /// </code>
        /// <param name="query">Query.</param>
        /// <param name="parameters">Parameters.</param>
        public void ExecuteNonQuery(string query, params SqliteParameter[] parameters) {
            DbCommand command = connection.CreateCommand();
            command.CommandText = query;
            foreach (SqliteParameter param in parameters)
                command.Parameters.Add(param);

            command.ExecuteNonQuery();

            command.Dispose();
            command = null;
        }

    }
}

