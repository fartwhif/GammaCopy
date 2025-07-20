using System.Collections.Generic;
using System.Xml.Serialization;

namespace GammaCopy.Formats
{
    public class Logiqx
    {
        // using System.Xml.Serialization;
        // XmlSerializer serializer = new XmlSerializer(typeof(Datafile));
        // using (StringReader reader = new StringReader(xml))
        // {
        //    var test = (Datafile)serializer.Deserialize(reader);
        // }

        [XmlRoot(ElementName = "header")]
        public class Header
        {

            [XmlElement(ElementName = "name")]
            public string Name { get; set; }

            [XmlElement(ElementName = "description")]
            public string Description { get; set; }

            [XmlElement(ElementName = "version")]
            public string Version { get; set; }

            [XmlElement(ElementName = "date")]
            public string Date { get; set; }

            [XmlElement(ElementName = "author")]
            public string Author { get; set; }

            [XmlElement(ElementName = "homepage")]
            public string Homepage { get; set; }

            [XmlElement(ElementName = "url")]
            public string Url { get; set; }

            [XmlElement(ElementName = "id")]
            public int Id { get; set; }

            [XmlElement(ElementName = "clrmamepro")]
            public Clrmamepro Clrmamepro { get; set; }
        }

        [XmlRoot(ElementName = "rom")]
        public class Rom
        {

            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }

            [XmlAttribute(AttributeName = "crc")]
            public string Crc { get; set; }

            [XmlAttribute(AttributeName = "md5")]
            public string Md5 { get; set; }

            [XmlAttribute(AttributeName = "sha1")]
            public string Sha1 { get; set; }

            [XmlAttribute(AttributeName = "sha256")]
            public string Sha256 { get; set; }

            [XmlAttribute(AttributeName = "status")]
            public string Status { get; set; }

            [XmlAttribute(AttributeName = "mia")]
            public string Mia { get; set; }


            [XmlIgnore]
            public long? Size { get; set; }
            [XmlAttribute(AttributeName = "size")]
            public string SizeSerializable
            {
                get
                {
                    if (Size != null)
                    {
                        return Size.ToString();
                    }
                    else
                    {
                        return "";
                    }
                }
                set
                {
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        Size = long.Parse(value);
                    }
                }
            }

        }

        [XmlRoot(ElementName = "game")]
        public class Game
        {
            [XmlElement(ElementName = "category")]
            public string Category { get; set; }

            [XmlElement(ElementName = "description")]
            public string Description { get; set; }

            [XmlElement(ElementName = "game_id")]
            public string GameId { get; set; }

            [XmlElement(ElementName = "rom")]
            public List<Rom> Rom { get; set; }

            [XmlAttribute(AttributeName = "name")]
            public string Name { get; set; }

            [XmlAttribute(AttributeName = "id")]
            public int Id { get; set; }

            [XmlText]
            public string Text { get; set; }
        }

        [XmlRoot(ElementName = "clrmamepro")]
        public class Clrmamepro
        {

            [XmlAttribute(AttributeName = "forcenodump")]
            public string Forcenodump { get; set; }
        }

        [XmlRoot(ElementName = "datafile")]
        public class Datafile
        {
            [XmlElement(ElementName = "header")]
            public Header Header { get; set; }

            [XmlElement(ElementName = "game")]
            public List<Game> Game { get; set; }
        }
    }
}
