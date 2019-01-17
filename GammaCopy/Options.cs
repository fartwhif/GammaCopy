using CommandLine;
using System.Collections.Generic;
using System.Linq;
namespace GammaCopy
{
    [Verb("parse", HelpText = "Make an SMDB file")]
    internal class ParseOptions
    {
        [Option('p', "parse-folder", Required = true, HelpText = "Folders to be parsed.")]
        public IEnumerable<string> Folders { get; set; }

        [Option('w', "parsed-output-destination", Required = true, HelpText = "Write parsed output to this file.")]
        public string OutputPath { get; set; }

        [Option('r', "global-prepend", Required = false, HelpText = "prepend to all paths.")]
        public string GlobalPrepend { get; set; }

        [Option('t', "prepend-last-folder", Required = false, HelpText = "prepend the rightmost folder of the current parse folder to paths.")]
        public bool PrependLastFolder { get; set; }

        public override string ToString()
        {
            return $@"Parse Options:
parse-folder(s): {((Folders.Any()) ? Folders.Aggregate((a, b) => a + " | " + b) : "")}
parsed-output-destination: {OutputPath}
global-prepend: {GlobalPrepend}
prepend-last-folder: {PrependLastFolder}";
        }
    }
    [Verb("build", HelpText = "Check coverage or build output based on DB files of various types (currently only supports SMDB).")]
    internal class BuildOptions
    {
        [Option('d', "database", Required = true, HelpText = "DB Files to process.")]
        public IEnumerable<string> SMDBs { get; set; }

        [Option('o', "output", Required = false, HelpText = "Path where the output will go.")]
        public string OutputPath { get; set; }

        [Option('n', "containers", Required = false, HelpText = "Write output to <outputpath>/<databasefilename>/")]
        public bool Containers { get; set; }

        [Option('f', "coverage-hybrid-to-file", Required = false, HelpText = "Gather entries not in output location or metadata cache. Output coverage information to file.")]
        public bool CoverageHybridFile { get; set; }

        [Option('c', "coverage-hybrid-to-stdout", Required = false, HelpText = "Gather entries not in output location or metadata cache. Output coverage summary to console.")]
        public bool CoverageHybridStdout { get; set; }

        [Option('u', "coverage-existant-to-file", Required = false, HelpText = "Gather entries not in output location. Output coverage information to file.")]
        public bool CoverageExistantFile { get; set; }

        [Option('v', "coverage-existant-to-stdout", Required = false, HelpText = "Gather entries not in output location. Output coverage summary to console.")]
        public bool CoverageExistantStdout { get; set; }

        [Option('h', "coverage-meta-to-file", Required = false, HelpText = "Gather entries not in metadata cache. Output coverage information to file.")]
        public bool CoverageMetadataFile { get; set; }

        [Option('b', "coverage-meta-to-stdout", Required = false, HelpText = "Gather entries not in metadata cache. Output coverage summary to console.")]
        public bool CoverageMetadataStdout { get; set; }

        [Option('j', "stdout-coverage-full", Required = false, HelpText = "When outputting coverage to console, DO NOT omit the missing entry list.")]
        public bool StdoutCoverageFull { get; set; }

        [Option('k', "delete-extras", Required = false, HelpText = "Delete extra files found in the output path but not in the DB file.")]
        public bool DeleteExtraFiles { get; set; }

        [Option('l', "delete-empty-folders", Required = false, HelpText = "Delete empty folders found in the output path.")]
        public bool DeleteEmptyFolders { get; set; }

        //[Option('m', "limit-memory", Default = 8, Required = false, HelpText = "Stop caching sources after using this much memory (GB)")]
        //public int MemoryLimit { get; set; }

        [Option('g', "go", Required = false, HelpText = "Go ahead with the build process and output the data to the output folder.")]
        public bool Go { get; set; }


        public override string ToString()
        {
            return $@"Build Options:
go: {Go}
database(s): {((SMDBs.Any()) ? SMDBs.Aggregate((a, b) => a + " | " + b) : "")}
output: {OutputPath}
containers: {Containers}
delete-extras: {DeleteExtraFiles}
delete-empty-folders: {DeleteEmptyFolders}
coverage-hybrid-to-file: {CoverageHybridFile}
coverage-hybrid-to-stdout:  {CoverageHybridStdout}
coverage-existant-to-file:  {CoverageExistantFile}
coverage-existant-to-stdout:  {CoverageExistantStdout}
coverage-meta-to-file:  {CoverageMetadataFile}
coverage-meta-to-stdout:  {CoverageMetadataStdout}
stdout-coverage-full: {StdoutCoverageFull}";
        }

    }

    [Verb("index", HelpText = "Gather metadata for the source locations and save it in the metadata cache.")]
    internal class IndexOptions
    {
        [Option('s', "source", Required = true, HelpText = "Source locations to index or refresh.")]
        public IEnumerable<string> SourcePaths { get; set; }

        [Option('e', "onebyone", Required = false, HelpText = "Process files one by one. (default is in parallel, one per processor).  Use this if you have a mechanical hard drive to reduce wear.  This option may result in lower memory requirements.")]
        public bool Sequential { get; set; }

        [Option('x', "disable-archive-traversal", Required = false, HelpText = "Do not traverse archive files.")]
        public bool DisableArchiveTraversal { get; set; }

        public override string ToString()
        {
            return $@"Index Options:
source(s): {((SourcePaths.Any()) ? SourcePaths.Aggregate((a, b) => a + " | " + b) : "")}
onebyone: {Sequential}
disable-archive-traversal: {DisableArchiveTraversal}";
        }
    }
}
