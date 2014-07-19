namespace SetDPI
{
    using System;
    using System.Text;
    
    /// <summary>
    /// Methods for accessing and modifying PNG files.
    /// </summary>
    internal static class PNG
    {
        public static bool HasPPI(byte[] data)
        {
            return GetChunkOffset("pHYs", data, true) != -1;
        }

        /// <summary>
        /// Returns the horizontal and vertical points per metre (PPM).
        /// </summary>
        /// <param name="data">PNG bytes.</param>
        /// <returns>Array of length two with data { PPMX, PPMY } or null.</returns>
        public static int[] GetPPM(byte[] data)
        {
            int headerindex = GetChunkOffset("pHYs", data, true);

            if (headerindex == -1) return null;

            // read the chunk length
            int length = BytesToInt(data, headerindex - 4, false);

            // ensure that the length is 9 and that there's enough space in the data
            if (length == 9 && headerindex + 4 + length + 4 < data.Length)
            {
                int ppmx = BytesToInt(data, headerindex + 4, false);
                int ppmy = BytesToInt(data, headerindex + 8, false);
                bool unitismeter = data[headerindex + 12] == 1;

                // if the unit isn't metres, the points-per-unit is undefined
                if (unitismeter) return new int[] { ppmx, ppmy };
            }

            return null;
        }

        /// <summary>
        /// Converts PPM to PPI.
        /// </summary>
        /// <param name="ppm">Points per metre.</param>
        /// <returns>Points per inch.</returns>
        public static double PPMtoPPI(int ppm)
        {
            return ppm * 0.0254;
        }

        /// <summary>
        /// Converts PPI to PPM.
        /// </summary>
        /// <param name="ppi">Points per inch.</param>
        /// <returns>Points per metre.</returns>
        public static uint PPItoPPM(double ppi)
        {
            return (uint)Math.Round(ppi / 0.0254);
        }

        /// <summary>
        /// Gets the position of the specified chunk (starting at the chunk type, not the length).
        /// </summary>
        /// <param name="chunk">Chunk type (e.g. pHYs).</param>
        /// <param name="data">PNG data.</param>
        /// <param name="precedesIDAT">Set this to true if the chunk must appear before the first IDAT chunk.</param>
        /// <returns>The position of the specified chunk, or -1 if it doesn't exist.</returns>
        private static int GetChunkOffset(string chunk, byte[] data, bool precedesIDAT)
        {
            // we could use a stream here to avoid reading all the bytes

            ASCIIEncoding encoding = new ASCIIEncoding();

            byte[] chunkbytes = encoding.GetBytes(chunk);

            // chunk type be 4 bytes
            if (chunkbytes.Length != 4) return -1;

            //byte[] IDATbytes = encoding.GetBytes("IDAT");
            //byte[] idatbytes = { 0x49, 0x44, 0x41, 0x54 };

            const int maxlen = 4;

            int idatHeaderIndex = -1, chunkHeaderIndex = -1;

            // start searching from byte #9 - the first 8 bytes are the PNG signature
            for (int i = 8; chunkHeaderIndex == -1 && idatHeaderIndex == -1 && i + maxlen <= data.Length; i++)
            {
                // this search could be made faster by reading the length of chunks and skipping forwards accordingly

                // search for the chunk header
                for (int j = i; j < i + chunkbytes.Length; j++)
                {
                    if (data[j] != chunkbytes[j - i]) break;
                    if (j - i == chunkbytes.Length - 1) chunkHeaderIndex = i;
                }

                // stop searching if we hit IDAT before finding a chunk that must precede it
                //if (data[i] == idatbytes[0] && data[i + 1] == idatbytes[1] && data[i + 2] == idatbytes[2] && data[i + 3] == idatbytes[3])
                if (precedesIDAT)
                    if (data[i] == 0x49 && data[i + 1] == 0x44 && data[i + 2] == 0x41 && data[i + 3] == 0x54)
                        idatHeaderIndex = i;
            }

            if (chunkHeaderIndex != -1 && chunkHeaderIndex - 4 >= 8)
            {
                return chunkHeaderIndex;
            }

            return -1;
        }

        public static byte[] SetDPI(double ppix, double ppiy, byte[] data)
        {
            int chunkindex = GetChunkOffset("pHYs", data, true);

            byte[] newdata = data;

            int offset = -1;

            if (chunkindex == -1)
            {
                newdata = new byte[data.Length + 8 + 9 + 4];
                for (int i = 0; i < data.Length; i++)
                {
                    if (offset == -1 && data[i + 4] == 0x49 && data[i + 5] == 0x44 && data[i + 6] == 0x41
                        && data[i + 7] == 0x54) offset = i;

                    newdata[i + (offset != -1 ? 8 + 9 + 4 : 0)] = data[i];
                }
            }

            if (offset == -1) offset = chunkindex - 4;

            // note: integers are big endian

            // set length (9)
            newdata[offset + 0] = 0;
            newdata[offset + 1] = 0;
            newdata[offset + 2] = 0;
            newdata[offset + 3] = 9;

            // setup 13-byte array to hold chunk type and data (calculate CRC of this later)
            byte[] phystypedatabytes = new byte[13];

            // pHYs = 0x70 0x48 0x59 0x73
            phystypedatabytes[0] = 0x70;
            phystypedatabytes[1] = 0x48;
            phystypedatabytes[2] = 0x59;
            phystypedatabytes[3] = 0x73;

            // set chunk type (pHYs)
            newdata[offset + 4] = phystypedatabytes[0];
            newdata[offset + 5] = phystypedatabytes[1];
            newdata[offset + 6] = phystypedatabytes[2];
            newdata[offset + 7] = phystypedatabytes[3];

            // convert PPI to PPM
            uint ppmx = PPItoPPM(ppix);
            uint ppmy = PPItoPPM(ppiy);

            byte[] ppmxbytes = IntToBytes(ppmx, false);
            byte[] ppmybytes = IntToBytes(ppmy, false);

            phystypedatabytes[4] = ppmxbytes[0];
            phystypedatabytes[5] = ppmxbytes[1];
            phystypedatabytes[6] = ppmxbytes[2];
            phystypedatabytes[7] = ppmxbytes[3];

            phystypedatabytes[8] = ppmybytes[0];
            phystypedatabytes[9] = ppmybytes[1];
            phystypedatabytes[10] = ppmybytes[2];
            phystypedatabytes[11] = ppmybytes[3];

            phystypedatabytes[12] = 1;

            // set ppm x
            newdata[offset + 8] = phystypedatabytes[4];
            newdata[offset + 9] = phystypedatabytes[5];
            newdata[offset + 10] = phystypedatabytes[6];
            newdata[offset + 11] = phystypedatabytes[7];

            // set ppm y
            newdata[offset + 12] = phystypedatabytes[8];
            newdata[offset + 13] = phystypedatabytes[9];
            newdata[offset + 14] = phystypedatabytes[10];
            newdata[offset + 15] = phystypedatabytes[11];

            // set unit is meter (1)
            newdata[offset + 16] = phystypedatabytes[12];

            uint crc = PNG.crc(phystypedatabytes);
            byte[] crcbytes = IntToBytes(crc, false);

            // set crc
            newdata[offset + 17] = crcbytes[0];
            newdata[offset + 18] = crcbytes[1];
            newdata[offset + 19] = crcbytes[2];
            newdata[offset + 20] = crcbytes[3];

            return newdata;
        }

        private static int BytesToInt(byte[] bytes, int offset, bool islittleendian)
        {
            return islittleendian
                       ? ((bytes[0 + offset] | (bytes[1 + offset] << 8)) | (bytes[2 + offset] << 0x10)) | (bytes[3 + offset] << 0x18)
                       : (((bytes[0 + offset] << 0x18) | (bytes[1 + offset] << 0x10)) | (bytes[2 + offset] << 8)) | bytes[3 + offset];
        }

        private static byte[] IntToBytes(uint integer, bool islittleendian)
        {
            return islittleendian
                       ? new byte[] { (byte)(integer & 0xFF), (byte)((integer >> 8) & 0xFF), (byte)((integer >> 0x10) & 0xFF), (byte)((integer >> 0x18) & 0xFF) }
                       : new byte[] { (byte)((integer >> 0x18) & 0xFF), (byte)((integer >> 0x10) & 0xFF), (byte)((integer >> 8) & 0xFF), (byte)(integer & 0xFF) };
        }

        // CRC implementation from PNG 1.2 spec

        /* Table of CRCs of all 8-bit messages. */
        private static uint[] crc_table;

        /* Flag: has the table been computed? Initially false. */
        static bool crc_table_computed = false;
        /* Make the table for a fast CRC. */
        private static void make_crc_table()
        {
            crc_table = new uint[256];

            uint c;
            uint n, k;
            for (n = 0; n < 256; n++)
            {
                c = n;
                for (k = 0; k < 8; k++)
                    if ((c & 1) != 0)
                        c = 0xedb88320 ^ (c >> 1);
                    else
                        c = c >> 1;
                crc_table[n] = c;
            }
            crc_table_computed = true;
        }

        /* Update a running CRC with the bytes buf[0..len-1]--the CRC
        should be initialized to all 1’s, and the transmitted value
        is the 1’s complement of the final running CRC (see the
        crc() routine below)). */

        private static uint update_crc(uint crc, byte[] buffer)
        {
            uint c = crc;
            int n;
            
            if (!crc_table_computed)
                make_crc_table();

            for (n = 0; n < buffer.Length; n++)
                c = crc_table[(c ^ buffer[n]) & 0xff] ^ (c >> 8);

            return c;
        }

        private static uint crc(byte[] buffer)
        {
            return update_crc(0xffffffff, buffer) ^ 0xffffffff;
        }
    }
}
