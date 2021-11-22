using System.Runtime.InteropServices;
using System.Text;

namespace Niind.Structures.TitlesSystem
{
    
    [StructLayout(LayoutKind.Sequential)]
    public struct RawContentMapEntry
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x8)]
        public byte[] SharedId;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] SHA1;

        public RawContentMapEntry(uint index, byte[] sha1)
        {
            SharedId = Encoding.ASCII.GetBytes($"{index:X8}");
            SHA1 = sha1;
        }
    }
}