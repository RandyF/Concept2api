using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;

using System.Diagnostics;


namespace AJR_Utils
{
    [Serializable]
    class AJRUtils
    {
        private const uint HEXDUMP_WIDTH = 32;


        /*
         * Dump a buffer in hexdump / ascii format.
         */
        public void hexdump(byte[] buff, uint len)
        {
            int i = 0;

            foreach (byte b in buff)
            {
                System.Console.Write("%02x ", b);
                if (i % HEXDUMP_WIDTH == 0)
                    System.Console.Write("\n");
            }
        }

        public void warning(string sWarning)
        {
            Console.WriteLine(sWarning);
        }

        public void error(string sError)
        {
            Console.WriteLine(sError);
        }

        public string DumpHex(byte[] buff)
        {
            int iIdx = 0;
            string tStr = String.Format("[{0,03}] ---\n", buff.Length);
            foreach (byte b in buff)
            {
                tStr += String.Format("{0:x02} ", b);

                if (++iIdx % HEXDUMP_WIDTH == 0) tStr += "\n";
            }
            tStr += "\n      ---   \n";
            return tStr;
        }

    }


}
