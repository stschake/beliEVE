using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using beliEVE;

namespace LogServerHook
{

    public class LogServerPlugin : IPlugin
    {
        private int _mappingHandle;
        private IntPtr _logBase;
        private int _eventC;
        private int _eventD;
        private int _mutexA;
        private int _mutexB;
        private Thread _readerThread;
        private BinaryWriter _rawLog;

        public string Name
        {
            get { return "Log Server Hook"; }
        }

        public string ShortName
        {
            get { return "logServer"; }
        }

        public string Description
        {
            get { return "Collects log data that normally goes to the log server"; }
        }

        public int Revision
        {
            get { return 1; }
        }

        public PluginType Type
        {
            get { return PluginType.Generic; }
        }

        public void SetInterface(PluginInterface iface)
        {
        }

        private void Reader()
        {
            while (true)
            {
                if (Marshal.ReadInt32(_logBase, 8) != 0)
                {
                    Native.WaitForSingleObject(_mutexA, -1);
                    var count = Marshal.ReadInt32(_logBase, 8);
                    Core.Log(LogSeverity.Minor, "Got " + count + " messages");
                    var off = (uint)(_logBase.ToInt64() + 0x17718);
                    for (int i = 0; i < count; i++)
                    {
                        off += (uint)(0x250*i);
                        off += 0x1c;
                        var msg = Marshal.PtrToStringAnsi(new IntPtr(off));
                        Core.Log(LogSeverity.Minor, "Game: " + msg);
                    }
                    // reset count
                    Marshal.WriteInt32(_logBase, 8, 0);
                    Native.WaitForSingleObject(_eventC, 0);
                    Native.ResetEvent(_eventC);
                    Native.SetEvent(_eventD);
                    Native.ReleaseMutex(_mutexA);
                }
                Thread.Sleep(50);
            }
        }
        
        // no idea - prepares for new message?!
        private void Charge()
        {
            Marshal.WriteInt32(_logBase, 0x70000);
            Marshal.WriteInt32(_logBase, 4, 0x9C4);
            Marshal.WriteInt32(_logBase, 8, 0);
            Marshal.WriteInt32(_logBase, 20, 0);
        }

        public bool Initialize(LaunchStage stage)
        {
            return true;

            if (stage == LaunchStage.Startup)
            {
                const int size = 0x180c5f;
                _mappingHandle = Native.CreateFileMapping(-1, IntPtr.Zero, 0x04, 0, size, "EVE");
                _logBase = Native.MapViewOfFile(_mappingHandle, 0x06, 0, 0, size);
                _eventC = Native.CreateEvent(IntPtr.Zero, true, false, "cEVE");
                _eventD = Native.CreateEvent(IntPtr.Zero, true, false, "dEVE");
                _mutexA = Native.CreateMutex(IntPtr.Zero, false, "aEVE");
                _mutexB = Native.CreateMutex(IntPtr.Zero, false, "bEVE");
                Charge();
                
                _rawLog = new BinaryWriter(File.Create("messages.raw"));

                _readerThread = new Thread(Reader);
                _readerThread.IsBackground = true;
                _readerThread.Start();
                Core.Log(LogSeverity.Minor, "Initialized Log Server Hook");
            }

            return true;
        }

        public void ProcessCommand(string command)
        {
            
        }
    }

}