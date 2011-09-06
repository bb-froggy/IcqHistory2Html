using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Icq2003Pro2Html
{
    // TODO: Abstract this with an interface, so all messages use the same interface
    /// <summary>
    /// Represents an instant message either received or sent
    /// </summary>
    class MessageMetadata : DataPacket, IICQMessage
    {
        public string OtherPartyName
        {
            get;
            protected set;
        }

        public string Text { get; protected set; }
        public string TextRTF { get; protected set; }

        public string MessageType;

        public DateTime TimeOfMessage { get; protected set; }

        public bool isOutgoing
        {
            get;
            protected set;
        }

        internal void selectTextFromContentPacket(MessageContent mc)
        {
            if (string.IsNullOrEmpty(Text))
                Text = mc.Text;
        }

        internal MessageMetadata(Stream dataStream)
            : base(dataStream)
        {
        }

        protected override void parseInnerData(byte[] innerData)
        {
            ICQDataStream strmContent = new ICQDataStream(new MemoryStream(innerData, false));
            byte[] baPacketHead = strmContent.readFixedBinary(0x1a);
            isOutgoing = ((baPacketHead[0x16] & 0x01) == 0x01);

            while (strmContent.Position < strmContent.Length - 1)
            {
                string nextLabel = strmContent.readString();
                handleTag(nextLabel, strmContent);
            }
            //string answerFlagsLabel = readString(ms);   // "AnswerFlags"
            //handleTag(answerFlagsLabel, ms);

            //string bodyLabel = readString(ms);
            //handleTag(bodyLabel, ms);

            //string clsidLabel = readString(ms);
            //handleTag(clsidLabel, ms);

            //string extidLabel = readString(ms);
            //handleTag(extidLabel, ms);
        }

        private void handleTag(string tagName, ICQDataStream streamContent)
        {
            switch(tagName)
            {
                case "AnswerFlags":
                    byte[] baAnswerFlags = streamContent.readFixedBinary(5);
                    break;
                case "SendFlags":
                case "Status":
                case "Read":
                    byte[] baSendFlags = streamContent.readFixedBinary(5); // TODO: this is a special Uint format: First 0x69 then four bytes of uint
                    break;
                case "Body":
                    byte[] baBodyHeader = new byte[0x13];
                    streamContent.Read(baBodyHeader, 0, 0x3);

                    if (baBodyHeader[1] == 0xEB)    // as opposed to 0xEA
                    {
                        streamContent.Read(baBodyHeader, 3, 0x10);
                        //if (isOutgoing != ((baBodyHeader[4] & 0x01) == 0x01))
                        //    throw new InvalidDataException("Outgoing flag is different in packet head and body head");
                    }

                    byte[] bodyContent = streamContent.readBinary();
                    if (null != bodyContent)
                    {
                        // bodyContent contains 0x58 as start tag for only ANSI text (ICQ Pro 2003a) or RTF and ANSI (ICQ Pro 2003b)
                        ICQDataStream strmBody = new ICQDataStream(bodyContent);
                        if (0x58 == bodyContent[0])     // maybe bodyContent[0] and bodyContent[1] together are a package type number?
                        {
                            strmBody.Seek(2, SeekOrigin.Begin);
                            UInt32 iICQ2003bIndicator = strmBody.readUInt32();
                            if (0 != iICQ2003bIndicator)    // this means the RTF starts immediately. ICQ Pro 2003b style.
                            {
                                strmBody.Seek(-4, SeekOrigin.Current);
                                parseICQ2003bBody(strmBody);
                            }
                        }
                        else if (0x23 == bodyContent[0])    // this means:  ANSI + RTF + UTF-8
                            strmBody.Seek(2, SeekOrigin.Begin);
                        else if (0x03 == bodyContent[0])    // observed cases are only "The user has added you to his/her Contact list"
                                                            // + "You added him/her to your Contact List"
                        {
                            MessageType = "Added2ContactList";
                            return;
                        }
                        else if (0x01 == bodyContent[0])
                        {
                            MessageType = "AuthorizationRequestAccepted";
                            return;
                        }
                        else
                            throw new NotImplementedException("Message start tag " + bodyContent[0].ToString() + " not implemented");

                        parseICQ2003aBody(strmBody);
                    }
                    
                    break;
                case "CLSID":
                    byte clsidStartTag = Convert.ToByte(streamContent.ReadByte());
                    byte[] clsidData = streamContent.readBinary();
                    break;
                case "ExtId":
                    byte[] extidContent = streamContent.readFixedBinary(3);
                    break;
                case "ExtName":
                    byte extName = Convert.ToByte(streamContent.ReadByte());
                    MessageType = streamContent.readString();
                    break;
                case "ExtVers":
                    byte[] extVers = streamContent.readFixedBinary( 5);
                    break;
                case "Folder":
                    byte[] folder = streamContent.readFixedBinary(3);
                    break;
                case "MsgUserName":
                    byte startTag = (byte)streamContent.ReadByte();
                    OtherPartyName = streamContent.readString();
                    break;
                case "Time":
                    byte timeStartTag = (byte)streamContent.ReadByte();
                    if (timeStartTag != 0x69)
                        throw new ArgumentException("Time tag expected to be 0x69, but it was " + timeStartTag.ToString());
                    TimeOfMessage = streamContent.readUnixTime();
                    break;
                default:
                    throw new NotImplementedException("Unknown tag \"" + tagName + "\"");
            }
        }

        private void parseICQ2003aBody(ICQDataStream strmBody)
        {
            byte[] baText = strmBody.readBinary();
            if (null != baText && baText.Length > 0)
            {
                Text = Encoding.Default.GetString(baText);
                string textUTF8Temp;
                string textRTFTemp;
                strmBody.parsePossiblyRemainingRTFandUTF8(out textRTFTemp, out textUTF8Temp);
                TextRTF = textRTFTemp;  // TextRTF will be null before that operation anyway
                if (null != textUTF8Temp)
                    Text = textUTF8Temp;
            }
        }

        private void parseICQ2003bBody(ICQDataStream strmBody)
        {
            byte[] baRTFText = strmBody.readBinary();
            if (null != baRTFText && baRTFText.Length > 0)
                TextRTF = Encoding.Default.GetString(baRTFText);

            byte[] baText = strmBody.readBinary();
            if (null != baText && baText.Length > 0)
                Text = Encoding.Default.GetString(baText);
        }

        protected override bool validateStartTag(uint startTag)
        {
            return 0x1 == startTag;
        }

        public override string ToString()
        {
            return OtherPartyName.ToString() + " (" + TimeOfMessage.ToLocalTime().ToString() + "): " + (isOutgoing ? "->" : "<-") + " " + Text;
        }
    }
}
