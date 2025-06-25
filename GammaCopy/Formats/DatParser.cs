using DiscUtils.Iso9660;
using Microsoft.Win32.SafeHandles;
using SevenZip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using ZetaLongPaths;
using ZetaLongPaths.Native;
using FileAccess = ZetaLongPaths.Native.FileAccess;
using FileShare = ZetaLongPaths.Native.FileShare;

namespace GammaCopy.Formats
{
    internal class DatParser
    {
        public DatParser() { }
        public enum DatFileType
        {
            Unknown,
            SMDB,
            Logiqx,
            ClrmamePro
        }
        public string fpDatFile { get; set; }
        private static readonly bool UseOverlappedAsyncIO = false;
        public Dictionary<string, List<SMDBEntry>> Entries { get; set; }
        public List<SMDBEntry> MergedEntries
        {
            get
            {
                if (Entries == null) { return null; }
                var g = Entries.SelectMany(k => k.Value).ToList();
                int i = 0;
                foreach (var k in g)
                {
                    k.Index = i;
                    i++;
                }
                return g;
            }
        }
        public void Parse()
        {
            SafeFileHandle safeFileHandle = ZlpIOHelper.CreateFileHandle(fpDatFile, CreationDisposition.OpenExisting, FileAccess.GenericRead, FileShare.None, UseOverlappedAsyncIO);
            using (FileStream stream = new FileStream(safeFileHandle, System.IO.FileAccess.Read, 65536, UseOverlappedAsyncIO))
            {
                List<KeyValuePair<string, byte[]>> filsBytes = null;
                string ext = Path.GetExtension(fpDatFile).ToLower().Trim();
                if (ext == ".iso")
                {
                    Console.WriteLine($"{Path.GetFileName(fpDatFile)} type is ISO");
                    filsBytes = GetFilesFromISO(stream);
                    stream.Seek(0, SeekOrigin.Begin);
                }
                if (filsBytes == null)
                {
                    InArchiveFormat? sevenZipFormat = ext.FindSevenZipFormat(stream).ConvertSevenZipExtractorToSevenZipSharpFormat();
                    stream.Seek(0, SeekOrigin.Begin);
                    if (sevenZipFormat != null)
                    {
                        Console.WriteLine($"{Path.GetFileName(fpDatFile)} type is {sevenZipFormat}");
                        filsBytes = GetFilesFromArchive(stream, sevenZipFormat.Value);
                    }
                }
                if (filsBytes == null)
                {
                    Console.WriteLine($"{Path.GetFileName(fpDatFile)} is a plain file");
                    filsBytes = new List<KeyValuePair<string, byte[]>>();
                    filsBytes.Add(GetPlainFile(fpDatFile, stream));
                    stream.Seek(0, SeekOrigin.Begin);
                }
                if (filsBytes != null)
                {
                    Parse2(filsBytes);
                }
            }
        }
        private void Parse2(List<KeyValuePair<string, byte[]>> files)
        {
            Entries = new Dictionary<string, List<SMDBEntry>>();
            foreach (var file in files)
            {
                Entries[file.Key] = new List<SMDBEntry>();
                using (MemoryStream ms = new MemoryStream(file.Value))
                {
                    using (TextReader tr = new StreamReader(ms))
                    {
                        string st = tr.ReadToEnd();
                        string st2 = st.Replace("\r\n", "\n");
                        string[] lines = st2.Split('\n');

                        DatFileType type = DatFileType.Unknown;
                        if (type == DatFileType.Unknown)
                        {
                            if (Probably.ProbablyXML(lines))
                            {
                                if (Probably.ProbablyLogiqx(lines))
                                {
                                    type = DatFileType.Logiqx;
                                }
                            }
                        }
                        if (type == DatFileType.Unknown)
                        {
                            if (Probably.ProbablyClrmamePro(lines))
                            {
                                type = DatFileType.ClrmamePro;
                            }
                        }
                        if (type == DatFileType.Unknown)
                        {
                            if (Probably.ProbablySMDB(lines))
                            {
                                type = DatFileType.SMDB;
                            }
                        }
                        if (type != DatFileType.Unknown)
                        {
                            Console.WriteLine($"{Path.GetFileName(file.Key)} type is {type}");
                            switch (type)
                            {
                                case DatFileType.ClrmamePro:
                                    Entries[file.Key].AddRange(ClrmamePro.ToSMDB(ClrmamePro.Parse(lines)));
                                    break;
                                case DatFileType.SMDB:
                                    Entries[file.Key].AddRange(SMDBEntry.ParseSMDB(lines));
                                    break;
                                case DatFileType.Logiqx:
                                    Entries[file.Key].AddRange(ParseLogiqx(st));
                                    break;
                            }
                            var badEntries = Entries[file.Key].Where(k => string.IsNullOrWhiteSpace(k.MD5)).ToList();
                            if (badEntries.Count > 0)
                            {
                                Console.WriteLine($"WARNING: GammaCopy supports only MD5 data hashes. Discarding {badEntries.Count} Entries missing an MD5 hash.");
                                Entries[file.Key] = Entries[file.Key].Except(badEntries).ToList();
                                SMDBEntry.Renumber(Entries[file.Key]);
                            }
                        }
                    }
                }
            }
        }
        private List<SMDBEntry> ParseLogiqx(string xml)
        {
            List<SMDBEntry> entries = new List<SMDBEntry>();
            XmlSerializer serializer = new XmlSerializer(typeof(Logiqx.Datafile));
            using (StringReader reader = new StringReader(xml))
            {
                var dat = (Logiqx.Datafile)serializer.Deserialize(reader);
                int index = 0;
                foreach (var game in dat.Game)
                {
                    foreach (var rom in game.Rom)
                    {
                        SMDBEntry entry = new SMDBEntry
                        {
                            Index = index,
                            SHA256 = null,
                            Path = rom.Name,
                            SHA1 = rom.Sha1,
                            MD5 = rom.Md5,
                            CRC32 = rom.Crc
                        };
                        index++;
                        entries.Add(entry);
                    }
                }
            }
            return entries;
        }


