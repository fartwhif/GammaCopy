using DiscUtils.Iso9660;
using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using SharpCompress.Common.SevenZip;
using SharpCompress.Readers;
using System;
using System.Collections.Generic;
using System.IO;

namespace GammaCopy
{
    public class ArchiveEntry
    {
        public string Name { get; set; }
        public Stream Stream { get; set; }
        public DateTime Modified { get; set; }
        public DateTime Created { get; set; }
        public long Size { get; set; }
        public long ArchiveIndex { get; set; }
        public int ArchiveEntryCount { get; set; }
    }
    public class ArchiveInterface
    {
        public Action<ArchiveEntry> EntryHandler { get; set; }

        public string Name { get; set; }
        public ArchiveInterfaceType Type { get; set; }
        public CDReader Cd { get; set; }
        public IReader Reader { get; set; }
        public IArchive Archive { get; set; }
        public IArchiveEntry Entry { get; set; }
        public List<string> CdfilePaths { get; set; }
        public override string ToString()
        {
            return Name ?? "";
        }
        public int ArchiveEntryCount
        {
            get
            {
                switch (Type)
                {
                    case ArchiveInterfaceType.SharpCompressSevenZip:
                        return ((AbstractArchive<SevenZipArchiveEntry, SevenZipVolume>)Archive).Entries.Count;
                    case ArchiveInterfaceType.SharpCompressAuto:
                        //todo: use type cast instead for property access
                        int r = 0;
                        using (var e = Archive.Entries.GetEnumerator())
                        {
                            while (e.MoveNext())
                            {
                                r++;
                            }
                            return r;
                        }
                    case ArchiveInterfaceType.Iso:
                        return CdfilePaths.Count;
                }
                return 0;
            }
        }
    }
    public enum ArchiveInterfaceType
    {
        Unknown,
        Iso,
        SharpCompressAuto,
        SharpCompressSevenZip,
        //SevenZipExtractor
    }
    internal static class ArchiveReader
    {
        public static void Read(string ArcFilePath, Stream data, Action<ArchiveEntry> entryHandler)
        {
            data.Seek(0, SeekOrigin.Begin);
            string ext = Path.GetExtension(ArcFilePath).ToLower().Trim();
            var sevenZipFormat = ext.FindFileFormat(data).GetSupportedFileFormat();
            data.Seek(0, SeekOrigin.Begin);
            if (sevenZipFormat != null)
            {
                switch (sevenZipFormat)
                {
                    case FileFormat.Iso:
                        CDReader cd = new CDReader(data, true);
                        List<string> cdfilePaths = cd.GetAllCDFilePaths("\\");
                        foreach (string cdfile in cdfilePaths)
                        {
                            Broker(new ArchiveInterface()
                            {
                                Cd = cd,
                                Name = cdfile,
                                Type = ArchiveInterfaceType.Iso,
                                CdfilePaths = cdfilePaths
                            });
                        }
                        break;

                    case FileFormat.SevenZip:
                        using (var archive = SevenZipArchive.Open(data))
                        {
                            using (var reader = archive.ExtractAllEntries())
                            {
                                while (reader.MoveToNextEntry())
                                {
                                    if (!reader.Entry.IsDirectory)
                                    {
                                        Broker(new ArchiveInterface()
                                        {
                                            Name = reader.Entry.Key,
                                            Reader = reader,
                                            //SevenZipArchive = archive,
                                            Archive = archive,
                                            Type = ArchiveInterfaceType.SharpCompressSevenZip,
                                        });
                                    }
                                }
                            }
                        }
                        break;

                    //Supported Reader Formats: Arc, Zip, GZip, BZip2, Tar, Rar, LZip, XZ'
                    case FileFormat.Arc:
                    case FileFormat.Zip:
                    case FileFormat.GZip:
                    case FileFormat.BZip2:
                    case FileFormat.Tar:
                    case FileFormat.Rar:
                    case FileFormat.Rar5:
                    case FileFormat.lzip:
                    case FileFormat.XZ:
                        using (var archive = ArchiveFactory.Open(data))
                        {
                            foreach (var entry in archive.Entries)
                            {
                                if (!entry.IsDirectory)
                                {
                                    Broker(new ArchiveInterface()
                                    {
                                        Name = entry.Key,
                                        Archive = archive,
                                        Type = ArchiveInterfaceType.SharpCompressAuto,
                                        Entry = entry,
                                    });
                                }
                            }
                        }
                        break;
                }
            }
        }

        private static void Broker(ArchiveInterface archive)
        {
            switch (archive.Type)
            {
                case ArchiveInterfaceType.Iso:
                    using (Stream fileStream = archive.Cd.OpenFile(archive.Name, FileMode.Open))
                    {
                        archive.EntryHandler(new ArchiveEntry()
                        {
                            Stream = fileStream,
                            Size = fileStream.Length,
                            Name = archive.Name.Replace("/", "\\"),
                            Modified = archive.Cd.GetLastWriteTime(archive.Name),
                            Created = archive.Cd.GetCreationTime(archive.Name),
                            ArchiveEntryCount = archive.ArchiveEntryCount,
                        });
                    }
                    break;
                case ArchiveInterfaceType.SharpCompressSevenZip:
                case ArchiveInterfaceType.SharpCompressAuto:
                    using (var ms = new MemoryStream())
                    {
                        IEntry entry = null;
                        switch (archive.Type)
                        {
                            case ArchiveInterfaceType.SharpCompressSevenZip:
                                archive.Reader.WriteEntryTo(ms);
                                entry = archive.Reader.Entry;
                                break;
                            case ArchiveInterfaceType.SharpCompressAuto:
                                archive.Entry.WriteTo(ms);
                                entry = archive.Entry;
                                break;
                        }
                        var ae = new ArchiveEntry()
                        {
                            Stream = ms,
                            Size = ms.Length,
                            Name = archive.Name.Replace("/", "\\"),
                            Created = entry.CreatedTime ?? DateTime.MinValue,
                            Modified = entry.LastModifiedTime ?? DateTime.MinValue,
                            ArchiveIndex = entry.VolumeIndexFirst,
                            ArchiveEntryCount = archive.ArchiveEntryCount,
                        };
                        if (ae.Created == DateTime.MinValue && ae.Modified != DateTime.MinValue)
                        {
                            ae.Created = ae.Modified;
                        }
                        else if (ae.Created != DateTime.MinValue && ae.Modified == DateTime.MinValue)
                        {
                            ae.Modified = ae.Created;
                        }

                        archive.EntryHandler(ae);
                    }
                    break;
            }
        }

    }
}