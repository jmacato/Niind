using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using Niind.Structures;

namespace Niind
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Loading Files...");

            var rawFullDump =
                File.ReadAllBytes("/Users/jumarmacato/Desktop/Wii NAND Experiment/wiinandfolder/nand-04z02504.bin");
            Console.WriteLine("Nand File Loaded.");

            var rawKeyFile =
                File.ReadAllBytes("/Users/jumarmacato/Desktop/Wii NAND Experiment/wiinandfolder/keys-04z02504.bin");
            Console.WriteLine("Key File Loaded.");

            NandDump nandData;
            KeyFile keyFileData;

            unsafe
            {
                fixed (void* x = &rawFullDump[0])
                    nandData = Marshal.PtrToStructure<NandDump>(new IntPtr(x));

                Console.WriteLine("NAND Dump marshalled to C# structs.");

                fixed (void* x = &rawKeyFile[0])
                    keyFileData = Marshal.PtrToStructure<KeyFile>(new IntPtr(x));

                Console.WriteLine("Key File marshalled to C# structs.");
            }

            if (Marshal.SizeOf(nandData) != rawFullDump.LongLength)
            {
                throw new FormatException("The NAND dump's internal structure is not correct!" +
                                          " Try to dump your console via BootMii again.");
            }

            var bootMiiHeaderText = Encoding.ASCII.GetString(nandData.BootMiiFooterBlock.HeaderString).Trim((char)0)
                .Trim('\n');

            Console.WriteLine($"BootMii Metadata Header: {bootMiiHeaderText}");

            var consoleId = BitConverter.ToString(keyFileData.ConsoleID).Replace("-", "");

            if (!bootMiiHeaderText.Contains(consoleId, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidOperationException("The Key File provided is not for this specific NAND dump.");
            }

            Console.WriteLine($"Key file matches the NAND dump.");

            var superBlockBaseAbsClusterAddress = 0x7F00u;
            var superBlockAbsClusterAddressIncrement = 16u;
            var superBlocks = new List<(uint absoluteCluster, long baseOffset, uint generationNumber)>();

            for (uint i = superBlockBaseAbsClusterAddress; i <= 0x7FFFu; i += superBlockAbsClusterAddressIncrement)
            {
                var sp = NandAddressTranslation.AbsoluteClusterToBlockCluster(i);
                var f1 = nandData.Blocks[sp.Block].Clusters[sp.Cluster];
                var header = Encoding.ASCII.GetString(f1.Pages[0].MainData[0..4]);
                if (header == "SFFS")
                {
                    var absOffset = NandAddressTranslation.BCPToByte(sp.Block, sp.Cluster, 0);
                    Console.WriteLine($"Found a superblock at Cluster 0x{i:X} Offset 0x{absOffset:X} ");
                    superBlocks.Add((i, absOffset));
                }
            }
        }

        public static class NandAddressTranslation
        {
            public static (uint Block, uint Cluster) AbsoluteClusterToBlockCluster(uint absoluteCluster)
            {
                var block = (uint)Math.Floor((float)absoluteCluster / 8);
                var cluster = absoluteCluster % 8;
                return (block, cluster);
            }

            static readonly long NandBlockByteSize = Marshal.SizeOf<NandBlock>();
            static readonly long NandClusterByteSize = Marshal.SizeOf<NandCluster>();
            static readonly long NandPageByteSize = Marshal.SizeOf<NandPage>();

            public static long BCPToByte(uint block, uint cluster, uint page)
            {
                var b = block * NandBlockByteSize;
                var c = cluster * NandClusterByteSize;
                var p = page * NandPageByteSize;

                return b + c + p;
            }
        }
    }
}