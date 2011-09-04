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
    class MessageMetadata : DataPacket
    {
        public string ReceiverName;
        public string SenderName;

        public string Font;

        public string Text;

        public string MessageType;

        public DateTime TimeOfMessage;

        internal MessageMetadata(Stream dataStream)
            : base(dataStream)
        {
        }

        protected override void parseInnerData(byte[] innerData)
        {
            ICQDataStream strmContent = new ICQDataStream(new MemoryStream(innerData, false));
            strmContent.Seek(0x1a, SeekOrigin.Current);  // These are always the same (almost), so differentiation is not needed

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
                        streamContent.Read(baBodyHeader, 3, 0x10);

                    byte[] bodyContent = streamContent.readBinary();
                    // bodyContent contains 0x58 as start tag for plain text
                    // bodyContent contains 0x23 as start tag for plain text/rtf/plain text (yes, three times)

                    //Text = Encoding.Default.GetString(readBinary(ms));
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
                    SenderName = streamContent.readString(); // or is it the receivername?
                    break;
                case "Time":
                    byte timeStartTag = (byte)streamContent.ReadByte();
                    if (timeStartTag != 0x69)
                        throw new ArgumentException("Time tag expected to be 0x69, but it was " + timeStartTag.ToString());
                    TimeOfMessage = streamContent.readUnixTime();
                    break;
                // TODO: Parse remaining inner data
                default:
                    throw new NotImplementedException("Unknown tag \"" + tagName + "\"");
            }
        }

        protected override bool validateStartTag(uint startTag)
        {
            return 0x1 == startTag;
        }
    }
}
