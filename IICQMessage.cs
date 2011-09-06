using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Icq2003Pro2Html
{
    interface IICQMessage
    {
        string Text { get; }

        /// <summary>
        /// This seems to be incorrect sometimes. At least in some cases, the real time was one hour later for DATMessages.
        /// </summary>
        DateTime TimeOfMessage { get; }
        bool isOutgoing { get; }

        string OtherPartyName { get; }
    }
}
