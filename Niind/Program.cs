using System;
using System.Buffers;
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
    internal class Program
    {
        private static readonly ReadOnlyMemory<byte> SuperBlockHeaderBytes = Encoding.ASCII.GetBytes("SFFS").AsMemory();

        private static void Main(string[] args)
        {
            Console.WriteLine("Loading Files...");

            var rawFullDump =
                File.ReadAllBytes("/Users/jumarmacato/Desktop/Wii NAND Experiment/wiinandfolder/nand-04z02504.bin");
            Console.WriteLine("Nand File Loaded.");

            var rawKeyFile =
                File.ReadAllBytes("/Users/jumarmacato/Desktop/Wii NAND Experiment/wiinandfolder/keys-04z02504.bin");

            Console.WriteLine("Key File Loaded.");

            var nandData = rawFullDump.CastToStruct<NandDumpFile>();

            Console.WriteLine("NAND Dump marshalled to C# structs.");

            var keyData = rawKeyFile.CastToStruct<KeyFile>();

            Console.WriteLine("Key file marshalled to C# structs.");

            if (Marshal.SizeOf(nandData) != rawFullDump.LongLength)
                throw new FormatException("The NAND dump's internal structure is not correct!" +
                                          " Try to dump your console via BootMii again.");

            var bootMiiHeaderText = Encoding.ASCII.GetString(nandData.BootMiiFooterBlock.HeaderString).Trim((char)0)
                .Trim('\n');

            Console.WriteLine($"BootMii Metadata Header: {bootMiiHeaderText}");

            var consoleId = BitConverter.ToString(keyData.ConsoleID).Replace("-", "");

            if (!bootMiiHeaderText.Contains(consoleId, StringComparison.InvariantCultureIgnoreCase))
                throw new InvalidOperationException("The Key File provided is not for this specific NAND dump.");

            Console.WriteLine($"Key file matches the NAND dump.");

            var foundSuperblocks = new List<(uint absoluteCluster, long baseOffset, uint generationNumber)>();

            for (var i = Constants.SuperblocksBaseCluster;
                i <= Constants.SuperblocksEndCluster;
                i += Constants.SuperblocksClusterIncrement)
            {
                var sp = AddressTranslation.AbsoluteClusterToBlockCluster(i);

                var sbFirstPage = nandData.Blocks[sp.Block].Clusters[sp.Cluster].Pages[0].MainData;

                var sbHeader = sbFirstPage.AsSpan(0, 4);

                var sbGenNumber = SpanToBigEndianUInt(sbFirstPage.ToArray().AsSpan(5, 4));

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

            uint clNo = 0x2ce; //Setting.txt Cluster address

            var addr = AddressTranslation.AbsoluteClusterToBlockCluster(clNo);

            var cluster = nandData.Blocks[addr.Block].Clusters[addr.Cluster];

            var sampleDataClusterMainData = cluster.DecryptCluster(keyData);

            using var hmacsha1 = new HMACSHA1(keyData.NandHMACKey);

            // Salts needs to be 0x40 long... this is what tripping up the hmac before :facepalm:
            var sampleDataClusterSalt = new byte[0x40]
            {
                0x00, 0x00, 0x10, 0x00, 0x73, 0x65, 0x74, 0x74, 0x69, 0x6E,
                0x67, 0x2E, 0x74, 0x78, 0x74, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00
            };

            var mm = new MemoryStream();
            mm.Write(sampleDataClusterSalt);
            mm.Write(sampleDataClusterMainData);
            mm.Position = 0;

            var calculatedHMAC = ToHex(hmacsha1.ComputeHash(mm));


            var nandClusterHMAC = ToHex(cluster.Pages[6].SpareData[1..21]);

            if (calculatedHMAC == nandClusterHMAC)
            {
                Console.WriteLine("Cluster 0x2ce HMAC checks out.");
            }

            mm.Close();
            mm.Dispose();

            // verify candidate superblock's HMAC and ECC

            var superBlockBuffer = new MemoryStream();
            var sbBufCount = 0;

            Span<byte> sbSpare1 = null, sbSpare2 = null;

            for (uint i = candidateSb.absoluteCluster; i <= candidateSb.absoluteCluster + 0x0f; i++)
            {
                var addr2 = AddressTranslation.AbsoluteClusterToBlockCluster(i);

                var cluster2 = nandData.Blocks[addr2.Block].Clusters[addr2.Cluster];

                superBlockBuffer.Write(cluster2.GetRawMainPageData());

                if (i != candidateSb.absoluteCluster + 0x0f) continue;

                sbSpare1 = cluster2.Pages[6].SpareData;
                sbSpare2 = cluster2.Pages[7].SpareData;
            }

            var dbg0 = ToHex(sbSpare1.ToArray());
            var dbg1 = ToHex(sbSpare2.ToArray());

            var candidateSuperBlockHMAC = sbSpare1[1..21];

            var sbSalt = new byte[0x40];
            var sbStartAbsClusterBytes = BitConverter.GetBytes(candidateSb.absoluteCluster);

            sbSalt.AsSpan().Fill(0);

            // this is correct way of generating the sb salt. verified on wiiqt.

            sbSalt[0x12] = sbStartAbsClusterBytes[1];
            sbSalt[0x13] = sbStartAbsClusterBytes[0];

            using var mm2 = new MemoryStream();

            var hmac2 = new HMACSHA1(keyData.NandHMACKey);
            var xzx = superBlockBuffer.ToArray();
            
            mm2.Write(sbSalt);
            mm2.Write(xzx);
            mm2.Position = 0;

            var dbg2 = ToHex(candidateSuperBlockHMAC.ToArray());
            var dbg4 = ToHex(hmac2.ComputeHash(mm2));
 
            mm2.Close();
            mm2.Dispose();


            Console.WriteLine("Finished.");
        }

        private static string ToHex(byte[] inx)
        {
            return BitConverter.ToString(inx).Replace("-", "");
        }

        private static int Simplehash(byte[] data, int size)
        {
            var result = 0x7e7e;

            for (var i = 0; i < size; ++i)
            {
                int dax = data[i];
                result ^= dax++;
            }

            return result;
        }

        private static byte[] StringToByteArray(string hex)
        {
            var NumberChars = hex.Length;
            var bytes = new byte[NumberChars / 2];
            for (var i = 0; i < NumberChars; i += 2)
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