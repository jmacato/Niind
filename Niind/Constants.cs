using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Niind.Structures;

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
    }
}