using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SongDetailsCache {
	static unsafe class HexUtil {
        private static uint[] CreateLookup32Unsafe() {
            var result = new uint[256];
            for(int i = 0; i < 256; i++) {
                string s = i.ToString("X2");
                if(BitConverter.IsLittleEndian)
                    result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
                else
                    result[i] = ((uint)s[1]) + ((uint)s[0] << 16);
            }
            return result;
        }

        private static readonly uint[] _lookup32Unsafe = CreateLookup32Unsafe();
        private static readonly uint* _lookup32UnsafeP = (uint*)GCHandle.Alloc(_lookup32Unsafe, GCHandleType.Pinned).AddrOfPinnedObject();

        public static string ByteArrayToHexViaLookup32UnsafeDirect(byte[] bytes) {
            var lookupP = _lookup32UnsafeP;
            var result = new string((char)0, bytes.Length * 2);
            fixed(byte* bytesP = bytes)
            fixed(char* resultP = result) {
                uint* resultP2 = (uint*)resultP;
                for(int i = 0; i < bytes.Length; i++) {
                    resultP2[i] = lookupP[bytesP[i]];
                }
            }
            return result;
        }

        public unsafe static string SongBytesToHash(uint index) {
            var lookupP = _lookup32UnsafeP;
            var result = new string((char)0, SongDetails.HASH_SIZE_BYTES * 2);
            fixed(char* resultP = result) {
                byte* SrcP2 = SongDetails.hashBytes + (index * 20);
                uint* resultP2 = (uint*)resultP;
                for(int i = 0; i < SongDetails.HASH_SIZE_BYTES; i++)
                    resultP2[i] = lookupP[SrcP2[i]];
            }
            return result;
        }





        private readonly static Dictionary<char, byte> hexmap = new Dictionary<char, byte>() {
            { 'a', 0xA }, { 'b', 0xB }, { 'c', 0xC }, { 'd', 0xD },
            { 'e', 0xE }, { 'f', 0xF }, { 'A', 0xA }, { 'B', 0xB },
            { 'C', 0xC }, { 'D', 0xD }, { 'E', 0xE }, { 'F', 0xF },
            { '0', 0x0 }, { '1', 0x1 }, { '2', 0x2 }, { '3', 0x3 },
            { '4', 0x4 }, { '5', 0x5 }, { '6', 0x6 }, { '7', 0x7 },
            { '8', 0x8 }, { '9', 0x9 }
        };
        public static byte[] ToBytes(string hex) {
            byte[] bytesArr = new byte[hex.Length / 2];

            char left;
            char right;

            try {
                int x = 0;
                for(int i = 0; i < hex.Length; i += 2, x++) {
                    left = hex[i];
                    right = hex[i + 1];
                    bytesArr[x] = (byte)((hexmap[left] << 4) | hexmap[right]);
                }
                return bytesArr;
            } catch(KeyNotFoundException) {
                throw new FormatException("Hex string has non-hex character");
            }
        }
    }

    //static class SortUtil {
    //    public class ByteArrComparer : IComparer<byte[]> {
    //        public int Compare(byte[] x, byte[] y) {
    //            int result;
    //            for(int index = 0; index < SongDetails.HASH_SIZE_BYTES; index++) {
    //                result = x[index].CompareTo(y[index]);
    //                if(result != 0) return result;
    //            }
    //            return x.Length.CompareTo(y.Length);
    //        }
    //    }
    //}
}
