using System;
using System.Collections.Generic;
using System.Linq;
using beliEVE.Constant;

namespace beliEVE
{

    public class PyBytecode
    {
        private readonly PyCode _code;

        public PyBytecode(PyCode code)
        {
            _code = code;
        }

        public string Format(int offset)
        {
            var raw = _code.RawBytecode;
            var op = raw[offset];
            int arg = -1;
            if (op >= 90)
                arg = BitConverter.ToUInt16(new[] {raw[offset + 1], raw[offset + 2]}, 0);
            var ret = GetOpMnemonic(op) + " " + (arg != -1 ? GetArgument((PyOpcode) op, arg) : "");
            return ret;
        }

        private string GetArgument(PyOpcode op, int arg)
        {
            var nameField = new[]
                                {
                                    PyOpcode.STORE_NAME, PyOpcode.DELETE_NAME, PyOpcode.STORE_ATTR, PyOpcode.DELETE_ATTR,
                                    PyOpcode.STORE_GLOBAL, PyOpcode.DELETE_GLOBAL, PyOpcode.LOAD_NAME, PyOpcode.LOAD_ATTR,
                                    PyOpcode.IMPORT_NAME, PyOpcode.IMPORT_FROM, PyOpcode.LOAD_GLOBAL
                                };
            var localField = new[] {PyOpcode.LOAD_FAST, PyOpcode.STORE_FAST, PyOpcode.DELETE_FAST};

            if (nameField.Contains(op))
                return _code.Names[arg].ToString();
            if (localField.Contains(op))
                return _code.Variables[arg].ToString();
            if (op == PyOpcode.LOAD_CONST)
                return _code.Consts[arg].ToString();
            if (op == PyOpcode.LOAD_CLOSURE)
                return _code.FreeVariables[arg].ToString();
            return arg.ToString();
        }

        private static string GetOpMnemonic(byte opcode)
        {
            if (opcode >= 30 && opcode <= 33)
                return PyOpcode.SLICE.ToString();
            if (opcode >= 40 && opcode <= 43)
                return PyOpcode.STORE_SLICE.ToString();
            if (opcode >= 50 && opcode <= 53)
                return PyOpcode.DELETE_SLICE.ToString();
            return ((PyOpcode)opcode).ToString();
        }
    }

    [Flags]
    public enum PyCodeFlag
    {
        Optimized = 1,
        NewLocals = 1 << 1,
        VarArgs = 1 << 2,
        VarKeywords = 1 << 3,
        Nested = 1 << 4,
        Generator = 1 << 5,
        NoFree = 1 << 6,

        FutDivision = 1 << 13,
        FutAbsImport = 1 << 14,
        FutWithStatement = 1 << 15,
        FutPrintFunc = 1 << 16,
        FutUnicodeLiterals = 1 << 17
    }

    public class PyCode
    {
        private readonly PyObject _code;
        
        public PyCodeFlag Flags
        {
            get { return (PyCodeFlag)_code.GetInt("co_flags"); }
        }

        public string File { get; private set; }
        public string Name { get; private set; }

        public PyBytecode Bytecode { get; private set; }
        internal byte[] RawBytecode { get; private set; }

        public int FirstLine
        {
            get { return _code.GetInt("co_firstlineno"); }
        }

        public int LocalsCount
        {
            get { return _code.GetInt("co_nlocals"); }
        }

        public int Arguments
        {
            get { return _code.GetInt("co_argcount"); }
        }

        public int StackSize
        {
            get { return _code.GetInt("co_stacksize"); }
        }

        private PyObject _consts;
        public PyObject Consts
        {
            get { return _consts ?? (_consts = _code.Get("co_consts")); }
        }

        private PyObject _names;
        public PyObject Names
        {
            get { return _names ?? (_names = _code.Get("co_names")); }
        }

        private PyObject _variables;
        public PyObject Variables
        {
            get { return _variables ?? (_variables = _code.Get("co_varnames")); }
        }

