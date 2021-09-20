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

                var sbGenNumber = SpanToBigEndianUInt(sbFirstPage.AsSpan(5, 4));

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

            uint clNo = 0x2ce;

            var addr = AddressTranslation.AbsoluteClusterToBlockCluster(clNo);
            var cluster = nandData.Blocks[addr.Block].Clusters[addr.Cluster];

            var data = cluster.DecryptCluster(keyData);

            var dataHash = Simplehash(data, data.Length);

            using var xc = new HMACSHA1(keyData.NandHMACKey);


            var salt = new byte[0x40]
            {
                0x00, 0x00, 0x10, 0x00, 0x73, 0x65, 0x74, 0x74, 0x69, 0x6E,
                0x67, 0x2E, 0x74, 0x78, 0x74, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                0x00, 0x00, 0x00, 0x00
            };

            using (var sds = new SHA1Managed())
            {
                var os = Encoding.ASCII.GetBytes("Hello World");
                var s = ToHex(sds.ComputeHash(os));
                var ctx = new SHA1.SHA1Context();

                var sx = new SHA1();
                sx.SHA1Reset(ctx);
                sx.SHA1Input(ctx, os, os.Length);
                var h = sx.SHA1Result(ctx);

                var sxx = ToHex(ctx.GetHashBytes());

                if (s != sxx)
                {
                    throw new Exception("The universe is broken. SHA1 ported from C didnt match with C#'s");
                }
            }

            var ksd = new HMACSHA1Wii(keyData);
            
            ksd.hmac_update(salt, salt.Length);
            ksd.hmac_update(data, data.Length);
            var hash = ksd.hmac_final();


            var mm = new MemoryStream();
            
            mm.Write(salt);
            mm.Write(data);

            mm.Position = 0;

            var asd = ToHex(xc.ComputeHash(mm));
            
            var x = ToHex(hash);

            var nandClusterHMAC = ToHex(cluster.Pages[6].SpareData[1..21]);

            Console.WriteLine("Finished.");
        }

        static string ToHex(byte[] inx) => BitConverter.ToString(inx).Replace("-", "");

        static int Simplehash(byte[] data, int size)
        {
            int result = 0x7e7e;

            for (int i = 0; i < size; ++i)
            {
                int dax = data[i];
                result ^= dax++;
            }

            return result;
        }

        static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public static uint SpanToBigEndianUInt(Span<byte> input)
        {
            input.Reverse();
            return BitConverter.ToUInt32(input.ToArray(), 0);
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