using CommandLine;
using DamienG.Security.Cryptography;
using DiscUtils.Iso9660;
using GammaCopy.Formats;
using Microsoft.Win32.SafeHandles;
using SevenZip;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ZetaLongPaths;
using ZetaLongPaths.Native;
using static GammaCopy.Extensions;
using FileAccess = ZetaLongPaths.Native.FileAccess;
using FileShare = ZetaLongPaths.Native.FileShare;

namespace GammaCopy
{
    internal class Program
    {
        public static string DataPath
        {
            get
            {
                string pth = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GammaCopy");
                if (!Directory.Exists(pth))
                {
                    try
                    {
                        Directory.CreateDirectory(pth);
                    }
                    catch { return null; }
                }
                return pth;
            }
        }
        private static string IndexPath => Path.Combine(DataPath, "index.db");

        private static void Logout(string what = null)
        {
            if (!string.IsNullOrWhiteSpace(what))
            {
                Log(what);
            }
            //Thread.Sleep(100000000);
            Environment.Exit(0);
        }
        private static void Log(string what)
        {
            Console.WriteLine(what);
        }
        private static bool IndexExists => File.Exists(IndexPath);
        private static void StartNewIndex()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            string resourceName = "GammaCopy.index.db";
            byte[] dbTemplate = null;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    dbTemplate = ms.ToArray();
                }
            }
            File.WriteAllBytes(IndexPath, dbTemplate);
        }
        private static SQLiteConnection db = null;
        private static void Main(string[] args)
        {
            try
            {
                OpenIndex();
                Parser.Default.ParseArguments<ParseOptions, IndexOptions, BuildOptions>(args)
                    .WithParsed<ParseOptions>(opts => { Parse(opts); })
                    .WithParsed<IndexOptions>(opts => { Index(opts); })
                    .WithParsed<BuildOptions>(opts => { Build(opts); });
            }
            finally
            {
                CloseIndex();
            }
            Logout();
        }
        private static void Parse(ParseOptions opts)
        {
            Log(opts.ToString());
            Stopwatch sw1 = Stopwatch.StartNew();
            List<SMDBEntry> entries = new List<SMDBEntry>();
            List<Result> allFiles = new List<Result>();
            foreach (string src in opts.Folders)
            {
                Log($"recursively obtaining files within {src}");
                void status(string stus)
                {
                    Log(stus);
                }
                List<Result> fils = GetAllFiles(src, status);
                fils.ForEach(k => k.RootParsePath = src);
                allFiles.AddRange(fils);
            }
            using (ProgressBar progress = new ProgressBar())
            {
                int numerator = 0;
                foreach (Result fil in allFiles)
                {
                    progress.blurb = $"{numerator.ToString().PudLeft(4)} / {allFiles.Count} {fil.Path.Tail(40)}";
                    SMDBEntry entry = new SMDBEntry();
                    OpenFile(fil);
                    using (MD5 md5 = MD5.Create())
                    {
                        entry.MD5 = md5.ComputeHash(fil.FileStream).AsHex();
                    }
                    fil.FileStream.Seek(0, SeekOrigin.Begin);
                    using (SHA1 sha1 = SHA1.Create())
                    {
                        entry.SHA1 = sha1.ComputeHash(fil.FileStream).AsHex();
                    }
                    fil.FileStream.Seek(0, SeekOrigin.Begin);
                    using (SHA256 sha256 = SHA256.Create())
                    {
                        entry.SHA256 = sha256.ComputeHash(fil.FileStream).AsHex();
                    }
                    fil.FileStream.Seek(0, SeekOrigin.Begin);
                    using (Crc32 crc32 = new Crc32())
                    {
                        entry.CRC32 = crc32.ComputeHash(fil.FileStream).AsHex();
                    }
                    fil.FileStream.Dispose();
                    entry.Path = fil.Path.ReplaceFirst(fil.RootParsePath, "").TrimStart('\\');
                    if (opts.PrependLastFolder)
                    {
                        string[] arrParts = fil.RootParsePath.TrimEnd(Path.DirectorySeparatorChar).Split(Path.DirectorySeparatorChar);
                        if (arrParts.Length > 0)
                        {
                            string rightmost = arrParts[arrParts.Length - 1];
                            entry.Path = Path.Combine(rightmost, entry.Path);
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(opts.GlobalPrepend))
                    {
                        entry.Path = Path.Combine(opts.GlobalPrepend, entry.Path);
                    }
                    entry.Path = entry.Path.Replace('\\', '/');
                    entries.Add(entry);
                    numerator++;
                    progress.Report((double)numerator / allFiles.Count);
                }
            }
            Log($"Completed Parsing {allFiles.Count} files in {sw1.Elapsed}");
            ZlpIOHelper.WriteAllText(opts.OutputPath, entries.ToSMDB());
        }

        private static void Build(BuildOptions opts)
        {
            Log(opts.ToString());
            if (string.IsNullOrWhiteSpace(opts.OutputPath) && opts.CoverageExistantFile)
            {
                Logout("Coverage switch requires OutputPath to be set.");
            }
            if (string.IsNullOrWhiteSpace(opts.OutputPath) && opts.Go)
            {
                Logout("Go switch requires OutputPath to be set.");
            }
            if (string.IsNullOrWhiteSpace(opts.OutputPath) && !opts.CoverageExistantStdout)
            {
                Logout("Nothing to do. Build requires either OutputPath and/or SCoverage switch.");
            }
            Dictionary<string, List<SMDBEntry>> allJobs = new Dictionary<string, List<SMDBEntry>>();
            List<SMDBEntry> allJobsCombined = new List<SMDBEntry>();
            List<string> coverageFilePaths = new List<string>();
            Stopwatch sw1 = Stopwatch.StartNew();
            Stopwatch sw4 = Stopwatch.StartNew();

            foreach (string smdb in opts.SMDBs)
            {
                string jobName = Path.GetFileNameWithoutExtension(smdb);
                string RealOutputPath = (opts.Containers) ? Path.Combine(opts.OutputPath, jobName) : opts.OutputPath;
                string coverageFilePath = Path.Combine(RealOutputPath, jobName + ".coverage.txt");
                coverageFilePaths.Add(coverageFilePath);
                List<SMDBEntry> entries = new List<SMDBEntry>();

                DatParser parser = new DatParser();
                parser.fpDatFile = smdb;
                parser.Parse();

                allJobs[smdb] = parser.MergedEntries;
            }

            List<string> ExtraFileChecksDone = new List<string>();
            List<string> EmptyFolderChecksDone = new List<string>();
            //Log($"Read {allJobsCombined.Count} database entries in {sw1.Elapsed}");
            foreach (string smdb in opts.SMDBs)
            {
                Log($"Using: {smdb}");
                string jobName = Path.GetFileNameWithoutExtension(smdb);
                string RealOutputPath = (opts.Containers) ? Path.Combine(opts.OutputPath, jobName) : opts.OutputPath;
                string coverageFilePath = string.IsNullOrWhiteSpace(opts.CoverageFolder) ? Path.Combine(RealOutputPath, jobName + ".coverage.txt") : Path.Combine(opts.CoverageFolder, jobName + ".coverage.txt");
                List<SMDBEntry> allEntries = allJobs[smdb];
                Stopwatch sw3 = Stopwatch.StartNew();
                sw1 = Stopwatch.StartNew();
                sw1.Restart();
                List<SMDBEntry> missingEntries = getOutputPending(RealOutputPath, allEntries);
                Log($"{missingEntries.Count} missing files at output location");

                if (opts.DeleteExtraFiles && !ExtraFileChecksDone.Contains(RealOutputPath))
                {
                    int enigmasDeleted = DeleteExtraFiles(RealOutputPath, (opts.Containers) ? allEntries : allJobsCombined, coverageFilePaths);
                    if (enigmasDeleted > 0)
                    {
                        Log($"{enigmasDeleted} extra files deleted from {RealOutputPath}");
                    }
                    ExtraFileChecksDone.Add(RealOutputPath);
                }
                if (opts.DeleteEmptyFolders && !EmptyFolderChecksDone.Contains(RealOutputPath))
                {
                    int extraFoldersDeleted = DeleteExtraFolders(RealOutputPath);
                    if (extraFoldersDeleted > 0)
                    {
                        Log($"{extraFoldersDeleted} extra folders deleted from {RealOutputPath}");
                    }
                    EmptyFolderChecksDone.Add(RealOutputPath);
                }

                if (opts.CoverageExistantFile || opts.Go)
                {
                    if (!EnsureOutputFolder(RealOutputPath))
                    {
                        Log($"Unable to create directory: {RealOutputPath}");
                        return;
                    }
                }

                if (opts.CoverageExistantFile || opts.CoverageExistantStdout)
                {
                    Coverage h = GetCoverageReport(allEntries, missingEntries, Coverage.Type.Existant);
                    if (opts.CoverageExistantFile)
                    {
                        WriteCoverageReportFile(h, coverageFilePath);
                    }
                    if (opts.CoverageExistantStdout)
                    {
                        Log(h.FullCoverageDetail(opts.StdoutCoverageFull));
                    }
                }

                if (opts.CoverageMetadataFile || opts.CoverageMetadataStdout)
                {
                    Log($"Searching metadata cache.");
                    sw1.Restart();
                    GetMetadataCacheDataFor(allEntries);
                    Log($"Metadata search took {sw1.Elapsed}");

                    Coverage j = GetCoverageReport(allEntries, allEntries, Coverage.Type.Metadata);
                    if (opts.CoverageMetadataFile)
                    {
                        WriteCoverageReportFile(j, coverageFilePath);
                    }
                    if (opts.CoverageMetadataStdout)
                    {
                        Log(j.FullCoverageDetail(opts.StdoutCoverageFull));
                    }

                    if (opts.Go || opts.CoverageHybridFile || opts.CoverageHybridStdout)
                    {
                        missingEntries.ForEach(k =>
                        {
                            SMDBEntry h = allEntries.FirstOrDefault(g => g.Index == k.Index);
                            k.Results = h.Results;
                        });
                    }
                }
                else if (opts.Go || opts.CoverageHybridFile || opts.CoverageHybridStdout)
                {
                    Log($"Searching metadata cache.");
                    sw1.Restart();
                    GetMetadataCacheDataFor(missingEntries);
                    Log($"Metadata search took {sw1.Elapsed}");
                }

                if (opts.CoverageHybridFile || opts.CoverageHybridStdout)
                {
                    Coverage h = GetCoverageReport(allEntries, missingEntries, Coverage.Type.Hybrid);
                    if (opts.CoverageHybridFile)
                    {
                        WriteCoverageReportFile(h, coverageFilePath);
                    }
                    if (opts.CoverageHybridStdout)
                    {
                        Log(h.FullCoverageDetail(opts.StdoutCoverageFull));
                    }
                }

                if (opts.Go)
                {
                    if (missingEntries.Count < 1)
                    {
                        Log($"Nothing to do. Output location already contains the completed build.");
                        continue;
                    }
                    Log($"Building...");
                    sw1.Restart();
                    int filesWrote = BuildOutput(RealOutputPath, missingEntries, opts);
                    int p = missingEntries.Count - filesWrote;
                    string remain = (p > 0) ? $" {p} files missing." : "";
                    Log($"{jobName} completed in {sw1.Elapsed}.  Wrote {filesWrote} files.{remain}");
                }
            }
            Log($"Entire Build took {sw4.Elapsed}");
        }

        private static void WriteCoverageReportFile(Coverage coverage, string filePath)
        {
            ZlpIOHelper.WriteAllText(filePath, coverage.FullCoverageDetail(true));
            Log($"Wrote coverage report to: {filePath}");
        }

        private static int DeleteExtraFolders(string realOutputPath)
        {
            List<ZlpDirectoryInfo> dirs = GetAllFolders(realOutputPath);
            int n = 0;
            for (int i = 0; i < dirs.Count; i++)
            {
                n += DeleteEmptyFolderAndEmptyParents(dirs[i], realOutputPath);
            }
            return n;
        }
        private static int DeleteEmptyFolderAndEmptyParents(ZlpDirectoryInfo dir, string stopDir)
        {
            int n = 0;
            if (dir.SafeExists() && dir.IsEmpty)
            {
                dir.SafeDelete();
                n++;
                if (dir.Parent != null)
                {
                    string g = dir.Parent.OriginalPath;
                    if (g.Contains(stopDir) && !g.EndsWith(stopDir))
                    {
                        n += DeleteEmptyFolderAndEmptyParents(dir.Parent, stopDir);
                    }
                }
            }
            return n;
        }

        private static int DeleteExtraFiles(string outputPath, List<SMDBEntry> allEntries, List<string> coverageFilePaths)
        {
            int deleted = 0;
            Log("Searching for extra files at output location.");
            using (ProgressBar progress = new ProgressBar())
            {
                try
                {
                    List<Result> allfilesExistingAlready = GetAllFiles(outputPath);
                    int numerator = 0;
                    int numResults = allfilesExistingAlready.Count;
                    foreach (Result entry in allfilesExistingAlready)
                    {
                        if (coverageFilePaths.Contains(entry.Path))
                        {
                            continue;
                        }
                        progress.blurb = $"{numerator.ToString().PudLeft(4)} / {numResults} {entry.Path.Tail(40)}";
                        progress.Report((double)numerator / numResults);
                        numerator++;
                        if (!entry.Path.StartsWith(outputPath))
                        {
                            return deleted;
                        }
                        ZlpFileInfo destFil = new ZlpFileInfo(entry.Path);
                        string smdbPath = entry.Path.Replace(outputPath, "").TrimStart('\\').Replace('\\', '/');
                        SMDBEntry smdbEntry = allEntries.FirstOrDefault(k => k.Path == smdbPath && k.MD5 == destFil.MD5Hash);
                        if (smdbEntry == null)
                        {
                            try
                            {
                                destFil.SafeDelete();
                                deleted++;
                            }
                            catch { }
                        }
                    }
                }
                catch { }
            }
            return deleted;
        }
        private static readonly bool UseOverlappedAsyncIO = false;
        private static List<SMDBEntry> getOutputPending(string outputPath, List<SMDBEntry> entries)
        {
            Stopwatch sw1 = Stopwatch.StartNew();
            BlockingCollection<SMDBEntry> pending = new BlockingCollection<SMDBEntry>();
            Log($"Determining pending files.");
            int entryCount = entries.Count;
            using (ProgressBar progress = new ProgressBar())
            {
                try
                {
                    int numerator = 0;
                    int numResults = entries.Count();
                    Parallel.ForEach(entries, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (entry) =>
                    {
                        TaskCompletionSource<object> tsc = new TaskCompletionSource<object>();
                        Task z = new Task(new Action(() =>
                        {
                            try
                            {
                                progress.blurb = $"{numerator.ToString().PudLeft(4)} / {numResults} {entry.Path.Tail(40)}";
                                progress.Report((double)numerator / numResults);
                                numerator++;
                                string entryDir = Path.Combine(outputPath, ZlpPathHelper.GetDirectoryPathNameFromFilePath(entry.Path.Replace('/', '\\')));
                                string entryPath = Path.Combine(outputPath, entry.Path.Replace('/', '\\'));
                                ZlpFileInfo destFil = new ZlpFileInfo(entryPath);
                                if (destFil.SafeExists())
                                {
                                    string md5 = "";
                                    if (destFil.Length < 1024 * 1024 * 100)
                                    {
                                        SafeFileHandle safeFileHandle = ZlpIOHelper.CreateFileHandle(entryPath, CreationDisposition.OpenExisting, FileAccess.GenericRead, FileShare.None, UseOverlappedAsyncIO);
                                        using (FileStream stream = new FileStream(safeFileHandle, System.IO.FileAccess.Read, 4096, UseOverlappedAsyncIO))
                                        {
                                            byte[] b = new byte[stream.Length];
                                            stream.Read(b, 0, (int)stream.Length);
                                            using (MD5 _md5 = MD5.Create())
                                            {
                                                byte[] hash = _md5.ComputeHash(b);
                                                md5 = hash.AsHex();
                                            }
                                            b = null;
                                        }
                                    }
                                    else { md5 = destFil.MD5Hash; }
                                    if (md5 == entry.MD5)
                                    {
                                        tsc.SetResult(new object());
                                        return;
                                    }
                                    else
                                    {
                                        pending.Add(entry);
                                    }
                                }
                                else
                                {
                                    pending.Add(entry);
                                }
                            }
                            catch (Exception ex) { pending.Add(entry); }
                            tsc.SetResult(new object());
                        }), TaskCreationOptions.AttachedToParent);

                        z.Start();
                        await tsc.Task;
                    });
                }
                catch { }
            }
            double entriesPerSecond = entryCount / sw1.Elapsed.TotalSeconds;
            //Log($"Entries per second: {entriesPerSecond.ToString("F2")}");
            return pending.ToList();
        }

        private static List<SMDBEntry> SortSources(List<SMDBEntry> sources)
        {
            List<SMDBEntry> plainFiles = sources.Where(k => k.Results.First().ParentId == 0).ToList();
            List<SMDBEntry> extracters = sources.Except(plainFiles).ToList();
            extracters = extracters.OrderBy(k => k.Results.First().ParentId).ToList();

            List<SMDBEntry> r = new List<SMDBEntry>();
            r.AddRange(plainFiles);
            r.AddRange(extracters);
            return r;
        }

        private static int BuildOutput(string outputPath, List<SMDBEntry> entries, BuildOptions opts)
        {
            entries = entries.Where(k => k.Results.Any()).ToList();
            entries = SortSources(entries);

            int wrote = 0;
            //long memLimit = (long)opts.MemoryLimit * 1024 * 1024 * 1024;
            using (ProgressBar progress = new ProgressBar())
            {
                try
                {
                    int numerator = 0;
                    for (int entryIndex = 0; entryIndex < entries.Count; entryIndex++)
                    {
                        //foreach (SMDBEntry entry in entries)
                        //{
                        SMDBEntry entry = entries[entryIndex];
                        progress.blurb = $"{numerator.ToString().PudLeft(4)} / {entries.Count} {entry.Path.Tail(40)}";
                        progress.Report((double)numerator / entries.Count);
                        numerator++;
                        string entryDir = Path.Combine(outputPath, ZlpPathHelper.GetDirectoryPathNameFromFilePath(entry.Path));
                        string entryPath = Path.Combine(outputPath, entry.Path);
                        ZlpDirectoryInfo d = new ZlpDirectoryInfo(entryDir);
                        d.SafeCheckCreate();
                        ZlpFileInfo destFil = new ZlpFileInfo(entryPath.Replace(@"/", "\\"));

                        bool good = false;
                        foreach (Result srcFil in entry.Results.Where(k => k.ParentId == 0))
                        {
                            ZlpFileInfo zSrc = new ZlpFileInfo(srcFil.Path);
                            if (zSrc.SafeExists())
                            {
                                try
                                {
                                    zSrc.SafeCopy(destFil, true);
                                    destFil.CreationTime = srcFil.Created;
                                    destFil.LastWriteTime = srcFil.Modified;
                                    good = true;
                                    wrote++;
                                    break;
                                }
                                catch
                                {
                                }
                            }
                        }
                        if (!good)
                        {
                            foreach (Result leaf in entry.Results.Where(k => k.ParentId != 0))
                            {
                                Result prime = GetPrime(leaf);
                                if (prime != null)
                                {
                                    try
                                    {
                                        List<long> route = GetRoute(leaf);
                                        List<Result> streamHolders = new List<Result>();
                                        Traverse(prime, route, ref streamHolders);
                                        Result _leaf = GetLeaf(prime, route);
                                        if (_leaf.FileStream.Length > 0)
                                        {
                                            MemoryStream ms = (MemoryStream)_leaf.FileStream;
                                            destFil.WriteAllBytes(ms.ToArray());
                                            destFil.LastWriteTime = _leaf.Modified;
                                            destFil.CreationTime = _leaf.Created;
                                        }
                                        else
                                        {
                                            destFil.WriteAllBytes(new byte[] { });
                                            destFil.LastWriteTime = _leaf.Modified;
                                            destFil.CreationTime = _leaf.Created;
                                        }
                                        good = true;
                                        wrote++;
                                        DisposeStream(streamHolders.Last());
                                        bool shouldKeepParent = entries.Skip(entryIndex + 1).SelectMany(k => k.Results).Any(k => k.ParentId == _leaf.ParentId);
                                        if (streamHolders.Count > 1 && !shouldKeepParent)
                                        {
                                            DisposeStream(streamHolders.Skip(1).First());
                                        }
                                        //streamHolders.Reverse();
                                        //streamHolders.ForEach(sh => DisposeStream(sh));
                                        //if (streamHolders.Count > 0)
                                        //{
                                        //    streamHolders.Take(streamHolders.Count - 1).ToList().ForEach(sh => DisposeStream(sh));
                                        //}
                                        //else
                                        //{
                                        //    streamHolders.ForEach(sh => DisposeStream(sh));
                                        //}
                                        //DisposeStream(streamHolders.First());

                                        //bool shouldKeepParent = entries.Skip(entryIndex).SelectMany(k => k.Results).Any(k => k.ParentId == _leaf.ParentId);
                                        //bool shouldKeepLeaf = entries.Skip(entryIndex).SelectMany(k => k.Results).Any(k => k.Id == _leaf.Id);
                                        //long memUsage = Process.GetCurrentProcess().WorkingSet64;
                                        //bool overMem = memUsage > memLimit;
                                        //Backtrack(_leaf, 9999);
                                        //if (overMem)
                                        //{
                                        //    Backtrack(_leaf, 9999);
                                        //}
                                        //if (!shouldKeepLeaf)
                                        //{
                                        //    Backtrack(_leaf, 1);
                                        //}
                                        //if (!shouldKeepParent)
                                        //{
                                        //    Backtrack(_leaf, 2);
                                        //}

                                        break;
                                    }
                                    catch { }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Exception(ex);
                    return wrote;
                }
            }
            return wrote;
        }
        private static void Exception(Exception ex)
        {
            Log(ex.ToString());
        }
        private static Result GetLeaf(Result prime, List<long> route)
        {
            if (route.Count == 1)
            {
                return prime;
            }
            route = route.Skip(1).ToList();
            return GetLeaf(prime.Children.FirstOrDefault(k => k.Id == route.First()), route);
        }
        private static void OpenFile(Result res)
        {
            if (res.FileStream == null || !res.FileStream.CanRead)
            {
                SafeFileHandle safeFileHandle = ZlpIOHelper.CreateFileHandle(res.Path, CreationDisposition.OpenExisting, FileAccess.GenericRead, FileShare.None, UseOverlappedAsyncIO);
                FileStream stream = new FileStream(safeFileHandle, System.IO.FileAccess.Read, 65536, UseOverlappedAsyncIO);
                res.FileStream = stream;
            }
        }
        private static List<long> GetRoute(Result segment)
        {
            List<long> reversed = GetReversedRoute(segment);
            reversed.Reverse();
            return reversed;
        }
        private static List<long> GetReversedRoute(Result segment)
        {
            List<long> route = new List<long>
            {
                segment.Id
            };
            if (segment.ParentId == 0)
            {
                return route;
            }
            List<long> prime = GetReversedRoute(segment.Parent);
            route.AddRange(prime);
            return route;
        }
        private static void Traverse(Result segment, List<long> route, ref List<Result> streamHolders)
        {
            if (segment.ParentId == 0)
            {
                OpenFile(segment);
                streamHolders.Add(segment);
            }
            if (route.Count == 1)
            {
                return; // reached the end
            }
            route = route.Skip(1).ToList();
            Result child = segment.Children.First(k => k.Id == route.First());
            if (child.FileStream != null && child.FileStream.CanRead)
            {
                Traverse(child, route, ref streamHolders);
            }
            else
            {
                if (EstablishFileStream(segment, child))
                {
                    streamHolders.Add(child);
                }

                Traverse(child, route, ref streamHolders);
            }
        }
        private static void Spread(List<Result> nodes)
        {
            foreach (Result node in nodes)
            {
                if (node.FileStream != null)
                {
                    node.FileStream.Dispose();
                    node.FileStream = null;
                }
                if (node.Archive != null)
                {
                    node.Archive.Dispose();
                    node.Archive = null;
                }
                if (node.Cd != null)
                {
                    node.Cd.Dispose();
                    node.Cd = null;
                }
                if (node.Children != null && node.Children.Count > 0)
                {
                    Spread(node.Children);
                }
            }
        }
        private static void DisposeStream(Result node)
        {
            if (node.FileStream != null)
            {
                node.FileStream.Dispose();
                node.FileStream = null;
            }
            if (node.Archive != null)
            {
                node.Archive.Dispose();
                node.Archive = null;
            }
            if (node.Cd != null)
            {
                node.Cd.Dispose();
                node.Cd = null;
            }
        }
        private static void Backtrack(Result node, int depth)
        {
            if (depth == 0)
            {
                return;
            }
            DisposeStream(node);
            //if (node.Children != null && node.Children.Count > 0)
            //{
            //    Spread(node.Children);
            //}
            if (node.Parent != null)
            {
                depth--;
                Backtrack(node.Parent, depth);
            }
        }
        private static bool EstablishFileStream(Result segment, Result child)
        {
            segment.FileStream.Seek(0, SeekOrigin.Begin);
            string ext = Path.GetExtension(segment.Path).ToLower().Trim();
            if (ext == ".iso")
            {
                if (segment.Cd == null)
                {
                    segment.Cd = new CDReader(segment.FileStream, true);
                }
                child.FileStream = segment.Cd.OpenFile(child.Path, FileMode.Open);
                return true;
            }
            else
            {
                //check if archive
                InArchiveFormat? sevenZipFormat = ext.FindSevenZipFormat(segment.FileStream).ConvertSevenZipExtractorToSevenZipSharpFormat();
                if (sevenZipFormat != null)
                {


                    //if (segment.Archive2 == null)
                    //{
                    //    segment.Archive2 = ArchiveFactory.Open(segment.FileStream, new ReaderOptions() { LeaveStreamOpen = true, LookForHeader = true });
                    //}
                    if (segment.Archive == null)
                    {
                        segment.Archive = new SevenZipExtractor(segment.FileStream, sevenZipFormat.Value);
                    }
                    //var t = segment.Archive2.Entries.ToList();
                    //var childEntry = t[(int)child.ArchiveIndex];
                    //child.FileStream = childEntry.OpenEntryStream();
                    child.FileStream = new MemoryStream();
                    if (segment.Archive.IsSolid)
                    {
                        segment.Archive.ExtractFileSolid((int)child.ArchiveIndex, child.FileStream);
                    }
                    else
                    {
                        segment.Archive.ExtractFile((int)child.ArchiveIndex, child.FileStream);
                    }

                    return true;
                }
                else
                {
                }
            }
            return false;
        }
        private static List<Result> GetAllChecksums(Result result)
        {
            string ext = Path.GetExtension(result.Path).ToLower().Trim();
            if (ext == ".iso")
            {
                try
                {
                    CDReader cd = new CDReader(result.FileStream, true);
                    List<string> cdfilePaths = cd.GetAllCDFilePaths("\\");
                    foreach (string cdfile in cdfilePaths)
                    {
                        Result res = new Result
                        {
                            Path = cdfile
                        };
                        using (MD5 md5 = MD5.Create())
                        {
                            using (Stream fileStream = cd.OpenFile(cdfile, FileMode.Open))
                            {
                                byte[] hash = md5.ComputeHash(fileStream);
                                res.Md5 = hash.AsHex();
                                res.Md5Split = hash.Md5Split();
                                res.FileStream = fileStream;
                                res.Length = fileStream.Length;
                                res.Modified = cd.GetLastWriteTime(cdfile);
                                res.Created = cd.GetCreationTime(cdfile);
                                GetAllChecksums(res);
                                result.Files.Add(res);
                                res.FileStream.Dispose();
                                res.FileStream = null;
                            }
                        }
                    }
                }
                catch { }
            }
            else
            {
                InArchiveFormat? sevenZipFormat = ext.FindSevenZipFormat(result.FileStream).ConvertSevenZipExtractorToSevenZipSharpFormat();
                if (sevenZipFormat != null)
                {
                    result.FileStream.Seek(0, SeekOrigin.Begin);
                    try
                    {
                        using (SevenZipExtractor arc = new SevenZipExtractor(result.FileStream, sevenZipFormat.Value))
                        {
                            arc.EventSynchronization = EventSynchronizationStrategy.AlwaysAsynchronous;
                            OutStreamWrapper osw = null;
                            MemoryStream ms = null;
                            Func<uint, OutStreamWrapper> getStream = (index) =>
                             {
                                 ms = new MemoryStream();
                                 osw = new OutStreamWrapper(ms, false);
                                 osw.BytesWritten += (source, e) =>
                                 {
                                     //bw.Write(e.Value);
                                 };
                                 return osw;
                             };
                            Action<FileInfoEventArgs> fileExtractStart = new Action<FileInfoEventArgs>((args) =>
                            {

                            });
                            Action<FileInfoEventArgs> fileExtractComplete = new Action<FileInfoEventArgs>((args) =>
                            {
                                if (ms != null && !args.FileInfo.IsDirectory/* && args.FileInfo.FileName != "[no name]"*/)
                                {
                                    Result res = new Result()
                                    {
                                        ArchiveIndex = args.FileInfo.Index,
                                        FileStream = ms,
                                        Path = args.FileInfo.FileName,
                                        Created = args.FileInfo.CreationTime,
                                        Modified = args.FileInfo.LastWriteTime,
                                        Length = (long)args.FileInfo.Size
                                    };
                                    using (MD5 md5 = MD5.Create())
                                    {
                                        res.FileStream.Seek(0, SeekOrigin.Begin);
                                        byte[] hash = md5.ComputeHash(res.FileStream);
                                        res.Md5 = hash.AsHex();
                                        res.Md5Split = hash.Md5Split();
                                        GetAllChecksums(res);
                                        //Global.IncrementCounterAndDisplay();
                                        res.FileStream.Dispose();
                                        res.FileStream = null;
                                        result.Files.Add(res);
                                    }
                                }
                            });
                            arc.ExtractFiles(null, getStream, fileExtractStart, fileExtractComplete);
                        }
                    }
                    catch
                    {
                    }
                }
            }
            return result.Files;
        }
        internal static Result GetPrime(Result child)
        {
            if (!Global.ResultsCurrentlyOpen.ContainsKey(child.Id))
            {
                Global.ResultsCurrentlyOpen[child.Id] = child;
            }
            if (child.ParentId == 0)
            {
                return child;
            }
            if (child.Parent != null)
            {
                return GetPrime(child.Parent);
            }
            if (Global.ResultsCurrentlyOpen.ContainsKey(child.ParentId))
            {
                child.Parent = Global.ResultsCurrentlyOpen[child.ParentId];
                if (!child.Parent.Children.Any(k => k.Id == child.Id))
                {
                    child.Parent.Children.Add(child);
                }
                return GetPrime(child.Parent);
            }
            Result parent = GetIndexResult(child.ParentId);
            if (parent == null)
            {
                return null; //broken lineage
            }
            parent.Children.Add(child);
            child.Parent = parent;
            Global.ResultsCurrentlyOpen.Add(parent.Id, parent);
            if (parent.ParentId == 0)
            {
                return parent;
            }
            return GetPrime(parent);
        }
        internal static bool EnsureOutputFolder(string folder)
        {
            try
            {
                if (!ZlpIOHelper.DirectoryExists(folder))
                {
                    ZlpIOHelper.CreateDirectory(folder);
                    if (!ZlpIOHelper.DirectoryExists(folder))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message);
                return false;
            }
        }

        internal static void GetMetadataCacheDataFor(List<SMDBEntry> missingEntries)
        {
            List<Result> dbFiles = GetAllResults().OrderBy(k => k.ParentId).ToList();
            using (ProgressBar progress = new ProgressBar())
            {
                int numerator = 0;
                Parallel.ForEach(missingEntries, (entry) =>
                {
                    progress.blurb = entry.Path.Tail(40);
                    Tuple<long, long> md5split = entry.MD5.Md5Split();
                    List<Result> found = dbFiles.Where(k => k.Md5Split.Equals2(md5split)).ToList();
                    found = found.OrderBy(k => k.ParentId).ToList();
                    entry.Results.AddRange(found);
                    if (Interlocked.Increment(ref numerator) % 100 == 0)
                    {
                        progress.Report((double)numerator / missingEntries.Count);
                    }
                });
            }
        }

        internal static Coverage GetCoverageReport(List<SMDBEntry> allEntries, List<SMDBEntry> missingEntries, Coverage.Type coverageType)
        {
            Coverage coverage = new Coverage() { CoverageType = coverageType };
            allEntries.ForEach(entry =>
            {
                SMDBEntry q = missingEntries.FirstOrDefault(k => k.Index == entry.Index);
                if (q == null)
                {
                    coverage.Found++;
                }
                else if ((coverage.CoverageType == Coverage.Type.Hybrid || coverage.CoverageType == Coverage.Type.Metadata) && q.Results.Any())
                {
                    coverage.Found++;
                }
                else
                {
                    coverage.Missing++;
                    coverage.MissingEntries.Add(entry);
                }
            });
            return coverage;
        }
        internal static void CloseIndex()
        {
            if (db != null)
            {
                try { db.Close(); }
                catch { }
            }
        }
        internal static void OpenIndex()
        {
            if (!IndexExists)
            {
                StartNewIndex();
                Log("Created new metadata cache: " + IndexPath);
            }
            else
            {
                Log("Using metadata cache: " + IndexPath);
            }
            if (db == null)
            {
                db = new SQLiteConnection("Data Source='" + IndexPath + "';Version=3;");
                db.Open();
            }
        }
        internal static List<Result> GetParentlessResults()
        {
            List<Result> results = new List<Result>();
            try
            {
                using (SQLiteCommand command = new SQLiteCommand(db)
                {
                    CommandText = "select * from result where parent = 0",
                    CommandType = CommandType.Text
                })
                {
                    SQLiteDataReader reader = command.ExecuteReader();
                    results.AddRange(ReadResults(reader));
                }
            }
            catch (Exception ex) { Log(ex.ToString()); }
            return results;
        }
        internal static List<Result> GetAllResults()
        {
            List<Result> results = new List<Result>();
            try
            {
                using (SQLiteCommand command = new SQLiteCommand(db)
                {
                    CommandText = "select * from result",
                    CommandType = CommandType.Text
                })
                {
                    SQLiteDataReader reader = command.ExecuteReader();
                    results.AddRange(ReadResults(reader));
                }
            }
            catch (Exception ex) { Log(ex.ToString()); }
            return results;
        }
        internal static List<Result> ReadResults(SQLiteDataReader reader)
        {
            List<Result> results = new List<Result>();
            while (reader.Read())
            {
                Result v = new Result
                {
                    Id = reader.SafeGetLong("id"),
                    ParentId = reader.SafeGetLong("parent"),
                    Path = reader.SafeGetString("path"),
                    PathMd5Split = new Tuple<long, long>(reader.SafeGetLong("pathmd5_0"), reader.SafeGetLong("pathmd5_1")),
                    Md5 = reader.SafeGetString("md5"),
                    Md5Split = new Tuple<long, long>(reader.SafeGetLong("md5_0"), reader.SafeGetLong("md5_1")),
                    Length = reader.SafeGetLong("length"),
                    ArchiveIndex = reader.SafeGetLong("archive_index"),
                    Created = ((double)reader.SafeGetInt("created")).ToDateTime(),
                    Modified = ((double)reader.SafeGetInt("lastwrite")).ToDateTime()
                };
                results.Add(v);
            }
            return results;
        }
        internal static List<Result> GetResultsByMD5(Tuple<long, long> Md5)
        {
            return GetResults(() => "select * from result where md5_0 = @param1 and md5_1 = @param2", (cmd) =>
            {
                cmd.Parameters.Add(new SQLiteParameter("@param1", Md5.Item1));
                cmd.Parameters.Add(new SQLiteParameter("@param2", Md5.Item2));
            });
        }
        internal static Result GetIndexResult(string path)
        {
            return GetResult(() => "select * from result where path = @param1", (cmd) => cmd.Parameters.Add(new SQLiteParameter("@param1", path)));
        }
        internal static Result GetIndexResult(long id)
        {
            return GetResult(() => "select * from result where id = @param1", (cmd) => cmd.Parameters.Add(new SQLiteParameter("@param1", id)));
        }

        internal static List<Result> GetResults(Func<string> statement, Action<SQLiteCommand> prepare = null)
        {
            try
            {
                using (SQLiteCommand command = new SQLiteCommand(db)
                {
                    CommandText = statement(),
                    CommandType = CommandType.Text
                })
                {
                    prepare?.Invoke(command);
                    SQLiteDataReader reader = command.ExecuteReader();
                    List<Result> results = ReadResults(reader);
                    return results;
                }
            }
            catch (Exception ex) { Log(ex.ToString()); }
            return null;
        }
        internal static Result GetResult(Func<string> statement, Action<SQLiteCommand> prepare = null)
        {
            List<Result> results = GetResults(statement, prepare);
            return results?.FirstOrDefault();
        }

        internal static long DeleteResult(long indexId)
        {
            try
            {
                using (SQLiteCommand command = new SQLiteCommand(db)
                {
                    CommandText = "DELETE FROM result WHERE id = @param1",
                    CommandType = CommandType.Text
                })
                {
                    command.Parameters.Add(new SQLiteParameter("@param1", indexId));
                    return command.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { Log(ex.ToString()); }
            return 0;
        }
        internal static Result UpdateResult(Result res)
        {
            if (res.Id < 1)
            {
                throw new InvalidOperationException("can't update an entity with a null primary key");
            }
            try
            {
                using (SQLiteCommand command = new SQLiteCommand(db)
                {
                    CommandText = "UPDATE result set parent = @param1, path = @param2, md5 = @param3, length = @param4, lastwrite = @param5, created = @param6, archive_index = @param8, md5_0 = @param9, md5_1 = @param10 WHERE id = @param7",
                    CommandType = CommandType.Text
                })
                {
                    command.Parameters.Add(new SQLiteParameter("@param1", res.ParentId));
                    command.Parameters.Add(new SQLiteParameter("@param2", res.Path));
                    command.Parameters.Add(new SQLiteParameter("@param3", res.Md5));
                    command.Parameters.Add(new SQLiteParameter("@param4", res.Length));
                    command.Parameters.Add(new SQLiteParameter("@param5", (int)res.Modified.ToTimestamp()));
                    command.Parameters.Add(new SQLiteParameter("@param6", (int)res.Created.ToTimestamp()));
                    command.Parameters.Add(new SQLiteParameter("@param7", res.Id));
                    command.Parameters.Add(new SQLiteParameter("@param8", res.ArchiveIndex));
                    command.Parameters.Add(new SQLiteParameter("@param9", res.Md5Split.Item1));
                    command.Parameters.Add(new SQLiteParameter("@param10", res.Md5Split.Item2));
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex) { Log(ex.ToString()); }
            return null;
        }
        internal static long GetLastInsertedId()
        {
            using (SQLiteCommand cmd = db.CreateCommand())
            {
                cmd.CommandText = @"SELECT last_insert_rowid()";
                cmd.ExecuteNonQuery();
                long lastID = Convert.ToInt64(cmd.ExecuteScalar());
                return lastID;
            }
        }

        private static IndexOptions currentIndexOptions = null;

        internal static void Index(IndexOptions opts)
        {
            currentIndexOptions = opts;
            Log(opts.ToString());
            Stopwatch sw2 = Stopwatch.StartNew();
            Stopwatch sw1 = Stopwatch.StartNew();
            List<Result> dbFiles = GetParentlessResults();
            Log($"Finding orphaned metadata cache entries, metadata cache size: {dbFiles.Count}.");
            long i = 0;
            List<Result> orphans = new List<Result>();
            foreach (Result res in dbFiles)
            {
                if (!ZlpIOHelper.FileExists(res.Path))
                {
                    orphans.Add(res);
                }
            }
            if (orphans.Count > 0)
            {
                using (Deleter deleter = new Deleter(db))
                {
                    orphans.ForEach(k => deleter.Delete(k));
                }
                Log($"Deleted {orphans.Count} orphaned metadata cache entries for files that no longer exist.");
            }
            Log($"metadata cache pruning took {sw1.Elapsed}.");
            List<Result> freshMeta = new List<Result>();
            foreach (string src in opts.SourcePaths)
            {
                sw1.Restart();
                Log("Refreshing metadata cache for: " + src);
                List<Result> fils = GetAllFiles(src);
                Log($"Found {fils.Count} files for {src} in {sw1.Elapsed}"); sw1.Restart();
                BlockingCollection<Result> filesToRefresh = new BlockingCollection<Result>();

                fils.ForEach(k => { k.PathMd5Split = k.Path.AsMd5().Md5Split(); });
                dbFiles = GetParentlessResults();
                ConcurrentDictionary<LongPair, Result> dbLookup = new ConcurrentDictionary<LongPair, Result>();
                Parallel.ForEach(dbFiles, dbFil =>
                {
                    dbLookup[new LongPair(dbFil.PathMd5Split)] = dbFil;
                });
                Parallel.ForEach(fils, file =>
                 {
                     dbLookup.TryGetValue(new LongPair(file.PathMd5Split), out Result res);
                     if (res == null || res.Length != file.Length || (int)res.Created.ToTimestamp() != (int)file.Created.ToTimestamp() || (int)res.Modified.ToTimestamp() != (int)file.Modified.ToTimestamp())
                     {
                         filesToRefresh.Add(res ?? file);
                     }
                 });

                Log($"Lookup completed in {sw1.Elapsed}, {filesToRefresh.Count} stale or unindexed files for {src}"); sw1.Restart();

                List<Result> staleDbData = filesToRefresh.Where(k => k.Id > 0).ToList();

                if (staleDbData.Count > 0)
                {
                    using (Deleter deleter = new Deleter(db))
                    {
                        staleDbData.ForEach(k => deleter.Delete(k));
                    }
                    Log($"Deleted {i} stale metadata cache entries for files that changed.");
                }

                List<Result> processedFiles = null;
                if (opts.Sequential)
                {
                    processedFiles = ProcessAllFilesSequential(filesToRefresh.ToList());
                }
                else
                {
                    processedFiles = ProcessAllFiles(filesToRefresh.ToList(), opts);
                }

                Log($"Gathered metadata for {src} in {sw1.Elapsed}"); sw1.Restart();
                freshMeta.AddRange(processedFiles);
            }
            if (freshMeta.Count > 0)
            {
                Log($"Saving metadata...");
                sw1.Restart();
                int indexFilesSaved = 0;
                using (ProgressBar progress = new ProgressBar())
                {
                    using (Inserter inserter = new Inserter(db))
                    {
                        indexFilesSaved = SaveResults(freshMeta, inserter, progress);
                    }
                }
                Log($"Saved {indexFilesSaved} entries in {sw1.Elapsed}");
            }
            Console.WriteLine($"Indexing took {sw2.Elapsed}");
        }
        internal static int SaveResults(List<Result> files, Inserter inserter, ProgressBar progress = null)
        {
            int i = 0;
            Parallel.ForEach(files, (file) =>
            {
                if (file.Id > 0)
                {
                    long r = DeleteResult(file.Id);
                }
            });
            for (int a = 0; a < files.Count; a++)
            {
                Result file = files[a];
                if (progress != null)
                {
                    progress.blurb = $"{a.ToString().PudLeft(4)} / {files.Count}";
                }
                if (file.PathMd5Split == null)
                {
                    file.PathMd5Split = file.Path.AsMd5().Md5Split();
                }

                inserter.Insert(file);
                i++;
                file.Files.ForEach(k => { k.ParentId = file.Id; });
                i += SaveResults(file.Files, inserter);
                progress?.Report((double)a / files.Count);
            }
            return i;
        }
        internal static List<Result> ProcessAllFiles(List<Result> files, IndexOptions opts)
        {
            if (files.Any(k => k == null))
            {
                throw new Exception("null");
            }

            long numThreads = 0;
            long index = 0;
            AutoResetEvent threadDone = new AutoResetEvent(false);

            using (ProgressBar progress = new ProgressBar())
            {
                double numerator = 0;
                for (; index < files.Count;)
                {
                    while (Interlocked.Increment(ref numThreads) < Environment.ProcessorCount)
                    {
                        if (index >= files.Count)
                        {
                            break;
                        }

                        Result file = files[(int)index];
                        index++;
                        numerator++;
                        (new Thread(() =>
                        {
                            using (MD5 md5 = MD5.Create())
                            {
                                progress.blurb = $"{numerator.ToString().PudLeft(4)} / {files.Count} {file.Path.Tail(40)}";

                                try
                                {
                                    SafeFileHandle safeFileHandle = ZlpIOHelper.CreateFileHandle(file.Path, CreationDisposition.OpenExisting, FileAccess.GenericRead, FileShare.None, UseOverlappedAsyncIO);
                                    using (FileStream stream = new FileStream(safeFileHandle, System.IO.FileAccess.Read, 65536, UseOverlappedAsyncIO))
                                    {
                                        byte[] hash = md5.ComputeHash(stream);
                                        file.Md5 = hash.AsHex();
                                        file.Md5Split = hash.Md5Split();
                                        file.FileStream = stream;
                                        if (currentIndexOptions == null || !currentIndexOptions.DisableArchiveTraversal)
                                        {
                                            GetAllChecksums(file);
                                        }

                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                                finally
                                {
                                    if (file.FileStream != null)
                                    {
                                        try
                                        {
                                            file.FileStream.Dispose();
                                            file.FileStream = null;
                                        }
                                        catch (Exception ex) { }
                                    }
                                    progress.Report(numerator / files.Count);
                                }
                            }
                            Interlocked.Decrement(ref numThreads);
                            threadDone.Set();
                        })
                        { Name = $"Summing of file {index - 1}" }).Start();
                    }
                    Interlocked.Decrement(ref numThreads);
                    threadDone.WaitOne();
                }
            }
            while (numThreads > 0)
            {
                Thread.Sleep(100);
            }



            //ThreadPool.SetMaxThreads(16, 32);
            //Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = 16 },
            //    file =>
            //    {

            //    }
            //);

            //files.AsParallel().ForAll(k =>
            //{
            //    using (MD5 md5 = MD5.Create())
            //    {
            //        SafeFileHandle safeFileHandle = ZlpIOHelper.CreateFileHandle(k.Path, CreationDisposition.OpenExisting, FileAccess.GenericRead, FileShare.None, UseOverlappedAsyncIO);
            //        using (FileStream stream = new FileStream(safeFileHandle, System.IO.FileAccess.Read, 65536, UseOverlappedAsyncIO))
            //        {
            //            k.Md5 = md5.ComputeHash(stream).AsHex();
            //            k.FileStream = stream;
            //            GetAllChecksums(k);
            //        }
            //    }
            //});
            return files;
        }
        internal static List<Result> ProcessAllFilesSequential(List<Result> files)
        {
            if (files.Any(k => k == null))
            {
                throw new Exception("null");
            }
            using (ProgressBar progress = new ProgressBar())
            {
                double numerator = 0;

                files.ForEach(k =>
                {
                    numerator++;
                    using (MD5 md5 = MD5.Create())
                    {
                        progress.blurb = $"{numerator.ToString().PudLeft(4)} / {files.Count} {k.Path.Tail(40)}";

                        SafeFileHandle safeFileHandle = ZlpIOHelper.CreateFileHandle(k.Path, CreationDisposition.OpenExisting, FileAccess.GenericRead, FileShare.None, UseOverlappedAsyncIO);
                        using (FileStream stream = new FileStream(safeFileHandle, System.IO.FileAccess.Read, 65536, UseOverlappedAsyncIO))
                        {
                            byte[] hash = md5.ComputeHash(stream);
                            k.Md5 = hash.AsHex();
                            k.Md5Split = hash.Md5Split();
                            k.FileStream = stream;
                            if (currentIndexOptions == null || !currentIndexOptions.DisableArchiveTraversal)
                            {
                                GetAllChecksums(k);
                            }
                            progress.Report(numerator / files.Count);
                            k.FileStream.Dispose();
                            k.FileStream = null;
                        }
                    }
                });
            }
            return files;
        }
        internal static List<Result> GetAllFiles(string path, Action<string> status = null)
        {
            BlockingCollection<Result> results = new BlockingCollection<Result>();
            ZlpDirectoryInfo di = ZlpDirectoryInfo.FromString(path);
            ZlpFileInfo[] files = di.GetFiles(SearchOption.AllDirectories);
            foreach (ZlpFileInfo file in files)
            {
                Result res = new Result
                {
                    Path = file.FullName,
                    Created = file.CreationTime,
                    Modified = file.LastWriteTime,
                    Length = file.Length
                };
                results.Add(res);
                status?.Invoke(res.Path);
            }
            return results.ToList();
        }
        internal static List<ZlpDirectoryInfo> GetAllFolders(string path, Action<string> status = null)
        {
            BlockingCollection<Result> results = new BlockingCollection<Result>();
            ZlpDirectoryInfo di = ZlpDirectoryInfo.FromString(path);
            ZlpDirectoryInfo[] dirs = di.GetDirectories(SearchOption.AllDirectories);
            return dirs.ToList();
        }
        internal static List<Result> GetAllFiles2(string path, Action<string> status = null)
        {
            BlockingCollection<Result> results = new BlockingCollection<Result>();
            ZlpDirectoryInfo di = ZlpDirectoryInfo.FromString(path);
            ZlpFileInfo[] files = di.GetFiles(SearchOption.TopDirectoryOnly);//Directory.EnumerateFiles(path);
            foreach (ZlpFileInfo file in files)
            {
                Result res = new Result
                {
                    Path = file.FullName,
                    Created = file.CreationTime,
                    Modified = file.LastWriteTime,
                    Length = file.Length
                };
                results.Add(res);
                status?.Invoke(res.Path);
            }
            ZlpDirectoryInfo[] folders = di.GetDirectories(SearchOption.TopDirectoryOnly);
            folders.AsParallel().ForAll(k =>
            {
                List<Result> innerResults = GetAllFiles2(k.FullName, status);
                innerResults.ForEach(r => results.Add(r));
            });
            return results.ToList();
        }

    }
}
