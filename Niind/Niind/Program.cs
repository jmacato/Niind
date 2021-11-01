using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Niind.Helpers;
using Niind.Structures.FileSystem;

namespace Niind
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            
            Console.WriteLine("Loading Files...");
            
            var rawFullDump =
                File.ReadAllBytes(
                    "/Users/jumarmacato/Documents/Electronics/Wii NAND Experiment/wiinandfolder/nand-perfect-04186005-h0133gb copy 2.bin");

            Console.WriteLine("Nand File Loaded.");

            var rawKeyFile =
                File.ReadAllBytes(
                    "/Users/jumarmacato/Documents/Electronics/Wii NAND Experiment/wiinandfolder/keys-perfect-04186005-h0133gb.bin");

            Console.WriteLine("Key File Loaded.");

            var nandData = rawFullDump.CastToStruct<NandDumpFile>();

            Console.WriteLine("NAND Dump marshalled to C# structs.");

            var keyData = rawKeyFile.CastToStruct<KeyFile>();

            Console.WriteLine("Key file marshalled to C# structs.");

            if (Marshal.SizeOf(nandData) != rawFullDump.LongLength)
                throw new FormatException("The NAND dump's internal structure is not correct!" +
                                          " Try to dump your console via BootMii again.");

            var bootMiiHeaderText = Encoding.ASCII.GetString(nandData.BootMiiFooterBlock.HeaderString).Trim((char)0x0)
                .Trim('\n');

            Console.WriteLine($"BootMii Metadata Header: {bootMiiHeaderText}");

            var consoleId = BitConverter.ToString(keyData.ConsoleID).Replace("-", "");

            if (!bootMiiHeaderText.Contains(consoleId, StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidOperationException("The Key File provided is not for this specific NAND dump.");

            Console.WriteLine("Key file matches the NAND dump.");
            
            
            
            

            var distilledNand = new DistilledNand(nandData, keyData);
 

            var x = new NintendoUpdateServerDownloader();
            await x.GetUpdateAsync(distilledNand.KeyFile);
            
            
            var currentRoot = new NandRootNode(distilledNand);

            currentRoot.CreateDirectory("/sys");
            currentRoot.CreateDirectory("/ticket");
            currentRoot.CreateDirectory("/title", group: NodePerm.Read);
            currentRoot.CreateDirectory("/shared1");
            currentRoot.CreateDirectory("/shared2", group: NodePerm.Read);
            currentRoot.CreateDirectory("/import");
            currentRoot.CreateDirectory("/meta", 0x1000, 1, group: NodePerm.RW);
            currentRoot.CreateDirectory("/tmp", group: NodePerm.RW);

            var rndSrc = new Random();

            var testFile =
                new byte[Math.Max(1, (int)((Constants.NandClusterNoSpareByteSize * 5) * rndSrc.NextDouble()))];

            rndSrc.NextBytes(testFile);

            var h1 = EncryptionHelper.GetSHA1(testFile);

            currentRoot.CreateFile("/tmp/test1.txt", testFile,
                other: NodePerm.Read);

            currentRoot.CreateFile("/shared1/content.map", Array.Empty<byte>(),
                other: NodePerm.Read);

            currentRoot.CreateFile("/sys/uid.sys", Array.Empty<byte>(),
                other: NodePerm.Read);


            distilledNand = currentRoot.WriteAndCommitToNand();

            Console.WriteLine("Checking the reformatted NAND.");

            distilledNand.NandProcessAndCheck();

            var retTestFile = GetFileContent(distilledNand, "test1.txt");
            var h2 = EncryptionHelper.GetSHA1(retTestFile);

            if (h1.SequenceEqual(h2))
            {
                Console.WriteLine($"Random Test File Verification Success. Hash: {ToHex(h1)}");
            }
            
            
            

            File.WriteAllBytes(
                "/Users/jumarmacato/Documents/Electronics/Wii NAND Experiment/wiinandfolder/nand-perfect-test-cleaned.bin",
                distilledNand.NandDumpFile.CastToArray());
        }


        static byte[] GetFileContent(DistilledNand distilledNand, string filename)
        {
            var rawNode = distilledNand.RootNode
                .GetDescendants().FirstOrDefault(x => x.Filename == filename);

            if (rawNode is null) return Array.Empty<byte>();

            var encData = rawNode.GetFileContents(distilledNand.NandDumpFile, distilledNand.KeyFile);
            return encData;
        }

        private static Dictionary<string, string>? GetSystemInfo(DistilledNand distilledNand)
        {
            var xww = distilledNand.RootNode
                .GetDescendants().FirstOrDefault(x => x.Filename == "setting.txt");

            if (xww is null) return null;

            var encData = xww.GetFileContents(distilledNand.NandDumpFile, distilledNand.KeyFile);
            SettingTxtCrypt(ref encData);

            var h = Encoding.ASCII.GetString(encData);
            var match = Regex.Matches(h, @"(?<pair>(?<key>.*?)\=(?<val>.*?))\r\n");

            return match.Select(x => (x.Groups["key"], x.Groups["val"]))
                .ToDictionary(x => x.Item1.ToString(), x => x.Item2.ToString());
        }


        private static void SetSystemInfo(
            DistilledNand distilledNand, Dictionary<string, string> setting)
        {
            var xww = distilledNand.RootNode
                .GetDescendants().FirstOrDefault(x => x.Filename == "setting.txt");

            var k = string.Join("", setting.Select(x => $"{x.Key}={x.Value}\r\n"));
            var h = Encoding.ASCII.GetBytes(k);

            SettingTxtCrypt(ref h, true);

            if (!xww.ModifyFileContentsNoResize(distilledNand.NandDumpFile, distilledNand.KeyFile, h))
            {
                Console.WriteLine("Failed to write setting.txt.");
            }
        }

        static void SettingTxtCrypt(ref byte[] rawtxt, bool is_enc = false)
        {
            var buffer = new byte[256];
            var key = 0x73B5DBFAu;
            int i, len = 256;

            rawtxt.CopyTo(buffer, 0);

            for (i = 0; i < len; i++)
            {
                buffer[i] ^= (byte)(key & 0xff);
                key = (key << 1) | (key >> 31);
            }

            rawtxt = buffer.ToArray();
        }

        private static string ToHex(byte[] inx)
        {
            return BitConverter.ToString(inx).Replace("-", "");
        }
    }
}