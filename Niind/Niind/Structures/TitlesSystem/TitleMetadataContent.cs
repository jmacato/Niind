namespace Niind.Structures.TitlesSystem
{
    public class TitleMetadataContent
    {
        public uint ContentID;
        public ushort Index;
        public ushort Type;
        public ulong Size;
        public byte[] SHA1;
        public byte[] DecryptedContent;
        public byte[] DecryptionKey;
        public byte[] DecryptionIV;
        public TitleMetadata ParentTMD;
        public RawTicket Ticket;
    }
}