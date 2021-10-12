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
            /*
0x100000004ul v65280
0x10000000aul v768
0x10000000bul v256
0x100000010ul v512
0x100000014ul
0x10000001eul
0x100000022ul
0x100000032ul
0x100000033ul
0x10000003cul
0x1000000deul
0x1000000dful
0x1000000f9ul
0x1000000faul
0x100000100ul
0x100000101ul
0x1000248414141ul
0x1000248414341ul
0x1000248414641ul
0x1000248414645ul
0x1000248414741ul
0x1000248414745ul
0x1000248415941ul
0x1000848414c45ul






0000000100000002v513
Received a completed download from NUS
Installed title 0000000100000002  to nand
 

0000000100000009v1034
Received a completed download from NUS
Installed title 0000000100000009 v1034 to nand
 
 
000000010000000cv526
Received a completed download from NUS
Installed title 000000010000000c v526 to nand

000000010000000dv1032
Received a completed download from NUS
Installed title 000000010000000d v1032 to nand

000000010000000ev1032
Received a completed download from NUS
Installed title 000000010000000e v1032 to nand

000000010000000fv1032
Received a completed download from NUS
Installed title 000000010000000f v1032 to nand
 

0000000100000011v1032
Received a completed download from NUS
Installed title 0000000100000011 v1032 to nand

0000000100000014v256
Received a completed download from NUS
Installed title 0000000100000014 v256 to nand

0000000100000015v1039
Received a completed download from NUS
Installed title 0000000100000015 v1039 to nand

0000000100000016v1294
Received a completed download from NUS
Installed title 0000000100000016 v1294 to nand

000000010000001cv1807
Received a completed download from NUS
Installed title 000000010000001c v1807 to nand

000000010000001ev2816
Received a completed download from NUS
Installed title 000000010000001e v2816 to nand

000000010000001fv3608
Received a completed download from NUS
Installed title 000000010000001f v3608 to nand

0000000100000021v3608
Received a completed download from NUS
Installed title 0000000100000021 v3608 to nand

0000000100000022v3348
Received a completed download from NUS
Installed title 0000000100000022 v3348 to nand

0000000100000023v3608
Received a completed download from NUS
Installed title 0000000100000023 v3608 to nand

0000000100000024v3608
Received a completed download from NUS
Installed title 0000000100000024 v3608 to nand

0000000100000025v5663
Received a completed download from NUS
Installed title 0000000100000025 v5663 to nand

0000000100000026v4124
Received a completed download from NUS
Installed title 0000000100000026 v4124 to nand

0000000100000028v3072
Received a completed download from NUS
Installed title 0000000100000028 v3072 to nand

0000000100000029v3607
Received a completed download from NUS
Installed title 0000000100000029 v3607 to nand

000000010000002bv3607
Received a completed download from NUS
Installed title 000000010000002b v3607 to nand

000000010000002dv3607
Received a completed download from NUS
Installed title 000000010000002d v3607 to nand

000000010000002ev3607
Received a completed download from NUS
Installed title 000000010000002e v3607 to nand

0000000100000030v4124
Received a completed download from NUS
Installed title 0000000100000030 v4124 to nand

0000000100000032v5120
Received a completed download from NUS
Installed title 0000000100000032 v5120 to nand

0000000100000033v4864
Received a completed download from NUS
Installed title 0000000100000033 v4864 to nand

0000000100000034v5888
Received a completed download from NUS
Installed title 0000000100000034 v5888 to nand

0000000100000035v5663
Received a completed download from NUS
Installed title 0000000100000035 v5663 to nand

0000000100000037v5663
Received a completed download from NUS
Installed title 0000000100000037 v5663 to nand

0000000100000038v5662
Received a completed download from NUS
Installed title 0000000100000038 v5662 to nand

0000000100000039v5919
Received a completed download from NUS
Installed title 0000000100000039 v5919 to nand

000000010000003av6176
Received a completed download from NUS
Installed title 000000010000003a v6176 to nand

000000010000003cv6400
Received a completed download from NUS
Installed title 000000010000003c v6400 to nand

000000010000003dv5662
Received a completed download from NUS
Installed title 000000010000003d v5662 to nand

0000000100000046v6912
Received a completed download from NUS
Installed title 0000000100000046 v6912 to nand

0000000100000050v6944
Received a completed download from NUS
Installed title 0000000100000050 v6944 to nand

00000001000000dev65280
Received a completed download from NUS
Installed title 00000001000000de v65280 to nand

00000001000000dfv65280
Received a completed download from NUS
Installed title 00000001000000df v65280 to nand

00000001000000f9v65280
Received a completed download from NUS
Installed title 00000001000000f9 v65280 to nand

00000001000000fav65280
Received a completed download from NUS
Installed title 00000001000000fa v65280 to nand

00000001000000fev65280
Received a completed download from NUS
Installed title 00000001000000fe v65280 to nand

0000000100000100v6
Received a completed download from NUS
Installed title 0000000100000100 v6 to nand

0000000100000101v10
Received a completed download from NUS
Installed title 0000000100000101 v10 to nand

0001000248414141v2
Received a completed download from NUS
Installed title 0001000248414141 v2 to nand

0001000248414241v20
Received a completed download from NUS
Installed title 0001000248414241 v20 to nand

0001000248414341v6
Received a completed download from NUS
Installed title 0001000248414341 v6 to nand

0001000248414641v3
Received a completed download from NUS
Installed title 0001000248414641 v3 to nand

0001000248414645v7
Received a completed download from NUS
Installed title 0001000248414645 v7 to nand

0001000248414741v3
Received a completed download from NUS
Installed title 0001000248414741 v3 to nand

0001000248414745v7
Received a completed download from NUS
Installed title 0001000248414745 v7 to nand

0001000248415941v3
Received a completed download from NUS
Installed title 0001000248415941 v3 to nand

0001000848414b45v3
Received a completed download from NUS
Installed title 0001000848414b45 v3 to nand

0001000848414c45v2
Received a completed download from NUS
Installed title 0001000848414c45 v2 to nand
*/
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