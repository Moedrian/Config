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
        private readonly Regex _sectionMatch = new Regex(@"^[;#/]*?\[.*\]$");

        public Ini(string filepath)
        {
            _iniFile = filepath;
        }


        public Dictionary<string, Dictionary<string, string>> Read()
        {
            var ini = new Dictionary<string, Dictionary<string, string>>();

            var validLines = new List<string>();
            foreach (var line in File.ReadLines(_iniFile))
                if (IsValidLine(line))
                    validLines.Add(line);

            var sections = DelimitFileBySection(validLines);

            foreach (var section in sections)
            {
                var sectionList = section.ToList();
                var sectionName = sectionList.First();
                sectionList.RemoveAt(0);
                var kvPairs = new Dictionary<string, string>();
                foreach (var kvPair in sectionList)
                {
                    var kv = kvPair.Split('=');
                    kvPairs.Add(kv[0].Trim(), kv[1].Trim());
                }
                ini.Add(sectionName.Trim('[', ']').Trim(), kvPairs);
            }

            return ini;
        }


        private class Section
        {
            public string SectionName;
            public List<string> Properties;
            public Section(string sectionName, List<string> properties)
            {
                SectionName = sectionName;
                Properties = properties;
            }
        }


        // IEnumerable of string array for less key strokes
        // Uncomment or add section/properties if they are included in contents
        public void Write(IEnumerable<string[]> triples, bool keepTemp = false)
        {
            var tmpFile = _iniFile + ".tmp~";
            
            if (File.Exists(tmpFile))
                File.Delete(tmpFile);

            File.Copy(_iniFile, tmpFile);

            var fileStart = new List<string>();
            foreach (var line in File.ReadLines(_iniFile))
            {
                if (_sectionMatch.IsMatch(line))
                    break;
                fileStart.Add(line);
            }

            var origin = File.ReadLines(_iniFile).ToList();
            var segments = DelimitFileBySection(origin).ToList();

            var sections = new List<Section>();
            foreach (var segment in segments)
            {
                var seg = segment.ToList();
                var title = seg.First();
                seg.RemoveAt(0);
                sections.Add(new Section(title, seg));
            }


            foreach (var triple in triples)
            {
                var (s, p, v) = (triple[0], triple[1], triple[2]);
                var sectionChanged = false;
                var newProperty = $"{p}={v}";

                foreach (var section in sections)
                {
                    if (_sectionMatch.IsMatch(section.SectionName) && section.SectionName.Contains(s))
                    {
                        sectionChanged = true;
                        section.SectionName = $"[{s}]";
                        var propChanged = false;
                        var changedIndex = -1;

                        foreach (var prop in section.Properties)
                        {
                            if (prop.Contains(p) && prop.Contains("="))
                            {
                                changedIndex = section.Properties.IndexOf(prop);
                                propChanged = true;
                            }
                        }

                        if (propChanged)
                            section.Properties[changedIndex] = newProperty;
                        else
                            section.Properties.Add($"{p}={v}");
                    }
                }
                if (!sectionChanged)
                    sections.Add(new Section($"[{s}]", new List<string>(new []{$"{p}={v}"})));
            }

            File.WriteAllText(_iniFile, string.Empty);
            using (var sw = new StreamWriter(_iniFile, true))
            {
                foreach (var line in fileStart)
                    sw.WriteLine(line);
                foreach (var section in sections)
                {
                    sw.WriteLine(section.SectionName);
                    foreach (var line in section.Properties)
                        sw.WriteLine(line);
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


        private IEnumerable<IEnumerable<string>> DelimitFileBySection(IEnumerable<string> rawLines)
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
