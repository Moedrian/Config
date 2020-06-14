using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection.Emit;

namespace Config
{
    public class Ini
    {
        private readonly string _iniFile;
        private readonly Regex _sectionMatch = new Regex(@"^[;#/]*?\[.*\]$");
        private char[] _commentChars = { '/', '#', ';' };

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


        // IEnumerable of string array for less key strokes
        // Uncomment or add section/properties if they are included in contents
        public void Write(IEnumerable<string[]> triples)
        {
            var tmpFile = _iniFile + ".tmp~";
            var lineCount = File.ReadLines(_iniFile).Count();

            foreach (var triple in triples)
            {
                if (File.Exists(tmpFile))
                {
                    File.Delete(tmpFile);
                }

                File.Copy(_iniFile, tmpFile);

                var (inputSection, inputKey, inputValue) = (triple[0], triple[1], triple[2]);

                // find the section to be write, if exists
                var sectionStart = 1;
                foreach (var line in File.ReadLines(_iniFile))
                {
                    if (line.Contains(inputSection) && line.Contains("[") && line.Contains("]"))
                        break;
                    sectionStart++;
                }

                // If there is no input section exists, the new section will
                // be append to the end of the ini file
                if (sectionStart == lineCount)
                {
                    using (var sw = new StreamWriter(_iniFile, true))
                    {
                        sw.WriteLine($"[{inputSection}]");
                        sw.WriteLine($"{inputKey}={inputValue}");
                    }
                }
                else
                {
                    // find the section
                    var sectionEnd = sectionStart;
                    var cursor = 1;
                    var block = new List<string>();
                    foreach (var line in File.ReadLines(_iniFile))
                    {
                        if (cursor < sectionStart)
                        {
                            cursor++;
                            continue;
                        }
                        if (_sectionMatch.IsMatch(line) && sectionEnd > sectionStart || sectionEnd == sectionStart && sectionEnd == lineCount)
                            break; 
                        block.Add(line);
                        sectionEnd++;
                    }

                    // Uncomment the current section block
                    block[0] = $"[{inputSection}]";
                    var propertyIndex = 0;
                    foreach (var line in block)
                    {
                        if (line.Contains(inputKey) && line.Contains("="))
                            propertyIndex = block.IndexOf(line);
                    }

                    var newPropertyValue = $"{inputKey}={inputValue}";

                    if (propertyIndex == 0)
                    {
                        block.Add(newPropertyValue);
                    }
                    else
                    {
                        block[propertyIndex] = newPropertyValue;
                    }


                    // Now write the contents back to original file
                    // Clear the original file
                    File.WriteAllText(_iniFile, string.Empty);
                    using (var sw = new StreamWriter(_iniFile, true))
                    {
                        // Contents before section to be written
                        cursor = 1;
                        foreach (var line in File.ReadLines(tmpFile))
                        {
                            if (cursor == sectionStart)
                                break;
                            sw.WriteLine(line);
                            cursor++;
                        }

                        // the changed section
                        foreach (var line in block)
                        {
                            sw.WriteLine(line);
                        }

                        // Contents after the section
                        var leftContentsIndex = 1;
                        foreach (var line in File.ReadLines(tmpFile))
                        {
                            if (leftContentsIndex < sectionEnd)
                            {
                                leftContentsIndex++;
                                continue;
                            }

                            sw.WriteLine(line);
                        }
                    }
                }
            }

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
