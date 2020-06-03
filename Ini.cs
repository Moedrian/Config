using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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


        // Accepts a 2-D array as argument
        public void Write(string[,] contents)
        {
            var wholeFile = Read();

            for (int i = 0; i < contents.Length; i++)
            {
                string section = contents[i, 0];
                string key = contents[i, 1];
                string value = contents[i, 2];

                if (wholeFile.ContainsKey(section))
                {
                    wholeFile[section][key] = value;
                }
                else
                {
                    var newKvPair = new Dictionary<string, string>();
                    newKvPair[key] = value;
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

            using (StreamReader reader = new StreamReader(filepath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                    yield return line;
            }
        }
    }
}
