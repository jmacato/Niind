using System.Runtime.InteropServices;

namespace Niind.Structures.TitlesSystem
{
    public readonly struct RawSharedContentEntry
    {
        public readonly uint SharedID;
        public readonly uint Unknown;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public readonly byte[] SHA1;

        public RawSharedContentEntry(uint sharedId, byte[] sha1)
        {
            SharedID = sharedId;
            SHA1 = sha1;
            Unknown = 0;
        }
    }
}