using System;
using System.Collections.Generic;
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


        private class Section
        {
            public string SectionTitle { get; set; }
            public List<string> Properties { get; }
            public Section(string sectionTitle, List<string> properties)
            {
                SectionTitle = sectionTitle;
                Properties = properties;
            }
        }


        // IEnumerable of string array for less key strokes
        // Uncomment or add section/properties if they are already included in contents
        public void Write(IEnumerable<string[]> triples, bool keepTemp = false)
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

            // Store the contents e.g. comments before valid sections
            var head = new List<string>();
            foreach (var line in File.ReadLines(_iniFile))
            {
                if (_sectionMatch.IsMatch(line))
                    break;
                head.Add(line);
            }

            // Read all file
            var origin = File.ReadLines(_iniFile).ToList();

            var segments = DelimitBySection(origin).ToList();

            var sections = new List<Section>();
            foreach (var segment in segments)
            {
                var seg = segment.ToList();
                var title = seg.First();
                seg.RemoveAt(0);
                sections.Add(new Section(title, seg));
            }

            // File modification
            foreach (var triple in triples)
            {
                var (s, k, v) = (triple[0], triple[1], triple[2]);
                var newSectionTitle = $"[{s}]";
                var newProperty = $"{k}={v}";

                var sectionChanged = false;

                foreach (Section section in sections)
                {
                    if (section.SectionTitle.Contains(s))
                    {
                        sectionChanged = true;
                        section.SectionTitle = newSectionTitle;
                        var propertyChanged = false;
                        var changedIndex = -1;

                        foreach (var prop in section.Properties)
                        {
                            if (prop.Contains(k) && _propertyMatch.IsMatch(prop))
                            {
                                changedIndex = section.Properties.IndexOf(prop);
                                propertyChanged = true;
                            }
                        }

                        if (propertyChanged)
                            section.Properties[changedIndex] = newProperty;
                        else
                            section.Properties.Add(newProperty);
                    }
                }

                if (!sectionChanged)
                    sections.Add(new Section(Environment.NewLine + newSectionTitle, new List<string>(new[] { newProperty })));
            }

            // Clear all the contents
            File.WriteAllText(_iniFile, string.Empty);

            // Write new contents
            using (var sw = new StreamWriter(_iniFile, true))
            {
                foreach (var line in head)
                {
                    if (line.Length == 0)
                        sw.Write(line);
                    sw.WriteLine(line);
                }

                foreach (var section in sections)
                {
                    sw.WriteLine(section.SectionTitle);
                    foreach (var line in section.Properties)
                    {
                        if (line.Length == 0)
                            sw.Write(line);
                        sw.WriteLine(line);
                    }
                }
            }

            if (!keepTemp)
                File.Delete(tmpFile);
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
                    new ArraySegment<string>(lines.ToArray(), sectionIndices[i], sectionIndices[i + 1] - sectionIndices[i]);
                sections.Add(sectionSegment.ToList());
            }

            return sections;
        }
    }
}
