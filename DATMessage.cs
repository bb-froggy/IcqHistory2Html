using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Icq2003Pro2Html
{
    class DATMessage
    {
        public string Text;
        public string TextRTF;
        public DateTime SendDate;
        public bool isOutgoing;

        public UInt32 UIN;

        /// <summary>
        /// 1 - IM
        /// 4 - URL
        /// </summary>
        public UInt16 iMessageType;

        internal DATMessage(byte[] baContent)
        {
            parseStream(new ICQDataStream(baContent));
        }

        /// <summary>
        /// Compares two arrays. Bits are compared only when the bit in the comparisionBits array is set. Only the
        /// bytes with corresponding bytes in comparisonBits are compared, i.e. if a1 or a2 is longer than comparisonBits,
        /// these bytes are not compared
        /// </summary>
        /// <returns>true if equal, false if there are differences</returns>
        bool byteArraysAreEqual(byte[] a1, byte[] a2, byte[] comparisonBits)
        {
            if (a1.Length < comparisonBits.Length || a2.Length < comparisonBits.Length)
                return false;

            for (int i = 0; i < comparisonBits.Length; ++i)
                if ((comparisonBits[i] & a1[i]) != (comparisonBits[i] & a2[i]))
                    return false;
 
            return true;
        }

        private void parseStream(ICQDataStream streamContent)
        {
            UInt32 iMessageLength = streamContent.readUInt32();
            if (0 == iMessageLength || iMessageLength > 65536)
                throw new InvalidDataException("This is not a message packet. Unusal packet length: " + iMessageLength.ToString());

            byte[] strangeHeading = streamContent.readFixedBinary(0x1E);  
            // Byte strangeHeading[0x1a], lowest bit seems to indicate incoming messages
            // strangeHeading is either 00 00 00 00 xx xx xx xx 50 3b c1 5c 5c 95 d3 11 8d d7 00 10 4b 06 46 2e xx 02 xx 00 00 00
            //                   or                             e0 23 a3 db df b8 d1 11 8a 65 00 60 08 71 a3 91
            byte[] comparisonBits = new byte[] { 
                0xFF, 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 
                0x00, 0xFF, 0x00, 0xFF, 0xFF, 0xFF
            };

            if (!byteArraysAreEqual(strangeHeading,
                new byte[] 
                {
                    0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, // the latter four bytes are disregared because of comparisonBits
                    0x50, 0x3b, 0xc1, 0x5c, 0x5c, 0x95, 0xd3, 0x11,
                    0x8d, 0xd7, 0x00, 0x10, 0x4b, 0x06, 0x46, 0x2e,
                    0xFF, 0x02, 0xFF, 0x00, 0x00, 0x00
                },
                comparisonBits)
                &&
                !byteArraysAreEqual(strangeHeading,
                new byte[]
                {
                    0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, // the latter four bytes are disregared because of comparisonBits
                    0xe0, 0x23, 0xa3, 0xdb, 0xdf, 0xb8, 0xd1, 0x11, 
                    0x8a, 0x65, 0x00, 0x60, 0x08, 0x71, 0xa3, 0x91,
                    0xFF, 0x02, 0xFF, 0x00, 0x00, 0x00
                },
                comparisonBits)
           )
                throw new InvalidDataException("This is not a message packet. The identifying bits do not match!");


            iMessageType = streamContent.readUInt16();
            if (1 != iMessageType)
            {
                Debugger.Break();   // TODO: this is an interesting special case. Let's have a look at it!
            }

            UIN = streamContent.readUInt32();
            Text = streamContent.readString();

            byte[] messageFlags = streamContent.readFixedBinary(0x0a);    // Mostly 0x00, but last two bytes are 0xC8 0x01(?), 0x23 0x02, or 0x19 0x02
            // the first four bytes seem to be ff ff ff ff in case of an SMS
            isOutgoing = ((messageFlags[4] & 0x01) == 0x01);

            SendDate = streamContent.readUnixTime();

            byte[] zeroes = streamContent.readFixedBinary(0x13);

            if (streamContent.Position + 0x30 < streamContent.Length - 1)  // the message is there also in another format
            {
                UInt16 possibleRTFLength = streamContent.readUInt16();
                if (0 != possibleRTFLength) // seems to be an RTF
                {
                    streamContent.Seek(-2, SeekOrigin.Current);
                    TextRTF = streamContent.readString();
                }
                if (streamContent.Position + 3 < streamContent.Length - 1)  // again, the message in plain text. This time it's UTF-8
                    Text = streamContent.readString(Encoding.UTF8);
            }

//            byte[] tail = streamContent.readFixedBinary(0x08);  // zeroes for incoming messages, E4 04 00 00 00 80 80 00 for outgoing
        }
    }
}
