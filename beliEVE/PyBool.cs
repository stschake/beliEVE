using System;
using System.Runtime.InteropServices;

namespace beliEVE
{

    public class PyBool : PyObject
    {
        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyBool_FromLong(int value);

        public PyBool(IntPtr ptr)
            : base(ptr)
        {
            
        }

        public PyBool(bool value)
            : base(value ? True.Pointer : False.Pointer)
        {
            
        }

        public bool Value
        {
            get { return Pointer == _true.Pointer; }
        }

        /// <summary>
        /// Every sane Python implementation will use two interned objects
        /// to represent a boolean, not keep creating new objects.
        /// </summary>

        private static PyObject _true;
        public static PyObject True
        {
            get
            {
                if (_true == null)
                    _true = new PyObject(PyBool_FromLong(1));
                _true.IncRef();
                return _true;
            }
        }

        private static PyObject _false;
        public static PyObject False
        {
            get
            {
                if (_false == null)
                    _false = new PyObject(PyBool_FromLong(0));
                _false.IncRef();
                return _false;
            }
        }
    }

}