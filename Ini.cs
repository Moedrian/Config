using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;


namespace Ini
{
    public class Ini
    {
        private readonly string _iniFile;

        private readonly Regex _sectionMatch = new Regex(@"^[;#\/\s]*?\[.+?\]$");
        private readonly Regex _propertyMatch = new Regex(@"^[;#\/\s]*?\w+?\s*?=.*?$");
        private readonly char[] _validCommentCharacters = { '/', ';', '#' };

        public Ini(string filepath)
        {
            _iniFile = filepath;

            // create the file if not exists
            if (!File.Exists(_iniFile))
            {
                var fs = File.Create(_iniFile);
                fs.Close();
            }
        }


        public Dictionary<string, Dictionary<string, string>> Read(bool ignoreEmptySection = true)
        {
            var ini = new Dictionary<string, Dictionary<string, string>>();
            var validLines = File.ReadLines(_iniFile).Where(IsValidLine);

            var sections = ignoreEmptySection ? GetSections(validLines).Where(o => o.Count > 1) : GetSections(validLines);

            foreach (var properties in sections)
            {
                var sectionTitle = properties.First().Trim('[', ']').Trim();
                properties.RemoveAt(0);

                var kvs = new Dictionary<string, string>();

                foreach (var property in properties)
                {
                    var kv = property.Split('=');
                    kvs.Add(kv[0].Trim(), kv[1].Trim());
                }

                ini.Add(sectionTitle, kvs);
            }
            return ini;
        }


        public void WriteProperty(string section, string property, string value, bool addSpace = false)
        {
            var lines = File.ReadLines(_iniFile).ToList();
            var lineCount = lines.Count;

            var newProperty = addSpace ? $"{property} = {value}" : $"{property}={value}";
            var newSection = $"[{section}]";

            var targetSectionLine = 0;
            var targetPropertyLine = 0;

            for (var i = 1; i <= lineCount; i++)
            {
                var line = lines[i - 1];
                if (line.Contains(section) && _sectionMatch.IsMatch(line))
                {
                    targetSectionLine = i;
                    continue;
                }

                if (targetSectionLine == 0) continue;
                if (!_propertyMatch.IsMatch(line)) continue;
                if (property != line.Split('=')[0].TrimStart(_validCommentCharacters).Trim()) continue;
                targetPropertyLine = i;
                break;
            }

            if (targetSectionLine == 0)
            {
                lines.Add($"[{section}]");
                lines.Add(newProperty);
            }
            else
            {
                if (!IsValidLine(lines[targetSectionLine - 1]))
                    lines[targetSectionLine - 1] = newSection;

                if (targetPropertyLine == 0)
                {
                    lines.Insert(targetSectionLine, newProperty);
                }
                else
                {
                    lines[targetPropertyLine - 1] = newProperty;
                }
            }

            using (var sw = new StreamWriter(_iniFile))
            {
                foreach (var line in lines)
                    sw.WriteLine(line);
            }
        }


        private static bool IsValidLine(string inputLine)
        {
            var line = inputLine.Trim();
            return !line.StartsWith("//") && !line.StartsWith("#") && !line.StartsWith(";") && line.Length != 0;
        }


        /* using System;
         * ArraySegment requires higher version of .Net Framework
         *
        private List<List<string>> GetSections(IEnumerable<string> rawLines)
        {
            var lines = rawLines.ToList();

            var titleIndices = new List<int>();
            var sections = new List<List<string>>();

            // Get list of section indices
            foreach (var line in lines)
                if (_sectionMatch.IsMatch(line))
                    titleIndices.Add(lines.IndexOf(line));

            // manually add the delimiter, which is equal to IndexOfUpperBound + 1
            titleIndices.Add(lines.Count);
            for (int i = 0; i < titleIndices.Count - 1; i++)
            {
                // ArraySegment provides ability to use offset, and no modification of origin array
                var sectionSegment =
                    new ArraySegment<string>(lines.ToArray(), titleIndices[i],
                        titleIndices[i + 1] - titleIndices[i]);
                sections.Add(new List<string>(sectionSegment));
            }

            return sections;
        }
        */


        private List<List<string>> GetSections(IEnumerable<string> fileLines)
        {
            var lines = fileLines.ToArray();

            var section = new List<string>();
            var sections = new List<List<string>>();
            var lineCount = lines.Length;

            var foundSectionCtr = 0;

            for (var i = 1; i <= lineCount; i++)
            {
                var line = lines[i - 1];

                if (_sectionMatch.IsMatch(line))
                {

                    foundSectionCtr++;

                    if (foundSectionCtr == 2)
                    {
                        foundSectionCtr = 0;

                        // avoid Reference Clear
                        var sectionCopy = new List<string>();
                        foreach (var property in section)
                            sectionCopy.Add(property.Clone().ToString());

                        // clear this section for next section
                        sections.Add(sectionCopy);
                        section.Clear();

                        section.Add(line);
                        foundSectionCtr++;

                        // last line
                        if (i == lineCount)
                        {
                            sections.Add(new List<string> { line });
                            break;
                        }

                        continue;
                    }

                    section.Add(line);
                    continue;
                }

                if (foundSectionCtr < 2)
                    section.Add(line);
            }

            return sections;
        }
    }
}