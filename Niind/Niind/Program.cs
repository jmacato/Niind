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

internal static class Program
{
    private static async Task Main()
    {
        Console.WriteLine("Loading Files...");

        var rawFullDump =
            await File.ReadAllBytesAsync("/Users/jumarmacato/Desktop/nand-test-unit/working copy/nand.bin");

        Console.WriteLine("Nand File Loaded.");

        var rawKeyFile =
            await File.ReadAllBytesAsync("/Users/jumarmacato/Desktop/nand-test-unit/working copy/keys.bin");

        Console.WriteLine("Key File Loaded.");

        var nandData = rawFullDump.CastToStruct<NandDumpFile>();

        Console.WriteLine("NAND Dump marshalled to C# structs.");

        var keyData = rawKeyFile.CastToStruct<KeyFile>();

        Console.WriteLine("Key file marshalled to C# structs.");

        if (Marshal.SizeOf(nandData) != rawFullDump.LongLength)
            throw new FormatException("The NAND dump's internal structure is not correct!" +
                                      " Try to dump your console via BootMii again.");

        var bootMiiHeaderText = Encoding.ASCII.GetString(nandData.BootMiiFooterBlock.HeaderString).Trim((char) 0x0)
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
            .Zip(Enumerable.Range(0x1001, nus.DecryptedTitles.Count).Select(x => (uint) x))
            .ToDictionary(x => x.First.Key, x => (x.Second, x.First));

        var uidSysList = new Dictionary<ulong, RawUIDSysEntry>();

        uidSysList.Add(0x100000002, new RawUIDSysEntry(0x100000002, 0x1000));
        uidSysList.Add(0x100003132314a, new RawUIDSysEntry(0x100003132314a, 0x1001));

        foreach (var content in orderedTitleContents)
        {
            Console.WriteLine($"----");

            var contents = content.Value.First.AsEnumerable();
            var uid = content.Value.Second;
            ushort gid = 0;
            ulong tid = 0;
            string tidS = null, tidH = null, tidL = null;
            TitleMetadataContent lastContent = null;
            
            foreach (var tmc in contents)
            {
                lastContent = tmc;
                gid = tmc.ParentTMD.Header.GroupID;
                tid = tmc.ParentTMD.Header.TitleID;
                tidS = tid.ToString("X16").ToLowerInvariant();
                tidH = tidS[..8].ToLowerInvariant();
                tidL = tidS[8..].ToLowerInvariant();

                var cidS = tmc.ContentID.ToString("X8").ToLowerInvariant();

                var contentPath = $"/title/{tidH}/{tidL}/content/{cidS}.app";

                currentRoot.CreateFile(contentPath, tmc.DecryptedContent,
                    other: NodePerm.None,
                    group: NodePerm.RW,
                    owner: NodePerm.RW);

                Console.WriteLine($"Content ID {cidS} installed.");
            }

            var titleHighDir = $"/title/{tidH}";
            var titleLowDir = $"/title/{tidH}/{tidL}";
            var dataDir = $"/title/{tidH}/{tidL}/data";
            var contentDir = $"/title/{tidH}/{tidL}/content";

            currentRoot.CreateDirectory(titleHighDir, owner: NodePerm.RW, group: NodePerm.None, other: NodePerm.Read);
            currentRoot.CreateDirectory(titleLowDir, owner: NodePerm.RW, group: NodePerm.RW, other: NodePerm.Read);
            currentRoot.CreateDirectory(dataDir, owner: NodePerm.RW, group: NodePerm.None, other: NodePerm.None,
                userID: uid, groupID: gid);
            currentRoot.CreateDirectory(contentDir, owner: NodePerm.RW, group: NodePerm.RW, other: NodePerm.None);

            Console.WriteLine($"Installing Title ID {tidS} ({tidH}/{tidL})  UID {uid:X4} GID {gid:X4}");

            Console.WriteLine($"Adding Title ID {tidS} to uid.sys");

            if (!uidSysList.ContainsKey(tid))
            {
                uidSysList.Add(tid, new RawUIDSysEntry(tid, uid));
            }

            var tik = lastContent!.DecodedTicket!;

            Console.WriteLine($"Installing Ticket ID {BitConverter.ToUInt64(tik.TicketID):X16} " +
                              $"for Title ID {tidS}");

            var ticketPath = $"/ticket/{tidH}/{tidL}.tik";

            currentRoot.CreateFile(ticketPath, lastContent.DownloadedTicket[..0x2a4],
                other: NodePerm.None,
                group: NodePerm.RW,
                owner: NodePerm.RW);

            Console.WriteLine($"Installed Ticket to {ticketPath}");

            Console.WriteLine($"Installing Title Metadata for Title ID {tidS}");

            var tmdPath = $"/title/{tidH}/{tidL}/content/title.tmd";

            var tmdBlockSize = 0x1E4 + (lastContent.ParentTMD.ContentDescriptors.Count * 36);

            currentRoot.CreateFile(tmdPath, lastContent.DownloadedTMD[..tmdBlockSize],
                other: NodePerm.None,
                group: NodePerm.RW,
                owner: NodePerm.RW);

            Console.WriteLine($"Installed TMD to {tmdPath} byte size {tmdBlockSize}");

            currentRoot.GetNode(titleHighDir)?
                .SetPermissions(NodePerm.RW, NodePerm.None, NodePerm.Read);
            currentRoot.GetNode(titleLowDir)?
                .SetPermissions(NodePerm.RW, NodePerm.RW, NodePerm.Read);
            currentRoot.GetNode(dataDir)?
                .SetPermissions(NodePerm.RW, NodePerm.None);
            currentRoot.GetNode(contentDir)?
                .SetPermissions();
        }

        await using var uidSysStream = new MemoryStream();

        foreach (var entry in uidSysList.Select(x => x.Value))
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

        distilledNand = currentRoot.WriteAndCommitToNand();

        Console.WriteLine("Checking the reformatted NAND.");

        distilledNand.NandProcessAndCheck();

        var rawNand = distilledNand.NandDumpFile.CastToArray();

        await File.WriteAllBytesAsync(
            "/Users/jumarmacato/Desktop/nand-test-unit/working copy/niind-nand-blank2.bin", rawNand
        );
    }


