using System;
using System.IO;
using beliEVE;

namespace beliEVEConsole
{

    public static class Helper
    {
        
        public static bool GetGamePath(out string gamePath)
        {
            const string gamePathFile = "game_path.txt";

            if (!File.Exists(gamePathFile))
            {
                if (Utility.FindGamePath(out gamePath))
                {
                    Core.Log(LogSeverity.Minor, "Found game at: " + gamePath);
                    Core.Log(LogSeverity.Minor, "Is this path correct? (y/n)");
                    var decision = Console.ReadKey(true);
                    if (decision.KeyChar == 'n')
                    {
                        var path = Utility.AskForPath("Select EVE directory");
                        if (path == null)
                        {
                            Core.Log(LogSeverity.Fatal, "Invalid file selected. Terminating.");
                            return false;
                        }
                        gamePath = path;
                    }
                    Core.Log(LogSeverity.Minor, "Saving your choice in " + gamePathFile);
                    File.WriteAllText(gamePathFile, gamePath);
                }
            }
            else
            {
                Core.Log(LogSeverity.Minor, "Using path from " + gamePathFile);
                gamePath = File.ReadAllText(gamePathFile);
            }
            return true;
        }

    }

}