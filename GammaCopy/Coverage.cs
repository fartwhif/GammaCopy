using GammaCopy.Formats;
using System;
using System.Collections.Generic;
using System.Text;

namespace GammaCopy
{
    internal class Coverage
    {
        public enum Type
        {
            Existant,
            Metadata,
            Hybrid
        }
        public Type CoverageType = Type.Hybrid;
        public string CoverageReport => $@"{CoverageType} coverage total: {Found + Missing}
Found: {Found} - {PercentileFound.ToString("F2")}%
Missing: {Missing} - {PercentileMissing.ToString("F2")}%";
        public string MissingList
        {
            get
            {
                if (MissingEntries == null)
                {
                    return string.Empty;
                }
                StringBuilder missingList = new StringBuilder();
                MissingEntries.ForEach((entry) =>
                {
                    missingList.AppendLine(entry.ToString());
                });
                return missingList.ToString();
            }
        }
        public List<SMDBEntry> MissingEntries { get; set; } = new List<SMDBEntry>();
        public double PercentileFound => ((double)Found / Count) * 100;
        public double PercentileMissing => ((double)Missing / Count) * 100;
        public int Missing { get; set; } = 0;
        public int Found { get; set; } = 0;
        public override string ToString()
        {
            return CoverageReport;
        }
        public int Count => Found + Missing;
        public string FullCoverageDetail(bool includeMissingList)
        {
            if (!includeMissingList)
            {
                return CoverageReport;
            }
            string g = "";
            if (!string.IsNullOrEmpty(MissingList))
            {
                g = $"{Environment.NewLine}Missing list follows:{Environment.NewLine}{MissingList}";
            }
            return $"{CoverageReport}{g}";
        }
    }
}
