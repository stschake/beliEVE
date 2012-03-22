using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace beliEVE
{
    public enum ReferenceType
    {
        New,
        Borrowed
    }

    public class PyObject : IEnumerable<PyObject>
    {
        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PyObject_Hash(IntPtr obj);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PyObject_Length(IntPtr obj);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int PyObject_HasAttrString(IntPtr obj, string attr);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr PyObject_GetAttrString(IntPtr obj, string attr);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PyObject_RichCompareBool(IntPtr a, IntPtr b, CompareOp op);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PyCallable_Check(IntPtr obj);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyObject_GetItem(IntPtr obj, IntPtr key);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PyObject_SetItem(IntPtr obj, IntPtr key, IntPtr value);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern int PyObject_IsTrue(IntPtr obj);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int PyObject_IsInstance(IntPtr obj, IntPtr type);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr PyObject_GetIter(IntPtr obj);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr PyIter_Next(IntPtr iter);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyObject_Dir(IntPtr obj);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyObject_CallObject(IntPtr obj, IntPtr argsTuple);

        [DllImport(Python.DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyObject_Call(IntPtr obj, IntPtr args, IntPtr kw);
        
        public IntPtr Pointer { get; protected set; }
        public int References { get { return IsInvalid ? 0 : Marshal.ReadInt32(Pointer); } }
        public ReferenceType ReferenceType { get; internal set; }

        public PyObject(IntPtr ptr)
            : this(ptr, ReferenceType.New)
        {
        }

        public PyObject(IntPtr ptr, ReferenceType refType)
        {
            Pointer = ptr;
            ReferenceType = refType;
        }

        ~PyObject()
        {
            if (IsValid && ReferenceType == ReferenceType.New)
                DecRef();
        }

        public PyObject CallKeywords(PyTuple args, PyTuple kws)
        {
            if (IsInvalid || !IsCallable)
                return null;

            var ret =
                PyObject_Call(Pointer, args != null ? args.Pointer : IntPtr.Zero,
                              kws != null ? kws.Pointer : IntPtr.Zero);
            return GetSpecialized(ret);
        }

        public PyObject Call(params PyObject[] args)
        {
            if (IsInvalid || args == null)
                return null;
            var tuple = new PyTuple(args);
            if (tuple.IsInvalid)
                return null;
            if (!IsCallable)
                return null;
            var ret = new PyObject(PyObject_CallObject(Pointer, tuple.Pointer));
            tuple.DecRef();
            return ret;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual PyObject this[string key]
        {
            get
            {
                if (IsInvalid || string.IsNullOrEmpty(key))
                    return null;
                var k = PyString.PyString_FromString(key);
                if (k == IntPtr.Zero)
                    return null;
                var ret = PyObject_GetItem(Pointer, k);
                var obj = GetSpecialized(ret);
                obj.ReferenceType = ReferenceType.Borrowed;
                return obj;
            }
            set
            {
                if (IsInvalid || value.IsInvalid || string.IsNullOrEmpty(key))
                    return;
                var k = PyString.PyString_FromString(key);
                if (k == IntPtr.Zero)
                    return;
                value.IncRef();
                PyObject_SetItem(Pointer, k, value.Pointer);
            }
        }

        public virtual PyObject this[int key]
        {
            get
            {
                if (IsInvalid)
                    return null;
                var k = PyInt.PyInt_FromLong(key);
                var ret = PyObject_GetItem(Pointer, k);
                var obj = GetSpecialized(ret);
                obj.ReferenceType = ReferenceType.Borrowed;
                return obj;
            }
            set
            {
                if (IsInvalid || value.IsInvalid)
                    return;
                var k = PyInt.PyInt_FromLong(key);
                value.IncRef();
                PyObject_SetItem(Pointer, k, value.Pointer);
            }
        }

        public virtual PyObject this[PyObject key]
        {
            get
            {
                if (IsInvalid || key.IsInvalid)
                    return null;
                var ptr = PyObject_GetItem(Pointer, key.Pointer);
                if (ptr == IntPtr.Zero)
                    return null;
                var obj = GetSpecialized(ptr);
                obj.ReferenceType = ReferenceType.Borrowed;
                return obj;
            }
            set
            {
                if (IsInvalid || key.IsInvalid || value.IsInvalid)
                    return;
                key.IncRef();
                value.IncRef();
                PyObject_SetItem(Pointer, key.Pointer, value.Pointer);
            }
        }

        public bool IsValid { get { return !IsInvalid; } }
        public bool IsInvalid { get { return Pointer == IntPtr.Zero; } }
        public bool IsCallable { get { return !IsInvalid && PyCallable_Check(Pointer) == 1; } }
        public bool IsTrue { get { return !IsInvalid && PyObject_IsTrue(Pointer) == 1; } }

        public bool IsUnicode { get { return !IsInvalid && PyObject_IsInstance(Pointer, Python.Type.Unicode) == 1; } }
        public bool IsString { get { return !IsInvalid && PyObject_IsInstance(Pointer, Python.Type.String) == 1; } }
        public bool IsBool { get { return !IsInvalid && PyObject_IsInstance(Pointer, Python.Type.Bool) == 1; } }
        public bool IsInt { get { return !IsInvalid && PyObject_IsInstance(Pointer, Python.Type.Int) == 1; } }
        public bool IsFloat { get { return !IsInvalid && PyObject_IsInstance(Pointer, Python.Type.Float) == 1; } }
        public bool IsDict { get { return !IsInvalid && PyObject_IsInstance(Pointer, Python.Type.Dict) == 1; } }
        public bool IsList { get { return !IsInvalid && PyObject_IsInstance(Pointer, Python.Type.List) == 1; } }
        public bool IsTuple { get { return !IsInvalid && PyObject_IsInstance(Pointer, Python.Type.Tuple) == 1; } }
        public bool IsLong { get { return !IsInvalid && PyObject_IsInstance(Pointer, Python.Type.Long) == 1; } }

        public void IncRef()
        {
            if (!IsInvalid)
                Python.Py_IncRef(Pointer);
        }

        public void DecRef()
        {
            var oldRef = References;
            if (!IsInvalid)
                Python.Py_DecRef(Pointer);
            if (oldRef == 1)
                Pointer = IntPtr.Zero;
        }

        public bool HasAttr(string name)
        {
            return PyObject_HasAttrString(Pointer, name) == 1;
        }

        public PyObject Get(string attr)
        {
            if (IsInvalid)
                return null;
            var p = GetAttr(attr);
            if (p == IntPtr.Zero)
                return null;
            return GetSpecialized(p);
        }

        public int GetInt(string attr)
        {
            var p = GetAttr(attr);
            if (p == IntPtr.Zero)
                return -1;
            return PyInt.GetValueInternal(p);
        }

        public string GetString(string attr)
        {
            var p = GetAttr(attr);
            if (p == IntPtr.Zero)
                return "";
            return PyString.GetValueInternal(p);
        }

        public byte[] GetRaw(string attr)
        {
            var p = GetAttr(attr);
            if (p == IntPtr.Zero)
                return null;
            return PyString.GetRawInternal(p);
        }

        public long GetLong(string attr)
        {
            var p = GetAttr(attr);
            if (p == IntPtr.Zero)
                return -1;
            return PyLong.GetValueInternal(p);
        }

        public double GetFloat(string attr)
        {
            var p = GetAttr(attr);
            if (p == IntPtr.Zero)
                return double.NaN;
            return PyFloat.GetValueInternal(p);
        }

        public IntPtr GetAttr(string name)
        {
            if (string.IsNullOrEmpty(name))
                return IntPtr.Zero;
            return PyObject_GetAttrString(Pointer, name);
        }

        public int Size { get { return IsValid ? PyObject_Length(Pointer) : -1; } }
        public int Count { get { return Size; } }
        public int Length { get { return Size; } }

        public IEnumerator<PyObject> GetEnumerator()
        {
            if (IsInvalid)
                yield break;

            var iter = PyObject_GetIter(Pointer);
            if (iter == IntPtr.Zero)
                yield break;
            
            if (Python.ErrorSet)
            {
                Python.ClearError();
                yield break;
            }

            IntPtr p = PyIter_Next(iter);
            while (p != IntPtr.Zero)
            {
                yield return GetSpecialized(p);

                p = PyIter_Next(iter);
            }
        }

        public string[] Directory
        {
            get
            {
                if (IsInvalid)
                    return null;

                var dir = new PyObject(PyObject_Dir(Pointer));
                var n = dir.Count;
                var ret = new string[n];
                int i = 0;
                foreach (var obj in dir)
                {
                    if (i >= n)
                        break;
                    if (obj is PyString)
                        ret[i++] = (obj as PyString).Value;
                }
                return ret;
            }
        }

        public override string ToString()
        {
            return Python.Repr(Pointer);
        }

        public override int GetHashCode()
        {
            return PyObject_Hash(Pointer);
        }

        private enum CompareOp
        {
            Less = 0,
            LessEqual,
            Equal,
            Greater = 4,
            GreaterEqual
        }

        public static bool operator <(PyObject a, PyObject b)
        {
            return !a.IsInvalid && !b.IsInvalid && PyObject_RichCompareBool(a.Pointer, b.Pointer, CompareOp.Less) == 1;
        }

        public static bool operator >(PyObject a, PyObject b)
        {
            return !a.IsInvalid && !b.IsInvalid && PyObject_RichCompareBool(a.Pointer, b.Pointer, CompareOp.Greater) == 1;
        }

        public static bool operator <=(PyObject a, PyObject b)
        {
            return !a.IsInvalid && (a.Equals(b) || PyObject_RichCompareBool(a.Pointer, b.Pointer, CompareOp.LessEqual) == 1);
        }

        public static bool operator >=(PyObject a, PyObject b)
        {
            return !a.IsInvalid && (a.Equals(b) || PyObject_RichCompareBool(a.Pointer, b.Pointer, CompareOp.GreaterEqual) == 1);
        }

        public static bool operator ==(PyObject a, PyObject b)
        {
            if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
                return true;
            if (!ReferenceEquals(a, null) && !ReferenceEquals(b, null) && a.Pointer == b.Pointer)
                return true;
            return Equals(a, b);
        }

        public static bool operator !=(PyObject a, PyObject b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || (!obj.GetType().IsSubclassOf(GetType()) && GetType() != obj.GetType()))
                return false;

            return PyObject_RichCompareBool(Pointer, ((PyObject) obj).Pointer, CompareOp.Equal) == 1;
        }

        public bool IsNone
        {
            get
            {
                if (_none == IntPtr.Zero)
                    _none = Native.GetProcAddress(Python.Library, "_Py_NoneStruct");
                return Pointer == _none;
            }
        }

        private static IntPtr _none;
        public static IntPtr None
        {
            get
            {
                if (_none == IntPtr.Zero)
                    _none = Native.GetProcAddress(Python.Library, "_Py_NoneStruct");
                Python.Py_IncRef(_none);
                return _none;
            }
        }

        private static IntPtr _zero;
        public static IntPtr Zero
        {
            get
            {
                if (_zero == IntPtr.Zero)
                    _zero = Native.GetProcAddress(Python.Library, "_Py_ZeroStruct");
                Python.Py_IncRef(_zero);
                return _zero;
            }
        }

        private static IntPtr _notImplemented;
        public static IntPtr NotImplemented
        {
            get
            {
                if (_notImplemented == IntPtr.Zero)
                    _notImplemented = Native.GetProcAddress(Python.Library, "_Py_NotImplementedStruct");
                Python.Py_IncRef(_notImplemented);
                return _notImplemented;
            }
        }

        internal static PyObject GetSpecialized(IntPtr obj)
        {
            if (PyObject_IsInstance(obj, Python.Type.String) == 1
                || PyObject_IsInstance(obj, Python.Type.Unicode) == 1)
                return new PyString(obj);
            if (PyObject_IsInstance(obj, Python.Type.Tuple) == 1)
                return new PyTuple(obj);
            if (PyObject_IsInstance(obj, Python.Type.Float) == 1)
                return new PyFloat(obj);
            if (PyObject_IsInstance(obj, Python.Type.Int) == 1)
                return new PyInt(obj);
            if (PyObject_IsInstance(obj, Python.Type.Dict) == 1)
                return new PyDict(obj);
            if (PyObject_IsInstance(obj, Python.Type.List) == 1)
                return new PyList(obj);
            if (PyObject_IsInstance(obj, Python.Type.Long) == 1)
                return new PyLong(obj);
            if (PyObject_IsInstance(obj, Python.Type.Bool) == 1)
                return new PyBool(obj);

            // no specialized type available
            return new PyObject(obj);
        }
    }

}
