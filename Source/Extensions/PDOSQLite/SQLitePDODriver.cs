﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using PHP.Core;
using System.IO;

namespace PHP.Library.Data
{
    public sealed class SQLitePDODriver : PDODriver
    {
        public override string Scheme { get { return "sqlite"; } }

        public override PDOConnection OpenConnection(ScriptContext context, string dsn_data, string username, string password, object argdriver_options)
        {
            //Determine file path
            string filename = dsn_data.Replace('/', Path.DirectorySeparatorChar);
            string filePath = Path.GetFullPath(Path.Combine(context.WorkingDirectory, filename));

            SQLiteConnectionStringBuilder csb = new SQLiteConnectionStringBuilder();
            csb.DataSource = filePath;
            csb.Version = 3;

            SQLiteConnection con = new SQLiteConnection(csb.ConnectionString);
            con.Open();

            return new SQLitePDOConnection(this, con);
        }

        public override object Quote(ScriptContext context, object strobj, PDOStatics.pdo_param_type param_type)
        {
            //From mysql extension
            if (strobj == null)
                return string.Empty;

            // binary aware:
            if (strobj.GetType() == typeof(PhpBytes))
            {
                var strbytes = (PhpBytes)strobj;
                if (strbytes.Length == 0) return strobj;

                var bytes = strbytes.ReadonlyData;
                List<byte>/*!*/result = new List<byte>(bytes.Length);
                for (int i = 0; i < bytes.Length; i++)
                {
                    switch (bytes[i])
                    {
                        case (byte)'\0': result.Add((byte)'\\'); goto default;
                        case (byte)'\\': result.Add((byte)'\\'); goto default;
                        case (byte)'\n': result.Add((byte)'\\'); result.Add((byte)'n'); break;
                        case (byte)'\r': result.Add((byte)'\\'); result.Add((byte)'r'); break;
                        case (byte)'\u001a': result.Add((byte)'\\'); result.Add((byte)'Z'); break;
                        case (byte)'\'': result.Add((byte)'\\'); goto default;
                        case (byte)'"': result.Add((byte)'\\'); goto default;
                        default: result.Add(bytes[i]); break;
                    }
                }

                return new PhpBytes(result.ToArray());
            }

            // else
            string str = Core.Convert.ObjectToString(strobj);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str.Length; i++)
            {
                char c = str[i];
                switch (c)
                {
                    case '\0': sb.Append(@"\0"); break;
                    case '\\': sb.Append(@"\\"); break;
                    case '\n': sb.Append(@"\n"); break;
                    case '\r': sb.Append(@"\r"); break;
                    case '\u001a': sb.Append(@"\Z"); break;
                    case '\'': sb.Append(@"\'"); break;
                    case '"': sb.Append("\\\""); break;
                    default: sb.Append(c); break;
                }
            }

            return sb.ToString();
        }
    }
}