    private static Dictionary<string, string> GetSystemInfo(DistilledNand distilledNand)
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
            var index = (uint) vt.Second;
            var sha1 = vt.First.SHA1;
            var content = vt.First.Content;

            var fileName = $"{index:X8}.app";

            folder.Add((fileName, content));
            contentMapList.Add(new RawContentMapEntry(index, sha1));
        }

        var rawCm = new List<byte>();

        foreach (var rawEntry in contentMapList.Select(tx => tx.CastToArray())) rawCm.AddRange(rawEntry);

        return (rawCm.ToArray(), folder);
    }


    private static byte[] Generate4_3USystemInfo()
    {
        var setting = new Dictionary<string, string>
        {
            {"AREA", "USA"},
            {"MODEL", "RVL-001(USA)"},
            {"DVD", "0"},
            {"MPCH", "0x7FFE"},
            {"CODE", "LU"},
            {"SERNO", "632011873"},
            {"VIDEO", "NTSC"},
            {"GAME", "US"}
        };


        var k = string.Join("", setting.Select(x => $"{x.Key}={x.Value}\r\n"));
        var h = Encoding.ASCII.GetBytes(k);

        SettingTxtCrypt(ref h);

        return h;
    }

    private static void SettingTxtCrypt(ref byte[] rawtxt)
    {
        var buffer = new byte[256];
        var key = 0x73B5DBFAu;
        int i, len = 256;

        rawtxt.CopyTo(buffer, 0);

        for (i = 0; i < len; i++)
        {
            buffer[i] ^= (byte) (key & 0xff);
            key = (key << 1) | (key >> 31);
        }

        rawtxt = buffer.ToArray();
    }
}