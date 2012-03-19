using System;
using System.Text;
using System.Threading;
using beliEVE;

namespace beliEVEConsole
{
    class TestModule : NativeModule
    {
        public TestModule()
            : base("MyTestModule")
        {
            AddMethod("Test", HandleTest);
            Initialize();
        }

        private IntPtr HandleTest(IntPtr self, IntPtr args)
        {
            Core.Log(LogSeverity.Minor, "In HandleTest");
            var tup = new PyTuple(args);
            foreach (var obj in tup)
                Core.Log(LogSeverity.Minor, "arg: " + obj);
            return new PyInt(1).Pointer;
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
                        code.Append(command.Replace(@"\t", "\t") + "\n");
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
                    if (command.StartsWith(plugin.Name))
                    {
                        command = command.Substring(plugin.Name.Length + 1);
                        plugin.ProcessCommand(command);
                        break;
                    }
                }
            }
        }
    }
}
