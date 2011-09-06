using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Icq2003Pro2Html
{
    class MessageContent : DataPacket
    {
        public string Text;

        internal MessageContent(Stream dataStream)
            : base(dataStream)
        {
        }

        protected override void parseInnerData(byte[] innerData)
        {
            Text = System.Text.Encoding.Unicode.GetString(innerData);   // TODO: I think this isn't UTF-16 actually. All characters just seem to be have a 00 byte in between instead.
        }

        protected override bool validateStartTag(uint startTag)
        {
            return 0x1 == startTag;
        }
    }
}
