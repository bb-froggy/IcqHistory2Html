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

        public ICQDataStream(Stream streamBackend)
        {
            this.streamBackend = streamBackend;
        }

        public string readString()
        {
            UInt16 length = readUInt16();

            byte[] baStringData = new byte[length - 1];
            streamBackend.Read(baStringData, 0, (int)length - 1);
            int iStringNullTerminator = streamBackend.ReadByte();
            if (0 != iStringNullTerminator)
                throw new ArgumentException("This string was not terminated by a zero!");
            return System.Text.Encoding.Default.GetString(baStringData);
        }

        public UInt32 readUInt32()
        {
            byte[] baNumber = new byte[4];
            streamBackend.Read(baNumber, 0, 4);
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
            uint bodyLength = 0;
            while (0 == bodyLength) // strangely, sometimes the first length tag is zero and only the following contains the length.
            {
                bodyLength = readUInt32();
                if (65536 < bodyLength)
                    throw new ArgumentOutOfRangeException("Body Length larger than 65536 bytes. That's too long!");
            }

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
            return new DateTime(1970, 1, 1).AddSeconds(readUInt32());
        }
    
#region Pass along stream functions to the backend
        public override bool  CanRead
        {
	        get { return streamBackend.CanRead }
        }

        public override bool  CanSeek
        {
	        get { return streamBackend.CanSeek }
        }

        public override bool CanWrite
        {
	        get { return streamBackend.CanWrite }
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
		        return streamBackend.Position
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
