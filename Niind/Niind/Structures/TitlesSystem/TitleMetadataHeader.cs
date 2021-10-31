using System.Runtime.InteropServices;

namespace Niind.Structures.TitlesSystem
{
    public struct TitleMetadataHeader
    {
        public SignatureType SignatureType;
        public byte[] Signature;
        public byte[] Issuer;
        public byte Version;
        public byte ca_crl_version;
        public byte signer_crl_version;
        public bool IsVWii;
        public ulong SystemVersion;
        public ulong TitleID;
        public uint TitleType;
        public ushort GroupID;
        public ushort Region;
        public byte[] Ratings;
        public byte[] IPCMask;
        public uint AccessRights;
        public ushort TitleVersion;
        public ushort NumberOfContents;
        public ushort BootIndex;
    }
    
    public readonly struct SharedContentEntry
    {
        public readonly uint SharedID;
        public readonly uint Unknown;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public readonly byte[] SHA1;

        public SharedContentEntry(uint sharedId, byte[] sha1)
        {
            SharedID = sharedId;
            SHA1 = sha1;
            Unknown = 0;
        }
    }
}