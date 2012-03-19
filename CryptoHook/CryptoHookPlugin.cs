using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using WhiteMagic.Internals;
using beliEVE;

namespace CryptoHook
{
    
    public class CryptoHookPlugin : IPlugin
    {
        private readonly List<Delegate> _keep = new List<Delegate>(3);
        private BinaryWriter _encFile;
        private BinaryWriter _decFile;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool CryptEncryptDelegate(
            IntPtr hKey, IntPtr hHash, bool final, uint dwFlags, IntPtr pbData, IntPtr pdwLength, uint dwBufLen);
        private static Detour _cryptEncryptDetour;

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate bool CryptDecryptDelegate(
            IntPtr hKey, IntPtr hHash, bool final, uint dwFlags, IntPtr pbData, IntPtr pdwDataLen);
        private static Detour _cryptDecryptDetour;

        private PacketSourceHandler _handler;

        public string Name
        {
            get { return "Crypto Hook"; }
        }

        public string Description
        {
            get { return "Hooks crypto APIs used by the game for packet logging"; }
        }

        public int Revision
        {
            get { return 1; }
        }

        public PluginType Type
        {
            get { return PluginType.PacketSource; }
        }

        public void SetInterface(PluginInterface iface)
        {
            _handler = iface as PacketSourceHandler;
        }

        public bool Initialize(LaunchStage stage)
        {
            return true;
            if (stage == LaunchStage.PostBlue)
            {
                _encFile = new BinaryWriter(File.Create("encrypted.log"));
                _decFile = new BinaryWriter(File.Create("decrypted.log"));

                // Encryption & Decryption
                _cryptEncryptDetour = DetourAPI<CryptEncryptDelegate>("advapi32.dll", "CryptEncrypt", HandleCryptEncrypt);
                _cryptDecryptDetour = DetourAPI<CryptDecryptDelegate>("advapi32.dll", "CryptDecrypt", HandleCryptDecrypt);
                Core.Log(LogSeverity.Minor, "CryptoHook active");
            }

            return true;
        }

        public void ProcessCommand(string command)
        {
            
        }

        private bool HandleCryptEncrypt(IntPtr hKey, IntPtr hHash, bool final, uint dwFlags, IntPtr pbData, IntPtr pdwLength, uint dwBufLen)
        {
            var length = Utility.Magic.Read<int>(pdwLength);
            if (length > 0 && pbData != IntPtr.Zero)
            {
                var dataBuffer = new byte[length];
                Marshal.Copy(pbData, dataBuffer, 0, length);

                _encFile.Write(Utility.HexDump(dataBuffer));
                _encFile.Write("\r\n");
                _encFile.Flush();
            }

            return (bool)_cryptEncryptDetour.CallOriginal(hKey, hHash, final, dwFlags, pbData, pdwLength, dwBufLen);
        }

        private bool HandleCryptDecrypt(IntPtr hKey, IntPtr hHash, bool final, uint dwFlags, IntPtr pbData, IntPtr pdwLength)
        {
            var result = (bool)_cryptDecryptDetour.CallOriginal(hKey, hHash, final, dwFlags, pbData, pdwLength);
            if (!result)
                return false;

            var length = Utility.Magic.Read<int>(pdwLength);
            if (length <= 0)
                return true;
            var dataBuffer = new byte[length];
            if (pbData == IntPtr.Zero)
                return true;
            Marshal.Copy(pbData, dataBuffer, 0, length);

            if (dataBuffer[0] == 0x78)
                dataBuffer = Utility.Decompress(dataBuffer);

            _decFile.Write(Utility.HexDump(dataBuffer));
            _decFile.Write("\r\n");
            _decFile.Flush();

            return true;
        }

        private Detour DetourAPI<T>(string library, string name, T handler) where T : class
        {
            var module = Native.LoadLibrary(library);
            if (module == IntPtr.Zero)
                return null;

            var func = Native.GetProcAddress(module, name);
            if (func == IntPtr.Zero)
                return null;

            var victim = Utility.Magic.RegisterDelegate<T>(func);
            if (victim == null)
                return null;

            // to keep them from being collected
            _keep.Add(victim as Delegate);
            _keep.Add(handler as Delegate);

            return Utility.Magic.Detours.CreateAndApply(victim as Delegate, handler as Delegate, name);
        }
    }

}