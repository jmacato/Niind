using System;
using System.Linq;
using System.Security.Cryptography;
using Niind.Structures;

namespace Niind
{
    public class HMACSHA1Wii
    {
        private readonly byte[] hmac_key;
        private readonly SHA1.SHA1Context hash_ctx;
        private readonly SHA1 Hasher;

        public HMACSHA1Wii(KeyFile keyFile)
        {
            hmac_key = new byte[0x40];
            hash_ctx = new SHA1.SHA1Context();
            Hasher = new SHA1();

            keyFile.NandHMACKey.AsSpan().CopyTo(hmac_key.AsSpan());

            for (int i = 0; i < 0x40; ++i)
                hmac_key[i] ^= 0x36; // ipad

            Hasher.SHA1Reset(hash_ctx);
            Hasher.SHA1Input(hash_ctx, hmac_key, hmac_key.Length);
        }

        public void hmac_update(byte[] data, int size)
        {
            Hasher.SHA1Input(hash_ctx, data, size);
        }

        public byte[] hmac_final()
        {
            //int i;
            var hash = new byte[0x14];
            Hasher.SHA1Result(hash_ctx);
            hash = hash_ctx.GetHashBytes();

            for (int i = 0; i < 0x40; ++i)
                hmac_key[i] ^= 0x36 ^ 0x5c; // opad

            Hasher.SHA1Reset(hash_ctx);
            Hasher.SHA1Input(hash_ctx, hmac_key, 0x40);
            Hasher.SHA1Input(hash_ctx, hash, 0x14);
            Hasher.SHA1Result(hash_ctx);

            return hash_ctx.GetHashBytes();
        }
    }

    public class SHA1
    {
        private void Main()
        {
            new SHA1Managed();
        }

        private uint SHA1CircularShift(int bits, uint word)
        {
            return ((word << bits) & 0xFFFFFFFF) |
                   (word >> (32 - bits));
        }

        public class SHA1Context
        {
            public SHA1Context()
            {
                Message_Digest = new uint[5]; /* Message Digest (output)          */
                Message_Block = new byte[64];
            }

            public byte[] GetHashBytes()
            {
                return Message_Digest.Select(BitConverter.GetBytes).Reverse().SelectMany(x => x).Reverse().ToArray();
            }

            public uint[] Message_Digest; /* Message Digest (output)          */

            public uint Length_Low; /* Message length in bits           */
            public uint Length_High; /* Message length in bits           */

            public byte[] Message_Block; /* 512-bit message blocks      */
            public int Message_Block_Index; /* Index into message block array   */

            public bool Computed; /* Is the digest computed?          */
            public bool Corrupted; /* Is the message digest corruped?  */
        };


        public void SHA1Reset(SHA1Context context)
        {
            context.Length_Low = 0;
            context.Length_High = 0;
            context.Message_Block_Index = 0;

            context.Message_Digest[0] = 0x67452301u;
            context.Message_Digest[1] = 0xEFCDAB89u;
            context.Message_Digest[2] = 0x98BADCFEu;
            context.Message_Digest[3] = 0x10325476u;
            context.Message_Digest[4] = 0xC3D2E1F0u;

            context.Computed = false;
            context.Corrupted = false;
        }

        public bool SHA1Result(SHA1Context context)
        {
            if (context.Corrupted) return false;

            if (!context.Computed)
            {
                SHA1PadMessage(context);
                context.Computed = true;
            }

            return true;
        }

        public void SHA1Input(SHA1Context context,
            byte[] message_array,
            int length)
        {
            if (length == 0) return;

            if (context.Computed || context.Corrupted)
            {
                context.Corrupted = true;
                return;
            }

            var msgArray = 0;

            while (length-- != 0 && !context.Corrupted)
            {
                context.Message_Block[context.Message_Block_Index++] =
                    (byte)(message_array[msgArray] & 0xFF);

                context.Length_Low += 8;
                /* Force it to 32 bits */
                context.Length_Low &= 0xFFFFFFFF;
                if (context.Length_Low == 0)
                {
                    context.Length_High++;
                    /* Force it to 32 bits */
                    context.Length_High &= 0xFFFFFFFF;
                    if (context.Length_High == 0)
                        /* Message is too long */
                        context.Corrupted = true;
                }

                if (context.Message_Block_Index == 64) SHA1ProcessMessageBlock(context);

                msgArray++;
            }
        }

