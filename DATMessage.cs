using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace Icq2003Pro2Html
{
    class DATMessage : IICQMessage
    {
        public string Text { get; protected set; }
        public string TextRTF { get; protected set; }

        /// <summary>
        /// This seems to be incorrect sometimes. At least in some cases, the real time was one hour later.
        /// </summary>
        public DateTime TimeOfMessage { get; protected set; }
        public bool isOutgoing { get; protected set; }

        public UInt32 UIN;
        public string OtherPartyName
        {
            get
            {
                return UIN.ToString();
            }
        }

        /// <summary>
        /// This number usually increases, although not linearly
        /// </summary>
        public UInt32 MessageNumber;

        /// <summary>
        /// 1 - IM
        /// 4 - URL
        /// 0x0D - Internet Message
        /// 0x13 - Contacts
        /// </summary>
        public UInt16 iMessageType;

        internal DATMessage(byte[] baContent)
        {
            try
            {
                parseStream(new ICQDataStream(baContent));
            }
            catch (EndOfStreamException eosEx)
            {
                throw new InvalidDataException("This is not a message packet. Data could not be read.", eosEx);
            }
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
                0x00, 0xF0, 0x00, 0xFF, 0xFF, 0xFF
            };

            if (!byteArraysAreEqual(strangeHeading,
                new byte[] 
                {
                    0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, // the latter four bytes are disregared because of comparisonBits
                    0x50, 0x3b, 0xc1, 0x5c, 0x5c, 0x95, 0xd3, 0x11,
                    0x8d, 0xd7, 0x00, 0x10, 0x4b, 0x06, 0x46, 0x2e,
                    0xFF, 0x00, 0xFF, 0x00, 0x00, 0x00
                },
                comparisonBits)
                &&
                !byteArraysAreEqual(strangeHeading,
                new byte[]
                {
                    0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0x00, 0x00, // the latter four bytes are disregared because of comparisonBits
                    0xe0, 0x23, 0xa3, 0xdb, 0xdf, 0xb8, 0xd1, 0x11, 
                    0x8a, 0x65, 0x00, 0x60, 0x08, 0x71, 0xa3, 0x91,
                    0xFF, 0x00, 0xFF, 0x00, 0x00, 0x00
                },
                comparisonBits)
           )
                throw new InvalidDataException("This is not a message packet. The identifying bits do not match!");


            MessageNumber = BitConverter.ToUInt32(strangeHeading, 4);   // this number increases, although not linearly
            UInt16 SessionNumber = BitConverter.ToUInt16(strangeHeading, 24);   // this number also increases, but more slowly

            iMessageType = streamContent.readUInt16();
            UIN = streamContent.readUInt32();

            if (1 == iMessageType)
            {
                parseMessagePacket(streamContent);
                return;
            }

            if (0x0d == iMessageType)
            {
                // it's an internet message. The UIN will not be correct (I've seen 0x0a as UIN)
                parseMessagePacket(streamContent);
                return;
            }

            if (0x13 == iMessageType)
            {
                UInt16 contactListLength = streamContent.readUInt16();
                //byte[] contacts = streamContent.readFixedBinary(contactListLength);

                // this is a list of contacts. First comes the number of contacts in decimal. 
                // Then, for each contact, the UIN and then the display name is stored.
                // Different contacts as well as UIN and display name and the number of contacts are separated with 0xFE.
                // Contact names are encoded in the current code page
                return;
            }

            if (0x04 == iMessageType)   // URL
            {
                parseMessagePacket(streamContent);  // Looks almost like a message, except that a 0xFE separates a custom message from the URL
                return;
            }

            if (0x0c == iMessageType)   // only seen once and then it was mostly empty
            {
                parseMessagePacket(streamContent);
                return;
            }

            if (0x0e == iMessageType)   // Email
            {
                parseMessagePacket(streamContent);  // contains some specials: 0xFEs separate Sender Displayname, ?, ?, Sender mail address, ?, Message with HTML tags
                return;
            }

            Debugger.Break();   // TODO: this is an interesting special case. Let's have a look at it!
        }

        private enum AfterTextFooterType { StrangeHeaderBeforeRTF, NoFooterBeforeRTF, SMSText, NoFooterAtAll };

        private void parseMessagePacket(ICQDataStream streamContent)
        {
            Text = streamContent.readString();

            byte[] messageFlags = streamContent.readFixedBinary(0x0a);    // Mostly 0x00, but last two bytes are 0xC8 0x01(?), 0x23 0x02, or 0x19 0x02, or 0x03 0x02
            // the first four bytes seem to be ff ff ff ff in case of an SMS
            isOutgoing = ((messageFlags[4] & 0x01) == 0x01);

            TimeOfMessage = streamContent.readUnixTime();

            byte[] zeroes = streamContent.readFixedBinary(0x13);
            if (streamContent.Position > streamContent.Length - 8)
                return; // not enough to read anymore
            if (zeroes.Any(zeroByte => zeroByte != 0x00))
                return;     // The RTF messages are always zero in these bytes
            UInt16 possibleRTFLength = streamContent.readUInt16();
            byte[] baPossibleStrangeHeader = streamContent.readFixedBinary(6);      // in later versions of ICQ, these prepend the RTF
            AfterTextFooterType footerType = parseFooterTypeFromStrangeHeader(possibleRTFLength, baPossibleStrangeHeader);

            if (footerType == AfterTextFooterType.NoFooterBeforeRTF || footerType == AfterTextFooterType.SMSText)
                streamContent.Seek(-8, SeekOrigin.Current); // seek back the 8 bytes for the header
            //else if ()
            //    streamContent.Seek(-6, SeekOrigin.Current); // Only an UTF-8 text will be provided
            else if (footerType == AfterTextFooterType.NoFooterAtAll)
                return;
            //else
            //{
            //    UInt32 nextStrangeNumber = streamContent.readUInt32();
            //    if (0x00c0c0c0 != (0xFFc0c0c0 & nextStrangeNumber))
            //        return;
            //    // if the two strange numbers are there... just go on and parse the RTF :-)
            //}

            try
            {
                string textUTF8Temp;
                string textRTFTemp;
                streamContent.parsePossiblyRemainingRTFandUTF8(out textRTFTemp, out textUTF8Temp);
                TextRTF = textRTFTemp;  // TextRTF will be null before that operation anyway
                if (null != textUTF8Temp)
                    Text = textUTF8Temp;
            }
            catch (Exception ex)
            {
                throw new InvalidDataException("Parsed message text \"" + Text + "\" after which a footer of type "
                    + footerType.ToString() + " was detected, but there was a problem parsing the RTF/UTF-8 footer.", ex);
            }

            //            byte[] tail = streamContent.readFixedBinary(0x08);  // zeroes for incoming messages, E4 04 00 00 00 80 80 00 for outgoing
        }

        private AfterTextFooterType parseFooterTypeFromStrangeHeader(ushort possibleRTFLength,byte[] baPossibleStrangeHeader)
        {
            if (0x00 == possibleRTFLength)
            {
                UInt16 possibleSMSLength = BitConverter.ToUInt16(baPossibleStrangeHeader, 0);
                if (0x00 == possibleSMSLength || 0x0120 > possibleSMSLength)
                    if(baPossibleStrangeHeader.Skip(2).SequenceEqual(Encoding.ASCII.GetBytes(@"SMS:")))
                        return AfterTextFooterType.SMSText;
                    else if (possibleSMSLength == Encoding.UTF8.GetByteCount(Text) + 1 &&
                        Text.StartsWith(Encoding.UTF8.GetString(baPossibleStrangeHeader,2 , 4))
                        )   // it may still be an incoming SM in later ICQ versions
                        return AfterTextFooterType.SMSText;

            }

            if (possibleRTFLength < 0x1000 &&
                baPossibleStrangeHeader.SequenceEqual(Encoding.ASCII.GetBytes(@"{\rtf1")))
                return AfterTextFooterType.NoFooterBeforeRTF;

            if (0x00c0c0c0 == (0xFFc0c0c0 & BitConverter.ToUInt32(baPossibleStrangeHeader,2)))
                return AfterTextFooterType.StrangeHeaderBeforeRTF;

            return AfterTextFooterType.NoFooterAtAll;

            //    // Now check whether the PossibleStrangeHeader really looks like a strange header
            //if (possibleRTFLength > 0x1000)
            //    strangeHeaderFound = true;
            //else if (baPossibleStrangeHeader.Contains((byte)0x00))
            //    strangeHeaderFound = true;
            //else 
            //else
            //    return AfterTextFooterType.NoFooterAtAll; // hmm. This does not look like an RTF but neither like a strange header.
        }

        public override string ToString()
        {
            return  UIN.ToString() + " (" + TimeOfMessage.ToLocalTime().ToString() + "): " + (isOutgoing ? "->" : "<-") + " " + Text;
        }
    }
}
