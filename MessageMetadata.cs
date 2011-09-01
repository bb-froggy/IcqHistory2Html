using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Icq2003Pro2Html
{
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
            MemoryStream ms = new MemoryStream(innerData, false);
            ms.Seek(0x1a, SeekOrigin.Current);  // These are always the same (almost), so differentiation is not needed

            while (ms.Position < ms.Length)
            {
                string nextLabel = readString(ms);
                handleTag(nextLabel, ms);
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

        private void handleTag(string tagName, Stream streamContent)
        {
            switch(tagName)
            {
                case "AnswerFlags":
                    byte[] baAnswerFlags = readFixedBinary(streamContent, 5);
                    break;
                case "SendFlags":
                case "Status":
                case "Read":
                    byte[] baSendFlags = readFixedBinary(streamContent, 5); // TODO: this is a special Uint format: First 0x69 then four bytes of uint
                    break;
                case "Body":
                    byte[] baBodyHeader = new byte[0x13];
                    streamContent.Read(baBodyHeader, 0, 0x3);

                    if (baBodyHeader[1] == 0xEB)    // as opposed to 0xEA
                        streamContent.Read(baBodyHeader, 3, 0x10);

                    byte[] bodyContent = readBinary(streamContent);
                    // bodyContent contains 0x58 as start tag for plain text
                    // bodyContent contains 0x23 as start tag for plain text/rtf/plain text (yes, three times)

                    //Text = Encoding.Default.GetString(readBinary(ms));
                    break;
                case "CLSID":
                    byte clsidStartTag = Convert.ToByte(streamContent.ReadByte());
                    byte[] clsidData = readBinary(streamContent);
                    break;
                case "ExtId":
                    byte[] extidContent = readFixedBinary(streamContent, 3);
                    break;
                case "ExtName":
                    byte extName = Convert.ToByte(streamContent.ReadByte());
                    MessageType = readString(streamContent);
                    break;
                case "ExtVers":
                    byte[] extVers = readFixedBinary(streamContent, 5);
                    break;
                case "Folder":
                    byte[] folder = readFixedBinary(streamContent, 3);
                    break;
                case "MsgUserName":
                    byte startTag = (byte)streamContent.ReadByte();
                    SenderName = readString(streamContent); // or is it the receivername?
                    break;
                case "Time":
                    byte timeStartTag = (byte)streamContent.ReadByte();
                    if (timeStartTag != 0x69)
                        throw new ArgumentException("Time tag expected to be 0x69, but it was " + timeStartTag.ToString());
                    UInt32 iUnixTime = readUInt32(streamContent);
                    TimeOfMessage = new DateTime(1970, 1, 1).AddSeconds(iUnixTime);
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

        private string readString(Stream innerData)
        {
            byte[] baLength = new byte[2];
            innerData.Read(baLength, 0, 2);
            UInt16 length = BitConverter.ToUInt16(baLength, 0);
 
            byte[] baStringData = new byte[length-1];
            innerData.Read(baStringData, 0, (int)length-1);
            int iStringNullTerminator = innerData.ReadByte();
            if (0 != iStringNullTerminator)
                throw new ArgumentException("This string was not terminated by a zero!");
            return System.Text.Encoding.Default.GetString(baStringData);
        }

        private UInt32 readUInt32(Stream streamContent)
        {
            byte[] baNumber = new byte[4];
            streamContent.Read(baNumber, 0, 4);
            return BitConverter.ToUInt32(baNumber, 0);
        }

        private byte[] readBinary(Stream innerData)
        {
            uint bodyLength = 0;
            while (0 == bodyLength) // strangely, sometimes the first length tag is zero and only the following contains the length.
            {
                bodyLength = readUInt32(innerData);
                if (65536 < bodyLength)
                    throw new ArgumentOutOfRangeException("Body Length larger than 65536 bytes. That's too long!");
            }

            byte[] baBody = new byte[bodyLength];
            innerData.Read(baBody, 0, (int)bodyLength);
            return baBody;
        }

        private byte[] readFixedBinary(Stream streamContent, uint length)
        {
            if (length > 65536)
                throw new ArgumentOutOfRangeException("length", "Length is too large...");

            byte[] buf = new byte[length];
            streamContent.Read(buf, 0, (int)length);

            return buf;
        }
    }
}
