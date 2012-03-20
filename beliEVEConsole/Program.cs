using System;
using System.Text;
using System.Threading;
using beliEVE;

namespace beliEVEConsole
{
    class ConsoleModule : NativeModule
    {
        public ConsoleModule()
            : base("bEconsole")
        {
            AddMethod("Log", HandleLog);
            Initialize();
        }

        private IntPtr HandleLog(IntPtr self, IntPtr args)
        {
            var tup = new PyTuple(args);
            if (tup.IsInvalid || !tup.IsTuple || tup.Size <= 0)
                return PyObject.None;
            var obj = tup[0];
            Core.Log(LogSeverity.Minor, obj.ToString());
            return PyObject.None;
        }
    }

    class Program
    {

        [STAThread]
        static void Main()
        {
            string gamePath;
            if (!Helper.GetGamePath(out gamePath))
                return;

            Core.Log(LogSeverity.Minor, "Loading plugins..");
            Core.Plugins.LoadAll("Plugins/");
            Core.Plugins.CreateInterfaces();
            Core.Plugins.Initialize(LaunchStage.Startup);

            Core.Log(LogSeverity.Minor, "Preparing EVE launch..");
            BlueOS.Initialize(gamePath);

            // blue module is now in memory but still dormant
            Core.Plugins.Initialize(LaunchStage.PreBlue);
            
            if (!BlueOS.Startup())
                Core.Log(LogSeverity.Fatal, "Failed to launch EVE (BlueOS::Startup failed)");

            // at this point, blue has initialized the python environment, so init our wrapper
            Python.Initialize();
            Core.Plugins.Initialize(LaunchStage.PostBlue);

            var console = new ConsoleModule();

            Core.Log(LogSeverity.Minor, "Leaving main thread, switching to command processor");
            var thread = new Thread(CommandProcessor);
            thread.Start();

            // run BeOS and the game - blocking!
            BlueOS.Run();
            Thread.Sleep(Timeout.Infinite);
        }

        static void CommandProcessor()
        {
            Console.WriteLine();
            while (true)
            {
                Console.Write("> ");
                var command = Console.ReadLine();
                if (command == null)
                    continue;

                if (command.StartsWith("run "))
                {
                    var code = new StringBuilder(200);
                    command = command.Substring(4);
                    code.Append(command + "\n");
                    Console.Write("      ");
                    while ((command = Console.ReadLine()) != "")
                    {
                        Console.Write("      ");
                        code.Append(command.Replace(@"\t", "\t") + "\n");
                    }
                    var cb = new CodeBite(code.ToString());
                    try
                    {
                        var res = Python.Repr(cb.Run());
                        if (res != "0" && res != "None")
                            Console.WriteLine(" => " + res);
                    }
                    catch (CompileException)
                    {
                        Console.WriteLine(" => compiler error");
                        continue;
                    }
                }

                foreach (var plugin in Core.Plugins)
                {
                    if (command.StartsWith(plugin.ShortName))
                    {
                        command = command.Substring(plugin.ShortName.Length + 1);
                        plugin.ProcessCommand(command);
                        break;
                    }
                }
            }
        }
    }
}
