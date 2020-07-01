using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;


namespace Config
{
    public class Ini
    {
        private readonly string _iniFile;
        private readonly Regex _sectionMatch = new Regex(@"^[;#\/\s]*?\[.+?\]$");
        private readonly Regex _propertyMatch = new Regex(@"^[;#\/\s]*?\w+?\s*?=.*?$");

        public Ini(string filepath)
        {
            _iniFile = filepath;
        }


        private class Section
        {
            public string SectionTitle { get; set; }
            public readonly Dictionary<string, string> Properties = new Dictionary<string, string>();

            public Section(string sectionTitle)
            {
                SectionTitle = sectionTitle;
            }
        }


        public Dictionary<string, Dictionary<string, string>> Read()
        {
            var ini = new Dictionary<string, Dictionary<string, string>>();

            try
            {
                var validLines = new List<string>();
                foreach (var line in File.ReadLines(_iniFile))
                    if (IsValidLine(line))
                        validLines.Add(line);

                var sections = DelimitBySection(validLines);

                foreach (var section in sections)
                {
                    var properties = section.ToList();
                    var sectionTitle = properties.First();
                    properties.RemoveAt(0);

                    var kvs = new Dictionary<string, string>();

                    foreach (var property in properties)
                    {
                        var kv = property.Split('=');
                        kvs.Add(kv[0].Trim(), kv[1].Trim());
                    }

                    ini.Add(sectionTitle.Trim('[', ']').Trim(), kvs);
                }
            }
            catch (FileNotFoundException e)
            {
                ini.Add("Please Check File Existence", new Dictionary<string, string>
                {
                    {"Error Message", e.Message}
                });

                return ini;
            }

            return ini;
        }


        public void WriteL(IEnumerable<string[]> triples, bool keepTemp = false)
        {
            // If the ini file doesn't exist, create it
            if (File.Exists(_iniFile))
            {
                FileStream fs = File.Create(_iniFile);
                fs.Dispose();
            }

            // Temporary file creation
            var tmpFile = _iniFile + ".tmp~";
            if (File.Exists(tmpFile))
                File.Delete(tmpFile);

            File.Copy(_iniFile, tmpFile);

            var origin = File.ReadAllLines(_iniFile);

            if (origin.Length == 0)
            {
            }
            else
            {
            }
        }


        private void OutputSections(IEnumerable<Section> sections)
        {
        }


        private List<Section> FormatTriples(IReadOnlyCollection<string[]> triples)
        {
            var sections = new List<Section>();
            
            var sectionTitles = new List<string>();
            foreach (var triple in triples)
            {
                var s = triple[0];
                if (sectionTitles.Contains(s))
                    continue;
                sectionTitles.Add(s);
            }
            
            foreach (var sectionTitle in sectionTitles)
                sections.Add(new Section(sectionTitle));

            foreach (var triple in triples)
            {
                var s = triple[0];
                var k = triple[1];
                var v = triple[2];

                foreach (var section in sections)
                {
                    if (section.SectionTitle == s)
                        section.Properties[k] = v;
                }
            }

            return sections;
        }


        private bool IsValidLine(string inputLine)
        {
            var line = inputLine.Trim();
            return !line.StartsWith("//") && !line.StartsWith("#") && !line.StartsWith(";") && line.Length != 0;
        }


        private IEnumerable<IEnumerable<string>> DelimitBySection(IEnumerable<string> rawLines)
        {
            var lines = rawLines.ToList();

            var sectionIndices = new List<int>();
            var sections = new List<IEnumerable<string>>();

            // Get list of section indices
            foreach (var line in lines)
                if (_sectionMatch.IsMatch(line))
                    sectionIndices.Add(lines.IndexOf(line));

            // manually add the delimiter, which is equal to IndexOfUpperBound + 1
            sectionIndices.Add(lines.Count);
            for (int i = 0; i < sectionIndices.Count - 1; i++)
            {
                // ArraySegment provides ability to use offset, and no modification of origin array
                var sectionSegment =
                    new ArraySegment<string>(lines.ToArray(), sectionIndices[i],
                        sectionIndices[i + 1] - sectionIndices[i]);
                sections.Add(sectionSegment.ToList());
            }

            return sections;
        }
    }
}