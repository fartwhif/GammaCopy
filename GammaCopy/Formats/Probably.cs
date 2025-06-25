using System.Text.RegularExpressions;

namespace GammaCopy.Formats
{
    internal class Probably
    {
        public static bool ProbablyClrmamePro(string[] lines)
        {
            int i = 0;
            foreach (string line in lines)
            {
                if (line.ToUpper().StartsWith("clrmamepro".ToUpper()))
                {
                    if (line.Contains("("))
                    {
                        return true;
                    }
                    if (lines.Length > i + 2)
                    {
                        if (lines[i + 1].Contains("("))
                        {
                            return true;
                        }
                    }
                    return true;
                }
                i++;
                if (i > 4)
                {
                    return false;
                }
            }
            return false;
        }

        public static bool ProbablyLogiqx(string[] lines)
        {
            int i = 0;
            foreach (string line in lines)
            {
                if (line.ToUpper().Contains("http://www.logiqx.com/Dats/datafile.dtd".ToUpper()))
                {
                    return true;
                }
                i++;
                if (i > 4)
                {
                    return false;
                }
            }
            return false;
        }
        public static bool ProbablyXML(string[] lines)
        {
            int i = 0;
            foreach (string line in lines)
            {
                if (line.ToUpper().Contains("<?xml version=".ToUpper()))
                {
                    return true;
                }
                i++;
                if (i > 4)
                {
                    return false;
                }
            }
            return false;
        }
        public static bool ProbablySMDB(string[] lines)
        {
            int i = 0;
            foreach (string line in lines)
            {
                Match match = Regex.Match(line, @"([a-fA-F0-9]+)\t([^\t]+)\t([a-fA-F0-9]+)\t([a-fA-F0-9]+)\t([a-fA-F0-9]+)");
                if (match.Success)
                {
                    return true;
                }
                i++;
                if (i > 10)
                {
                    return false;
                }
            }
            return false;
        }
    }
}
