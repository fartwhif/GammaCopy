using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GammaCopy.Formats
{
    internal class SMDBEntry
    {
        public string SHA256 { get; set; }
        public string SHA1 { get; set; }
        public string MD5 { get; set; }
        public string CRC32 { get; set; }
        public string Path { get; set; }
        public int Index { get; set; }
        public override string ToString()
        {
            return $"{SHA256}\t{Path}\t{SHA1}\t{MD5}\t{CRC32}";
        }

        public List<Result> Results { get; set; } = new List<Result>();


        public static List<SMDBEntry> Renumber(List<SMDBEntry> entries)
        {
            int i = 0;
            foreach (var k in entries)
            {
                k.Index = i;
                i++;
            }
            return entries;
        }

        public static List<SMDBEntry> ParseSMDB(string[] lines)
        {
            List<SMDBEntry> entries = new List<SMDBEntry>();
            int index = 0;
            foreach (string line in lines)
            {
                Match match = Regex.Match(line, @"([a-fA-F0-9]+)\t([^\t]+)\t([a-fA-F0-9]+)\t([a-fA-F0-9]+)\t([a-fA-F0-9]+)");
                if (match.Success)
                {
                    SMDBEntry entry = new SMDBEntry
                    {
                        Index = index,
                        SHA256 = match.Groups[1].Value.ToLower(),
                        Path = match.Groups[2].Value,
                        SHA1 = match.Groups[3].Value.ToLower(),
                        MD5 = match.Groups[4].Value.ToLower(),
                        CRC32 = match.Groups[5].Value.ToLower()
                    };
                    index++;
                    entries.Add(entry);
                }
            }
            return entries;
        }
    }
}
