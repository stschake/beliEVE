using System;
using System.Runtime.InteropServices;

namespace beliEVE
{

    public static class Native
    {
        public static IntPtr Kernel32
        {
            get { return LoadLibrary("kernel32.dll"); }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadLibrary(string lpszLib);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern bool SetCurrentDirectory(string path);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public static extern bool SetDllDirectory(string path);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi,
            CharSet = CharSet.Ansi)]
        public static extern int CreateEvent(IntPtr attr, bool manualReset, bool initialState, string name);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi,
            CharSet = CharSet.Ansi)]
        public static extern int CreateFileMapping(int file, IntPtr attr, int protect, int maxSizeHigh, int maxSizeLow,
                                                   string name);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi,
            CharSet = CharSet.Ansi)]
        public static extern IntPtr MapViewOfFile(int fileMapping, int access, int offsetHigh, int offsetLow, int size);

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi,
            CharSet = CharSet.Ansi)]
        public static extern int CreateMutex(IntPtr attr, bool initialOwner, string name);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int WaitForSingleObject(int handle, int millisec);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern bool ReleaseMutex(int handle);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern bool ResetEvent(int handle);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        public static extern bool SetEvent(int handle);
    }

}