        public void SHA1ProcessMessageBlock(SHA1Context context)
        {
            var K = new uint[] /* Constants defined in SHA-1   */
            {
                0x5A827999u,
                0x6ED9EBA1u,
                0x8F1BBCDCu,
                0xCA62C1D6u
            };

            int t; /* Loop counter                 */
            uint temp; /* Temporary word value         */
            var W = new uint[80]; /* Word sequence                */
            uint A, B, C, D, E; /* Word buffers                 */

            /*
             *  Initialize the first 16 words in the array W
             */
            for (t = 0; t < 16; t++)
            {
                W[t] = (uint)context.Message_Block[t * 4] << 24;
                W[t] |= (uint)context.Message_Block[t * 4 + 1] << 16;
                W[t] |= (uint)context.Message_Block[t * 4 + 2] << 8;
                W[t] |= (uint)context.Message_Block[t * 4 + 3];
            }

            for (t = 16; t < 80; t++) W[t] = SHA1CircularShift(1, W[t - 3] ^ W[t - 8] ^ W[t - 14] ^ W[t - 16]);

            A = context.Message_Digest[0];
            B = context.Message_Digest[1];
            C = context.Message_Digest[2];
            D = context.Message_Digest[3];
            E = context.Message_Digest[4];

            for (t = 0; t < 20; t++)
            {
                temp = SHA1CircularShift(5, A) +
                       ((B & C) | (~B & D)) + E + W[t] + K[0];
                temp &= 0xFFFFFFFF;
                E = D;
                D = C;
                C = SHA1CircularShift(30, B);
                B = A;
                A = temp;
            }

            for (t = 20; t < 40; t++)
            {
                temp = SHA1CircularShift(5, A) + (B ^ C ^ D) + E + W[t] + K[1];
                temp &= 0xFFFFFFFF;
                E = D;
                D = C;
                C = SHA1CircularShift(30, B);
                B = A;
                A = temp;
            }

            for (t = 40; t < 60; t++)
            {
                temp = SHA1CircularShift(5, A) +
                       ((B & C) | (B & D) | (C & D)) + E + W[t] + K[2];
                temp &= 0xFFFFFFFF;
                E = D;
                D = C;
                C = SHA1CircularShift(30, B);
                B = A;
                A = temp;
            }

            for (t = 60; t < 80; t++)
            {
                temp = SHA1CircularShift(5, A) + (B ^ C ^ D) + E + W[t] + K[3];
                temp &= 0xFFFFFFFF;
                E = D;
                D = C;
                C = SHA1CircularShift(30, B);
                B = A;
                A = temp;
            }

            context.Message_Digest[0] =
                (context.Message_Digest[0] + A) & 0xFFFFFFFF;
            context.Message_Digest[1] =
                (context.Message_Digest[1] + B) & 0xFFFFFFFF;
            context.Message_Digest[2] =
                (context.Message_Digest[2] + C) & 0xFFFFFFFF;
            context.Message_Digest[3] =
                (context.Message_Digest[3] + D) & 0xFFFFFFFF;
            context.Message_Digest[4] =
                (context.Message_Digest[4] + E) & 0xFFFFFFFF;

            context.Message_Block_Index = 0;
        }

        public void SHA1PadMessage(SHA1Context context)
        {
            if (context.Message_Block_Index > 55)
            {
                context.Message_Block[context.Message_Block_Index++] = 0x80;
                while (context.Message_Block_Index < 64) context.Message_Block[context.Message_Block_Index++] = 0;

                SHA1ProcessMessageBlock(context);

                while (context.Message_Block_Index < 56) context.Message_Block[context.Message_Block_Index++] = 0;
            }
            else
            {
                context.Message_Block[context.Message_Block_Index++] = 0x80;
                while (context.Message_Block_Index < 56) context.Message_Block[context.Message_Block_Index++] = 0;
            }

            context.Message_Block[56] = (byte)((context.Length_High >> 24) & 0xFF);
            context.Message_Block[57] = (byte)((context.Length_High >> 16) & 0xFF);
            context.Message_Block[58] = (byte)((context.Length_High >> 8) & 0xFF);
            context.Message_Block[59] = (byte)(context.Length_High & 0xFF);
            context.Message_Block[60] = (byte)((context.Length_Low >> 24) & 0xFF);
            context.Message_Block[61] = (byte)((context.Length_Low >> 16) & 0xFF);
            context.Message_Block[62] = (byte)((context.Length_Low >> 8) & 0xFF);
            context.Message_Block[63] = (byte)(context.Length_Low & 0xFF);

            SHA1ProcessMessageBlock(context);
        }
    }
}