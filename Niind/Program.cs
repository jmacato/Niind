using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
                var sp = AddressTranslation.AbsoluteClusterToBlockCluster(i);

                var sbFirstPage = nandData.Blocks[sp.Block].Clusters[sp.Cluster].Pages[0].MainData;

                var sbHeader = sbFirstPage.AsSpan(0, 4);

                var sbGenNumber = BitConverter.ToUInt32(sbFirstPage.AsSpan(5, 4));

                if (sbHeader.SequenceEqual(SuperBlockHeaderBytes.Span))
                {
                    var absOffset = AddressTranslation.BCPToOffset(sp.Block, sp.Cluster, 0);
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


            string ToHex(byte[] inx) => BitConverter.ToString(inx).Replace("-", "");
            // string ToHex(byte[] inx) => Encoding.UTF8.GetString(inx);
            
            // Testing filesystem data cluster HMAC.
            var clNo = 0x2CEu;
            
            var j = AddressTranslation.AbsoluteClusterToBlockCluster(clNo); // Test to see the hmac data they're saying.
            var x = AddressTranslation.BCPToOffset(j.Block, j.Cluster, 0) == 12131328;
            var xc = new HMACSHA1(keyData.NandHMACKey);
            
            var curcl = nandData.Blocks[j.Block].Clusters[j.Cluster];
            
            var mk = ToHex(keyData.NandHMACKey);

            int simplehash(byte[] data, int size)
            {
                int result = 0x7e7e;

                for (int i = 0; i < size; ++i)
                {
                    int dax = data[i];
                    result ^= dax++;
                }
                return result;
            }

            var dec = curcl.DecryptCluster(keyData);
            var sad = ToHex(dec);
            var part = sad.Contains("BBA6AC929A04CC77");
            var adsda = simplehash(dec, 0x4000);

            var a1 = ToHex(curcl.Pages[0].SpareData);
            var a2 = ToHex(curcl.Pages[1].SpareData);
            var a3 = ToHex(curcl.Pages[2].SpareData);
            var a4 = ToHex(curcl.Pages[3].SpareData);
            var a5 = ToHex(curcl.Pages[4].SpareData);
            var a6 = ToHex(curcl.Pages[5].SpareData);
            var a7 = ToHex(curcl.Pages[6].SpareData);
            var a8 = ToHex(curcl.Pages[7].SpareData);

            var p = curcl.Pages[7].SpareData;
 
            var extrasaltstr =
                "0000100073657474696E672E74787400000000000000002000000000000000000000000000000000000000000000000000000000000000000000000000000000";
            
            xc.ComputeHash(StringToByteArray(extrasaltstr));
            xc.ComputeHash(dec);

            var hmc = ToHex(xc.Hash);


            Console.WriteLine("Finished.");
        }
 
        
       static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        
        public static class AddressTranslation
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