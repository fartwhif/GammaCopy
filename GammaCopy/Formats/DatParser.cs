using DiscUtils.Iso9660;
using Microsoft.Win32.SafeHandles;
using SharpCompress.Archives;
using SharpCompress.Common.Arc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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
            NoIntro,
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
            List<KeyValuePair<string, byte[]>> filsBytes = new List<KeyValuePair<string, byte[]>>();

            SafeFileHandle safeFileHandle = ZlpIOHelper.CreateFileHandle(fpDatFile, CreationDisposition.OpenExisting, FileAccess.GenericRead, FileShare.None, UseOverlappedAsyncIO);
            using (FileStream stream = new FileStream(safeFileHandle, System.IO.FileAccess.Read, 65536, UseOverlappedAsyncIO))
            {
                string ext = Path.GetExtension(fpDatFile).ToLower().Trim();
                var archiveFormat = ext.FindFileFormat(stream).GetSupportedFileFormat();

                if (archiveFormat == null)
                {
                    Console.WriteLine($"{Path.GetFileName(fpDatFile)} is a plain file");
                    filsBytes.Add(GetPlainFile(fpDatFile, stream));
                    stream.Seek(0, SeekOrigin.Begin);
                }
                else
                {
                    Console.WriteLine($"{Path.GetFileName(fpDatFile)} type is {archiveFormat}");
                    using (ProgressBar progress = new ProgressBar())
                    {
                        int numerator = 0;
                        ArchiveReader.Read(fpDatFile, stream, (archive) =>
                        {
                            numerator++;
                            progress.blurb = $"{numerator.ToString("N0").PudLeft(4)} / {archive.ArchiveEntryCount:N0} {archive.ToString().Tail(40)}";
                            progress.Report(archive.ArchiveEntryCount > 0 ? ((double)numerator / archive.ArchiveEntryCount) : 100);
                            try
                            {
                                using (var ms = new MemoryStream())
                                {
                                    archive.Stream.CopyTo(ms);
                                    filsBytes.Add(new KeyValuePair<string, byte[]>(Path.GetFileName(archive.Name), ms.ToArray()));
                                }
                            }
                            catch { }
                        });
                    }
                }
                Parse2(filsBytes);
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
                                if (Probably.ProbablyNoIntro(lines))
                                {
                                    type = DatFileType.NoIntro;
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
                                case DatFileType.NoIntro:
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
                            Path = Path.Combine(game.Name, rom.Name),
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
                using (var archive = ArchiveFactory.Open(stream))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.IsDirectory)
                        {
                            using (var ms = new MemoryStream())
                            {
                                entry.WriteTo(ms);
                                filsBytes.Add(new KeyValuePair<string, byte[]>(Path.GetFileName(entry.Key), ms.ToArray()));
                            }
                        }
                    }
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
