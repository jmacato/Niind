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
}