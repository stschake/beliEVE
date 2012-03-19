using System;
using System.Runtime.InteropServices;
using System.Text;

namespace beliEVE
{

    public class PyString : PyObject
    {
        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal static extern IntPtr PyString_FromString(string value);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PyString_Size(IntPtr obj);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyString_AsString(IntPtr obj);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyString_FromStringAndSize(IntPtr buffer, int size);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr PyUnicodeUCS2_AsEncodedString(IntPtr obj, string encoding, string errors);

        public PyString(IntPtr ptr)
            : base(ptr)
        {
            
        }

        public PyString(string value)
            : base(PyString_FromString(value))
        {
            
        }

        public string Value
        {
            get
            {
                if (IsInvalid)
                    return "";

                return GetValueInternal(Pointer);
            }
        }

        public PyString(byte[] data)
            : base(IntPtr.Zero)
        {
            var native = Marshal.AllocHGlobal(data.Length);
            try
            {
                Marshal.Copy(data, 0, native, data.Length);
                Pointer = PyString_FromStringAndSize(native, data.Length);
            }
            finally
            {
                Marshal.FreeHGlobal(native);
            }
        }

        public byte[] Raw
        {
            get
            {
                if (IsInvalid)
                    return null;
                return GetRawInternal(Pointer);
            }
        }

        internal static byte[] GetRawInternal(IntPtr pointer)
        {
            var ret = new byte[PyString_Size(pointer)];
            var buffer = PyString_AsString(pointer);
            Marshal.Copy(buffer, ret, 0, ret.Length);
            return ret;
        }

        internal static string GetValueInternal(IntPtr pointer)
        {
            if (PyObject_IsInstance(pointer, Python.Type.Unicode) == 0)
                return Encoding.ASCII.GetString(GetRawInternal(pointer));

            var mem = PyUnicodeUCS2_AsEncodedString(pointer, "utf-8", "ignore");
            if (mem == IntPtr.Zero)
                return "";
            var res = Marshal.PtrToStringAnsi(mem);
            Python.Py_DecRef(mem);
            return res;
        }

    }

}