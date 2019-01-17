using DiscUtils.Iso9660;
//using SharpCompress.Archives;
using System;
using System.Collections.Generic;
using System.IO;

namespace GammaCopy
{
    internal class Result
    {
        //public IArchive Archive2 { get; set; }
        public SevenZip.SevenZipExtractor Archive { get; set; }
        public CDReader Cd { get; set; }
        public Result Parent { get; set; }
        public List<Result> Children { get; set; } = new List<Result>();
        public long Id { get; set; }
        public long ParentId { get; set; }
        public long Length { get; set; }
        public long ArchiveIndex { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public string Path { get; set; }
        public Tuple<long, long> PathMd5Split { get; set; }
        public string RootParsePath { get; set; }
        public string Md5 { get; set; }
        public Tuple<long, long> Md5Split { get; set; }
        public List<Result> Files { get; set; } = new List<Result>();
        public Stream FileStream { get; set; } = null;
        public override string ToString()
        {
            return Path;
        }
    }
}
