using System.Runtime.InteropServices;
using Niind.Structures;

namespace Niind
{
    public class Constants
    {
        public static readonly byte[] EmptyAESIVBytes = new byte[16];
        public static readonly byte[] EmptyECCBytes = new byte[16]
        {
            0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,
            0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF,0xFF
        };
        public static readonly long NandBlockByteSize = Marshal.SizeOf<NandBlock>();
        public static readonly long NandClusterByteSize = Marshal.SizeOf<NandCluster>();
        public static readonly long NandPageByteSize = Marshal.SizeOf<NandPage>();
        public static readonly long NandPageNoSpareByteSize = Marshal.SizeOf<NandPage>() - 64;
        public static readonly long NandClusterNoSpareByteSize = NandPageNoSpareByteSize * 8;

    }
}