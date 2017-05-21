using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

// 
//      1.0.0 
//          - First version written and release. 
// [ CREDITS ] ---------------------------------------------------------------------------- 
// 
// sigScan is based on the FindPattern code written by 
// dom1n1k and Patrick at GameDeception.net 
// 
// C# Port by atom0s
// Heavily modified by Simple_AOB for custom range scanning
// ---------------------------------------------------------------------------------------- 
namespace SSL_Steam
{
    public class SimpleScan_NoMem
    {

        private int mStart;
        private int mEnd;
        private int mSize;
        private Process Target;
        private string Mask;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="read">Address where to start reading</param>
        /// <param name="size">Address where to stop reading</param>
        /// <param name="t">Target to dump from</param>
        public SimpleScan_NoMem(int start, int end, string t)
        {
            this.Target = GetProcess(t);
            this.mEnd = end;
            this.mStart = start;
            this.mSize = end - start;
        }
        private Process GetProcess(string str)
        {
            //Assumes the result at index 0 is the target
            if (Process.GetProcessesByName(str)[0].Handle != null) return Process.GetProcessesByName(str)[0];
            return null;
        }
        public List<int> PatternScan(string name, string sig, string mask, bool scanExecuteOnly)
        {
            List<int> results = new List<int>();
            Target.PriorityClass = ProcessPriorityClass.Idle;
            List<Thread> t = new List<Thread>();
            //Logical Processor Count
            int LP = Environment.ProcessorCount;
            //int LP = 1;
            int ThreadBlock = mSize / LP;
            //int ThreadBlock = mSize;
            int loc = 0;
            for (var i = 0; i < LP; i++)
            {
                PatternScanThreads pst = new PatternScanThreads(i, name, sig, mask, loc, loc + ThreadBlock, scanExecuteOnly, Target.Handle);
                ThreadStart ts = new ThreadStart(pst.Start);
                Thread newThread = new Thread(ts);
                newThread.Priority = ThreadPriority.Highest;
                newThread.Start();
                t.Add(newThread);
                loc += ThreadBlock;
            }
            bool AllDone = false;
            int NumDone = 0;
            while (!AllDone)
            {
                if (NumDone == t.Count)
                {
                    AllDone = true;
                    break;
                }
                for (var i = 0; i < t.Count; i++)
                {
                    string fn = Environment.CurrentDirectory + string.Format("\\{0}.pstr.{1}", name, i);
                    try
                    {
                        if (File.Exists(fn))
                        {
                            string[] lines = File.ReadAllLines(fn);
                            foreach (string l in lines)
                            {
                                if (l != "00000000")
                                {
                                    results.Add(Convert.ToInt32(l));
                                }
                            }
                            NumDone++;
                            File.Delete(fn);
                        }
                    } catch (Exception ex)
                    {

                    }
                }
            }
            Target.PriorityClass = ProcessPriorityClass.Normal;
            return results;
        }

       
    }
    class PatternScanThreads {
        // REQUIRED CONSTS
        const int PROCESS_QUERY_INFORMATION = 0x0400;
        const int MEM_COMMIT = 0x00001000;
        const int PAGE_READWRITE = 0x04;
        const int PROCESS_WM_READ = 0x0010;

        // REQUIRED METHODS
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        // REQUIRED STRUCTS
        public struct MEMORY_BASIC_INFORMATION
        {
            public int BaseAddress;
            public int AllocationBase;
            public int AllocationProtect;
            public int RegionSize;
            public int State;
            public int Protect;
            public int lType;
        }

        public struct SYSTEM_INFO
        {
            public ushort processorArchitecture;
            ushort reserved;
            public uint pageSize;
            public IntPtr minimumApplicationAddress;
            public IntPtr maximumApplicationAddress;
            public IntPtr activeProcessorMask;
            public uint numberOfProcessors;
            public uint processorType;
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }

        private volatile string sig;
        private volatile string mask;
        private volatile int assignedStart;
        private volatile int assignedEnd;
        private volatile IntPtr handle;
        private volatile bool scanExecuteOnly;

