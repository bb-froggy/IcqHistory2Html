using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Icq2003Pro2Html
{
    abstract class DataPacket
    {
        protected abstract void parseInnerData(byte[] innerData);

        protected abstract bool validateStartTag(uint startTag);

        private uint readIntegerBigEndian(byte[] baInteger)
        {
            return (uint)baInteger[3] + ((uint)baInteger[2]<<8) + ((uint)baInteger[1]<<16) + ((uint)baInteger[0]<<24);
        }

        protected DataPacket(Stream dataStream)
        {
            byte[] buf = new byte[4];
            int iBytesRead = dataStream.Read(buf, 0, 4);
            if (iBytesRead < 4)
                throw new EndOfStreamException("Stream does not contain a whole packet anymore");

            uint firstTag = readIntegerBigEndian(buf);
            if (!validateStartTag(firstTag))
                throw new ArgumentException("This is not a usual packet. Packet tag is " + firstTag.ToString());

            dataStream.Read(buf, 0, 4);
            uint lengthTag = readIntegerBigEndian(buf);

            if (65536 < lengthTag)
                throw new ArgumentException("This inner data is too long. Only 65536 bytes are supported!");

            byte[] innerData = new byte[lengthTag];
            dataStream.Read(innerData, 0, (int)lengthTag);

            parseInnerData(innerData);
            
            dataStream.Seek(alignBytes(lengthTag + 8), SeekOrigin.Current);
        }

        protected virtual uint alignBytes(uint readInThisPacket)
        {
            uint iCurrentByteAlignment = readInThisPacket % 0x80;  // ICQ aligns all packet on 0x80 bytes
            if (0 == iCurrentByteAlignment)
                return 0;
            else
                return 0x80 - iCurrentByteAlignment;
        }
    }
}
