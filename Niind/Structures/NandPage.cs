using System;
using System.Runtime.InteropServices;

namespace Niind.Structures
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NandPage
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x800)]
        public byte[] MainData;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x40)]
        public byte[] SpareData;

        public byte[] CalculatePageECC()
        {
            var ecc = new byte[0x10];
            var eccCount = 0x0;
            for (var i = 0x0; i < 0x800; i += 0x200)
            {
                CalculateBlockECC(MainData.AsSpan(i, 0x200), ecc.AsSpan(eccCount, 0x4));
                eccCount += 0x4;
            }

            return ecc;
        }

        public bool IsECCCorrect()
        {
            if (IsECCBlank()) return true; // Ignore if the ECC is blank.

            return SpareData.AsSpan(0x30, 0x10)
                .SequenceEqual(CalculatePageECC());
        }

        public bool IsECCBlank()
        {
            return !SpareData.AsSpan(0x30, 0x10)
                .SequenceEqual(Constants.EmptyECCBytes);
        }

        private byte ByteParity(byte x)
        {
            byte y = 0x0;

            while (x > 0x0)
            {
                y = (byte)(y ^ (x & 0x1));
                x >>= 0x1;
            }

            return y;
        }

        private void CalculateBlockECC(Span<byte> data, Span<byte> ecc)
        {
            var a = new byte[0xC, 0x2];

            int i, j;
            int a1;
            byte x;

            for (i = 0x0; i < 0x200; i++)
            {
                x = data[i];
                for (j = 0x0; j < 0x9; j++) a[0x3 + j, (i >> j) & 0x1] ^= x;
            }

            x = (byte)(a[0x3, 0x0] ^ a[0x3, 0x1]);
            a[0x0, 0x0] = (byte)(x & 0x55);
            a[0x0, 0x1] = (byte)(x & 0xAA);
            a[0x1, 0x0] = (byte)(x & 0x33);
            a[0x1, 0x1] = (byte)(x & 0xCC);
            a[0x2, 0x0] = (byte)(x & 0xF);
            a[0x2, 0x1] = (byte)(x & 0xF0);

            for (j = 0x0; j < 0xC; j++)
            {
                a[j, 0x0] = ByteParity(a[j, 0x0]);
                a[j, 0x1] = ByteParity(a[j, 0x1]);
            }

            var a0 = a1 = 0x0;

            for (j = 0x0; j < 0xC; j++)
            {
                a0 |= a[j, 0x0] << j;
                a1 |= a[j, 0x1] << j;
            }

            ecc[0x0] = (byte)(a0 & 0xFF);
            ecc[0x1] = (byte)(a0 >> 0x8);
            ecc[0x2] = (byte)(a1 & 0xFF);
            ecc[0x3] = (byte)(a1 >> 0x8);
        }
    }
}