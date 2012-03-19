using System;
using System.IO;
using System.Runtime.InteropServices;
using WhiteMagic.Internals;
using beliEVE;

namespace DestinyTool
{

    public static class StreamHooks
    {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int WriteDelegate(IntPtr stream, IntPtr buffer, int size);

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate int ReadDelegate(IntPtr stream, IntPtr buffer, int size);

        private static uint _memStreamReadOff;
        private static ReadDelegate _memStreamReadOrig;
        private static ReadDelegate _memStreamReadFake;
        private static Detour _memStreamReadDetour;

        private static uint _memStreamWriteOff;
        private static WriteDelegate _memStreamWriteOrig;
        private static WriteDelegate _memStreamWriteFake;
        private static Detour _memStreamWriteDetour;

        public static bool Initialized { get; private set; }

        public static void Initialize()
        {
            if (Initialized)
                return;

            Core.Log(LogSeverity.Minor, "initializing stream hooks..");
            _memStreamReadOff = Utility.FindPattern("blue",
                                                    "55 8b ec 56 8b 75 0c 57 8b f9 85 f6 78 ? 8b 47 18 8d 0c 30 3b 4f 10 76 ? 8b 47 18 8b 77 10");
            _memStreamWriteOff = Utility.FindPattern("blue",
                                                     "55 8b ec 53 8b 5d 0c 56 8b f1 57 8b 7e 18 03 fb 3b 7e 10 76 ? 56 8b c7 e8 ? ? ? ? 84 c0 75 ? 5f 5e");
            if (_memStreamReadOff != 0 && _memStreamWriteOff != 0)
            {
                Core.Log(LogSeverity.Minor,
                         "stream functions: Read: 0x" + (_memStreamReadOff - BlueOS.Library.ToInt64()).ToString("X") +
                         " Write: 0x" + (_memStreamWriteOff - BlueOS.Library.ToInt64()).ToString("X"));

                _memStreamReadOrig = Utility.Magic.RegisterDelegate<ReadDelegate>(_memStreamReadOff);
                _memStreamWriteOrig = Utility.Magic.RegisterDelegate<WriteDelegate>(_memStreamWriteOff);
                _memStreamReadFake = HandleRead;
                _memStreamWriteFake = HandleWrite;
                _memStreamReadDetour = Utility.Magic.Detours.Create(_memStreamReadOrig, _memStreamReadFake,
                                                                            "MemStream::Read");
                _memStreamWriteDetour = Utility.Magic.Detours.Create(_memStreamWriteOrig, _memStreamWriteFake,
                                                                             "MemStream::Write");
                Core.Log(LogSeverity.Minor, "stream functions hooked");
            }
            else
                Core.Log(LogSeverity.Minor, "pattern failed to find Read/Write stream functions");

            Initialized = true;
        }

        public static void Enable()
        {
            if (_memStreamReadDetour != null)
                _memStreamReadDetour.Apply();
            if (_memStreamWriteDetour != null)
                _memStreamWriteDetour.Apply();
        }

        public static void Disable()
        {
            if (_memStreamReadDetour != null)
                _memStreamReadDetour.Remove();
            if (_memStreamWriteDetour != null)
                _memStreamWriteDetour.Remove();
        }

        private static int HandleWrite(IntPtr stream, IntPtr buffer, int size)
        {
            var ret = (int) _memStreamWriteDetour.CallOriginal(stream, buffer, size);
            var buf = new byte[ret];
            Marshal.Copy(buffer, buf, 0, ret);
            //Core.Log(LogSeverity.Minor, "write " + size + ":\r\n" + Utility.HexDump(buf));
            return ret;
        }

        private static int HandleRead(IntPtr stream, IntPtr buffer, int size)
        {
            var ret = (int) _memStreamReadDetour.CallOriginal(stream, buffer, size);
            var buf = new byte[ret];
            Marshal.Copy(buffer, buf, 0, ret);
            Core.Log(LogSeverity.Minor, "read " + size + ", got " + ret + ":\r\n" + Utility.HexDump(buf));
            return ret;
        }
    }

}