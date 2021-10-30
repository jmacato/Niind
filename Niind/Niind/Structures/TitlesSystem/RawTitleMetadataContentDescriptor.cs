using System.Runtime.InteropServices;
using Niind.Helpers;

namespace Niind.Structures.TitlesSystem
{
    public struct RawTitleMetadataContentDescriptor : IMaterialize<TitleMetadataContent>
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] ContentID;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Index;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Type;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Size;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] SHA1;

        public TitleMetadataContent ToManagedObject()
        {
            return new TitleMetadataContent
            {
                ContentID = CastingHelper.BEToLE_UInt32(ContentID),
                Index = CastingHelper.BEToLE_UInt16(Index),
                Type = CastingHelper.BEToLE_UInt16(Type),
                Size = CastingHelper.BEToLE_UInt64(Size),
                SHA1 = SHA1
            };
        }
    }
}