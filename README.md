# Config

A C# implementation of reading/writing configuration files.

## Before You Start

* This ini parser doesn't support section nesting, yet.
* `;`, `#` and `//` will be identified as valid comment characters.
* Property names shall follow C-style variable nomenclature.
* Comments right after property values (i.e. in a same line) will be identified as part of the value.
* .Net version 4.7.2 (tuple used)

## Usage

```C#

using Config;

// first you need to create a new instance
var iniFile = new Ini(pathToIniFile);

// returns a Dictionary<string, Dictionary<string, string>> variable
// [section -> [key -> value],],
var contents = Ini.Read();

// argument be like
Ini.Write(new [] {
    new [] {
        "newSection1", "newProperty1", "newValue1"
    },
    new [] {
        "newSection2", "newProperty2", "newValue2"
    }
});

```
