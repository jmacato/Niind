using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Niind.Helpers;
using Niind.Structures.FileSystem;
using Niind.Structures.TitlesSystem;

namespace Niind;

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
            Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);


        var nus = new NintendoUpdateServerDownloader();
        await nus.GetUpdateAsync(distilledNand.KeyFile);

        var generateShared1 = GenerateShared1(nus);

        var nandErrRaw = distilledNand.RootNode.GetNode("/shared2/test2/nanderr.log")
            ?.GetFileContents(distilledNand.NandDumpFile, distilledNand.KeyFile);

        var certSysRawNode = distilledNand.RootNode.GetNode("/sys/cert.sys");

        var certSysRaw = certSysRawNode
            ?.GetFileContents(distilledNand.NandDumpFile, distilledNand.KeyFile);

        if (nandErrRaw is not null)
        {
            var nandErrLogContent = Encoding.ASCII.GetString(nandErrRaw).Trim();
            Console.WriteLine($"nanderr.log content {nandErrLogContent}");
        }

        var certSysRawSha1String = EncryptionHelper.GetSHA1String(certSysRaw);
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

        currentRoot.CreateDirectory("/sys", owner: NodePerm.RW, group: NodePerm.RW, other: NodePerm.None);
        currentRoot.CreateDirectory("/ticket", owner: NodePerm.RW, group: NodePerm.RW, other: NodePerm.None);
        currentRoot.CreateDirectory("/title", owner: NodePerm.RW, group: NodePerm.RW, other: NodePerm.Read);
        currentRoot.CreateDirectory("/shared1", owner: NodePerm.RW, group: NodePerm.RW, other: NodePerm.None);
        currentRoot.CreateDirectory("/shared2", owner: NodePerm.RW, group: NodePerm.RW, other: NodePerm.RW);
        currentRoot.CreateDirectory("/import", owner: NodePerm.RW, group: NodePerm.RW, other: NodePerm.None);
        currentRoot.CreateDirectory("/meta", owner: NodePerm.RW, group: NodePerm.RW, other: NodePerm.RW, userID: 0x1000,
            groupID: 1);
        currentRoot.CreateDirectory("/tmp", owner: NodePerm.RW, group: NodePerm.RW, other: NodePerm.RW);


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
            .Zip(Enumerable.Range(0x1000, nus.DecryptedTitles.Count).Select(x => (uint)x))
            .ToDictionary(x => x.First.Key, x => (x.Second, x.First));

        var uidSysList = new List<RawUIDSysEntry>();

        foreach (var content in orderedTitleContents)
        {
            Console.WriteLine($"----");

            var contents = content.Value.First.AsEnumerable();
            var uid = content.Value.Second;
            var firstContent = contents.First();
            ushort gid = 0;
            ulong tid = 0;
            string tidS = null, tidH = null, tidL = null;

            foreach (var v in contents)
            {
                gid = v.ParentTMD.Header.GroupID;
                tid = v.ParentTMD.Header.TitleID;
                tidS = $"{tid:X16}".ToLowerInvariant();
                tidH = tidS[..8].ToLowerInvariant();
                tidL = tidS[8..].ToLowerInvariant();

                var cidS = $"{v.ContentID:X8}".ToLowerInvariant();

                Console.WriteLine(
                    $"Installing Content ID {cidS} for Title ID {tidS}");

                var contentPath = $"/title/{tidH}/{tidL}/content/{cidS}.app";

                currentRoot.CreateFile(contentPath, v.DecryptedContent,
                    other: NodePerm.None,
                    group: NodePerm.RW,
                    owner: NodePerm.RW);

                Console.WriteLine($"Content ID {cidS} installed.");
            }

            var dataPath = $"/title/{tidH}/{tidL}/data";

            currentRoot.CreateDirectory(dataPath,
                other: NodePerm.None,
                group: NodePerm.None,
                owner: NodePerm.RW,
                userID: uid,
                groupID: gid);

            Console.WriteLine($"Created {dataPath} for {tidS} with Uid {uid:X}/Gid {gid:X}.");


            Console.WriteLine($"Installing Title ID {tidS} ({tidH}/{tidL})  UID {uid:X4} GID {gid:X4}");

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

            Console.WriteLine($"Installing Title Metadata for Title ID {tidS}");

            var tmdPath = $"/title/{tidH}/{tidL}/content/title.tmd";

            var j = 0x1E4 + (firstContent.ParentTMD.ContentDescriptors.Count * 36);

            currentRoot.CreateFile(tmdPath, firstContent.DownloadedTMD[..j],
                other: NodePerm.None,
                group: NodePerm.RW,
                owner: NodePerm.RW);

            Console.WriteLine($"Installed TMD to {tmdPath} byte size {j}");
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

        currentRoot.CreateFile("/title/00000001/00000002/data/setting.txt", Generate4_3USystemInfo(),
            other: NodePerm.Read,
            group: NodePerm.Read,
            owner: NodePerm.Read,
            userID: 0x1000,
            groupID: 1);
        //
        // currentRoot.CreateFile("/sys/uid.sys", new byte[] { 00, 00, 00, 1, 00, 00, 00, 2, 1, 0, 0, 0 },
        //     other: NodePerm.Read);


        distilledNand = currentRoot.WriteAndCommitToNand();

        Console.WriteLine("Checking the reformatted NAND.");

        distilledNand.NandProcessAndCheck();

        var u = distilledNand.NandDumpFile.CastToArray();

        Console.WriteLine($"Hash {EncryptionHelper.GetSHA1String(u)}.");


        await File.WriteAllBytesAsync(
            "/Users/jumarmacato/Desktop/nand-test-unit/working copy/niind-nand-blank2.bin", u
        );
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

        foreach (var rawEntry in contentMapList.Select(tx => tx.CastToArray())) rawCM.AddRange(rawEntry);

        return (rawCM.ToArray(), folder);
    }


    private static byte[] Generate4_3USystemInfo()
    {
        var setting = new Dictionary<string, string>
        {
            { "AREA", "USA" },
            { "MODEL", "RVL-001(USA)" },
            { "DVD", "0" },
            { "MPCH", "0x7FFE" },
            { "CODE", "LU" },
            { "SERNO", "632011873" },
            { "VIDEO", "NTSC" },
            { "GAME", "US" }
        };


        var k = string.Join("", setting.Select(x => $"{x.Key}={x.Value}\r\n"));
        var h = Encoding.ASCII.GetBytes(k);

        SettingTxtCrypt(ref h, true);

        return h;
    }

    private static void SettingTxtCrypt(ref byte[] rawtxt, bool is_enc = false)
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
        var words = (from Match matche in matches select matche.Groups[1].Value).ToList();

        return words.Aggregate(
            format,
            (current, key) =>
            {
                var colonIndex = key.IndexOf(':');
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