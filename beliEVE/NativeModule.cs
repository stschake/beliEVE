using System;
using System.Collections.Generic;

namespace beliEVE
{

    public abstract class NativeModule
    {
        protected List<PyMethodDef> Methods = new List<PyMethodDef>();

        public bool Initialized { get; private set; }
        public string Name { get; private set; }
        public PyObject Module { get; protected set; }

        protected NativeModule(string name)
        {
            Name = name;
        }

        protected void Initialize()
        {
            if (!Initialized)
            {
                Module = new PyObject(Python.InitModule(Name, Methods.ToArray()), ReferenceType.Borrowed);
                Module.IncRef();
                Initialized = true;
            }
        }

        protected void AddMethod(string name, Python.CFunction function)
        {
            if (Initialized)
                throw new InvalidOperationException("Tried to add method after module was initialized");

            var def = new PyMethodDef {Name = name, Documentation = "", Flags = MethodFlag.VarArgs, Method = function};
            Methods.Add(def);
        }

    }

}