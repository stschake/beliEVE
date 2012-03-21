using System;

namespace beliEVE
{

    public class PyException : Exception
    {
        public PyObject TypeObject { get; private set; }
        public PyObject ValueObject { get; private set; }
        public PyObject TracebackObject { get; private set; }

        public string Type { get; private set; }
        public string Value { get; private set; }

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

            // TODO: extract frames data into suitable c# structures
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