        private volatile List<int> results;

        private volatile int threadNum;
        private volatile string name;
        public PatternScanThreads(int _threadNum, string _name, string _sig, string _mask, int start, int end, bool _scanExecuteOnly, IntPtr _handle)
        {
            threadNum = _threadNum;
            sig = _sig;
            assignedStart = start;
            assignedEnd = end;
            mask = _mask;
            handle = _handle;
            scanExecuteOnly = _scanExecuteOnly;
            name = _name;
        }
        public void Start()
        {
            string fn = Environment.CurrentDirectory + string.Format("\\{0}.pstr.{1}",name,threadNum);
            if (File.Exists(fn)) File.Delete(fn);
            results = PatternScan();
            using (StreamWriter sw = new StreamWriter(fn))
            {
                if (results.Count == 0)
                {
                    sw.Write("00000000");
                }
                else
                {
                    foreach (int r in results)
                    {
                        sw.WriteLine(r.ToString());
                    }
                }
            }
        }
        private List<int> PatternScan()
        {
            List<int> results = new List<int>();
            byte[] _sig = StringToByteArray(sig.Replace(" ", string.Empty));
            SYSTEM_INFO sys_info = new SYSTEM_INFO();
            GetSystemInfo(out sys_info);

            IntPtr proc_min_address = sys_info.minimumApplicationAddress;

            int proc_min_address_l = assignedStart;
            int proc_max_address_l = assignedEnd;
            
            MEMORY_BASIC_INFORMATION mem_basic_info = new MEMORY_BASIC_INFORMATION();
            while (proc_min_address_l < proc_max_address_l)
            {
                VirtualQueryEx(handle, proc_min_address, out mem_basic_info, 28);
                if (scanExecuteOnly)
                {
                    if (mem_basic_info.Protect == 0x40 && mem_basic_info.State == MEM_COMMIT)
                    {
                        List<int> r = ReadProcess(mem_basic_info, _sig, proc_min_address_l);
                        foreach (int res in r)
                        {
                            results.Add(res);
                        }
                    }
                } else
                {
                    if (mem_basic_info.Protect == 0x04 && mem_basic_info.State == MEM_COMMIT)
                    {
                        List<int> r = ReadProcess(mem_basic_info, _sig, proc_min_address_l);
                        foreach (int res in r) results.Add(res);
                    }
                }
                
                // move to the next memory chunk
                proc_min_address_l += mem_basic_info.RegionSize;
                proc_min_address = new IntPtr(proc_min_address_l);
            }
            return results;
        }
        private bool MaskCheck(int nOffset, IEnumerable<byte> btPattern, string strMask, byte[] block)
        {
            return !btPattern.Where((t, x) => strMask[x] != '?' && ((strMask[x] == 'x') && (t != block[nOffset + x]))).Any();
        }
        private List<int> ReadProcess(MEMORY_BASIC_INFORMATION mbi, byte[] _sig, int proc_min_address_l)
        {
            List<int> results = new List<int>();
            byte[] buffer = new byte[mbi.RegionSize];
            int bytesRead = 0;
            // read everything in the buffer above
            ReadProcessMemory((int)handle, mbi.BaseAddress, buffer, mbi.RegionSize, ref bytesRead);
            int offset = FindPattern(_sig, mask, buffer);
            if (offset > 0)
            {
                results.Add(proc_min_address_l + offset);
            }
            return results;
        }
        private int FindPattern(byte[] btPattern, string strMask, byte[] block, int nOffset = 0)
        {
            try
            {
                for (int x = 0; x < block.Length; x += 4)
                {
                    if ((block.Length - x) < strMask.Length)
                    {
                        continue;
                    }
                    else
                    {
                        if (this.MaskCheck(x, btPattern, strMask, block))
                        {
                            return (x + nOffset);
                        }
                    }
                }
                return 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }
        private static byte[] StringToByteArray(String hex)
        {
            hex = hex.Replace("??", "90");
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}