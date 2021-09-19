using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using Niind.Structures;

namespace Niind
{
    class Program
    {
        private static readonly ReadOnlyMemory<byte> SuperBlockHeaderBytes = Encoding.ASCII.GetBytes("SFFS").AsMemory();

        static void Main(string[] args)
        {
            Console.WriteLine("Loading Files...");

            var rawFullDump =
                File.ReadAllBytes("/Users/jumarmacato/Desktop/Wii NAND Experiment/wiinandfolder/nand-04z02504.bin");
            Console.WriteLine("Nand File Loaded.");

            var rawKeyFile =
                File.ReadAllBytes("/Users/jumarmacato/Desktop/Wii NAND Experiment/wiinandfolder/keys-04z02504.bin");
            
            Console.WriteLine("Key File Loaded.");

            var weqwe = "10-B1-CA-5D-23-C5-B4-56-CF-C2-37-50-CE-D4-08-C7".Replace("-", "");
            
            var nandData = rawFullDump.CastToStruct<NandDumpFile>();
  
            Console.WriteLine("NAND Dump marshalled to C# structs.");
            
            var keyData = rawKeyFile.CastToStruct<KeyFile>();
            
            Console.WriteLine("Key file marshalled to C# structs.");

            if (Marshal.SizeOf(nandData) != rawFullDump.LongLength)
            {
                throw new FormatException("The NAND dump's internal structure is not correct!" +
                                          " Try to dump your console via BootMii again.");
            }

            var bootMiiHeaderText = Encoding.ASCII.GetString(nandData.BootMiiFooterBlock.HeaderString).Trim((char)0)
                .Trim('\n');

            Console.WriteLine($"BootMii Metadata Header: {bootMiiHeaderText}");

            var consoleId = BitConverter.ToString(keyData.ConsoleID).Replace("-", "");

            if (!bootMiiHeaderText.Contains(consoleId, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidOperationException("The Key File provided is not for this specific NAND dump.");
            }

            Console.WriteLine($"Key file matches the NAND dump.");
            
            var foundSuperblocks = new List<(uint absoluteCluster, long baseOffset, uint generationNumber)>();

            var sbBaseAbsClusterAddress = 0x7F00u;
            var sbEndAbsClusterAddress = 0x7FFFu;
            var sbAbsClusterAddressIncrement = 16u;
            
            for (var i = sbBaseAbsClusterAddress; i <= sbEndAbsClusterAddress; i += sbAbsClusterAddressIncrement)
            {
                var sp = NandAddressTranslation.AbsoluteClusterToBlockCluster(i);

                var sbFirstPage = nandData.Blocks[sp.Block].Clusters[sp.Cluster].Pages[0].MainData;

                var sbHeader = sbFirstPage.AsSpan(0, 4);

                var sbGenNumber = BitConverter.ToUInt32(sbFirstPage.AsSpan(5, 4));

                if (sbHeader.SequenceEqual(SuperBlockHeaderBytes.Span))
                {
                    var absOffset = NandAddressTranslation.BCPToOffset(sp.Block, sp.Cluster, 0);
                    Console.WriteLine(
                        $"Found a superblock at Cluster 0x{i:X} Offset 0x{absOffset:X} Generation Number  0x{sbGenNumber:X}");
                    foundSuperblocks.Add((i, absOffset, sbGenNumber));
                }
            }

            var candidateSb = foundSuperblocks
                .OrderByDescending(x => x.generationNumber)
                .First();

            Console.WriteLine(
                $"Candidate superblock with highest gen number: Cluster 0x{candidateSb.absoluteCluster:X} Offset 0x{candidateSb.baseOffset:X} Generation Number 0x{candidateSb.generationNumber:X}");

            
            for (uint i = 0x40; i < 0x7EFF; i++)
            {
                var xzz = NandAddressTranslation.AbsoluteClusterToBlockCluster(i);
                var xc = nandData.Blocks[xzz.Block].Clusters[xzz.Cluster].DecryptCluster(keyData);
                var hzx = Encoding.UTF8.GetString(xc);
                if (hzx.Contains("JPN"))
                {
                }
            }
            
            Console.WriteLine("Finished.");
            //
            // var xzz = NandAddressTranslation.AbsoluteClusterToBlockCluster(0x40);
            //
            // var xc = nandData.Blocks[xzz.Block].Clusters[xzz.Cluster].DecryptCluster(keyData);
            // var hzx = Encoding.UTF8.GetString(xc);
            //
            // var xczxc = nandData.Blocks[xzz.Block].Clusters[xzz.Cluster].Pages[0].IsECCCorrect();
        }

        public static class NandAddressTranslation
        {
            public static (uint Block, uint Cluster) AbsoluteClusterToBlockCluster(uint absoluteCluster)
            {
                var block = (uint)Math.Floor((float)absoluteCluster / 8);
                var cluster = absoluteCluster % 8;
                return (block, cluster);
            }

            public static long BCPToOffset(uint block, uint cluster, uint page)
            {
                var b = block * Constants.NandBlockByteSize;
                var c = cluster * Constants.NandClusterByteSize;
                var p = page * Constants.NandPageByteSize;
                return b + c + p;
            }
        }
    }
}