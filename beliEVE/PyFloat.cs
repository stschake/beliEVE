using System;
using System.Runtime.InteropServices;

namespace beliEVE
{

    public class PyFloat : PyObject
    {
        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyFloat_FromDouble(double value);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern double PyFloat_AsDouble(IntPtr obj);

        public PyFloat(IntPtr ptr)
            : base(ptr)
        {
            
        }

        public PyFloat(double value)
            : base (PyFloat_FromDouble(value))
        {
            
        }

        public double Value
        {
            get { return (IsInvalid ? double.NaN : GetValueInternal(Pointer)); }
        }

        internal static double GetValueInternal(IntPtr pointer)
        {
            return PyFloat_AsDouble(pointer);
        }
    }

}