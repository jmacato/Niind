namespace Niind.Structures.TitlesSystem
{
    public class SharedContent
    {
        public byte[] SHA1;
        public byte[] Content;

        public SharedContent( byte[] sha1, byte[] content)
        {
            SHA1 = sha1;
            Content = content;
        }
    }
}