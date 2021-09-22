using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Niind.Structures;

namespace Niind
{
    public class Constants
    {
        public static readonly byte[] EmptyAESIVBytes = new byte[0x10];

        public static readonly byte[] EmptyECCBytes = new byte[0x10]
        {
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
            0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        };

        public static readonly long NandBlockByteSize = Marshal.SizeOf<NandBlock>();
        public static readonly long NandClusterByteSize = Marshal.SizeOf<NandCluster>();
        public static readonly long NandPageByteSize = Marshal.SizeOf<NandPage>();
        public static readonly long NandPageNoSpareByteSize = Marshal.SizeOf<NandPage>() - 0x40;
        public static readonly long NandClusterNoSpareByteSize = NandPageNoSpareByteSize * 0x8;
        public static readonly uint SuperblocksBaseCluster = 0x7F00u;
        public static readonly uint SuperblocksEndCluster = 0x7FFFu;
        public static readonly uint SuperblocksClusterIncrement = 0x10;
        public static readonly uint FSTSubEndCapValue = uint.MaxValue;
        public static byte[] EmptyFST = new byte[0x20];
    }
}