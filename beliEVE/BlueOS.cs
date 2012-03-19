using System;
using System.Runtime.InteropServices;
using System.Text;

namespace beliEVE
{

    public static class BlueOS
    {
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate bool BeOsStartup(IntPtr beos, uint version, uint optimize);
        private static BeOsStartup _startup;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate IntPtr GetBePaths(IntPtr beos);
        private static GetBePaths _getBePaths;

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        private delegate int GetModFile(IntPtr module, IntPtr buffer, int maxSize);
        private static GetModFile _origGetModFile;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        private delegate bool BeOsRun(IntPtr beos);
        private static BeOsRun _run;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr GetBeOSDelegate();

        public static string EVE { get; private set; }
        public static IntPtr Library { get; set; }

        public static void Initialize(string eveBase)
        {
            EVE = eveBase;

            // necessary to fixup DLL loading, shouldn't matter for our managed program
            Native.SetDllDirectory(eveBase + "\\bin");

            Library = Native.LoadLibrary(eveBase + "\\bin\\blue.dll");
        }

        private static IntPtr BeOS
        {
            get
            {
                var func = Native.GetProcAddress(Library, "GetBeOS");
                var dele = Utility.Magic.RegisterDelegate<GetBeOSDelegate>(func);
                return dele();
            }
        }

        public static bool Run()
        {
            var func = Utility.Magic.GetObjectVtableFunction(BeOS, 24);
            _run = Utility.Magic.RegisterDelegate<BeOsRun>(func);
            return _run(BeOS);
        }

        public static bool Startup()
        {
            // hook GetModuleFilenameW to trick blue into thinking its in EVE directory
            var func = Native.GetProcAddress(Native.Kernel32, "GetModuleFileNameW");
            _origGetModFile = Utility.Magic.RegisterDelegate<GetModFile>(func);
            var our = new GetModFile(HandleGetModFile);
            var fakeModuleFile = Utility.Magic.Detours.CreateAndApply(_origGetModFile, our, "GetModuleFileNameW");

            using (fakeModuleFile)
            {
                // this is necessary to initialize some internal memory, ugh
                var getBePaths = Native.GetProcAddress(Library, "GetBePaths");
                _getBePaths = Utility.Magic.RegisterDelegate<GetBePaths>(getBePaths);
                _getBePaths(BeOS);
            }

            func = Utility.Magic.GetObjectVtableFunction(BeOS, 7);
            _startup = Utility.Magic.RegisterDelegate<BeOsStartup>(func);
            return _startup(BeOS, 0x0D, 1);
        }

        private static int HandleGetModFile(IntPtr module, IntPtr buffer, int maxsize)
        {
            var s = EVE + "\\bin\\ExeFile.exe";
            var bytes = Encoding.Unicode.GetBytes(s);
            // maxsize is set to MAX_FILE (260) by EVE, so safe
            Marshal.Copy(bytes, 0, buffer, bytes.Length);
            return s.Length;
        }
    }

}