using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Niind.Structures.FileSystem;

// ReSharper disable All

namespace Niind
{
    public class Constants
    {
        public static readonly byte[] EmptyAESIVBytes = new byte[0x10];
        public static readonly byte[] EmptyECCBytes = new byte[0x10];
        public static readonly uint NandBlockByteSize = (uint)Marshal.SizeOf<NandBlock>();
        public static readonly uint NandClusterByteSize = (uint)Marshal.SizeOf<NandCluster>();
        public static readonly uint NandPageByteSize = (uint)Marshal.SizeOf<NandPage>();
        public static readonly uint NandPageNoSpareByteSize = (uint)Marshal.SizeOf<NandPage>() - 0x40;
        public static readonly uint NandClusterNoSpareByteSize = NandPageNoSpareByteSize * 0x8;
        public static readonly ushort SuperBlocksBaseCluster = 0x7F00;
        public static readonly ushort SuperBlocksEndCluster = 0x7FFF;
        public static readonly ushort SuperBlocksClusterIncrement = 0x10;
        public static readonly uint FSTSubEndCapValue = uint.MaxValue;

        public static byte[] EmptyFST = new byte[0x20];
        public static byte[] EmptyPageRawData = new byte[NandPageNoSpareByteSize];
        public static byte[] SuperBlockHeader = Encoding.ASCII.GetBytes("SFFS");


        public static string WiiShopUserAgent = "Opera/9.00 (Nintendo Wii; U; ; 1038-58; Wii Shop Channel/1.0; en)";
        public static string UpdaterUserAgent = "wii libnup/1.0";
        public static string VirtualConsoleUserAgent = "libec-3.0.7.06111123";

        public static string WiiConnect24UserAgent = "WiiConnect24/1.0FC4plus1 (build 061114161108)";

        public static Uri NUSBaseUrl = new Uri("http://nus.cdn.shop.wii.com/ccs/download/");


        public static List<(ulong TicketID, uint Version)> Version4_3U_Titles = new()
        {
            (0x100000002,0x201),
            (0x100000004,0xFF00),
            (0x100000009,0x40A),
            (0x10000000A,0x300),
            (0x10000000B,0x100),
            (0x10000000C,0x20E),
            (0x10000000D,0x408),
            (0x10000000E,0x408),
            (0x10000000F,0x408),
            (0x100000010,0x200),
            (0x100000011,0x408),
            (0x100000014,0x100),
            (0x100000015,0x40F),
            (0x100000016,0x50E),
            (0x10000001C,0x70F),
            (0x10000001E,0xB00),
            (0x10000001F,0xE18),
            (0x100000021,0xE18),
            (0x100000022,0xD14),
            (0x100000023,0xE18),
            (0x100000024,0xE18),
            (0x100000025,0x161F),
            (0x100000026,0x101C),
            (0x100000028,0xC00),
            (0x100000029,0xE17),
            (0x10000002B,0xE17),
            (0x10000002D,0xE17),
            (0x10000002E,0xE17),
            (0x100000030,0x101C),
            (0x100000032,0x1400),
            (0x100000033,0x1300),
            (0x100000034,0x1700),
            (0x100000035,0x161F),
            (0x100000037,0x161F),
            (0x100000038,0x161E),
            (0x100000039,0x171F),
            (0x10000003A,0x1820),
            (0x10000003C,0x1900),
            (0x10000003D,0x161E),
            (0x100000046,0x1B00),
            (0x100000050,0x1B20),
            (0x1000000DE,0xFF00),
            (0x1000000DF,0xFF00),
            (0x1000000F9,0xFF00),
            (0x1000000FA,0xFF00),
            (0x1000000FE,0xFF00),
            (0x100000100,0x6),
            (0x100000101,0xA),
            (0x1000248414141,0x2),
            (0x1000248414241,0x14),
            (0x1000248414341,0x6),
            (0x1000248414641,0x3),
            (0x1000248414645,0x7),
            (0x1000248414741,0x3),
            (0x1000248414745,0x7),
            (0x1000248415941,0x3),
            (0x1000848414B45,0x3),
            (0x1000848414C45,0x2),
        };
    }
}