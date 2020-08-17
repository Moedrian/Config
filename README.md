# Config

A C# simple implementation of reading/writing configuration files.

## Before You Start

* This ini parser doesn't support section nesting, yet.
* `;`, `#` and `//` will be identified as valid comment characters.
* Property names shall follow C-style variable nomenclature.
* Comments right after property values (i.e. in a same line) will be 
identified as part of the value.
* .NET Framework 4.0 +

## Usage

```C#

using Config;

// First you need to create a new instance
// Space for property is disabled by default, i.e. _property=_value 
// instead of _property = _value
var iniFile = new Ini(filepath:pathToIniFile, addSpaceForProperty:false);

// Returns a Dictionary<string, Dictionary<string, string>> variable
// [section -> [key -> value],],
// Dude this is very PHP
var contents = Ini.Read();

// If _section or _property is commented, this method will uncomment them
// Arguments be like:
Ini.WriteProperty("_section", "_property", "_value");

```
