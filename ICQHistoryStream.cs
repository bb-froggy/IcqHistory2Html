using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Icq2003Pro2Html
{
    class ICQHistoryStream
    {
        private Stream innerDataStream;

        public ICQHistoryStream(Stream dataStream)
        {
            innerDataStream = dataStream;
        }

        private bool firstPacket = true;
        private bool lastPacketWasMessage = true;
        
        public DataPacket parseNextPacket()
        {
            try
            {
                if (firstPacket)
                {
                    firstPacket = false;
                    return new HistoryStartPacket(innerDataStream);
                }

                if (lastPacketWasMessage)
                {
                    lastPacketWasMessage = false;
                    return new MessageMetadata(innerDataStream);
                }
                else
                {
                    lastPacketWasMessage = true;
                    return new MessageContent(innerDataStream);
                }
            }
            catch (EndOfStreamException)
            {
                return null;
            }
        }
    }
}
