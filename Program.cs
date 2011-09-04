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

            if (sInputFilePath.EndsWith(".fpt", StringComparison.InvariantCultureIgnoreCase))
                parseFPT(sInputFilePath);
            else if (sInputFilePath.EndsWith(".dat", StringComparison.InvariantCultureIgnoreCase)
                || sInputFilePath.EndsWith(".dat2", StringComparison.InvariantCultureIgnoreCase))
                parseDAT(sInputFilePath);
            else
                Console.WriteLine("File ends neither with .fpt nor with .dat. Is it really an ICQ History file?");

#if DEBUG
            Console.WriteLine("DEBUG: Finished! Press enter to exit!");
            Console.ReadLine();
#endif
        }

        private static void parseDAT(string sInputFilePath)
        {
            // TODO: Define some interface for History streams and put this method together with parseFPT
            using (FileStream fs = File.Open(sInputFilePath, FileMode.Open))
            {
                DATHistoryStream history = new DATHistoryStream(fs);

                for (DATMessage packet = history.parseNextPacket(); packet != null; packet = history.parseNextPacket())
                    Console.WriteLine(packet.UIN.ToString()+ " (" + packet.SendDate.ToLocalTime().ToString() + "): " + (packet.isOutgoing?"->":"<-") + packet.Text);
            }
        }

        private static void parseFPT(string sInputFilePath)
        {
            using (FileStream fs = File.Open(sInputFilePath, FileMode.Open))
            {
                FPTHistoryStream history = new FPTHistoryStream(fs);

                for (DataPacket packet = history.parseNextPacket(); packet != null; packet = history.parseNextPacket())
                {
                    //                    Console.WriteLine("Type: " + packet.GetType().ToString());
                    //MessageContent content = packet as MessageContent;
                    //if (null != content)
                    //    Console.WriteLine(content.Text);

                    MessageMetadata meta = packet as MessageMetadata;
                    if (null != meta)
                        Console.WriteLine(meta.SenderName + " (" + meta.TimeOfMessage.ToString() + "): " + meta.Text);
                }
            }
        }

        private static void printUsage()
        {
            Console.WriteLine("Written by Froggy. Licensed under WTFPL.");
            Console.WriteLine("Usage: icq2003Pro2Html HistoryFile");
            Console.WriteLine();
            Console.WriteLine("HistoryFile - Full path to a ICQ Pro 2003a History (DAT) or ICQ Pro 2003b History (FPT)");
        }
    }
}
