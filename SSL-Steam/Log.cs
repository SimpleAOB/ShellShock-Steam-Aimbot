using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSL_Steam
{
    class Log
    {
        private Dictionary<string, string> ELog;
        private string FileName;
        public Log(string fn)
        {
            ELog = new Dictionary<string, string>();
            Add("------------------------------");
            Add("Something not working? Message me on Discord: 'Simple_AOB#5526' with this log ready");
            Add("------------------------------");
            Add(Environment.OSVersion);
            Add(Environment.Version);
            FileName = fn;
        }
        private static long lastTimeStamp = DateTime.UtcNow.Ticks;
        public static long UtcNowTicks
        {
            get
            {
                long original, origts;
                do
                {
                    original = lastTimeStamp;
                    long now = DateTime.UtcNow.Ticks;
                    origts = Math.Max(now, original + 1);
                } while (System.Threading.Interlocked.CompareExchange
                             (ref lastTimeStamp, origts, original) != original);

                return origts;
            }
        }
        public void Add(object msg)
        {
            ELog.Add(UtcNowTicks.ToString(), msg.ToString());
        }
        public void Dump()
        {
            using (StreamWriter sw = new StreamWriter(Environment.CurrentDirectory + @"\"+FileName+".log"))
            {
                foreach (KeyValuePair<string, string> kp in ELog)
                {
                    sw.WriteLine(string.Format("{0}>> {1}", kp.Key, kp.Value));
                }
            }
        }
    }
}
