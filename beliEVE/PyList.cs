using System;
using System.Runtime.InteropServices;

namespace beliEVE
{

    public class PyList : PyObject
    {
        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyList_New(int size);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyList_GetItem(IntPtr obj, int index);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PyList_SetItem(IntPtr obj, int index, IntPtr value);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PyList_Sort(IntPtr obj);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PyList_Reverse(IntPtr obj);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyList_AsTuple(IntPtr obj);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PyList_Append(IntPtr obj, IntPtr item);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PyList_Insert(IntPtr obj, int index, IntPtr item);
        
        public PyList(IntPtr obj)
            : base(obj)
        {
            
        }

        public PyList(int size)
            : base(PyList_New(size))
        {
            
        }

        public PyList(params PyObject[] args)
            : base(PyList_New(args.Length))
        {
            // PyList_New failed to allocate list
            if (IsInvalid)
                return;
            for (int i = 0; i < args.Length; i++)
                this[i] = args[i];
        }

        public void Sort()
        {
            if (IsInvalid)
                return;
            PyList_Sort(Pointer);
        }

        public void Reverse()
        {
            if (IsInvalid)
                return;
            PyList_Reverse(Pointer);
        }

        public PyTuple AsTuple()
        {
            if (IsInvalid)
                return null;
            return new PyTuple(PyList_AsTuple(Pointer));
        }

        public bool Insert(int index, PyObject item)
        {
            return Insert(index, item.Pointer);
        }

        public bool Insert(int index, IntPtr item)
        {
            if (IsInvalid || item == IntPtr.Zero || index < 0)
                return false;
            var r = PyList_Insert(Pointer, index, item);
            if (r == -1)
            {
                Python.ClearError();
                return false;
            }
            return true;
        }

        public bool Append(IntPtr item)
        {
            if (IsInvalid || item == IntPtr.Zero)
                return false;
            var r = PyList_Append(Pointer, item);
            if (r == -1)
            {
                Python.ClearError();
                return false;
            }
            return true;
        }

        public bool Append(PyObject item)
        {
            return Append(item.Pointer);
        }

        public override PyObject this[int key]
        {
            get
            {
                if (IsInvalid || key < 0)
                    return null;
                return new PyObject(PyList_GetItem(Pointer, key), ReferenceType.Borrowed);
            }
            set
            {
                if (IsInvalid || key < 0 || value.IsInvalid)
                    return;
                if (PyList_SetItem(Pointer, key, value.Pointer) != 0)
                    Python.ClearError();
            }
        }

        public override PyObject this[PyObject key]
        {
            get
            {
                if (key is PyInt)
                    return this[(key as PyInt).Value];
                throw new InvalidOperationException("Can't index into a list with anything but an int");
            }
            set
            {
                if (key is PyInt)
                    this[(key as PyInt).Value] = value;
                else
                    throw new InvalidOperationException("Can't index into a list with anything but an int");
            }
        }

        public override PyObject this[string key]
        {
            get
            {
                throw new InvalidOperationException("Can't use a string to index into a PyList");
            }
            set
            {
                throw new InvalidOperationException("Can't use a string to index into a PyList");
            }
        }

    }

}