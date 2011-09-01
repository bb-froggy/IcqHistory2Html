using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Icq2003Pro2Html
{
    class DATMessage
    {
        public string Text;
        public DateTime SendDate;
        public bool isOutgoing;

        public UInt32 UIN;

        /// <summary>
        /// 1 - IM
        /// 4 - URL
        /// </summary>
        public UInt16 iMessageType;

        public DATMessage(ICQDataStream streamContent)
        {
            parseStream(streamContent);
        }

        private void parseStream(ICQDataStream streamContent)
        {
            iMessageType = streamContent.readUInt16();

            UIN = streamContent.readUInt32();
            Text = streamContent.readString();

            byte[] messageFlags = streamContent.readFixedBinary(10);    // Mostly 0x00, but last two bytes are 0xC8 0x01
            isOutgoing = ((messageFlags[4] & 0x01) == 0x01);

            SendDate = streamContent.readUnixTime();
        }
    }
}
