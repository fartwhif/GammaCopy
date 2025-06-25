using DiscUtils.Iso9660;
using GammaCopy.Formats;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace GammaCopy
{
    internal static class Extensions
    {
        #region https://stackoverflow.com/a/9995303/6620171
        public static byte[] StringToByteArrayFastest(string hex)
        {
            if (hex.Length % 2 == 1)
            {
                throw new Exception("The binary key cannot have an odd number of digits");
            }

            byte[] arr = new byte[hex.Length >> 1];

            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                arr[i] = (byte)((GetHexVal(hex[i << 1]) << 4) + (GetHexVal(hex[(i << 1) + 1])));
            }

            return arr;
        }
        public static int GetHexVal(char hex)
        {
            int val = hex;
            //For uppercase A-F letters:
            //return val - (val < 58 ? 48 : 55);
            //For lowercase a-f letters:
            return val - (val < 58 ? 48 : 87);
            //Or the two combined, but a bit slower:
            //return val - (val < 58 ? 48 : (val < 97 ? 55 : 87));
        }
        #endregion


        internal static List<string> GetAllCDFilePaths(this CDReader cd, string path)
        {
            List<string> files = new List<string>();
            files.AddRange(cd.GetFiles(path));
            string[] dirs = cd.GetDirectories(path);
            foreach (string dir in dirs)
            {
                files.AddRange(GetAllCDFilePaths(cd, dir));
            }
            return files;
        }

        public static bool Equals2(this Tuple<long, long> value, Tuple<long, long> value2)
        {
            return value.Item1 == value2.Item1 && value.Item2 == value2.Item2;
        }

        public static string JoinMd52(this Tuple<long, long> md5)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(md5.Item1));
            bytes.AddRange(BitConverter.GetBytes(md5.Item2));
            if (bytes.Count != 16)
            {
                throw new Exception("the checksum doesn't have a length of 128 bits.");
            }
            return bytes.ToArray().AsHex();
        }


        public class LongPair: Tuple<long, long>
        {
            public LongPair(Tuple<long,long> src):base(src.Item1,src.Item2)
            {

            }
            public override bool Equals(object obj)
            {
                var p = obj as Tuple<long, long>;
                return p.Equals2(this);
            }
        }

        public static byte[] JoinMd5(this Tuple<long, long> md5)
        {
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(md5.Item1));
            bytes.AddRange(BitConverter.GetBytes(md5.Item2));
            if (bytes.Count != 16)
            {
                throw new Exception("the checksum doesn't have a length of 128 bits.");
            }
            return bytes.ToArray();
        }
        public static byte[] AsMd5(this string subject)
        {
            return AsMd5(subject, Encoding.UTF8);
        }
        public static byte[] AsMd5(this string subject, Encoding encoding)
        {
            using (MD5 md5 = MD5.Create())
            {
                return md5.ComputeHash(encoding.GetBytes(subject));
            }
        }
        public static Tuple<long, long> Md5Split(this string md5Hex)
        {
            if (md5Hex.Length != 32)
            {
                throw new ArgumentOutOfRangeException("this isn't a 128 bit checksum in hex string format");
            }
            byte[] bytes = StringToByteArrayFastest(md5Hex);
            return bytes.Md5Split();
        }
        public static string AsHex(this byte[] bytes)
        {
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }
        public static Tuple<long, long> Md5Split(this byte[] md5)
        {
            if (md5.Length != 16)
            {
                throw new Exception("the checksum doesn't have a length of 128 bits.");
            }
            return new Tuple<long, long>(
                BitConverter.ToInt64(md5.Take(8).ToArray(), 0),
                BitConverter.ToInt64(md5.Skip(8).ToArray(), 0));
        }

        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
        public static string ToSMDB(this List<SMDBEntry> list)
        {
            StringBuilder sb = new StringBuilder();
            list.ForEach(k => sb.AppendLine(k.ToString()));
            return sb.ToString();
        }
        public static string Tail(this string subject, int length)
        {
            if (subject.Length >= length)
            {
                return subject.Substring(subject.Length - length);
            }
            else
            {
                int dif = length - subject.Length;
                return subject.PadLeft(dif, ' ');
            }
        }
        public static string PudLeft(this string subject, int length)
        {
            if (subject.Length >= length)
            {
                return " " + subject;
            }
            else
            {
                int dif = length - subject.Length;
                return subject.PadLeft(dif, ' ');
            }
        }

        public static double ToTimestamp(this DateTime dateTime)
        {
            return (dateTime.ToUniversalTime().Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }
        public static DateTime ToDateTime(this double timestamp)
        {
            DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return dtDateTime.AddSeconds(timestamp).ToLocalTime();
        }
        public static int ToBinaryInt(this bool bol)
        {
            return (bol) ? 1 : 0;
        }
        public static string SafeGetString(this SQLiteDataReader reader, string col)
        {
            if (!reader.IsDBNull(reader.GetOrdinal(col)))
            {
                return reader.GetString(reader.GetOrdinal(col));
            }

            return string.Empty;
        }
        public static int SafeGetInt(this SQLiteDataReader reader, string col)
        {
            if (!reader.IsDBNull(reader.GetOrdinal(col)))
            {
                return reader.GetInt32(reader.GetOrdinal(col));
            }

            return 0;
        }
        public static long SafeGetLong(this SQLiteDataReader reader, string col)
        {
            if (!reader.IsDBNull(reader.GetOrdinal(col)))
            {
                return reader.GetInt64(reader.GetOrdinal(col));
            }

            return 0;
        }


    }
}
