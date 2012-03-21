using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace beliEVE
{

    [Flags]
    public enum MethodFlag
    {
        OldArgs,
        VarArgs = 1,
        Keywords = 2,
        NoArgs = 4,
        SingleObject = 8
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct PyMethodDef
    {
        [FieldOffset(0x00)]
        [MarshalAs(UnmanagedType.LPStr)]
        public string Name;
        [FieldOffset(0x04)]
        [MarshalAs(UnmanagedType.FunctionPtr)]
        public Python.CFunction Method;
        [FieldOffset(0x08)]
        [MarshalAs(UnmanagedType.U4)]
        public MethodFlag Flags;
        [FieldOffset(0x0C)]
        [MarshalAs(UnmanagedType.LPStr)]
        public string Documentation;
    }

    public class CompileException : Exception
    {
        public string Code { get; private set; }

        public CompileException(string code)
        {
            Code = code;
        }
    }

    public delegate void RunFunction();

    public static class Python
    {
        public const string DLL = "python27.dll";

        // used for keeping references to various objects that may only exist in unmanaged space
        private static readonly List<object> RefKeeper = new List<object>(10);

        private static uint _pyCompileStringFlagsOff;
        private static IntPtr _verboseFlag;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate IntPtr CFunction(IntPtr self, IntPtr args);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr Py_InitModule4(string name, IntPtr methods, string doc, IntPtr self, int apiver);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate IntPtr PyCompileStringFlagsDel(
            [MarshalAs(UnmanagedType.LPStr)] string str, [MarshalAs(UnmanagedType.LPStr)] string filename, int tokenizerFlags,
            IntPtr flags);
        private static PyCompileStringFlagsDel _pyCompileStringFlags;

        public static IntPtr Library { get; private set; }
        public static bool RunSupport { get { return _pyCompileStringFlagsOff != 0; } }

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, EntryPoint = "PyImport_ImportModule")]
        private static extern IntPtr ImportModule([MarshalAs(UnmanagedType.LPStr)] string module);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "PyModule_GetDict")]
        public static extern IntPtr GetModuleDict(IntPtr module);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "PyInt_AsLong")]
        public static extern int IntAsLong(IntPtr obj);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "PyEval_AcquireLock")]
        public static extern void EvalAcquireLock();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "PyEval_ReleaseLock")]
        public static extern void EvalReleaseLock();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "PyGILState_Ensure")]
        public static extern IntPtr GILStateEnsure();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "PyGILState_Release")]
        public static extern void GILStateRelease(IntPtr state);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyErr_Occurred();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyErr_Print();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyErr_Fetch(out IntPtr type, out IntPtr value, out IntPtr traceback);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void PyErr_Restore(IntPtr type, IntPtr value, IntPtr traceback);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Py_IsInitialized")]
        public static extern bool IsInitialized();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyModule_GetDict(IntPtr module);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyImport_AddModule([MarshalAs(UnmanagedType.LPStr)] string name);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyEval_EvalCode(IntPtr obj, IntPtr globals, IntPtr locals);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "Py_AddPendingCall")]
        public static extern bool AddPendingCall(IntPtr func, IntPtr arg);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Py_DecRef(IntPtr obj);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Py_IncRef(IntPtr obj);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public delegate int PendingCallFunc(IntPtr arg);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern int PyDict_SetItemString(IntPtr obj, [MarshalAs(UnmanagedType.LPStr)] string key, IntPtr val);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr PyEval_GetBuiltins();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, EntryPoint = "PyErr_Clear")]
        public static extern void ClearError();

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr PyObject_Repr(IntPtr obj);

        [DllImport(DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern IntPtr PyString_AsString(IntPtr obj);

        public static bool Verbose
        {
            get
            {
                if (_verboseFlag != IntPtr.Zero)
                    return Marshal.ReadInt32(_verboseFlag) != 0;
                return false;
            }
            set
            {
                if (_verboseFlag != IntPtr.Zero)
                    Marshal.WriteInt32(_verboseFlag, value ? 1 : 0);
            }
        }

        public static IntPtr ImportRaw(string module)
        {
            return ImportModule(module);
        }
        
        public static PyObject Import(string module)
        {
            var p = ImportRaw(module);
            if (p == IntPtr.Zero)
                return null;
            return new PyObject(p);
        }

        public static bool ErrorSet
        {
            get
            {
                return PyErr_Occurred() != IntPtr.Zero;
            }
        }

        public static PyException GetError()
        {
            if (!ErrorSet)
                return null;

            IntPtr type, value, traceback;
            PyErr_Fetch(out type, out value, out traceback);
            PyErr_Restore(type, value, traceback);
            return new PyException(type, value, traceback);
        }

        public static IntPtr InitModule(string name, PyMethodDef[] methods)
        {
            // unlikely to change since Python27 has entered basic mainteneance only
            const int apiVersion = 1013;

            if (string.IsNullOrEmpty(name) || methods == null || methods.Length == 0)
                throw new InvalidDataException("Invalid parameters for InitModule");

            RefKeeper.Add(methods);

            int stride = Marshal.SizeOf(typeof (PyMethodDef));
            var mem = Marshal.AllocHGlobal(stride*(methods.Length + 1)).ToInt32();
            for (int i = 0; i < methods.Length; i++)
                Marshal.StructureToPtr(methods[i], new IntPtr(mem + (stride * i)), false);
            for (int i = 0; i < stride; i++)
                Marshal.WriteByte(new IntPtr(mem), i + (methods.Length * stride), 0);

            // no idea if we can free the memory - just leak it..

            return Py_InitModule4(name, new IntPtr(mem), null, IntPtr.Zero, apiVersion);
        }

        public static string Repr(IntPtr obj)
        {
            if (obj == IntPtr.Zero)
                return "0";
            var rep = PyObject_Repr(obj);
            if (rep == IntPtr.Zero)
                return "0";
            var str = PyString_AsString(rep);
            if (str == IntPtr.Zero)
                return "0";
            return Marshal.PtrToStringAnsi(str);
        }

        public static void PrintError()
        {
            PyErr_Print();
        }
        
        public static void Initialize()
        {
            Library = Native.LoadLibrary("python27.dll");
            _verboseFlag = Native.GetProcAddress(Library, "Py_VerboseFlag");
            _pyCompileStringFlagsOff = Utility.FindPattern("python27",
                                                        "55 8b ec 53 8b 5d 14 56 e8 ? ? ? ? 8b f0 85 f6 75 ? eb ? 85 db 74 ? f7 03 00 04 00 00 74 ? e8 ? ? ? ? 57 e8 ? ? ? ? 59");
            if (_pyCompileStringFlagsOff == 0)
            {
                Core.LogWarning(
                    "Warning: couldn't find Py_CompileStringFlags function in python27 module. Outdated pattern?");
            }
            else
                _pyCompileStringFlags = Utility.Magic.RegisterDelegate<PyCompileStringFlagsDel>(_pyCompileStringFlagsOff);
        }

        public static IntPtr Compile(string module, string code)
        {
            var codeObject = _pyCompileStringFlags(code, module, 257, IntPtr.Zero);
            return codeObject;
        }

        public static void Run(RunFunction func)
        {
            var cb = new CodeBite(func);
            cb.Run();
        }

        public static IntPtr Run(string code)
        {
            var cb = new CodeBite(code);
            return cb.Run();
        }

        public static IntPtr RunPure(string code)
        {
            var main = PyImport_AddModule("__main__");
            var mainDict = PyModule_GetDict(main);
            var codeObject = _pyCompileStringFlags(code, "beliEVE", 257, IntPtr.Zero);
            if (codeObject == IntPtr.Zero)
                throw new CompileException(code);
            var res = PyEval_EvalCode(codeObject, mainDict, mainDict);
            Py_DecRef(codeObject);
            return res;
        }

        public static void RunAsTasklet(string code)
        {
            var builder = new StringBuilder(100);
            builder.Append("import uthread\nimport sys\ndef fun():\n\ttry:\n\t\t");
            builder.Append(code.Replace("\n", "\n\t\t"));
            builder.Append("\n\texcept:\n\t\tpass\nuthread.new(fun)\n");
            Run(builder.ToString());
        }

        public static class Type
        {
            private static IntPtr _int, _long, _float, _bool, _dict, _list, _tuple, _string, _unicode;

            public static IntPtr Int
            {
                get { return _int == IntPtr.Zero ? (_int = Native.GetProcAddress(Library, "PyInt_Type")) : _int; }
            }

            public static IntPtr Long
            {
                get { return _long == IntPtr.Zero ? (_long = Native.GetProcAddress(Library, "PyLong_Type")) : _long; }
            }

            public static IntPtr Float
            {
                get { return _float == IntPtr.Zero ? (_float = Native.GetProcAddress(Library, "PyFloat_Type")) : _float; }
            }

            public static IntPtr Bool
            {
                get { return _bool == IntPtr.Zero ? (_bool = Native.GetProcAddress(Library, "PyBool_Type")) : _bool; }
            }

            public static IntPtr Dict
            {
                get { return _dict == IntPtr.Zero ? (_dict = Native.GetProcAddress(Library, "PyDict_Type")) : _dict; }
            }

            public static IntPtr List
            {
                get { return _list == IntPtr.Zero ? (_list = Native.GetProcAddress(Library, "PyList_Type")) : _list; }
            }

            public static IntPtr Tuple
            {
                get { return _tuple == IntPtr.Zero ? (_tuple = Native.GetProcAddress(Library, "PyTuple_Type")) : _tuple; }
            }

            public static IntPtr String
            {
                get { return _string == IntPtr.Zero ? (_string = Native.GetProcAddress(Library, "PyString_Type")) : _string; }
            }

            public static IntPtr Unicode
            {
                get
                {
                    return _unicode == IntPtr.Zero
                               ? (_unicode = Native.GetProcAddress(Library, "PyUnicode_Type"))
                               : _unicode;
                }
            }
        }

    }

}