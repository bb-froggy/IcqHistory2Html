using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Icq2003Pro2Html
{
    class FPTHistoryStream : IICQHistoryStream
    {
        private Stream innerDataStream;

        public FPTHistoryStream(Stream dataStream)
        {
            innerDataStream = dataStream;
        }

        private bool firstPacket = true;

        public MessageMetadata parseNextPacket()
        {
            try
            {
                if (firstPacket)
                {
                    firstPacket = false;
                    HistoryStartPacket hsp = new HistoryStartPacket(innerDataStream);
                }
                
                MessageMetadata mmd = new MessageMetadata(innerDataStream);
                MessageContent mc = new MessageContent(innerDataStream);    // that's kind of redundant, but we still need to parse it, otherwise
                                                                            // the next call to parseNextPacket will fail
                mmd.selectTextFromContentPacket(mc);

                return mmd;
            }
            catch (EndOfStreamException)
            {
                return null;
            }
        }

        IICQMessage IICQHistoryStream.parseNextPacket()
        {
            return parseNextPacket();
        }
    }
}
