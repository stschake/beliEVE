using System;
using System.Runtime.InteropServices;
using WhiteMagic.Internals;
using beliEVE;

namespace ZipImportHook
{
    
    public class ZipImportHook : IPlugin
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate int GetModuleInfoDel(IntPtr importer, string module);
        private GetModuleInfoDel _getModuleInfoOrig;
        private GetModuleInfoDel _getModuleInfoFake;
        private Detour _getModuleInfoDetour;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private delegate IntPtr GetModuleCodeDel(IntPtr importer, string fullname, IntPtr ppackage, IntPtr pmodpath);
        private GetModuleCodeDel _getModuleCodeOrig;
        private GetModuleCodeDel _getModuleCodeFake;
        private Detour _getModuleCodeDetour;

        public string Name
        {
            get { return "Zip Import hook"; }
        }

        public string ShortName
        {
            get { return "zip"; }
        }

        public string Description
        {
            get { return "Hook zip importer to control module loading"; }
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
                {
                    var addr = Utility.FindPattern("python27",
                                                   "55 8b ec 81 ec 04 01 00 00 53 ff 75 0c 8b 5d 08 e8 ? ? ? ? 59 8d 8d fc fe ff ff 51 50 ff ? 0c e8 ? ? ? ? 59 50 e8 ? ? ? ? 83 c4 ?");
                    if (addr == 0)
                    {
                        Core.Log(LogSeverity.Warning,
                                 "Can't find get_module_info function; pattern outdated? Zip Importer hook disabling.");
                        return true;
                    }

                    _getModuleInfoOrig = Utility.Magic.RegisterDelegate<GetModuleInfoDel>(addr);
                    _getModuleInfoFake = HandleGetModuleInfo;
                    _getModuleInfoDetour = Utility.Magic.Detours.CreateAndApply(_getModuleInfoOrig, _getModuleInfoFake,
                                                                                "get_module_info");
                }

                {
                    var addr = Utility.FindPattern("python27",
                                                   "55 8b ec 81 ec 10 01 00 00 56 ff 75 0c e8 ? ? ? ? 8b ? 08 59 8d ? ? ? ? ? 51 50 ff 76 ? e8 ? ? ? ? 59 50 e8 ? ? ? ? 83 c4 0c 85 c0 79 ? 33 c0");
                    if (addr == 0)
                    {
                        Core.Log(LogSeverity.Warning, "Can't find get_module_code function; pattern outdated? Zip Importer hook disabling.");
                        return true;
                    }

                    _getModuleCodeOrig = Utility.Magic.RegisterDelegate<GetModuleCodeDel>(addr);
                    _getModuleCodeFake = HandleGetModuleCode;
                    _getModuleCodeDetour = Utility.Magic.Detours.CreateAndApply(_getModuleCodeOrig, _getModuleCodeFake,
                                                                                "get_module_code");
                }

                Core.Log(LogSeverity.Minor, "initialized zip importer hooks");
            }
            
            return true;
        }

        public void ProcessCommand(string command)
        {
            
        }

        private IntPtr HandleGetModuleCode(IntPtr importer, string fullname, IntPtr ppackage, IntPtr pmodpath)
        {
            var ret = (IntPtr) _getModuleCodeDetour.CallOriginal(importer, fullname, ppackage, pmodpath);
            return ret;
        }

        private enum ModuleStatus
        {
            Error,
            NotFound,
            Module,
            Package
        }

        private int HandleGetModuleInfo(IntPtr importer, string module)
        {
            int ret = (int)_getModuleInfoDetour.CallOriginal(importer, module);
            return ret;
        }
    }

}