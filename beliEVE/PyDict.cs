using System;
using System.Runtime.InteropServices;

namespace beliEVE
{

    public class PyDict : PyObject
    {
        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyDict_New();

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern void PyDict_Clear(IntPtr obj);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PyDict_Contains(IntPtr obj, IntPtr key);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyDict_Copy(IntPtr obj);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PyDict_DelItem(IntPtr obj, IntPtr key);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int PyDict_DelItemString(IntPtr obj, string key);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyDict_Keys(IntPtr obj);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyDict_Values(IntPtr obj);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyDict_GetItem(IntPtr obj, IntPtr key);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PyDict_SetItem(IntPtr obj, IntPtr key, IntPtr value);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr PyDict_GetItemString(IntPtr obj, string key);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int PyDict_SetItemString(IntPtr obj, string key, IntPtr value);

        public PyDict(IntPtr obj)
            : base(obj)
        {
            
        }

        public PyDict()
            : base(PyDict_New())
        {
            
        }

        public PyList Values
        {
            get
            {
                if (IsInvalid)
                    return null;
                return new PyList(PyDict_Values(Pointer));
            }
        }

        public PyList Keys
        {
            get
            {
                if (IsInvalid)
                    return null;
                return new PyList(PyDict_Keys(Pointer));
            }
        }

        public bool Delete(string key)
        {
            if (IsInvalid || string.IsNullOrEmpty(key))
                return false;
            return PyDict_DelItemString(Pointer, key) == 0;
        }

        public bool Delete(PyObject key)
        {
            return Delete(key.Pointer);
        }

        public bool Delete(IntPtr key)
        {
            if (IsInvalid || key == IntPtr.Zero)
                return false;
            return PyDict_DelItem(Pointer, key) == 0;
        }

        public bool ContainsKey(PyObject key)
        {
            return ContainsKey(key.Pointer);
        }

        public bool ContainsKey(IntPtr key)
        {
            if (IsInvalid || key == IntPtr.Zero)
                return false;
            return PyDict_Contains(Pointer, key) == 1;
        }

        public void Clear()
        {
            if (IsInvalid)
                return;
            PyDict_Clear(Pointer);
        }

        public PyDict Copy()
        {
            if (IsInvalid)
                return null;
            return new PyDict(PyDict_Copy(Pointer));
        }

        public override PyObject this[PyObject key]
        {
            get
            {
                if (IsInvalid || key.IsInvalid)
                    return null;
                return new PyObject(PyDict_GetItem(Pointer, key.Pointer), ReferenceType.Borrowed);
            }
            set
            {
                if (IsInvalid || key.IsInvalid || value.IsInvalid)
                    return;
                PyDict_SetItem(Pointer, key.Pointer, value.Pointer);
            }
        }

        public override PyObject this[string key]
        {
            get
            {
                if (IsInvalid || string.IsNullOrEmpty(key))
                    return null;
                return new PyObject(PyDict_GetItemString(Pointer, key), ReferenceType.Borrowed);
            }
            set
            {
                if (IsInvalid || value.IsInvalid || string.IsNullOrEmpty(key))
                    return;
                PyDict_SetItemString(Pointer, key, value.Pointer);
            }
        }

    }

}