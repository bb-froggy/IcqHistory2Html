using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Icq2003Pro2Html
{
    class HistoryStartPacket : DataPacket
    {
        public HistoryStartPacket(Stream dataStream)
            : base(dataStream)
        {
        }

        protected override void parseInnerData(byte[] innerData)
        {
            // check whether this history stream contains the "0x20 instead of 0x00 bug" or not
        }

        protected override bool validateStartTag(uint startTag)
        {
            return true;    // these start packets have all kinds of start tags...
        }

        protected override uint alignBytes(uint readInThisPacket)
        {
            return 0x200 - readInThisPacket;   // Don't know whether this is a constant. There are always 80 bytes in the length tag, but then the next packet starts at 0x200
        }
    }
}