        private KeyValuePair<string, byte[]> GetPlainFile(string fpDat, FileStream stream)
        {
            KeyValuePair<string, byte[]> filsBytes = new KeyValuePair<string, byte[]>();
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                stream.Flush();
                filsBytes = new KeyValuePair<string, byte[]>(Path.GetFileName(fpDat), ms.ToArray());
            }
            return filsBytes;
        }
        private List<KeyValuePair<string, byte[]>> GetFilesFromArchive(FileStream stream, InArchiveFormat fmt)
        {
            List<KeyValuePair<string, byte[]>> filsBytes = new List<KeyValuePair<string, byte[]>>();
            try
            {
                using (SevenZipExtractor arc = new SevenZipExtractor(stream, fmt))
                {
                    arc.EventSynchronization = EventSynchronizationStrategy.AlwaysAsynchronous;
                    MemoryStream ms = null;
                    arc.ExtractFiles((ExtractFileCallbackArgs efca) =>
                    {
                        if (ms == null)
                        {
                            ms = new MemoryStream();
                        }
                        arc.ExtractFile(efca.ArchiveFileInfo.Index, ms);
                        filsBytes.Add(new KeyValuePair<string, byte[]>(Path.GetFileName(efca.ArchiveFileInfo.FileName), ms.ToArray()));
                        ms.Dispose();
                    }, null, null, null);
                }
            }
            catch
            {
            }
            return filsBytes;
        }
        private List<KeyValuePair<string, byte[]>> GetFilesFromISO(FileStream stream)
        {
            List<KeyValuePair<string, byte[]>> filsBytes = new List<KeyValuePair<string, byte[]>>();
            CDReader cd = new CDReader(stream, true);
            List<string> cdfilePaths = cd.GetAllCDFilePaths("\\");
            foreach (string cdfile in cdfilePaths)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (Stream fileStream = cd.OpenFile(cdfile, FileMode.Open))
                    {
                        fileStream.CopyTo(ms);
                        fileStream.Flush();
                    }
                    filsBytes.Add(new KeyValuePair<string, byte[]>(Path.GetFileName(cdfile), ms.ToArray()));
                }
            }
            return filsBytes;
        }
    }
}
