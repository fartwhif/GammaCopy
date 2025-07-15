using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GammaCopy
{
    /// <summary>
    /// Archive Formats
    /// </summary>
    public enum FileFormat
    {
        // Default invalid format value
        Undefined = 0,
        /// <summary>
        /// Open 7-zip archive format.
        /// </summary>  
        /// <remarks><a href="http://en.wikipedia.org/wiki/7-zip">Wikipedia information</a></remarks> 
        SevenZip,
        /// <summary>
        /// Proprietary Arj archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/ARJ">Wikipedia information</a></remarks>
        Arj,
        /// <summary>
        /// Open Bzip2 archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Bzip2">Wikipedia information</a></remarks>
        BZip2,
        /// <summary>
        /// Microsoft cabinet archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Cabinet_(file_format)">Wikipedia information</a></remarks>
        Cab,
        /// <summary>
        /// Microsoft Compiled HTML Help file format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Microsoft_Compiled_HTML_Help">Wikipedia information</a></remarks>
        Chm,
        /// <summary>
        /// Microsoft Compound file format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Compound_File_Binary_Format">Wikipedia information</a></remarks>
        Compound,
        /// <summary>
        /// Open Cpio archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Cpio">Wikipedia information</a></remarks>
        Cpio,
        /// <summary>
        /// Open Debian software package format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Deb_(file_format)">Wikipedia information</a></remarks>
        Deb,
        /// <summary>
        /// Open Gzip archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Gzip">Wikipedia information</a></remarks>
        GZip,
        /// <summary>
        /// Open ISO disk image format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/ISO_image">Wikipedia information</a></remarks>
        Iso,
        /// <summary>
        /// Open Lzh archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Lzh">Wikipedia information</a></remarks>
        Lzh,
        /// <summary>
        /// Open core 7-zip Lzma raw archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Lzma">Wikipedia information</a></remarks>
        Lzma,
        /// <summary>
        /// Nullsoft installation package format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/NSIS">Wikipedia information</a></remarks>
        Nsis,
        /// <summary>
        /// RarLab Rar archive format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/RAR_(file_format)">Wikipedia information</a></remarks>
        Rar,
        /// <summary>
        /// RarLab Rar archive format, version 5.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/RAR_(file_format)">Wikipedia information</a></remarks>
        Rar5,
        /// <summary>
        /// Open Rpm software package format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/RPM_Package_Manager">Wikipedia information</a></remarks>
        Rpm,
        /// <summary>
        /// Open split file format.
        /// </summary>
        /// <remarks><a href="?">Wikipedia information</a></remarks>
        Split,
        /// <summary>
        /// Open Tar archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Tar_(file_format)">Wikipedia information</a></remarks>
        Tar,
        /// <summary>
        /// Microsoft Windows Imaging disk image format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Windows_Imaging_Format">Wikipedia information</a></remarks>
        Wim,
        /// <summary>
        /// Open LZW archive format; implemented in "compress" program; also known as "Z" archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Compress">Wikipedia information</a></remarks>
        Lzw,
        /// <summary>
        /// Open Zip archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/ZIP_(file_format)">Wikipedia information</a></remarks>
        Zip,
        /// <summary>
        /// Open Udf disk image format.
        /// </summary>
        Udf,
        /// <summary>
        /// Xar open source archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Xar_(archiver)">Wikipedia information</a></remarks>
        Xar,
        /// <summary>
        /// Mub
        /// </summary>
        Mub,
        /// <summary>
        /// Macintosh Disk Image on CD.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/HFS_Plus">Wikipedia information</a></remarks>
        Hfs,
        /// <summary>
        /// Apple Mac OS X Disk Copy Disk Image format.
        /// </summary>
        Dmg,
        /// <summary>
        /// Open Xz archive format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Xz">Wikipedia information</a></remarks>        
        XZ,
        /// <summary>
        /// MSLZ archive format.
        /// </summary>
        Mslz,
        /// <summary>
        /// Flash video format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Flv">Wikipedia information</a></remarks>
        Flv,
        /// <summary>
        /// Shockwave Flash format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Swf">Wikipedia information</a></remarks>         
        Swf,
        /// <summary>
        /// Windows PE executable format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Portable_Executable">Wikipedia information</a></remarks>
        PE,
        /// <summary>
        /// Linux executable Elf format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Executable_and_Linkable_Format">Wikipedia information</a></remarks>
        Elf,
        /// <summary>
        /// Windows Installer Database.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/Windows_Installer">Wikipedia information</a></remarks>
        Msi,
        /// <summary>
        /// Microsoft virtual hard disk file format.
        /// </summary>
        /// <remarks><a href="http://en.wikipedia.org/wiki/VHD_%28file_format%29">Wikipedia information</a></remarks>
        Vhd,
        /// <summary>
        /// SquashFS file system format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/SquashFS">Wikipedia information</a></remarks>
        SquashFS,
        /// <summary>
        /// Lzma86 file format.
        /// </summary>
        Lzma86,
        /// <summary>
        /// Prediction by Partial Matching by Dmitry algorithm.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Prediction_by_partial_matching">Wikipedia information</a></remarks>
        Ppmd,
        /// <summary>
        /// TE format.
        /// </summary>
        TE,
        /// <summary>
        /// UEFIc format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Unified_Extensible_Firmware_Interface">Wikipedia information</a></remarks>
        UEFIc,
        /// <summary>
        /// UEFIs format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Unified_Extensible_Firmware_Interface">Wikipedia information</a></remarks>
        UEFIs,
        /// <summary>
        /// Compressed ROM file system format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Cramfs">Wikipedia information</a></remarks>
        CramFS,
        /// <summary>
        /// APM format.
        /// </summary>
        APM,
        /// <summary>
        /// Swfc format.
        /// </summary>
        Swfc,
        /// <summary>
        /// NTFS file system format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/NTFS">Wikipedia information</a></remarks>
        Ntfs,
        /// <summary>
        /// FAT file system format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/File_Allocation_Table">Wikipedia information</a></remarks>
        Fat,
        /// <summary>
        /// MBR format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Master_boot_record">Wikipedia information</a></remarks>
        Mbr,
        /// <summary>
        /// Mach-O file format.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Mach-O">Wikipedia information</a></remarks>
        MachO,
        /// <summary>
        /// Very popular in the early days of BBSes, one of the first to offer compression and archiving in a single program. Largely replaced by PKZIP.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/ARC_(file_format)">Wikipedia information</a></remarks>
        Arc,
        /// <summary>
        /// An alternate LZMA algorithm implementation, with support for checksums and ident bytes.
        /// </summary>
        /// <remarks><a href="https://en.wikipedia.org/wiki/Lzip">Wikipedia information</a></remarks>
        lzip,
    }
    /// <summary>
    /// adapted from https://github.com/adoconnection/SevenZipExtractor
    /// </summary>
    public static class FileFormatHelpers
    {
        public static FileFormat? GetSupportedFileFormat(this FileFormat? format)
        {
            if (format == null)
            {
                return null;
            }

            switch (format)
            {
                //SharpCompress:
                case FileFormat.SevenZip: return FileFormat.SevenZip;
                case FileFormat.Arc: return FileFormat.Arc;
                case FileFormat.Zip: return FileFormat.Zip;
                case FileFormat.GZip: return FileFormat.GZip;
                case FileFormat.BZip2: return FileFormat.BZip2;
                case FileFormat.Tar: return FileFormat.Tar;
                case FileFormat.Rar: return FileFormat.Rar;
                case FileFormat.Rar5: return FileFormat.Rar5;
                case FileFormat.lzip: return FileFormat.lzip;
                case FileFormat.XZ: return FileFormat.XZ;

                //DiscUtils:

                case FileFormat.Iso: return FileFormat.Iso;

                    //case FileFormat.APM: return null;
                    //case FileFormat.Arj: return FileFormat.Arj;

                    //case FileFormat.Cab: return FileFormat.Cab;
                    //case FileFormat.Chm: return FileFormat.Chm;
                    //case FileFormat.Compound: return FileFormat.Compound;
                    //case FileFormat.Cpio: return FileFormat.Cpio;
                    //case FileFormat.CramFS: return null;
                    //case FileFormat.Deb: return FileFormat.Deb;
                    //case FileFormat.Dmg: return FileFormat.Dmg;
                    //case FileFormat.Elf: return FileFormat.Elf;
                    //case FileFormat.Fat: return null;
                    //case FileFormat.Flv: return FileFormat.Flv;

                    //case FileFormat.Hfs: return FileFormat.Hfs;
                    //case FileFormat.Iso: return FileFormat.Iso;
                    //case FileFormat.Lzh: return FileFormat.Lzh;
                    //case FileFormat.Lzma: return FileFormat.Lzma;
                    //case FileFormat.Lzma86: return FileFormat.Lzma;
                    //case FileFormat.Lzw: return FileFormat.Lzw;
                    //case FileFormat.MachO: return null;
                    //case FileFormat.Mbr: return null;
                    //case FileFormat.Mslz: return FileFormat.Mslz;
                    //case FileFormat.Mub: return FileFormat.Mub;
                    //case FileFormat.Nsis: return FileFormat.Nsis;
                    //case FileFormat.Ntfs: return null;
                    //case FileFormat.PE: return FileFormat.PE;
                    //case FileFormat.Ppmd: return null;

                    //case FileFormat.Rpm: return FileFormat.Rpm;
          
                    //case FileFormat.Split: return FileFormat.Split;
                    //case FileFormat.SquashFS: return null;
                    //case FileFormat.Swf: return FileFormat.Swf;
                    //case FileFormat.Swfc: return FileFormat.Swf;

                    //case FileFormat.TE: return null;
                    //case FileFormat.Udf: return FileFormat.Udf;
                    //case FileFormat.UEFIc: return null;
                    //case FileFormat.UEFIs: return null;
                    //case FileFormat.Undefined: return null;
                    //case FileFormat.Vhd: return FileFormat.Vhd;
                    //case FileFormat.Wim: return FileFormat.Wim;
                    //case FileFormat.Xar: return FileFormat.Xar;


            }
            return null;
        }

        public static FileFormat? FindFileFormat(this string fileExtension, Stream data, FileFormat? fallback = null)
        {
            data.Seek(0, SeekOrigin.Begin);
            FileFormat szf = new FileFormat();
            if (GuessFormatFromSignature(data, out szf))
            {
                return szf;
            }
            return fallback;
        }

        public static FileFormat? FindFileFormat(this string fileExtension, Stream data)
        {
            fileExtension = fileExtension.TrimStart('.').ToLower();
            switch (fileExtension)
            {
                // https://github.com/ikk00/p7z-usr
                // https://en.wikipedia.org/wiki/List_of_archive_formats

                case "lz": return FindFileFormat(fileExtension, data, FileFormat.Lzma);

                case "arc": return FileFormat.Arc;

                case "rar": return FindFileFormat(fileExtension, data, FileFormat.Rar);
                case "r00": return FindFileFormat(fileExtension, data, FileFormat.Rar);

                case "tar": return FindFileFormat(fileExtension, data, FileFormat.GZip);//tar and bzip2 have sigs

                case "msi": return FileFormat.Compound;
                //case "msi": return SevenZipFormat.Msi;

                case "msp": return FileFormat.Compound;
                //case "msp": return SevenZipFormat.Msi;

                case "img": return FindFileFormat(fileExtension, data, FileFormat.Udf);//iso has sig, others are fat,ntfs


                case "apm": return FileFormat.APM;
                case "arj": return FileFormat.Arj;
                case "bz2": return FileFormat.BZip2;
                case "bzip2": return FileFormat.BZip2;
                case "tbz2": return FileFormat.BZip2;
                case "tbz": return FileFormat.BZip2;

                case "cab": return FileFormat.Cab;
                case "chm": return FileFormat.Chm;
                case "chi": return FileFormat.Chm;
                case "chq": return FileFormat.Chm;
                case "chw": return FileFormat.Chm;
                case "ppj": return FileFormat.Compound;


                case "doc": return FileFormat.Compound;
                case "xls": return FileFormat.Compound;
                case "ppt": return FileFormat.Compound;
                case "cpio": return FileFormat.Cpio;
                case "cramfs": return FileFormat.CramFS;
                case "deb": return FileFormat.Deb;
                case "udeb": return FileFormat.Deb;
                case "dmg": return FileFormat.Dmg;
                case "elf": return FileFormat.Elf;
                case "fat": return FileFormat.Fat;

                case "flv": return FileFormat.Flv;
                case "gz": return FileFormat.GZip;
                case "gzip": return FileFormat.GZip;
                case "tgz": return FileFormat.GZip;
                case "tpz": return FileFormat.GZip;

                case "hfs": return FileFormat.Hfs;
                case "hfsx": return FileFormat.Hfs;
                case "iso": return FindFileFormat(fileExtension, data, FileFormat.Iso);

                case "lzh": return FileFormat.Lzh;
                case "lza": return FileFormat.Lzh;
                case "lzma": return FileFormat.Lzma;
                case "lzma86": return FileFormat.Lzma86;
                case "z": return FileFormat.Lzw;
                case "lzw": return FileFormat.Lzw;
                case "o": return FileFormat.MachO;
                case "dylib": return FileFormat.MachO;
                case "bundle": return FileFormat.MachO;
                case "macho": return FileFormat.MachO;
                case "mbr": return FileFormat.Mbr;


                case "mslz": return FileFormat.Mslz;
                case "mub": return FileFormat.Mub;
                case "nsis": return FileFormat.Nsis;
                case "ntfs": return FileFormat.Ntfs;

                case "exe": return FileFormat.PE;
                case "dll": return FileFormat.PE;
                case "sys": return FileFormat.PE;
                case "ppmd": return FileFormat.Ppmd;

                case "rar5": return FileFormat.Rar5;


                case "rpm": return FileFormat.Rpm;
                case "7z": return FileFormat.SevenZip;
                //case "001": return SevenZipFormat.Split;
                case "squashfs": return FileFormat.SquashFS;
                case "swf": return FileFormat.Swf;
                case "swfc": return FileFormat.Swfc;

                case "ova": return FileFormat.Tar;
                case "te": return FileFormat.TE;
                case "udf": return FileFormat.Udf;

                case "scap": return FileFormat.UEFIc;
                case "uefis": return FileFormat.UEFIs;
                case "vhd": return FileFormat.Vhd;
                case "wim": return FileFormat.Wim;
                case "swm": return FileFormat.Wim;
                case "esd": return FileFormat.Wim;
                case "xar": return FileFormat.Xar;
                case "pkg": return FileFormat.Xar;
                case "xz": return FileFormat.XZ;
                case "txz": return FileFormat.XZ;
                case "zip": return FileFormat.Zip;
                case "zipx": return FileFormat.Zip;
                case "jar": return FileFormat.Zip;
                case "xpi": return FileFormat.Zip;
                case "odt": return FileFormat.Zip;
                case "ods": return FileFormat.Zip;
                case "docx": return FileFormat.Zip;
                case "xlsx": return FileFormat.Zip;
                case "epub": return FileFormat.Zip;

            }
            return null;
        }

        internal class FileSignature
        {
            public byte[] Magic { get; set; }
            public int Offset { get; set; } = 0;
        }

        internal static Dictionary<FileFormat, FileSignature> FileSignatures = new Dictionary<FileFormat, FileSignature>
        {

            {FileFormat.lzip, new FileSignature(){Magic = new byte[] { 0x4C, 0x5A, 0x49, 0x50 }}},
            {FileFormat.Rar5, new FileSignature(){Magic = new byte[] {0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00 }}},
            {FileFormat.Rar, new FileSignature(){Magic = new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00 }}},
            {FileFormat.Vhd, new FileSignature(){Magic = new byte[] { 0x63, 0x6F, 0x6E, 0x65, 0x63, 0x74, 0x69, 0x78 }}},
            {FileFormat.Deb, new FileSignature(){Magic = new byte[] { 0x21, 0x3C, 0x61, 0x72, 0x63, 0x68, 0x3E }}},
            {FileFormat.Dmg, new FileSignature(){Magic = new byte[] { 0x78, 0x01, 0x73, 0x0D, 0x62, 0x62, 0x60 }}},
            {FileFormat.SevenZip, new FileSignature(){Magic = new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }}},
            {FileFormat.Tar, new FileSignature(){Offset = 257, Magic = new byte[] { 0x75, 0x73, 0x74, 0x61, 0x72 }}}, // https://www.gnu.org/software/tar/manual/html_node/Standard.html
            {FileFormat.Iso, new FileSignature(){Magic = new byte[] { 0x43, 0x44, 0x30, 0x30, 0x31 }}},
            {FileFormat.Cab, new FileSignature(){Magic = new byte[] { 0x4D, 0x53, 0x43, 0x46 }}},
            {FileFormat.Rpm, new FileSignature(){Magic = new byte[] { 0xed, 0xab, 0xee, 0xdb }}},
            {FileFormat.Xar, new FileSignature(){Magic = new byte[] { 0x78, 0x61, 0x72, 0x21 }}},
            {FileFormat.Chm, new FileSignature(){Magic = new byte[] { 0x49, 0x54, 0x53, 0x46 }}},
            {FileFormat.BZip2, new FileSignature(){Magic = new byte[] { 0x42, 0x5A, 0x68 }}},
            {FileFormat.Flv, new FileSignature(){Magic = new byte[] { 0x46, 0x4C, 0x56 }}},
            {FileFormat.Swf, new FileSignature(){Magic = new byte[] { 0x46, 0x57, 0x53 }}},
            {FileFormat.GZip, new FileSignature(){Magic = new byte[] { 0x1f, 0x0b }}},
            {FileFormat.Zip, new FileSignature(){Magic = new byte[] { 0x50, 0x4b }}},
            {FileFormat.Arj, new FileSignature(){Magic = new byte[] { 0x60, 0xEA} }},
            {FileFormat.Lzh, new FileSignature(){Magic = new byte[] { 0x2D, 0x6C, 0x68 }}}
        };

        public static bool GuessFormatFromSignature(Stream stream, out FileFormat format)
        {
            FileSignature longestSig = FileSignatures.Values.OrderByDescending(v => v.Magic.Length + v.Offset).First();
            int longestSignatureLen = longestSig.Magic.Length + longestSig.Offset;

            byte[] archiveFileSignature = new byte[longestSignatureLen];
            int bytesRead = stream.Read(archiveFileSignature, 0, longestSignatureLen);

            stream.Position -= bytesRead; // go back to beginning

            if (bytesRead != longestSignatureLen)
            {
                format = FileFormat.Undefined;
                return false;
            }

            foreach (KeyValuePair<FileFormat, FileSignature> pair in FileSignatures)
            {
                if (archiveFileSignature.Skip(pair.Value.Offset).Take(pair.Value.Magic.Length).SequenceEqual(pair.Value.Magic))
                {
                    format = pair.Key;
                    return true;
                }
            }

            format = FileFormat.Undefined;
            return false;
        }
   
    }

}
