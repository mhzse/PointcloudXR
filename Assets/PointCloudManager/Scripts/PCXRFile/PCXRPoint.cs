/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;

namespace PCXRFile
{
    // Stride = 24 bytes
    public struct PCXRPoint
    {
        // Must be multiple of 16 bytes for Compute Buffer perfomance reason.
        public float x;
        public float y;
        public float z;
        public float red;
        public float green;
        public float blue;
        public float alpha;
        public float deleted;
        public float selected;
        public float intensity_normalized;
        public float classification;
        public float id;

        // Need to store a generated color for each point. This color is computed by a Compute shader
        // for performance. We should not calculate the color on the fly in main shader, doing so is
        // wasting GPU cycles.
        public float user_red;
        public float user_green;
        public float user_blue;
        public float user_alpha;

        // Need full support for LAS parameters
        public float scan_angle_rank;
        public float user_data;
        public float point_source_id;
        public float gps_time;

        public float visible;
        public float padding01;
        public float padding02;
        public float padding03;

        public byte[] ToByteArray()
        {
            byte[] bArr = new byte[24 * 4];

            BitConverter.GetBytes(x).CopyTo(bArr,                     0);
            BitConverter.GetBytes(y).CopyTo(bArr,                     4);
            BitConverter.GetBytes(z).CopyTo(bArr,                     8);
            BitConverter.GetBytes(red).CopyTo(bArr,                  12);
            BitConverter.GetBytes(green).CopyTo(bArr,                16);
            BitConverter.GetBytes(blue).CopyTo(bArr,                 20);
            BitConverter.GetBytes(alpha).CopyTo(bArr,                24);
            BitConverter.GetBytes(deleted).CopyTo(bArr,              28);
            BitConverter.GetBytes(selected).CopyTo(bArr,             32);
            BitConverter.GetBytes(intensity_normalized).CopyTo(bArr, 36);
            BitConverter.GetBytes(classification).CopyTo(bArr,       40);
            BitConverter.GetBytes(id).CopyTo(bArr,                   44);
            BitConverter.GetBytes(user_red).CopyTo(bArr,             48);
            BitConverter.GetBytes(user_green).CopyTo(bArr,           52);
            BitConverter.GetBytes(user_blue).CopyTo(bArr,            56);
            BitConverter.GetBytes(user_alpha).CopyTo(bArr,           60);
            BitConverter.GetBytes(scan_angle_rank).CopyTo(bArr,      64);
            BitConverter.GetBytes(user_data).CopyTo(bArr,            68);
            BitConverter.GetBytes(point_source_id).CopyTo(bArr,      72);
            BitConverter.GetBytes(gps_time).CopyTo(bArr,             76);
            BitConverter.GetBytes(visible).CopyTo(bArr, 80);
            BitConverter.GetBytes(padding01).CopyTo(bArr, 84);
            BitConverter.GetBytes(padding02).CopyTo(bArr, 88);
            BitConverter.GetBytes(padding03).CopyTo(bArr, 92);

            return bArr;
        }
    }
}
