using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SSL_Steam
{
    class SigManager
    {
        //private SimpleScan _s;
        private SimpleScan_NoMem _s;
        private Dictionary<string,string> SignatureDictionary = new Dictionary<string, string>();
        private List<int> AddressList = new List<int>();
        private bool ScanExecuteOnly;
        public string LastError { get; private set; }
        public bool HasScanned = false;
        /// <summary>
        /// Manager for sig scan code. Makes everything look neater
        /// </summary>
        /// <param name="startAddress">Where to start block search</param>
        /// <param name="endAddress">Where to end block search</param>
        /// <param name="Target">Application to scan</param>
        public SigManager(int startAddress, int endAddress, string Target, bool scanExecuteOnly = false) {
            _s = new SimpleScan_NoMem(startAddress, endAddress, Target);
            ScanExecuteOnly = scanExecuteOnly;
        }
        public bool AddSignature(string name, string sig)
        {
            if (!SignatureDictionary.ContainsKey(name))
            {
                SignatureDictionary.Add(name,sig);
                return true;
            }
            return false;
        }
        public bool RemoveSignature(string name)
        {
            if (SignatureDictionary.ContainsKey(name))
            {
                SignatureDictionary.Remove(name);
                return true;
            }
            return false;
        }
        public bool ScanForSignatures(string s)
        {
           try
            {
                string mask = "";
                string[] t = SignatureDictionary[s].Split(' ');
                for (var i = 0; i < t.Length; i++)
                {
                    mask += (t[i] == "??" ? "?" : "x");
                }
                ScanAddressHandler(_s.PatternScan(s, SignatureDictionary[s], mask, ScanExecuteOnly));
                HasScanned = true;
                return true;
            } catch (Exception ex)
            {
                LastError = ex.Message;
                return false;
            }
        }
        private void ScanAddressHandler(List<int> l)
        {
            foreach (int i in l)
            {
                AddressList.Add(i);
            }
        }
        public List<int> GetAddressList()
        {
            return AddressList;
        }
    }
}
