using System;
using System.Runtime.InteropServices;

namespace beliEVE
{

    public class PyTuple : PyObject
    {
        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyTuple_New(int size);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PyTuple_SetItem(IntPtr obj, int index, IntPtr value);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyTuple_GetItem(IntPtr obj, int index);

        public PyTuple(IntPtr obj)
            : base(obj)
        {
            
        }

        public PyTuple(int size)
            : base(PyTuple_New(size))
        {
            
        }

        public PyTuple(params PyObject[] args)
            : base(PyTuple_New(args.Length))
        {
            // PyTuple_New failed to allocate tuple
            if (IsInvalid)
                return;
            for (int i = 0; i < args.Length; i++)
                this[i] = args[i];
        }

        public override PyObject this[int key]
        {
            get
            {
                if (IsInvalid || key < 0)
                    return null;
                return new PyObject(PyTuple_GetItem(Pointer, key), ReferenceType.Borrowed);
            }
            set
            {
                if (IsInvalid || key < 0 || value.IsInvalid)
                    return;
                value.IncRef();
                if (PyTuple_SetItem(Pointer, key, value.Pointer) != 0)
                    Python.ClearError();
            }
        }

        public override PyObject this[string key]
        {
            get { throw new InvalidOperationException("Can't use a string to index into PyTuple"); }
            set { throw new InvalidOperationException("Can't use a string to index into PyTuple"); }
        }

        public override PyObject this[PyObject key]
        {
            get
            {
                if (key is PyInt)
                    return this[(key as PyInt).Value];
                throw new InvalidOperationException("Can't use anything but an int to index into PyTuple");
            }
            set
            {
                if (key is PyInt)
                    this[(key as PyInt).Value] = value;
                else
                    throw new InvalidOperationException("Can't unse anything but an int to index into PyTuple");
            }
        }
    }

}