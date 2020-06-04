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
        private readonly Regex _sectionMatch = new Regex(@"^\[.*\]$");

        public Ini(string filepath)
        {
            _iniFile = filepath;
        }


        public Dictionary<string, Dictionary<string, string>> Read()
        {
            var ini = new Dictionary<string, Dictionary<string, string>>();

            var validLines = new List<string>();
            var sectionIndices = new List<int>();
            var sections = new List<IEnumerable<string>>();

            foreach (var line in SplitToLines(_iniFile))
            {
                // To skip comments and empty lines
                if (line.Trim().StartsWith(";") || line.StartsWith("/") || line.Length == 0)
                    continue;

                if (line.Contains("=") || (line.StartsWith("[") && line.EndsWith("]")))
                    validLines.Add(line);
            }

            // Get list of section indices
            foreach (var line in validLines)
                if (_sectionMatch.IsMatch(line))
                    sectionIndices.Add(validLines.IndexOf(line));

            // manually add the delimiter, which is equal to IndexOfUpperBound + 1
            sectionIndices.Add(validLines.Count);
            for (int i = 0; i < sectionIndices.Count - 1; i++)
            {
                // ArraySegment provides ability to use offset, and no modification of origin array
                var sectionSegment =
                    new ArraySegment<string>(validLines.ToArray(), sectionIndices[i], sectionIndices[i + 1] - sectionIndices[i]);
                sections.Add(sectionSegment.ToList());
            }

            // Get the final dictionary containing the whole ini file
            // The comments of original file will be erased...
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
                ini.Add(sectionName.Trim('[', ']'), kvPairs);
            }

            return ini;
        }


        // Suggests using List to pass argument
        public void Write(IEnumerable<string[]> contents)
        {
            var wholeFile = Read();

            foreach (var combo in contents)
            {
                var (section, key, value) = (combo[0], combo[1], combo[2]);

                if (wholeFile.ContainsKey(section))
                {
                    wholeFile[section][key] = value;
                }
                else
                {
                    var newKvPair = new Dictionary<string, string>
                    {
                        [key] = value
                    };
                    wholeFile[section] = newKvPair;
                }
            }

            File.WriteAllText(_iniFile, string.Empty);
            using (var iniWriter = new StreamWriter(_iniFile))
            {
                foreach (var sectionPair in wholeFile)
                {
                    iniWriter.WriteLine("[" + sectionPair.Key + "]");
                    foreach (var kv in sectionPair.Value)
                    {
                        iniWriter.WriteLine(kv.Key + "=" + kv.Value);
                    }
                }
            }
        }


        private IEnumerable<string> SplitToLines(string filepath)
        {
            if(filepath == null)
                yield break;

            using (var reader = new StreamReader(filepath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    yield return line;
            }
        }
    }
}
