using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Niind.Structures;

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
            (0x100000009ul, 0x40A), // IOS9
            (0x10000000Cul, 0x20E), // IOS12
            (0x10000000Dul, 0x408), // IOS13
            (0x10000000Eul, 0x408), // IOS14
            (0x10000000Ful, 0x408), // IOS15
            (0x100000011ul, 0x408), // IOS17
            (0x100000015ul, 0x40F), // IOS21
            (0x100000016ul, 0x50E), // IOS22
            (0x10000001Cul, 0x70F), // IOS28
            (0x10000001Ful, 0xE18), // IOS31
            (0x100000021ul, 0xE18), // IOS33
            (0x100000021ul, 0xE18), // IOS34
            (0x100000023ul, 0xE18), // IOS35
            (0x100000024ul, 0xE18), // IOS36
            (0x100000025ul, 0x161F), // IOS37
            (0x100000026ul, 0x101C), // IOS38
            (0x100000028ul, 0xC00), // IOS40
            (0x100000029ul, 0xE17), // IOS41
            (0x10000002Bul, 0xE17), // IOS43
            (0x10000002Dul, 0xE17), // IOS45
            (0x10000002Eul, 0xE17), // IOS46
            (0x100000030ul, 0x101C), // IOS48
            (0x100000034ul, 0x1700), // IOS52
            (0x100000035ul, 0x161F), // IOS53
            (0x100000037ul, 0x161F), // IOS55
            (0x100000038ul, 0x161E), // IOS56
            (0x100000039ul, 0x171F), // IOS57
            (0x10000003Aul, 0x1820), // IOS58
            (0x10000003Dul, 0x161E), // IOS61
            (0x100000046ul, 0x1B00), // IOS70
            (0x100000050ul, 0x1B20), // IOS80
            (0x1000000FEul, 0xFF00), // IOS254
            (0x100000002ul, 0x201), // SystemMenu 4.3U
            (0x1000248414241ul, 0x14), // ShopChannel
            (0x1000848414B45ul, 0x3) // EULA
        };
    }
}