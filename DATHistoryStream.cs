using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Icq2003Pro2Html
{
    class DATHistoryStream : IICQHistoryStream
    {
        private ICQDataStream innerDataStream;

        public DATHistoryStream(Stream dataStream)
        {
            if (dataStream is ICQDataStream)
                innerDataStream = (ICQDataStream)dataStream;
            else
                innerDataStream = new ICQDataStream(dataStream);
        }

        private Int32 addressNextBreakerPacket = 0;
 
        public DATMessage parseNextPacket()
        {
            try
            {
                if (0 == addressNextBreakerPacket)
                {
                    if (0 != innerDataStream.Position)
                        throw new InvalidOperationException("Address of breaker packet unknown, but stream is not on Position 0");
                    byte[] baStart = innerDataStream.readFixedBinary(8);    // 04 00 00 00 08 00 00 00
                    readBreakerPacket();
                    innerDataStream.Position = 0x595;   // This seems to be the first real message
                }

                while (innerDataStream.Position < innerDataStream.Length - 1)
                {
                    if (addressNextBreakerPacket != -1)
                    {
                        if ((innerDataStream.Position % 0x40) != (addressNextBreakerPacket % 0x40))
                            throw new InvalidDataException("Next breaker packet is aligned different to the current parsing alignment");

                        if (innerDataStream.Position > addressNextBreakerPacket)
                            throw new InvalidCastException("Missed a breaker packet...");
                    }

                    if (innerDataStream.Position == addressNextBreakerPacket)
                        readBreakerPacket();

                    long positionBeforePacket = innerDataStream.Position;
                    try
                    {
                        byte[] baPacketData = readPacket();
                        return new DATMessage(baPacketData);
                    }
                    catch (InvalidDataException ide)
                    {
                        if (!ide.Message.StartsWith("This is not a message packet."))
                            throw;

                        innerDataStream.Seek(positionBeforePacket + 0x40, SeekOrigin.Begin);  // Next possible packet start
                    }
                }

                return null;
            }
            catch (EndOfStreamException)
            {
                return null;
            }
        }

        private void readBreakerPacket()
        {
            long positionBeforeBreaker = innerDataStream.Position;
            byte[] baBreaker = readPacket();
            addressNextBreakerPacket = BitConverter.ToInt32(baBreaker, 0x14);   // will be -1 for the last breaker
            innerDataStream.Seek(positionBeforeBreaker + baBreaker.Length, SeekOrigin.Begin);   // breakers are unaligned
        }

        byte[] readPacket()
        {
            UInt32 packetLength = innerDataStream.readUInt32();
            innerDataStream.Seek(-4, SeekOrigin.Current);   // the length is part of the packet
            packetLength += 4;  // add 4 for the length of the length
            if (4 >= packetLength || packetLength > 65536 || packetLength + innerDataStream.Position >= innerDataStream.Length) // probably read some garbage that's not actually a packet
                throw new InvalidDataException("This is not a message packet. Unusal packet length: " + packetLength.ToString());

            byte[] baPacketContent = innerDataStream.readFixedBinary(packetLength);   
            innerDataStream.Seek(0x40 - (packetLength % 0x40), SeekOrigin.Current); // packets are padded to 0x40 sizes. Sometimes more :-(
            return baPacketContent;
        }

        IICQMessage IICQHistoryStream.parseNextPacket()
        {
            return parseNextPacket();
        }
    }
}