        private PyObject _freeVariables;
        public PyObject FreeVariables
        {
            get { return _freeVariables ?? (_freeVariables = _code.Get("co_freevars")); }
        }

        private PyObject _cellVariables;
        public PyObject CellVariables
        {
            get { return _cellVariables ?? (_cellVariables = _code.Get("co_cellvars")); }
        }

        public PyCode(PyObject code)
        {
            _code = code;
            code.IncRef();

            RawBytecode = code.GetRaw("co_code");
            Name = code.GetString("co_name");
            File = code.GetString("co_filename");
            Bytecode = new PyBytecode(this);
        }

        ~PyCode()
        {
            _code.DecRef();
        }
    }

    public class PyStackFrame
    {
        private readonly PyObject _frame;

        private PyCode _code;
        public PyCode Code
        {
            get { return _code ?? (_code = new PyCode(_frame.Get("f_code"))); }
        }

        private PyObject _builtins;
        public PyObject Builtins
        {
            get { return _builtins ?? (_builtins = _frame.Get("f_builtins")); }
        }

        private PyObject _globals;
        public PyObject Globals
        {
            get { return _globals ?? (_globals = _frame.Get("f_globals")); }
        }

        private PyObject _locals;
        public PyObject Locals
        {
            get { return _locals ?? (_locals = _frame.Get("f_locals")); }
        }

        public int LastInstruction
        {
            get { return _frame.GetInt("f_lasti"); }
        }

        internal PyStackFrame(PyObject frame)
        {
            _frame = frame;
            frame.IncRef();
        }

        ~PyStackFrame()
        {
            _frame.DecRef();
        }
    }

    public class PyTraceback
    {
        private readonly PyObject _trace;

        public int Line
        {
            get { return _trace.GetInt("tb_lineno"); }
        }

        public int LastInstruction
        {
            get { return _trace.GetInt("tb_lasti"); }
        }

        private PyStackFrame _frame;
        public PyStackFrame Frame
        {
            get { return _frame ?? (_frame = new PyStackFrame(_trace.Get("tb_frame"))); }
        }

        internal PyTraceback(PyObject trace)
        {
            _trace = trace;
            trace.IncRef();
        }

        ~PyTraceback()
        {
            _trace.DecRef();
        }
    }

    public class PyException : Exception
    {
        public PyObject TypeObject { get; private set; }
        public PyObject ValueObject { get; private set; }
        public PyObject TracebackObject { get; private set; }

        public string Type { get; private set; }
        public string Value { get; private set; }

        /// <summary>
        /// From caller to callee (where the exception occoured)
        /// </summary>
        public List<PyTraceback> CallStack { get; private set; }

        public PyException(IntPtr type, IntPtr value, IntPtr traceback)
        {
            TypeObject = new PyObject(type);
            ValueObject = new PyObject(value);
            TracebackObject = new PyObject(traceback);
            TypeObject.IncRef();
            ValueObject.IncRef();
            TracebackObject.IncRef();

            Type = TypeObject.GetString("__name__");
            if (ValueObject.IsString)
                Value = PyString.GetValueInternal(ValueObject.Pointer);
            else if (ValueObject.HasAttr("message"))
                Value = ValueObject.GetString("message");
            else // use Py_Repr to atleast get some information
                Value = ValueObject.ToString();

            CallStack = new List<PyTraceback>(5);
            var cur = TracebackObject;
            while (cur != null && cur.IsValid && !cur.IsNone)
            {
                CallStack.Add(new PyTraceback(cur));
                cur = cur.Get("tb_next");
            }
        }

        ~PyException()
        {
            TypeObject.DecRef();
            ValueObject.DecRef();
            TracebackObject.DecRef();
        }

        private string _message;
        public override string Message
        {
            get
            {
                if (_message != null)
                    return _message;
                _message = TypeObject + ": " + ValueObject + "\r\n\r\n" + TracebackObject;
                return _message;
            }
        }
    }

}