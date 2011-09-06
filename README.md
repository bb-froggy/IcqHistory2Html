# IcqHistory2Html

A .NET Command Line tool to convert old ICQ Pro 2002/2003 history files to HTML, RTF, XML or something.

## Status

Currently, the tool parses FPT files (ICQ Pro 2003 b) and DAT files (ICQ Pro 2003 a and earlier).

Output is not HTML currently, only some debug output to console.

## Known Bugs

FPTs from ICQ 2003b:
- It is not always correctly determined whether a message was sent or received
- Unicode problems
- File Transfers and the like are not displayed correctly


## License

This program is free software. It comes without any warranty, to the extent permitted by applicable law. You can redistribute it and/or modify it under the terms of the Do What The Fuck You Want To Public License, Version 2, as published by Sam Hocevar. See http://sam.zoy.org/wtfpl/COPYING for more details.