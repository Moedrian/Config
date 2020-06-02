using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Config
{
    class Ini
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
            var sectionIndex = new List<int>();

            foreach (var line in SplitToLines(_iniFile))
            {
                if (line.Trim().StartsWith(";") || line != "")
                    continue;

                validLines.Add(line);
            }

            foreach (var line in validLines)
            {
                if (_sectionMatch.IsMatch(line))
                    sectionIndex.Add(validLines.IndexOf(line));
            }

            return ini;
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
