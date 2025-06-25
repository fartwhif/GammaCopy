using System.Collections.Generic;

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
    }
}
