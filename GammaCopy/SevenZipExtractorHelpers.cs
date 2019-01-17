using SevenZip;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GammaCopy
{
    /// <summary>
    /// adapted from https://github.com/adoconnection/SevenZipExtractor
    /// </summary>
    public static class SevenZipExtractorHelpers
    {
        public static InArchiveFormat? ConvertSevenZipExtractorToSevenZipSharpFormat(this SevenZipFormat? format)
        {
            if (format == null)
            {
                return null;
            }

            switch (format)
            {
                case SevenZipFormat.APM: return null;
                case SevenZipFormat.Arj: return InArchiveFormat.Arj;
                case SevenZipFormat.BZip2: return InArchiveFormat.BZip2;
                case SevenZipFormat.Cab: return InArchiveFormat.Cab;
                case SevenZipFormat.Chm: return InArchiveFormat.Chm;
                case SevenZipFormat.Compound: return InArchiveFormat.Compound;
                case SevenZipFormat.Cpio: return InArchiveFormat.Cpio;
                case SevenZipFormat.CramFS: return null;
                case SevenZipFormat.Deb: return InArchiveFormat.Deb;
                case SevenZipFormat.Dmg: return InArchiveFormat.Dmg;
                case SevenZipFormat.Elf: return InArchiveFormat.Elf;
                case SevenZipFormat.Fat: return null;
                case SevenZipFormat.Flv: return InArchiveFormat.Flv;
                case SevenZipFormat.GZip: return InArchiveFormat.GZip;
                case SevenZipFormat.Hfs: return InArchiveFormat.Hfs;
                case SevenZipFormat.Iso: return InArchiveFormat.Iso;
                case SevenZipFormat.Lzh: return InArchiveFormat.Lzh;
                case SevenZipFormat.Lzma: return InArchiveFormat.Lzma;
                case SevenZipFormat.Lzma86: return InArchiveFormat.Lzma;
                case SevenZipFormat.Lzw: return InArchiveFormat.Lzw;
                case SevenZipFormat.MachO: return null;
                case SevenZipFormat.Mbr: return null;
                case SevenZipFormat.Mslz: return InArchiveFormat.Mslz;
                case SevenZipFormat.Mub: return InArchiveFormat.Mub;
                case SevenZipFormat.Nsis: return InArchiveFormat.Nsis;
                case SevenZipFormat.Ntfs: return null;
                case SevenZipFormat.PE: return InArchiveFormat.PE;
                case SevenZipFormat.Ppmd: return null;
                case SevenZipFormat.Rar: return InArchiveFormat.Rar4;
                case SevenZipFormat.Rar5: return InArchiveFormat.Rar;
                case SevenZipFormat.Rpm: return InArchiveFormat.Rpm;
                case SevenZipFormat.SevenZip: return InArchiveFormat.SevenZip;
                case SevenZipFormat.Split: return InArchiveFormat.Split;
                case SevenZipFormat.SquashFS: return null;
                case SevenZipFormat.Swf: return InArchiveFormat.Swf;
                case SevenZipFormat.Swfc: return InArchiveFormat.Swf;
                case SevenZipFormat.Tar: return InArchiveFormat.Tar;
                case SevenZipFormat.TE: return null;
                case SevenZipFormat.Udf: return InArchiveFormat.Udf;
                case SevenZipFormat.UEFIc: return null;
                case SevenZipFormat.UEFIs: return null;
                case SevenZipFormat.Undefined: return null;
                case SevenZipFormat.Vhd: return InArchiveFormat.Vhd;
                case SevenZipFormat.Wim: return InArchiveFormat.Wim;
                case SevenZipFormat.Xar: return InArchiveFormat.Xar;
                case SevenZipFormat.XZ: return InArchiveFormat.XZ;
                case SevenZipFormat.Zip: return InArchiveFormat.Zip;
            }
            return null;
        }

        public static SevenZipFormat? FindSevenZipFormat(this string fileExtension, Stream data, SevenZipFormat? fallback = null)
        {
            data.Seek(0, SeekOrigin.Begin);
            SevenZipFormat szf = new SevenZipFormat();
            if (GuessFormatFromSignature(data, out szf))
            {
                return szf;
            }
            return fallback;
        }

        public static SevenZipFormat? FindSevenZipFormat(this string fileExtension, Stream data)
        {
            fileExtension = fileExtension.TrimStart('.').ToLower();
            switch (fileExtension)
            {
                // https://github.com/ikk00/p7z-usr
                // https://en.wikipedia.org/wiki/List_of_archive_formats



                case "rar": return FindSevenZipFormat(fileExtension, data, SevenZipFormat.Rar);
                case "r00": return FindSevenZipFormat(fileExtension, data, SevenZipFormat.Rar);

                case "tar": return FindSevenZipFormat(fileExtension, data, SevenZipFormat.GZip);//tar and bzip2 have sigs

                case "msi": return SevenZipFormat.Compound;
                //case "msi": return SevenZipFormat.Msi;

                case "msp": return SevenZipFormat.Compound;
                //case "msp": return SevenZipFormat.Msi;

                case "img": return FindSevenZipFormat(fileExtension, data, SevenZipFormat.Udf);//iso has sig, others are fat,ntfs


                case "apm": return SevenZipFormat.APM;
                case "arj": return SevenZipFormat.Arj;
                case "bz2": return SevenZipFormat.BZip2;
                case "bzip2": return SevenZipFormat.BZip2;
                case "tbz2": return SevenZipFormat.BZip2;
                case "tbz": return SevenZipFormat.BZip2;

                case "cab": return SevenZipFormat.Cab;
                case "chm": return SevenZipFormat.Chm;
                case "chi": return SevenZipFormat.Chm;
                case "chq": return SevenZipFormat.Chm;
                case "chw": return SevenZipFormat.Chm;
                case "ppj": return SevenZipFormat.Compound;


                case "doc": return SevenZipFormat.Compound;
                case "xls": return SevenZipFormat.Compound;
                case "ppt": return SevenZipFormat.Compound;
                case "cpio": return SevenZipFormat.Cpio;
                case "cramfs": return SevenZipFormat.CramFS;
                case "deb": return SevenZipFormat.Deb;
                case "udeb": return SevenZipFormat.Deb;
                case "dmg": return SevenZipFormat.Dmg;
                case "elf": return SevenZipFormat.Elf;
                case "fat": return SevenZipFormat.Fat;



                case "flv": return SevenZipFormat.Flv;
                case "gz": return SevenZipFormat.GZip;
                case "gzip": return SevenZipFormat.GZip;
                case "tgz": return SevenZipFormat.GZip;
                case "tpz": return SevenZipFormat.GZip;

                case "hfs": return SevenZipFormat.Hfs;
                case "hfsx": return SevenZipFormat.Hfs;
                case "iso": return SevenZipFormat.Iso;

                case "lzh": return SevenZipFormat.Lzh;
                case "lza": return SevenZipFormat.Lzh;
                case "lz": return SevenZipFormat.Lzma;
                case "lzma": return SevenZipFormat.Lzma;
                case "lzma86": return SevenZipFormat.Lzma86;
                case "z": return SevenZipFormat.Lzw;
                case "lzw": return SevenZipFormat.Lzw;
                case "o": return SevenZipFormat.MachO;
                case "dylib": return SevenZipFormat.MachO;
                case "bundle": return SevenZipFormat.MachO;
                case "macho": return SevenZipFormat.MachO;
                case "mbr": return SevenZipFormat.Mbr;


                case "mslz": return SevenZipFormat.Mslz;
                case "mub": return SevenZipFormat.Mub;
                case "nsis": return SevenZipFormat.Nsis;
                case "ntfs": return SevenZipFormat.Ntfs;

                case "exe": return SevenZipFormat.PE;
                case "dll": return SevenZipFormat.PE;
                case "sys": return SevenZipFormat.PE;
                case "ppmd": return SevenZipFormat.Ppmd;

                case "rar5": return SevenZipFormat.Rar5;


                case "rpm": return SevenZipFormat.Rpm;
                case "7z": return SevenZipFormat.SevenZip;
                //case "001": return SevenZipFormat.Split;
                case "squashfs": return SevenZipFormat.SquashFS;
                case "swf": return SevenZipFormat.Swf;
                case "swfc": return SevenZipFormat.Swfc;

                case "ova": return SevenZipFormat.Tar;
                case "te": return SevenZipFormat.TE;
                case "udf": return SevenZipFormat.Udf;

                case "scap": return SevenZipFormat.UEFIc;
                case "uefis": return SevenZipFormat.UEFIs;
                case "vhd": return SevenZipFormat.Vhd;
                case "wim": return SevenZipFormat.Wim;
                case "swm": return SevenZipFormat.Wim;
                case "esd": return SevenZipFormat.Wim;
                case "xar": return SevenZipFormat.Xar;
                case "pkg": return SevenZipFormat.Xar;
                case "xz": return SevenZipFormat.XZ;
                case "txz": return SevenZipFormat.XZ;
                case "zip": return SevenZipFormat.Zip;
                case "zipx": return SevenZipFormat.Zip;
                case "jar": return SevenZipFormat.Zip;
                case "xpi": return SevenZipFormat.Zip;
                case "odt": return SevenZipFormat.Zip;
                case "ods": return SevenZipFormat.Zip;
                case "docx": return SevenZipFormat.Zip;
                case "xlsx": return SevenZipFormat.Zip;
                case "epub": return SevenZipFormat.Zip;

            }
            return null;
        }

        internal class FileSignature
        {
            public byte[] Magic { get; set; }
            public int Offset { get; set; } = 0;
        }

        internal static Dictionary<SevenZipFormat, FileSignature> FileSignatures = new Dictionary<SevenZipFormat, FileSignature>
        {
            {SevenZipFormat.Rar5, new FileSignature(){Magic = new byte[] {0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x01, 0x00 }}},
            {SevenZipFormat.Rar, new FileSignature(){Magic = new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07, 0x00 }}},
            {SevenZipFormat.Vhd, new FileSignature(){Magic = new byte[] { 0x63, 0x6F, 0x6E, 0x65, 0x63, 0x74, 0x69, 0x78 }}},
            {SevenZipFormat.Deb, new FileSignature(){Magic = new byte[] { 0x21, 0x3C, 0x61, 0x72, 0x63, 0x68, 0x3E }}},
            {SevenZipFormat.Dmg, new FileSignature(){Magic = new byte[] { 0x78, 0x01, 0x73, 0x0D, 0x62, 0x62, 0x60 }}},
            {SevenZipFormat.SevenZip, new FileSignature(){Magic = new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }}},
            {SevenZipFormat.Tar, new FileSignature(){Offset = 257, Magic = new byte[] { 0x75, 0x73, 0x74, 0x61, 0x72 }}}, // https://www.gnu.org/software/tar/manual/html_node/Standard.html
            {SevenZipFormat.Iso, new FileSignature(){Magic = new byte[] { 0x43, 0x44, 0x30, 0x30, 0x31 }}},
            {SevenZipFormat.Cab, new FileSignature(){Magic = new byte[] { 0x4D, 0x53, 0x43, 0x46 }}},
            {SevenZipFormat.Rpm, new FileSignature(){Magic = new byte[] { 0xed, 0xab, 0xee, 0xdb }}},
            {SevenZipFormat.Xar, new FileSignature(){Magic = new byte[] { 0x78, 0x61, 0x72, 0x21 }}},
            {SevenZipFormat.Chm, new FileSignature(){Magic = new byte[] { 0x49, 0x54, 0x53, 0x46 }}},
            {SevenZipFormat.BZip2, new FileSignature(){Magic = new byte[] { 0x42, 0x5A, 0x68 }}},
            {SevenZipFormat.Flv, new FileSignature(){Magic = new byte[] { 0x46, 0x4C, 0x56 }}},
            {SevenZipFormat.Swf, new FileSignature(){Magic = new byte[] { 0x46, 0x57, 0x53 }}},
            {SevenZipFormat.GZip, new FileSignature(){Magic = new byte[] { 0x1f, 0x0b }}},
            {SevenZipFormat.Zip, new FileSignature(){Magic = new byte[] { 0x50, 0x4b }}},
            {SevenZipFormat.Arj, new FileSignature(){Magic = new byte[] { 0x60, 0xEA} }},
            {SevenZipFormat.Lzh, new FileSignature(){Magic = new byte[] { 0x2D, 0x6C, 0x68 }}}
        };

        public static bool GuessFormatFromSignature(Stream stream, out SevenZipFormat format)
        {
            FileSignature longestSig = FileSignatures.Values.OrderByDescending(v => v.Magic.Length + v.Offset).First();
            int longestSignatureLen = longestSig.Magic.Length + longestSig.Offset;

            byte[] archiveFileSignature = new byte[longestSignatureLen];
            int bytesRead = stream.Read(archiveFileSignature, 0, longestSignatureLen);

            stream.Position -= bytesRead; // go back o beginning

            if (bytesRead != longestSignatureLen)
            {
                format = SevenZipFormat.Undefined;
                return false;
            }

            foreach (KeyValuePair<SevenZipFormat, FileSignature> pair in FileSignatures)
            {
                if (archiveFileSignature.Skip(pair.Value.Offset).Take(pair.Value.Magic.Length).SequenceEqual(pair.Value.Magic))
                {
                    format = pair.Key;
                    return true;
                }
            }

            format = SevenZipFormat.Undefined;
            return false;
        }
        /// <summary>
        /// https://github.com/adoconnection/SevenZipExtractor
        /// </summary>
        public enum SevenZipFormat
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
            MachO
        }
    }

}
