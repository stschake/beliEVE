using System;
using System.Runtime.InteropServices;

namespace beliEVE
{

    public class PyInt : PyObject
    {
        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PyInt_AsLong(IntPtr obj);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr PyInt_FromLong(int value);

        public PyInt(IntPtr ptr)
            : base(ptr)
        {
            
        }

        public PyInt(int value)
            : base(PyInt_FromLong(value))
        {
            
        }

        public int Value
        {
            get { return (IsInvalid ? -1 : GetValueInternal(Pointer)); }
        }

        internal static int GetValueInternal(IntPtr pointer)
        {
            return PyInt_AsLong(pointer);
        }

    }

}