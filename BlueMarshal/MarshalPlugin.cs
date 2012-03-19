using System.IO;
using System.Text;
using beliEVE;

namespace BlueMarshal
{

    public class MarshalPlugin : IPlugin
    {
        public PyObject Marshal;
        public PyObject LoadFunc;

        public string Name
        {
            get { return "Blue Marshal Plugin"; }
        }

        public string ShortName
        {
            get { return "blue"; }
        }

        public string Description
        {
            get { return "Provides access to the official blue marshaler"; }
        }

        public int Revision
        {
            get { return 1; }
        }

        public PluginType Type
        {
            get { return PluginType.Generic; }
        }

        public void SetInterface(PluginInterface iface)
        {
        }

        public bool Initialize(LaunchStage stage)
        {
            if (stage == LaunchStage.PostBlue)
            {
                Marshal = Python.Import("blue").Get("marshal");
                LoadFunc = Marshal.Get("Load");
                Marshal.IncRef();
                LoadFunc.IncRef();
                if (Marshal.IsValid && LoadFunc.IsValid)
                    Core.Log(LogSeverity.Minor, "initialized blue marshal functions");
            }

            return true;
        }

        private PyObject Load(byte[] data)
        {
            var s = new PyString(data);
            var ret = LoadFunc.Call(s);
            return ret;
        }

        private void Print(PyObject data, StringBuilder ret, int indentment = 0)
        {
            string cls = "unknown";
            if (data.HasAttr("__class__"))
            {
                var clsobj = data.Get("__class__");
                cls = clsobj.GetString(clsobj.HasAttr("__guid__") ? "__guid__" : "__name__");
            }
            
            // TODO: print value
            ret.AppendLine(new string(' ', indentment*2) + "[" + cls + "]");

            if (data.HasAttr("__getstate__"))
            {
                var state = data.Get("__getstate__").Call();
                foreach (var obj in state)
                    Print(obj, ret, indentment+1);
            }
            else if (data.IsTuple || data.IsDict)
            {
                foreach (var obj in data)
                    Print(obj, ret, indentment + 1);
            }
            else if (cls == "MarshalStream" && data.HasAttr("Str"))
            {
                var str = data.Get("Str").Call();
                var unm = LoadFunc.Call(str);
                if (unm.IsValid)
                    Print(unm, ret, indentment+1);
            }
        }

        public void ProcessCommand(string command)
        {
            if (command.StartsWith("load "))
            {
                var path = command.Substring(5);
                var data = File.ReadAllBytes(path);
                PyObject ret = null;
                Python.Run(() => ret = Load(data));
                var builder = new StringBuilder();
                Python.Run(() => Print(ret, builder));
                Core.Log(LogSeverity.Minor, "result:\r\n" + builder);
            }
        }
    }

}