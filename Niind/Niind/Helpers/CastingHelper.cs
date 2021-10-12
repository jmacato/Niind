using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Niind
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

        public static ulong BEToLE_UInt64(byte[] input) =>
            BitConverter.ToUInt64(input.ToArray().Reverse().ToArray());

        public static uint BEToLE_UInt32(byte[] input) =>
            BitConverter.ToUInt32(input.ToArray().Reverse().ToArray());

        public static ushort BEToLE_UInt16(byte[] input) =>
            BitConverter.ToUInt16(input.ToArray().Reverse().ToArray());

        public static byte[] LEToBE_UInt16(ushort input) =>
            BitConverter.GetBytes(input).ToArray().Reverse().ToArray();

        public static byte[] LEToBE_UInt32(uint input) =>
            BitConverter.GetBytes(input).ToArray().Reverse().ToArray();
    }
}