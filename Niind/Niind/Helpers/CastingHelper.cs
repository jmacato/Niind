using System;
using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;

namespace Niind.Helpers
{
    public static class CastingHelper
    {
        public static T CastToStruct<T>(this byte[] data) where T : struct
        {
            var pData = GCHandle.Alloc(data, GCHandleType.Pinned);
            var result = (T)Marshal.PtrToStructure(pData.AddrOfPinnedObject(), typeof(T));
            pData.Free();
            return result;
        }

        public static byte[] CastToArray<T>(this T data) where T : struct
        {
            var result = new byte[Marshal.SizeOf(typeof(T))];
            var pResult = GCHandle.Alloc(result, GCHandleType.Pinned);
            Marshal.StructureToPtr(data, pResult.AddrOfPinnedObject(), true);
            pResult.Free();
            return result;
        }

        public static int IndexOf(this byte[] byteArray, byte byteToFind)
        {
            return Array.IndexOf(byteArray, byteToFind);
        }


        public static ushort BA_Swap16(byte[] input) =>
            BitConverter.ToUInt16(input.Reverse().ToArray());

        public static uint BA_Swap32(byte[] input) =>
            BitConverter.ToUInt32(input.Reverse().ToArray());

        public static ulong BA_Swap64(byte[] input) =>
            BitConverter.ToUInt64(input.Reverse().ToArray());

        public static byte[] Swap_BA(ushort input) =>
            BitConverter.GetBytes(input).ToArray().Reverse().ToArray();

        public static byte[] Swap_BA(uint input) =>
            BitConverter.GetBytes(input).ToArray().Reverse().ToArray();

        public static byte[] Swap_BA(ulong input) =>
            BitConverter.GetBytes(input).ToArray().Reverse().ToArray();

        public static ushort Swap_Val(ushort input) =>
            BitConverter.ToUInt16(Swap_BA(input));

        public static uint Swap_Val(uint input) =>
            BitConverter.ToUInt32(Swap_BA(input));

        public static ulong Swap_Val(ulong input) =>
            BitConverter.ToUInt64(Swap_BA(input));
    }
}