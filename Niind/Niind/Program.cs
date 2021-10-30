using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Niind.Helpers;
using Niind.Structures.FileSystem;

namespace Niind
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            /*
            
            Compilation inputCompilation = CreateCompilation(@"
namespace MyCode
{
    public class Program
    {
        public static void Main(string[] args)
        {
        }
    }

public partial class Test
{
    [AutoNotify]
    public int _testProp;
}
}
");
            var generator = new  Niind.Generators.AutoNotifyGenerator();

            
// Create the driver that will control the generation, passing in our generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

// Run the generation pass
            driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);

            static Compilation CreateCompilation(string source)
                => CSharpCompilation.Create("compilation",
                    new[] { CSharpSyntaxTree.ParseText(source) },
                    new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                    new CSharpCompilationOptions(OutputKind.ConsoleApplication));
            
            
*/
            
            


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
            
            
            
            
            
            var asdasd = new NintendoUpdateServerDownloader();
            
            asdasd.GetUpdate(keyData);
            

            var distilledNand = NandProcessAndCheck(nandData, keyData);


            Console.WriteLine("Getting Manufacturing System Info.");

            var info = GetSystemInfo(distilledNand);
            if (info is not null)
            {
                Console.WriteLine($"Serial Number: {info["CODE"]}{info["SERNO"]}");
                Console.WriteLine($"Region       : {info["AREA"]}");
            }

            Console.WriteLine("Trying to set Manufacturing System Info.");

            info["AREA"] = "USA";
            info["CODE"] = "NUL";
            info["SERNO"] = "13371337";

            SetSystemInfo(distilledNand, info);

            Console.WriteLine("Checking the NAND.");

            distilledNand = NandProcessAndCheck(distilledNand.NandDumpFile, distilledNand.KeyFile);

            var infoNew = GetSystemInfo(distilledNand);

            Console.WriteLine("New System Info.");

            Console.WriteLine($"Serial Number: {infoNew["CODE"]}{infoNew["SERNO"]}");
            Console.WriteLine($"Region       : {infoNew["AREA"]}");


            Console.WriteLine("Trying to reformat the NAND in memory (no writes to actual NAND).");

            NandBlankSlate(distilledNand);

            distilledNand = NandProcessAndCheck(distilledNand.NandDumpFile, distilledNand.KeyFile);

            var currentRoot = new NandRootNode(distilledNand);

            currentRoot.CreateDirectory("/sys");
            currentRoot.CreateDirectory("/ticket");
            currentRoot.CreateDirectory("/title", group: NodePerm.Read);
            currentRoot.CreateDirectory("/shared1");
            currentRoot.CreateDirectory("/shared2", group: NodePerm.Read);
            currentRoot.CreateDirectory("/import");
            currentRoot.CreateDirectory("/meta", 0x1000, 1, group: NodePerm.RW);
            currentRoot.CreateDirectory("/tmp", group: NodePerm.RW);

            // currentRoot.CreateFile("/tmp/test1.txt", Encoding.ASCII.GetBytes("Hello World!").AsMemory(),
            //     other: NodePerm.Read);
            //
            // currentRoot.CreateFile("/tmp/test2.txt", Encoding.ASCII.GetBytes("Hello World 2!").AsMemory(),
            //     other: NodePerm.Read);

            currentRoot.WriteAndCommitToNand(distilledNand);

            Console.WriteLine("Checking the reformatted NAND.");

            NandProcessAndCheck(distilledNand.NandDumpFile, distilledNand.KeyFile);


            File.WriteAllBytes("test2.bin", distilledNand.NandDumpFile.CastToArray());
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

        private static void NandBlankSlate(DistilledNand distilledNand)
        {
            var superBlockTarget = distilledNand.MainSuperBlockRaw.CastToStruct<SuperBlock>();

            var validClusters = distilledNand.ValidClusters.Keys;
            var clusterDeleted = 0;
            // Erase all used clusters.
            for (ushort i = 0; i < superBlockTarget.ClusterEntries.Length; i++)
            {
                if (validClusters.Contains(i))
                {
                    Console.Write($"\rDeleting cluster {clusterDeleted} of {validClusters.Count}");
                    // Mark cluster as free space.
                    superBlockTarget.ClusterEntries[i] = ByteWiseSwap((ushort)ClusterDescriptor.Empty);
#if DEBUG
                    
                    // Actually delete the data.
                    var addr = NandAddressTranslationHelper.AbsoluteClusterToBlockCluster(i);
                    var target = distilledNand.NandDumpFile.Blocks[addr.Block].Clusters[addr.Cluster];
                    target.EraseData(distilledNand.KeyFile);
#endif
                    clusterDeleted++;
                }
            }

            Console.WriteLine("Cluster deletion complete.");
            Console.WriteLine("Purging Filesystem Entries except for root.");

            var emptyFST = Constants.EmptyFST.CastToStruct<RawFileSystemTableEntry>();

            for (var i = 1; i < superBlockTarget.RawFileSystemTableEntries.Length; i++)
            {
                superBlockTarget.RawFileSystemTableEntries[i] = emptyFST;
            }

            Console.WriteLine("Capping root FST.");
            var rootFST = superBlockTarget.RawFileSystemTableEntries[0].SubBigEndian = BitConverter.GetBytes(0xFFFF);
            uint sbVersion = 0;

            // Rewrite every single superblocks to erase any trace of the old files.
            foreach (var desc in distilledNand.SuperBlockDescriptors)
            {
                Console.WriteLine(
                    $"Overwriting Superblock at Cluster 0x{desc.Cluster:X} Offset 0x{desc.Offset:X} Version {desc.Version}");
                Console.WriteLine($"Overwriting Version Number from {desc.Version} to {sbVersion++}");

                var curCluster = desc.Cluster;

                superBlockTarget.VersionBigEndian = BitConverter.GetBytes(sbVersion).Reverse().ToArray();

                var rawSB = superBlockTarget.CastToArray();

                for (var i = 0; i < Constants.SuperBlocksClusterIncrement; i++)
                {
                    var addr = NandAddressTranslationHelper.AbsoluteClusterToBlockCluster(
                        (uint)(curCluster + i));
                    var chunk = rawSB.AsSpan().Slice(i * (int)Constants.NandClusterNoSpareByteSize,
                        (int)Constants.NandClusterNoSpareByteSize);

                    distilledNand.NandDumpFile.Blocks[addr.Block].Clusters[addr.Cluster]
                        .WriteDataNoEncryption(chunk.ToArray());
                }

                SuperBlock.RecalculateHMAC(ref rawSB, distilledNand.NandDumpFile, distilledNand.KeyFile, curCluster);
            }
        }

        private static DistilledNand NandProcessAndCheck(NandDumpFile nandData, KeyFile keyData)
        {
            var foundSuperblocks = new List<SuperBlockDescriptor>();

            for (ushort i = Constants.SuperBlocksBaseCluster;
                i <= Constants.SuperBlocksEndCluster;
                i += Constants.SuperBlocksClusterIncrement)
            {
                var sp = NandAddressTranslationHelper.AbsoluteClusterToBlockCluster(i);

                var sbFirstPage = nandData.Blocks[sp.Block].Clusters[sp.Cluster].Pages[0x0].MainData;

                var sbHeader = sbFirstPage.AsSpan(0x0, 0x4);

                var sfd = sbFirstPage.AsSpan(0x4, 0x4);

                var sbVersion = BitConverter.ToUInt32(sfd.ToArray().Reverse().ToArray());

                if (sbHeader.SequenceEqual(Constants.SuperBlockHeader))
                {
                    var byteOffset = NandAddressTranslationHelper.BCPToOffset(sp.Block, sp.Cluster, 0x0);
                    Console.WriteLine(
                        $"Found a superblock at Cluster 0x{i:X} Offset 0x{byteOffset:X} Version {sbVersion}");
                    foundSuperblocks.Add(new SuperBlockDescriptor(i, byteOffset, sbVersion));
                }
            }

            var mainSuperBlock = foundSuperblocks
                .OrderByDescending(x => x.Version)
                .First();

            Console.WriteLine(
                $"Candidate superblock with highest gen number: Cluster 0x{mainSuperBlock.Cluster:X} Offset 0x{mainSuperBlock.Offset:X} Version {mainSuperBlock.Version}");

            using var hmacsha1 = new HMACSHA1(keyData.NandHMACKey);

            // verify candidate superblock's HMAC and ECC

            var superBlockBuffer = new MemoryStream();

            Span<byte> sbSpare1 = null, sbSpare2 = null;

            for (var i = mainSuperBlock.Cluster; i <= mainSuperBlock.Cluster + 0xF; i++)
            {
                var addr2 = NandAddressTranslationHelper.AbsoluteClusterToBlockCluster(i);

                var cluster2 = nandData.Blocks[addr2.Block].Clusters[addr2.Cluster];

                superBlockBuffer.Write(cluster2.GetRawMainPageData());

                if (i != mainSuperBlock.Cluster + 0xF) continue;

                sbSpare1 = cluster2.Pages[0x6].SpareData;
            }

            var candidateSuperBlockHMAC = sbSpare1[0x1..0x15];

            var sbSalt = new byte[0x40];

            var sbStartClusterBytes = BitConverter.GetBytes(mainSuperBlock.Cluster);

            // this is correct way of generating the sb salt. verified on wiiqt.

            sbSalt[0x12] = sbStartClusterBytes[0x1];
            sbSalt[0x13] = sbStartClusterBytes[0x0];

            using var mm2 = new MemoryStream();
            var mainSuperBlockRaw = superBlockBuffer.ToArray();

            mm2.Write(sbSalt);
            mm2.Write(mainSuperBlockRaw);
            mm2.Position = 0x0;

            if (candidateSuperBlockHMAC.SequenceEqual(hmacsha1.ComputeHash(mm2)))
            {
                Console.WriteLine("Candidate Superblock has valid HMAC.");
            }

            mm2.Close();
            mm2.Dispose();

            var readableSuperBlock = mainSuperBlockRaw.CastToStruct<SuperBlock>();
            Console.WriteLine("Casting Candidate Superblock to a C# struct.");


            var ValidFSTEntries = new Dictionary<uint, RawFileSystemTableEntry>();
            var ValidClusters = new Dictionary<ushort, ushort>();

            var entryCount = 0x0u;
            foreach (var entry in readableSuperBlock.RawFileSystemTableEntries.Where(x => !x.IsEmpty))
            {
                ValidFSTEntries.Add(entryCount, entry);
                entryCount++;
            }

            Console.WriteLine($"Found {entryCount} Filesystem Table Entries.");

            var badClusters = 0x0u;
            var reservedClusters = 0x0u;
            var freeClusters = 0x0u;
            var chainLastClusters = 0x0u;

            ushort clusterIndex = 0x0;

            foreach (var clusterDesc in readableSuperBlock.ClusterEntries.Select(ByteWiseSwap))
            {
                switch ((ClusterDescriptor)clusterDesc)
                {
                    case ClusterDescriptor.Bad:
                        badClusters++;
                        clusterIndex++;
                        continue;
                    case ClusterDescriptor.Reserved:
                        reservedClusters++;
                        clusterIndex++;
                        continue;
                    case ClusterDescriptor.Empty:
                        freeClusters++;
                        break;
                    case ClusterDescriptor.ChainLast:
                        chainLastClusters++;
                        break;
                }

                ValidClusters.Add(clusterIndex, clusterDesc);
                clusterIndex++;
            }

            Console.WriteLine($"Reserved Clusters:  {reservedClusters}");
            Console.WriteLine($"Bad Clusters:       {badClusters}");
            Console.WriteLine($"Free Clusters:      {freeClusters}");

            var nodes = new Dictionary<uint, RawFileSystemNode>();

            Console.WriteLine("Connecting File FST's to their clusters.");

            // process files first.
            foreach (var entry in ValidFSTEntries)
            {
                var rFST = entry.Value.ToReadableFST();

                if (!rFST.IsFile)
                {
                    var newDir = new RawFileSystemNode()
                    {
                        IsFile = false,
                        Filename = rFST.FileName,
                        FSTEntry = rFST,
                        FSTEntryIndex = entry.Key,
                    };

                    nodes.Add(entry.Key, newDir);
                }
                else
                {
                    var startingCluster = rFST.Sub;

                    var newFile = new RawFileSystemNode()
                    {
                        IsFile = true,
                        Filename = rFST.FileName,
                        FSTEntry = rFST,
                        FSTEntryIndex = entry.Key,
                    };

                    newFile.Clusters.Add(startingCluster);

                    var nextCluster = startingCluster;

                    var estimatedClusterCount = (rFST.FileSize / Constants.NandClusterNoSpareByteSize) + 0x1;

                    var watchdogCounter = 0x0;

                    while (watchdogCounter < estimatedClusterCount && rFST.FileSize > 0)
                    {
                        var curCluster = ValidClusters[nextCluster];

                        if ((ClusterDescriptor)curCluster == ClusterDescriptor.ChainLast)
                            break;

                        newFile.Clusters.Add(curCluster);

                        nextCluster = curCluster;

                        watchdogCounter++;
                    }

                    nodes.Add(entry.Key, newFile);
                }
            }

            Console.WriteLine("Connecting Directory FST's to their children.");

            // then connect directories
            foreach (var entry in nodes)
            {
                var dirNode = entry.Value;

                if (dirNode.IsFile) continue;

                var sub = entry.Value.FSTEntry.Sub;

                if (sub == Constants.FSTSubEndCapValue || !nodes.ContainsKey(sub))
                    continue;

                dirNode.Children.Add(nodes[sub]);

                var sib = dirNode.Children[0x0].FSTEntry.Sib;

                var currentSib = sib;

                while (true)
                {
                    if (currentSib == Constants.FSTSubEndCapValue || !nodes.ContainsKey(currentSib))
                        break;

                    var curSibNode = nodes[currentSib];
                    curSibNode.Parent = curSibNode;
                    dirNode.Children.Add(curSibNode);
                    currentSib = curSibNode.FSTEntry.Sib;
                }
            }

            Console.WriteLine("Verifying File Clusters Integrity...");

            foreach (var entry in nodes)
            {
                var file = entry.Value;

                if (!file.IsFile) continue;

                var saltF = new byte[0x40];

                var rawFST = entry.Value.FSTEntry.Source;
                rawFST.UserIDBigEndian.CopyTo(saltF.AsSpan()[..4]);
                rawFST.FileName.CopyTo(saltF.AsSpan()[0x4..]);

                var fstIndex = BitConverter.GetBytes(entry.Value.FSTEntryIndex).Reverse().ToArray();
                fstIndex.CopyTo(saltF.AsSpan().Slice(0x14, 4));
                rawFST.X3.CopyTo(saltF.AsSpan().Slice(0x18, 4));

                for (var i = 0x0; i < file.Clusters.Count; i++)
                {
                    if ((ClusterDescriptor)file.Clusters[i] == ClusterDescriptor.ChainLast)
                        break;

                    var clusterIndex1 = BitConverter.GetBytes((uint)i).Reverse().ToArray();
                    clusterIndex1.CopyTo(saltF.AsSpan().Slice(0x10, 4));

                    var dataAdr = NandAddressTranslationHelper.AbsoluteClusterToBlockCluster(file.Clusters[i]);
                    var rawCluster = nandData.Blocks[dataAdr.Block].Clusters[dataAdr.Cluster];
                    var decryptedCluster = rawCluster.DecryptCluster(keyData);

                    using var mmx = new MemoryStream();
                    mmx.Write(saltF);
                    mmx.Write(decryptedCluster);
                    mmx.Position = 0;

                    var calculatedHMACx = ToHex(hmacsha1.ComputeHash(mmx));

                    var nandClusterHMACx = ToHex(rawCluster.Pages[0x6].SpareData[0x1..0x15]);

                    if (calculatedHMACx != nandClusterHMACx)
                        Console.WriteLine($"{file.Filename} Cluster {file.Clusters[i]:x4} HMAC verification failed!");

                    if (!rawCluster.CheckECC())
                        Console.WriteLine($"{file.Filename} Cluster {file.Clusters[i]:x4} ECC verification failed!");
                }
            }

            Console.WriteLine("File Clusters Integrity Verified...");

            var rootNode = nodes.First(x => x.Value.Filename == "/");

            rootNode.Value.PrintPretty();

            return new DistilledNand(nandData, keyData, foundSuperblocks, mainSuperBlock, mainSuperBlockRaw,
                rootNode.Value,
                ValidClusters);
        }


        public static ushort ByteWiseSwap(ushort value)
        {
            return (ushort)((0x00FF & (value >> 8))
                            | (0xFF00 & (value << 8)));
        }

        public static uint ByteWiseSwap(uint value)
        {
            uint swapped = (0x000000FF) & (value >> 24)
                           | (0x0000FF00) & (value >> 8)
                           | (0x00FF0000) & (value << 8)
                           | (0xFF000000) & (value << 24);
            return swapped;
        }

        private static string ToHex(byte[] inx)
        {
            return BitConverter.ToString(inx).Replace("-", "");
        }
    }
}