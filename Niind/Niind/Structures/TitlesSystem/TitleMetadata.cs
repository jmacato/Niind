using System.Collections.Generic;
using System.Runtime.InteropServices;
using Niind.Helpers;

namespace Niind.Structures.TitlesSystem
{
    public struct TitleMetadata
    {
        public TitleMetadataHeader Header { get; }
        public IList<TitleMetadataContent> ContentDescriptors { get; }
        
        public int DecryptedContentCount { get; set; }

        private TitleMetadata(TitleMetadataHeader header,
            IList<TitleMetadataContent> contentDescriptors)
        {
            Header = header;
            ContentDescriptors = contentDescriptors;
            DecryptedContentCount = 0;
        }

        public static TitleMetadata FromByteArray(byte[] tmdBytes)
        {
            var tmdHeaderSize = Marshal.SizeOf<RawTitleMetadataHeader>();

            var rawHeader = tmdBytes.CastToStruct<RawTitleMetadataHeader>();

            var header = rawHeader.ToManagedObject();

            var contentSpan = tmdBytes[tmdHeaderSize..];

            var numberOfContents = header.NumberOfContents;

            var descSize = Marshal.SizeOf<RawTitleMetadataContentDescriptor>();

            var contentDescriptors = new List<TitleMetadataContent>();
            for (var i = 0; i < numberOfContents * descSize; i += descSize)
            {
                var contentDescBytes = contentSpan[i..(i + descSize)];

                var contentDesc = contentDescBytes.CastToStruct<RawTitleMetadataContentDescriptor>()
                    .ToManagedObject();

                contentDescriptors.Add(contentDesc);
            }

            return new TitleMetadata(header, contentDescriptors);
        }
    }
}