# Config

A C# implementation of reading/writing configuration files.

## Before You Start

Currently this snippet will erase all the comments and empty lines in
the original ini file. And it cannot validate if the input file is a 
valid ini file.

## Usage

```C#

using Config;

// first you need to create a new instance
var iniFile = new Ini(pathToIniFile);

// returns a Dictionary<string, Dictionary<string, string>> variable
// [section -> [key -> value],],
var contents = Ini.Read();

// argument be like 
string[,] toBeWriiten = new string[,] {{section, key, value},}
Ini.Write(toBeWritten);

```

