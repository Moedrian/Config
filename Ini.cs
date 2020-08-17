using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;


namespace Config
{
    public class Ini
    {
        private readonly string _iniFile;
        private readonly bool _addSpace;

        private readonly Regex _sectionMatch = new Regex(@"^[;#\/\s]*?\[.+?\]$");
        private readonly Regex _propertyMatch = new Regex(@"^[;#\/\s]*?\w+?\s*?=.*?$");
        private readonly char[] _validCommentCharacters = {'/', ';', '#'};

        public Ini(string filepath, bool addSpaceForProperty = false)
        {
            _iniFile = filepath;
            _addSpace = addSpaceForProperty;
        }


        public Dictionary<string, Dictionary<string, string>> Read()
        {
            var ini = new Dictionary<string, Dictionary<string, string>>();

            try
            {
                var validLines = File.ReadLines(_iniFile).Where(IsValidLine);

                var sections = GetSections(validLines);

                foreach (var properties in sections)
                {
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


        public void WriteProperty(string section, string property, string value)
        {
            var lines = File.ReadLines(_iniFile).ToList();
            var lineCount = lines.Count;

            var newProperty = _addSpace ? $"{property} = {value}" : $"{property}={value}";
            var newSection = $"[{section}]";

            var targetSectionLine = 0;
            var targetPropertyLine = 0;

            for (var i = 1; i <= lineCount; i++)
            {
                var line = lines[i - 1];
                if (line.Contains(section))
                {
                    targetSectionLine = i;
                    continue;
                }

                if (targetSectionLine != 0)
                {
                    if (!_propertyMatch.IsMatch(line)) continue;
                    if (property != line.Split('=')[0].Trim().TrimStart(_validCommentCharacters)) continue;
                    targetPropertyLine = i;
                    break;
                }
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


        /*
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
                sections.Add(sectionSegment.ToList());
            }

            return sections;
        }
        */


        private List<List<string>> GetSections(IEnumerable<string> rawLines)
        {
            var lines = rawLines.ToArray();

            var section = new List<string>();
            var sections = new List<List<string>>();
            var lineCount = lines.Length;

            var endFile = false;
            var foundSectionEntry = 0;

            for (var i = 1; i <= lineCount; i++)
            {
                var line = lines[i - 1];
                if (_sectionMatch.IsMatch(line))
                {
                    foundSectionEntry++;

                    if (foundSectionEntry == 2)
                    {
                        foundSectionEntry = 0;

                        // Avoid Reference Clear
                        var sectionCopy = new List<string>();
                        foreach (var property in section)
                            sectionCopy.Add(property.Clone().ToString());

                        sections.Add(sectionCopy);
                        section.Clear();

                        section.Add(line);
                        foundSectionEntry++;
                        continue;
                    }

                    section.Add(line);
                    continue;
                }

                if (foundSectionEntry < 2)
                {
                    section.Add(line);

                    if (endFile)
                    {
                        sections.Add(section);
                        break;
                    }

                    // Next loop execution will be the last valid line
                    if (i == lineCount - 1)
                        endFile = true;
                }
            }

            return sections;
        }

    }
}