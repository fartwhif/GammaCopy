using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static GammaCopy.Formats.ClrmamePro;
using static GammaCopy.Formats.Logiqx;

namespace GammaCopy.Formats
{
    internal class ClrmamePro
    {
        public enum keyword
        {
            clrmamepro,
            name,
            description,
            category,
            version,
            author,
            comment,
            game,
            rom,
            size,
            crc,
            md5,
            sha1
        }
        public class Entity
        {
            public keyword Keyword { get; set; }
            public string Data { get; set; }
            public List<Entity> Entities { get; set; } = new List<Entity>();

            public static SMDBEntry ToSMDB(Entity ent) { return ent.ToSMDB(); }
            public SMDBEntry ToSMDB()
            {
                return new SMDBEntry()
                {

                };
            }
        }

        static bool StartsWithKeyword(string line, out keyword word, out bool wrapped, out bool multiline)
        {
            word = keyword.rom;
            wrapped = false;
            multiline = false;

            var names = Enum.GetNames(typeof(keyword));
            foreach (var name in names)
            {
                if (line.Trim().StartsWith(name))
                {
                    word = (keyword)Enum.Parse(typeof(keyword), name);
                    if (line.Substring(name.Length + 1).Trim().StartsWith("("))
                    {
                        wrapped = true;
                        if (line.Trim().EndsWith("("))
                        {
                            multiline = true;
                        }
                    }
                    return true;
                }
            }
            return false;
        }


        public static List<SMDBEntry> ToSMDB(List<Entity> ents)
        {
            List<SMDBEntry> entries = new List<SMDBEntry>();
            if (ents.Count > 0)
            {
                foreach (var ent in ents)
                {
                    if (ent.Keyword == keyword.game)
                    {
                        if (ent.Entities.Count > 0)
                        {
                            var roms = ent.Entities.Where(k => k.Keyword == keyword.rom).ToList();
                            if (roms.Count > 0)
                            {
                                foreach (var rom in roms)
                                {
                                    SMDBEntry entry = new SMDBEntry();
                                    if (rom.Entities.Count > 0)
                                    {
                                        entry.Path = GetData(rom, keyword.name);
                                        entry.SHA1 = GetData(rom, keyword.sha1);
                                        entry.MD5 = GetData(rom, keyword.md5);
                                        entry.CRC32 = GetData(rom, keyword.crc);

                                        if (entry.Path != null)
                                            entries.Add(entry);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            int i = 0;
            foreach (var k in entries)
            {
                k.Index = i;
                i++;
            }

            return entries;
        }

        private static string GetData(Entity ent, keyword word)
        {
            var dat = ent.Entities.FirstOrDefault(k => k.Keyword == word);
            if (dat != null)
                return dat.Data;
            return null;
        }


        public static List<Entity> Parse(string[] lines)
        {
            List<Entity> entities = new List<Entity>();

            int i = 0;
            Entity ent = null;
            //foreach (string line in lines)
            for (i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (ent == null)
                {
                    keyword word = keyword.rom;
                    bool wrapped = false;
                    bool multiline = false;
                    if (StartsWithKeyword(line, out word, out wrapped, out multiline))
                    {
                        ent = new Entity()
                        {
                            Keyword = word
                        };
                        if (wrapped)
                        {
                            if (multiline)
                            {
                                List<string> _lines = new List<string>();
                                for (int a = i + 1; a < lines.Length; a++)
                                {
                                    var _line = lines[a];

                                    if (_line.Trim() == ")")
                                    {
                                        i = a + 1;
                                        break;
                                    }
                                    else
                                    {
                                        _lines.Add(_line);
                                    }
                                }
                                ent.Entities = Parse(_lines.ToArray());
                                entities.Add(ent);
                                ent = null;
                            }
                            else
                            {
                                var g = line.Trim().Substring(word.ToString().Length).Trim();
                                if (g.Length > 3)
                                {
                                    g = g.Substring(1, g.Length - 2).Trim();
                                    ent.Entities = ParseKeywordDataStream(g);
                                    entities.Add(ent);
                                    ent = null;
                                }
                            }
                        }
                        else
                        {
                            var g = line.Trim().Substring(word.ToString().Length).Trim();
                            ent.Data = g;
                            entities.Add(ent);
                            ent = null;
                        }
                    }
                }
            }

            return entities;
        }

        static List<Entity> ParseKeywordDataStream(string line)
        {
            List<Entity> entities = new List<Entity>();
            line = line.Trim();
            if (line.Length > 0)
            {
                keyword word = keyword.rom;
                bool wrapped = false;
                bool multiline = false;
                if (StartsWithKeyword(line, out word, out wrapped, out multiline))
                {
                    Entity ent = new Entity()
                    {
                        Keyword = word
                    };
                    var g = line.Substring(word.ToString().Length).Trim();
                    Match match = null;
                    switch (word)
                    {
                        case keyword.name:
                            match = Regex.Match(g, @"([^\s]+)\ssize");
                            break;
                        case keyword.size:
                            match = Regex.Match(g, @"([^\s]+)\scrc");
                            break;
                        case keyword.crc:
                            match = Regex.Match(g, @"([^\s]+)\smd5");
                            break;
                        case keyword.md5:
                            match = Regex.Match(g, @"([^\s]+)\ssha1");
                            break;
                        case keyword.sha1:
                            ent.Data = g;
                            entities.Add(ent);
                            ent = null;
                            break;
                    }
                    if (match != null )
                    {
                        if (match.Success)
                        {
                            ent.Data = match.Groups[1].Value;
                            g = g.Substring(ent.Data.Length);
                            entities.Add(ent);
                            entities.AddRange(ParseKeywordDataStream(g));
                        }
                        else
                        {
                            ent.Data = g;
                            entities.Add(ent);
                        }
                    }
                }
            }

            return entities;
        }
    }
}
