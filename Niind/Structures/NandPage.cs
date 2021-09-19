using System;
using System.Runtime.InteropServices;

namespace Niind.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NandPage
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2048)]
        public byte[] MainData;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] SpareData;

        public byte[] CalculatePageECC()
        {
            var ecc = new byte[16];
            var eccCount = 0;
            for (var i = 0; i < 2048; i += 512)
            {
                CalculateBlockECC(MainData.AsSpan(i, 512), ecc.AsSpan(eccCount, 4));
                eccCount += 4;
            }
            return ecc;
        }

        public bool IsECCCorrect()
        {
            if (IsECCBlank()) return true; // Ignore if the ECC is blank.
            
            return SpareData.AsSpan(48, 16)
                .SequenceEqual(CalculatePageECC());
        }

        public bool IsECCBlank()
        {
            return !SpareData.AsSpan(48, 16)
                .SequenceEqual(Constants.EmptyECCBytes);
        }

        private byte ByteParity(byte x)
        {
            byte y = 0;

            while (x > 0)
            {
                y = (byte)(y ^ (x & 1));
                x >>= 1;
            }

            return y;
        }

        private void CalculateBlockECC(Span<byte> data, Span<byte> ecc)
        {
            var a = new byte[12, 2];

            int i, j;
            int a1;
            byte x;

            for (i = 0; i < 512; i++)
            {
                x = data[i];
                for (j = 0; j < 9; j++)
                {
                    a[3 + j, (i >> j) & 1] ^= x;
                }
            }

            x = (byte)(a[3, 0] ^ a[3, 1]);
            a[0, 0] = (byte)(x & 0x55);
            a[0, 1] = (byte)(x & 0xaa);
            a[1, 0] = (byte)(x & 0x33);
            a[1, 1] = (byte)(x & 0xcc);
            a[2, 0] = (byte)(x & 0x0f);
            a[2, 1] = (byte)(x & 0xf0);

            for (j = 0; j < 12; j++)
            {
                a[j, 0] = ByteParity(a[j, 0]);
                a[j, 1] = ByteParity(a[j, 1]);
            }

            var a0 = a1 = 0;
            
            for (j = 0; j < 12; j++)
            {
                a0 |= a[j, 0] << j;
                a1 |= a[j, 1] << j;
            }

            ecc[0] = (byte)(a0 & 0x00FF);
            ecc[1] = (byte)(a0 >> 8);
            ecc[2] = (byte)(a1 & 0x00FF);
            ecc[3] = (byte)(a1 >> 8);
        }
    }
}