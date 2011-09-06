using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Icq2003Pro2Html
{
    // Decorator to provide functions specific to ICQ History files
    class ICQDataStream : Stream
    {
        private Stream streamBackend;

        public ICQDataStream(byte[] baData)
            : this(new MemoryStream(baData))
        {
        }

        public ICQDataStream(Stream streamBackend)
        {
            this.streamBackend = streamBackend;
        }

        public string readString()
        {
            return readString(Encoding.Default);
        }

        public string readString(Encoding stringEncoding)
        {
            UInt16 length = readUInt16();

            byte[] baStringData = new byte[length - 1];
            streamBackend.Read(baStringData, 0, (int)length - 1);
            int iStringNullTerminator = streamBackend.ReadByte();
            if (0 != iStringNullTerminator)
                throw new ArgumentException("This string was not terminated by a zero!");
            return stringEncoding.GetString(baStringData);
        }

        public UInt32 readUInt32()
        {
            byte[] baNumber = new byte[4];
            int lengthActuallyRead = streamBackend.Read(baNumber, 0, 4);
            if (lengthActuallyRead < 4)
                throw new EndOfStreamException();
            return BitConverter.ToUInt32(baNumber, 0);
        }

        public UInt16 readUInt16()
        {
            byte[] baNumber = new byte[2];
            streamBackend.Read(baNumber, 0, 2);
            return BitConverter.ToUInt16(baNumber, 0);
        }

        public byte[] readBinary()
        {
            //uint bodyLength = 0;
            //while (0 == bodyLength) // strangely, sometimes the first length tag is zero and only the following contains the length.
            //{
            uint bodyLength;
            try
            {
                bodyLength = readUInt32();
            }
            catch (EndOfStreamException)
            {
                return null;
            }
            if (65536 < bodyLength)
                throw new ArgumentOutOfRangeException("Body Length larger than 65536 bytes. That's too long!");
            //}

            if (0 == bodyLength)
                return new byte[] { };
            byte[] baBody = new byte[bodyLength];
            streamBackend.Read(baBody, 0, (int)bodyLength);
            return baBody;
        }

        public byte[] readFixedBinary(uint length)
        {
            if (length > 65536)
                throw new ArgumentOutOfRangeException("length", "Length is too large...");

            byte[] buf = new byte[length];
            streamBackend.Read(buf, 0, (int)length);

            return buf;
        }

        public DateTime readUnixTime()
        {
                // This one hour seems to be necessary for correct time in ICQ, although I have no explanation
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(readUInt32()).AddHours(1);
        }


        /// <summary>
        /// Looks whether the rest of the stream contains RTF and possibly UTF8 text. This combination occurs
        /// in DAT files as well as FPTs. The only way to check is to look for the length of the string. If
        /// the two types of messages are not stored in the string, nulls are returned.
        /// </summary>
        public void parsePossiblyRemainingRTFandUTF8(out string textRTF, out string textUTF8)
        {
            textRTF = null;
            textUTF8 = null;

            if (Position + 0x30 < Length - 1)  // the message is there also in another format
            {
                UInt16 possibleRTFLength = readUInt16();
                if (0 != possibleRTFLength) // seems to be an RTF
                {
                    Seek(-2, SeekOrigin.Current);
                    textRTF = readString();
                }
                if (Position + 3 < Length - 1)  // again, the message in plain text. This time it's UTF-8
                {
                    UInt16 possibleUTF8Length = readUInt16();
                    if (0 != possibleUTF8Length)
                    {
                        Seek(-2, SeekOrigin.Current);
                        textUTF8 = readString(Encoding.UTF8);
                    }

                    // in FPTs, we may find a file name here in case its a file transfer
                }
            }
        }
    
#region Pass along stream functions to the backend
        public override bool  CanRead
        {
            get { return streamBackend.CanRead; }
        }

        public override bool  CanSeek
        {
            get { return streamBackend.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return streamBackend.CanWrite; }
        }

        public override void  Flush()
        {
            streamBackend.Flush();
        }

        public override long  Length
        {
            get { return streamBackend.Length; }
        }

        public override long  Position
        {
	        get 
	        {
                return streamBackend.Position;
	        }
	        set 
	        {
                streamBackend.Position = value;
	        }
        }

        public override int  Read(byte[] buffer, int offset, int count)
        {
            return streamBackend.Read(buffer, offset, count);
        }

        public override long  Seek(long offset, SeekOrigin origin)
        {
            return streamBackend.Seek(offset, origin);
        }

        public override void  SetLength(long value)
        {
            streamBackend.SetLength(value);
        }

        public override void  Write(byte[] buffer, int offset, int count)
        {
            streamBackend.Write(buffer, offset, count);
        }
#endregion Pass along stream functions to the backend
    }
}
