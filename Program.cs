using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Icq2003Pro2Html
{
    /// <summary>
    /// Converts ICQ 2003 a/b History files (fpt) to human-readable HTML
    /// </summary>
    class Program
    {
        /// <example>icq2003pro2html xyz.fpt</example>
        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                printUsage();
                return;
            }

            string sInputFilePath = args[0];
            string sUserNameFilter = null;
            if (args.Length > 1)
                sUserNameFilter = args[1];

            if (sInputFilePath.EndsWith(".fpt", StringComparison.InvariantCultureIgnoreCase))
                parseFPT(sInputFilePath, sUserNameFilter);
            else if (sInputFilePath.EndsWith(".dat", StringComparison.InvariantCultureIgnoreCase)
                || sInputFilePath.EndsWith(".dat2", StringComparison.InvariantCultureIgnoreCase))
                parseDAT(sInputFilePath, sUserNameFilter);
            else
                Console.WriteLine("File ends neither with .fpt nor with .dat. Is it really an ICQ History file?");


#if DEBUG
            Console.WriteLine("DEBUG: Finished! Press enter to exit!");
            Console.ReadLine();
#endif
        }

        private static void processHistoryStream(IICQHistoryStream history, string sUserNameFilter)
        {
            ICollection<IICQMessage> packets = new LinkedList<IICQMessage>();
            for (IICQMessage packet = history.parseNextPacket(); packet != null; packet = history.parseNextPacket())
            {
                if (string.IsNullOrEmpty(sUserNameFilter) || packet.OtherPartyName.Contains(sUserNameFilter))
                    packets.Add(packet);
            }

            foreach (IICQMessage orderedPacket in packets.OrderBy<IICQMessage, DateTime>(packet => packet.TimeOfMessage))
                    Console.WriteLine(orderedPacket.ToString());
        }

        private static void parseDAT(string sInputFilePath, string sUserNameFilter)
        {
            // TODO: Define some interface for History streams and put this method together with parseFPT
            using (FileStream fs = File.Open(sInputFilePath, FileMode.Open))
            {
                DATHistoryStream history = new DATHistoryStream(fs);
                processHistoryStream(history, sUserNameFilter);
            }
        }

        private static void parseFPT(string sInputFilePath, string sUserNameFilter)
        {
            using (FileStream fs = File.Open(sInputFilePath, FileMode.Open))
            {
                FPTHistoryStream history = new FPTHistoryStream(fs);
                processHistoryStream(history, sUserNameFilter);    
            }
        }

        private static void printUsage()
        {
            Console.WriteLine("Written by Froggy. Licensed under WTFPL.");
            Console.WriteLine("Usage: icq2003Pro2Html HistoryFile [UserNameFilter]");
            Console.WriteLine();
            Console.WriteLine("HistoryFile    - Full path to a ICQ Pro 2003a History (DAT) or ICQ Pro 2003b History (FPT)");
            Console.WriteLine("UserNameFilter - A string that must be part of the username string otherwise messages are filtered out");
        }
    }
}
