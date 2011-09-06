using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icq2003Pro2Html
{
    interface IICQHistoryStream
    {
        IICQMessage parseNextPacket();
    }
}
