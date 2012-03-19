using System;
using beliEVE;

namespace DestinyTool
{

    public class DestinyToolModule : NativeModule
    {
        private PyObject _state;

        public byte[] State
        {
            set
            {
                _state = new PyString(value);
            }
        }

        public PyObject Ballpark { get; set; }

        public DestinyToolModule()
            : base("bEdestiny")
        {
            AddMethod("SetBallpark", HandleSetBallpark);
            AddMethod("GetStateData", HandleGetStateData);
            AddMethod("GetBallpark", HandleGetBallpark);
            Initialize();
        }

        private IntPtr HandleGetBallpark(IntPtr self, IntPtr args)
        {
            Ballpark.IncRef();
            return Ballpark.Pointer;
        }

        private IntPtr HandleGetStateData(IntPtr self, IntPtr args)
        {
            if (_state != null)
            {
                _state.IncRef();
                return _state.Pointer;
            }

            return PyObject.None;
        }

        private IntPtr HandleSetBallpark(IntPtr self, IntPtr args)
        {
            var tup = new PyTuple(args);
            if (tup.IsInvalid || tup.Size <= 0)
                return PyObject.None;

            Ballpark = tup[0];
            Ballpark.IncRef();
            Core.Log(LogSeverity.Minor, "received ballpark: " + Ballpark);

            return PyObject.None;
        }
    }

}