using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Schema;
using Niind.Helpers;
using Niind.Structures.FileSystem;
using Niind.Structures.TitlesSystem;

namespace Niind
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            Console.WriteLine("Loading Files...");

            var rawFullDump = File.ReadAllBytes("/Users/jumarmacato/Desktop/nand-test-unit/working copy/nand.bin");

            Console.WriteLine("Nand File Loaded.");

            var rawKeyFile = File.ReadAllBytes("/Users/jumarmacato/Desktop/nand-test-unit/working copy/keys.bin");

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

            foreach (var kvp in GetSystemInfo(distilledNand))
            {
                Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
            }


            var nus = new NintendoUpdateServerDownloader();
            await nus.GetUpdateAsync(distilledNand.KeyFile);

            var generateShared1 = GenerateShared1(nus);

            var nandErrRaw = distilledNand.RootNode.GetNode("/shared2/test2/nanderr.log")
                ?.GetFileContents(distilledNand.NandDumpFile, distilledNand.KeyFile);

            var certSysRawNode = distilledNand.RootNode.GetNode("/sys/cert.sys");

            var certSysRaw = certSysRawNode
                ?.GetFileContents(distilledNand.NandDumpFile, distilledNand.KeyFile);

            var contentMapRawNode = distilledNand.RootNode.GetNode("/shared1/content.map");

            var contentMapRaw = contentMapRawNode
                ?.GetFileContents(distilledNand.NandDumpFile, distilledNand.KeyFile);

            // var sasqrqw = distilledNand.RootNode.GetNode("/sys/uid.sys");

            // var uiio = sasqrqw.GetFileContents(distilledNand.NandDumpFile, distilledNand.KeyFile);
            //
            // foreach (var va in uiio.Chunk(12).Zip(Enumerable.Range(0, uiio.Length / 12)))
            // {
            //     Console.WriteLine($"UID Entry {va.Second} TID {EncryptionHelper.ByteArrayToHexString(va.First[..8])} " +
            //                       $"PAD {EncryptionHelper.ByteArrayToHexString(va.First[8..10])} " +
            //                       $"UID {EncryptionHelper.ByteArrayToHexString(va.First[10..12])}");
            // }

            //
            // foreach (var contentMapItem in contentMapRaw.Chunk(28).Select(x => x.ToArray()))
            // {
            //     if (contentMapItem.Length != 28)
            //     {
            //         break;
            //     }
            //
            //     var desc = contentMapItem.CastToStruct<RawContentMapEntry>();
            //     var sharedId = Encoding.ASCII.GetString(desc.SharedId);
            //     var testShared1RawNode =
            //         distilledNand.RootNode.GetNode($"/shared1/{sharedId}.app");
            //
            //     if (testShared1RawNode is null)
            //     {
            //         continue;
            //     }
            //
            //     var testShared1Raw =
            //         testShared1RawNode.GetFileContents(distilledNand.NandDumpFile, distilledNand.KeyFile);
            //
            //     var sha1 = EncryptionHelper.GetSHA1(testShared1Raw);
            //
            //     if (sha1.SequenceEqual(desc.SHA1))
            //     {
            //         Console.WriteLine($"Shared Content {sharedId}.app hash matches the one in content.map.");
            //     }
            // }


            var nandErrLogContent = Encoding.ASCII.GetString(nandErrRaw).Trim();
            var certSysRawSha1String = EncryptionHelper.GetSHA1String(certSysRaw);

            Console.WriteLine($"nanderr.log content {nandErrLogContent}");
            Console.WriteLine($"cert.sys SHA1 {certSysRawSha1String}");

            var certSysValid = certSysRawSha1String == Constants.ReferenceCertSysSHA1;
            Console.WriteLine($"cert.sys in nand is valid : {certSysValid}");

            // reformat the nand
            Console.WriteLine("Erasing NAND for First Phase");
            distilledNand.EraseAndReformat();
            Console.WriteLine("Erasing NAND for First Phase Complete");
            Console.WriteLine("Running First Phase Checks");
            distilledNand.NandProcessAndCheck();
            Console.WriteLine("Running First Phase Complete");

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

            currentRoot.CreateFile("/sys/cert.sys", certSysRaw,
                other: NodePerm.Read,
                group: NodePerm.RW);

            generateShared1.fileNames.Reverse();

            foreach (var sharedApps in generateShared1.fileNames)
            {
                currentRoot.CreateFile($"/shared1/{sharedApps.fileName}", sharedApps.content,
                    other: NodePerm.None,
                    group: NodePerm.RW,
                    owner: NodePerm.RW);
            }

            currentRoot.CreateFile("/shared1/content.map", generateShared1.rawContentMap,
                other: NodePerm.None,
                group: NodePerm.RW,
                owner: NodePerm.RW);

            var orderedTitleContents = nus.DecryptedTitles
                .OrderBy(x => x.ParentTMD.Header.TitleID)
                .GroupBy(x => x.ParentTMD.Header.TitleID)
                .Zip(Enumerable.Range(0x1000, nus.DecryptedTitles.Count))
                .ToDictionary(x => x.First.Key, x => (x.Second, x.First));

            var uidSysList = new List<RawUIDSysEntry>();

            foreach (var t in orderedTitleContents)
            {
                Console.WriteLine($"----");

                var contents = t.Value.First.AsEnumerable();
                var uid = t.Value.Second;
                var firstContent = contents.First();
                var gid = firstContent.ParentTMD.Header.GroupID;
                var tid = firstContent.ParentTMD.Header.TitleID;
                var tidS = $"{tid:X16}";
                var tidH = tidS[..8];
                var tidL = tidS[8..];
                var cnt = firstContent.ParentTMD.DecryptedContentCount;

                Console.WriteLine($"Installing Title ID {tidS} ({tidH}/{tidL})  UID {uid} GID {gid} Count {cnt}");

                Console.WriteLine($"Adding Title ID {tidS} to uid.sys");
                uidSysList.Add(new RawUIDSysEntry(tid, uid));

                var tik = firstContent.DecodedTicket;

                Console.WriteLine($"Installing Ticket ID {BitConverter.ToUInt64(tik.TicketID):X16} " +
                                  $"for Title ID {tidS}");

                var ticketPath = $"/ticket/{tidH}/{tidL}.tik";

                currentRoot.CreateFile(ticketPath, firstContent.DownloadedTicket,
                    other: NodePerm.None,
                    group: NodePerm.RW,
                    owner: NodePerm.RW);


                Console.WriteLine($"Installed Ticket to {ticketPath}");

                Console.WriteLine($"Installing TMD for Title ID {tidS}");

                var tmdPath = $"/title/{tidH}/{tidL}/content/title.tmd";

                currentRoot.CreateFile(tmdPath, firstContent.DownloadedTMD,
                    other: NodePerm.None,
                    group: NodePerm.RW,
                    owner: NodePerm.RW);

                Console.WriteLine($"Installed TMD to {tmdPath}");

                foreach (var v in contents)
                {
                    var cidS = $"{v.ContentID:X8}";

                    Console.WriteLine(
                        $"\tInstalling Content ID {cidS} for Title ID {tidS}");

                    var contentPath = $"/title/{tidH}/{tidL}/content/{cidS}.app";

                    currentRoot.CreateFile(contentPath, v.DecryptedContent,
                        other: NodePerm.None,
                        group: NodePerm.RW,
                        owner: NodePerm.RW);

                    Console.WriteLine($"\tContent ID {cidS} installed.");
                }

                var dataPath = $"/title/{tidH}/{tidL}/data";

                currentRoot.CreateDirectory(dataPath,
                    other: NodePerm.None,
                    group: NodePerm.None,
                    owner: NodePerm.RW,
                    userID: (uint)uid,
                    groupID: gid);

                Console.WriteLine($"\tCreated {dataPath} for {tidS} with Uid {uid:X}/Gid {gid:X}.");

                Console.WriteLine($"----");
            }

            using var uidSysStream = new MemoryStream();

            foreach (var entry in uidSysList)
            {
                await uidSysStream.WriteAsync(entry.CastToArray());
            }

            currentRoot.CreateFile("/sys/uid.sys", uidSysStream.ToArray(),
                other: NodePerm.None,
                group: NodePerm.RW,
                owner: NodePerm.RW);

            distilledNand = currentRoot.WriteAndCommitToNand();

            Console.WriteLine("Checking the reformatted NAND.");

            distilledNand.NandProcessAndCheck();

            await File.WriteAllBytesAsync(
                "/Users/jumarmacato/Desktop/nand-test-unit/working copy/reformatted-nand.bin",
                distilledNand.NandDumpFile.CastToArray());
        }


        private static Dictionary<string, string>? GetSystemInfo(DistilledNand distilledNand)
        {
            var xww = distilledNand.RootNode.GetNode("/title/00000001/00000002/data/setting.txt");

            if (xww is null) return null;

            var encData = xww.GetFileContents(distilledNand.NandDumpFile, distilledNand.KeyFile);
            SettingTxtCrypt(ref encData);

            var h = Encoding.ASCII.GetString(encData);
            var match = Regex.Matches(h, @"(?<pair>(?<key>.*?)\=(?<val>.*?))\r\n");

            return match.Select(x => (x.Groups["key"], x.Groups["val"]))
                .ToDictionary(x => x.Item1.ToString(), x => x.Item2.ToString());
        }




        private static (byte[] rawContentMap, List<(string fileName, byte[] content)> fileNames) GenerateShared1(
            NintendoUpdateServerDownloader nusDownloader)
        {
            var folder = new List<(string fileName, byte[] content)>();

            var contentMapList = new List<RawContentMapEntry>();

            foreach (var vt in nusDownloader.SharedContents.Zip(Enumerable.Range(0,
                         nusDownloader.SharedContents.Count)))
            {
                var index = (uint)vt.Second;
                var sha1 = vt.First.SHA1;
                var content = vt.First.Content;

                var fileName = $"{index:X8}.app";

                folder.Add((fileName, content));
                contentMapList.Add(new RawContentMapEntry(index, sha1));
            }

            var rawCM = new List<byte>();

            foreach (var rawEntry in contentMapList.Select(tx => tx.CastToArray()))
            {
                rawCM.AddRange(rawEntry);
            }

            return (rawCM.ToArray(), folder);
        }


        private static void SetSystemInfo(
            DistilledNand distilledNand, Dictionary<string, string> setting)
        {
            var xww = distilledNand.RootNode.GetNode("/title/00000001/00000002/data/setting.txt");

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

        public static string StringFormat(string format, IDictionary<string, object> values)
        {
            var matches = Regex.Matches(format, @"\{(.+?)\}");
            List<string> words = (from Match matche in matches select matche.Groups[1].Value).ToList();

            return words.Aggregate(
                format,
                (current, key) =>
                {
                    int colonIndex = key.IndexOf(':');
                    return current.Replace(
                        "{" + key + "}",
                        colonIndex > 0
                            ? string.Format("{0:" + key.Substring(colonIndex + 1) + "}",
                                values[key.Substring(0, colonIndex)])
                            : values[key].ToString());
                });
        }

        private static string ToHex(byte[] inx)
        {
            return BitConverter.ToString(inx).Replace("-", "");
        }
    }
}