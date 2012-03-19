using System;
using System.IO;
using System.Text;
using beliEVE;

namespace DestinyTool
{

    public class DestinyToolPlugin : IPlugin
    {
        public DestinyToolModule Module { get; private set; }

        public string Name
        {
            get { return "destiny"; }
        }

        public string Description
        {
            get { return "Interfaces and analyzes destiny"; }
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

        public void TriggerReadState()
        {
            Core.Log(LogSeverity.Minor, "trying to trigger ReadFullStateFromStream..");
            const string code = "import destiny\n"
                                + "import blue\n"
                                + "import bluepy\n"
                                + "import bEdestiny\n"
                                + "park = bEdestiny.GetBallpark()\n"
                                + "ms = blue.MemStream()\n"
                                + "state = bEdestiny.GetStateData()\n"
                                + "ms.Write(state)\n"
                                + "park._parent_ClearAll()\n"
                                + "park._parent_ReadFullStateFromStream(ms)\n";
            Python.Run(code);
        }

        public void CreateBallpark()
        {
            Python.Run(
                "import destiny\nimport blue\nimport bluepy\nimport bEdestiny\nclass MyBallpark(bluepy.WrapBlueClass('destiny.Ballpark')):\n\tpass\n\nbp = MyBallpark()\nbEdestiny.SetBallpark(bp)\n");
        }

        public bool Initialize(LaunchStage stage)
        {
            if (stage == LaunchStage.PostBlue)
            {
                Module = new DestinyToolModule();
                Core.Log(LogSeverity.Minor, "initialized Destiny plugin module");
            }
            return true;
        }

        public void ProcessCommand(string command)
        {
            if (command == "create ballpark")
            {
                Core.Log(LogSeverity.Warning, "creating ballpark...");
                CreateBallpark();
            }
            else if (command == "enable hooks")
            {
                if (!StreamHooks.Initialized)
                    StreamHooks.Initialize();
                StreamHooks.Enable();
            }
            else if (command == "disable hooks")
            {
                StreamHooks.Disable();
            }
            else if (command == "trigger")
            {
                TriggerReadState();
            }
            else if (command.StartsWith("use state "))
            {
                var path = command.Substring(10);
                var data = File.ReadAllBytes(path);
                Module.State = data;
                Core.Log(LogSeverity.Warning, "loaded " + path + " (" + data.Length + " bytes)");
            }
            else if (command.StartsWith("list balls"))
            {
                Python.Run(ListBalls);
            }
        }

        private static string PrintBall(PyObject ball)
        {
            string name = "?";
                if (ball.HasAttr("name"))
                    name = ball.GetString("name");
                double x = ball.GetFloat("x");
                double y = ball.GetFloat("y");
                double z = ball.GetFloat("z");
                double mass = ball.GetFloat("mass");
                double radius = ball.GetFloat("radius");
            var builder = new StringBuilder();
            builder.AppendLine("ball id " + ball.Get("id"));
            builder.AppendLine("\tname: " + name + "\t mass: " + mass + "\t radius: " + radius);
            builder.AppendLine("\txyz: [" + Math.Round(x) + ", " + Math.Round(y) + ", " + Math.Round(z) + "]");
            return builder.ToString();
        }

        private void ListBalls()
        {
            var bp = Module.Ballpark;
            var ballsDict = new PyDict(bp.GetAttr("balls"));
            var balls = ballsDict.Values;
            var count = balls.Count;
            if (count > 5)
            {
                Core.Log(LogSeverity.Minor, "got " + count + " balls, showing 5");
                count = 5;
            }
            for (int i = 0; i < count; i++)
            {
                var ball = balls[i];
                if (ball.IsInvalid)
                    continue;
                Core.Log(LogSeverity.Minor, PrintBall(ball));
            }
        }
    }

}