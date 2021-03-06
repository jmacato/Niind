using System.Runtime.InteropServices;
using Niind.Helpers;

namespace Niind.Structures.TitlesSystem
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RawTitleMetadataHeader : IMaterialize<TitleMetadataHeader>
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] SignatureType;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] Signature;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 60)]
        public byte[] Padding0;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] Issuer;

        public byte Version;

        public byte ca_crl_version;

        public byte signer_crl_version;

        public byte IsVWii;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] SystemVersion;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] TitleID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] TitleType;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] GroupID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Zero;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Region;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Ratings;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] Padding2;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] IPCMask;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        public byte[] Reserved;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] AccessRights;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] TitleVersion;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] NumberOfContents;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] BootIndex;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Unused;

        public TitleMetadataHeader ToManagedObject()
        {
            return new TitleMetadataHeader
            {
                SignatureType = (SignatureType)CastingHelper.BA_Swap32(SignatureType),
                Signature = Signature,
                Issuer = Issuer,
                Version = Version,
                ca_crl_version = ca_crl_version,
                signer_crl_version = signer_crl_version,
                IsVWii = IsVWii == 1,
                SystemVersion = CastingHelper.BA_Swap64(SystemVersion),
                TitleID = CastingHelper.BA_Swap64(TitleID),
                TitleType = CastingHelper.BA_Swap32(TitleType),
                GroupID = CastingHelper.BA_Swap16(GroupID),
                Region = CastingHelper.BA_Swap16(Region),
                Ratings = Ratings,
                IPCMask = IPCMask,
                AccessRights = CastingHelper.BA_Swap32(AccessRights),
                TitleVersion = CastingHelper.BA_Swap16(TitleVersion),
                NumberOfContents = CastingHelper.BA_Swap16(NumberOfContents),
                BootIndex = CastingHelper.BA_Swap16(BootIndex)
            };
        }
    }
}