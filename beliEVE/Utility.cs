using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using WhiteMagic;

namespace beliEVE
{

    public static class Utility
    {
        private class ModuleBounds
        {
            public uint Start;
            public uint Length;
        }

        private static readonly Dictionary<string, ModuleBounds> Boundaries = new Dictionary<string, ModuleBounds>();

        public static string AskForPath(string caption)
        {
            var dialog = new FolderBrowserDialog
                             {
                                 Description = caption,
                                 ShowNewFolderButton = false,
                                 RootFolder = Environment.SpecialFolder.MyComputer
                             };

            if (dialog.ShowDialog() == DialogResult.OK)
                return dialog.SelectedPath;
            return null;
        }

        public static byte[] Decompress(byte[] input)
        {
            // two bytes shaved off (zlib header)
            var sourceStream = new MemoryStream(input, 2, input.Length - 2);
            var stream = new DeflateStream(sourceStream, CompressionMode.Decompress);
            return stream.ReadAllBytes();
        }

        /// <summary>
        /// Reads the contents of the stream into a byte array.
        /// data is returned as a byte array. An IOException is
        /// thrown if any of the underlying IO calls fail.
        /// </summary>
        /// <param name="source">The stream to read.</param>
        /// <returns>A byte array containing the contents of the stream.</returns>
        /// <exception cref="NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="ObjectDisposedException">Methods were called after the stream was closed.</exception>
        /// <exception cref="System.IO.IOException">An I/O error occurs.</exception>
        public static byte[] ReadAllBytes(this Stream source)
        {
            var readBuffer = new byte[4096];

            int totalBytesRead = 0;
            int bytesRead;

            while ((bytesRead = source.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
            {
                totalBytesRead += bytesRead;

                if (totalBytesRead == readBuffer.Length)
                {
                    int nextByte = source.ReadByte();
                    if (nextByte != -1)
                    {
                        var temp = new byte[readBuffer.Length * 2];
                        Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                        Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                        readBuffer = temp;
                        totalBytesRead++;
                    }
                }
            }

            byte[] buffer = readBuffer;
            if (readBuffer.Length != totalBytesRead)
            {
                buffer = new byte[totalBytesRead];
                Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
            }
            return buffer;
        }

        public static string HexDump(byte[] bytes)
        {
            if (bytes == null) return "<null>";
            int len = bytes.Length;
            var result = new StringBuilder(((len + 15) / 16) * 78);
            var chars = new char[78];
            // fill all with blanks
            for (int i = 0; i < 75; i++)
                chars[i] = ' ';
            chars[76] = '\r';
            chars[77] = '\n';

            for (int i1 = 0; i1 < len; i1 += 16)
            {
                chars[0] = HexChar(i1 >> 28);
                chars[1] = HexChar(i1 >> 24);
                chars[2] = HexChar(i1 >> 20);
                chars[3] = HexChar(i1 >> 16);
                chars[4] = HexChar(i1 >> 12);
                chars[5] = HexChar(i1 >> 8);
                chars[6] = HexChar(i1 >> 4);
                chars[7] = HexChar(i1 >> 0);

                int offset1 = 11;
                int offset2 = 60;

                for (int i2 = 0; i2 < 16; i2++)
                {
                    if (i1 + i2 >= len)
                    {
                        chars[offset1] = ' ';
                        chars[offset1 + 1] = ' ';
                        chars[offset2] = ' ';
                    }
                    else
                    {
                        byte b = bytes[i1 + i2];
                        chars[offset1] = HexChar(b >> 4);
                        chars[offset1 + 1] = HexChar(b);
                        chars[offset2] = (b < 32 ? '·' : (char)b);
                    }
                    offset1 += (i2 == 7 ? 4 : 3);
                    offset2++;
                }
                result.Append(chars);
            }
            return result.ToString();
        }

        private static char HexChar(int value)
        {
            value &= 0xF;
            if (value >= 0 && value <= 9)
                return (char)('0' + value);
            return (char)('A' + (value - 10));
        }

        public static bool FindGamePath(out string result)
        {
            try
            {
                if (File.Exists("blue.dll"))
                {
                    result = Path.GetFullPath("../");
                    return true;
                }
                if (File.Exists("bin\\blue.dll"))
                {
                    result = Directory.GetCurrentDirectory();
                    return true;
                }

                var reg =
                    Registry.LocalMachine.OpenSubKey("SOFTWARE").OpenSubKey("Microsoft").
                        OpenSubKey("Windows").OpenSubKey("CurrentVersion").OpenSubKey("Uninstall").OpenSubKey("EVE");
                var path = reg.GetValue("InstallLocation").ToString();
                if (!File.Exists(path + "\\bin\\blue.dll"))
                {
                    result = "no game found";
                    return false;
                }
                result = path;
                return true;
            }
            catch (Exception)
            {
                result = "no game found";
                return false;
            }
        }

        private static void FindModule(string module, out uint start, out uint length)
        {
            var p = Process.GetCurrentProcess();
            start = length = 0;
            for (int i = 0; i < p.Modules.Count; i++)
            {
                var mod = p.Modules[i];
                if (mod.ModuleName == module)
                {
                    start = (uint)mod.BaseAddress.ToInt32();
                    length = (uint)mod.ModuleMemorySize;
                    break;
                }
            }
            if (start == 0 || length == 0)
                throw new Exception("Failed to find module " + module);
        }

        public static Magic Magic = new Magic();

        private unsafe static bool DataCompare(byte[] bytes, IEnumerable<bool> mask, long offset)
        {
            return !mask.Where((t, i) => t && bytes[i] != *(byte*)(offset + i)).Any();
        }

        public static uint FindPattern(string pattern)
        {
            return FindPattern("blue", pattern);
        }

        public static uint FindPattern(string module, string pattern)
        {
            var parts = pattern.Split(' ');
            var bytes = new byte[parts.Length];
            var mask = new bool[parts.Length];

            int i = 0;
            foreach (var part in parts)
            {
                if (!part.StartsWith("?"))
                {
                    mask[i] = true;
                    bytes[i++] = byte.Parse(part, NumberStyles.HexNumber);
                }
                else
                    mask[i++] = false;
            }
            
            module += ".dll";
            if (!Boundaries.ContainsKey(module))
            {
                uint start, length;
                FindModule(module, out start, out length);
                Boundaries.Add(module, new ModuleBounds {Length = length, Start = start});
            }

            var bounds = Boundaries[module];
            
            for (uint p = 0; p < bounds.Length; p++)
            {
                if (DataCompare(bytes, mask, bounds.Start + p))
                    return bounds.Start + p;
            }

            return 0;
        }
    }

}