using System;
using System.Runtime.InteropServices;

namespace beliEVE
{

    public class PyLong : PyObject
    {
        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern long PyLong_AsLongLongAndOverflow(IntPtr obj, out bool overflow);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern ulong PyLong_AsUnsignedLongLong(IntPtr obj);

        public PyLong(IntPtr obj)
            : base(obj)
        {
            if (!IsInvalid)
            {
                bool isUnsigned;
                PyLong_AsLongLongAndOverflow(Pointer, out isUnsigned);
                IsUnsigned = isUnsigned;
            }
        }

        public bool IsUnsigned { get; private set; }

        public long Value
        {
            get
            {
                if (IsInvalid)
                    return 0;

                return GetValueInternal(Pointer);
            }
        }

        public ulong Unsigned
        {
            get
            {
                if (IsInvalid)
                    return 0;

                return PyLong_AsUnsignedLongLong(Pointer);
            }
        }

        internal static long GetValueInternal(IntPtr pointer)
        {
            bool ignore;
            return PyLong_AsLongLongAndOverflow(pointer, out ignore);
        }
    }

}