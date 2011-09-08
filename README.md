# IcqHistory2Rtf

A .NET Command Line tool to convert old ICQ Pro 2002/2003 history files to RTF

## Status

Currently, the tool parses FPT files (ICQ Pro 2003 b) and DAT files (ICQ Pro 2003 a and earlier). It produces RTF output on the command line. Pipe stdout to a file to get the RTF text.

The program was once intended to produce HTML output but then it is easier to create RTF from ICQ history. Therefore you may see IcqHistory2Html on a couple of places (currently probably more often than IcqHistory2Rtf actually). It produces no HTML, although you can convert RTF to HTML probably easily.

## Known Bugs

FPTs from ICQ 2003b:
- It is not always correctly determined whether a message was sent or received
- Unicode problems
- File Transfers and the like are not displayed correctly

There are certainly a whole lot of other bugs, but I achieved what I wanted to (extract some part of my own ICQ history). If you think this tool could be useful for you but you suffer from some bug, just contact me. Maybe I will fix the bug myself, maybe I can help you to fix it yourself.

## License

This program is free software. It comes without any warranty, to the extent permitted by applicable law. You can redistribute it and/or modify it under the terms of the Do What The Fuck You Want To Public License, Version 2, as published by Sam Hocevar. See http://sam.zoy.org/wtfpl/COPYING for more details.