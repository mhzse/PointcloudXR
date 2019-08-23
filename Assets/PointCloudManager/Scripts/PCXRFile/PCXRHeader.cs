/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace PCXRFile
{
    public class PCXRHeader
    {
        public uint   Version           { get; set; }   // 4 bytes
        public uint   NumberOfPoints    { get; set; }   // 4 bytes
        public ushort Stride            { get; set; }   // 2 bytes
        public ushort OffsetToPointData { get; set; }   // 2 bytes

        public float UserStartPositionX { get; set; }   // 4 bytes
        public float UserStartPositionY { get; set; }   // 4 bytes
        public float UserStartPositionZ { get; set; }   // 4 bytes

        public float UserStartRotationW { get; set; }   // 4 bytes
        public float UserStartRotationX { get; set; }   // 4 bytes
        public float UserStartRotationY { get; set; }   // 4 bytes
        public float UserStartRotationZ { get; set; }   // 4 bytes

        public int   AddIntensityValueToColor { get; set; } // 4 bytes
        public int   IntensityAsColor         { get; set; } // 4 bytes
        public float ColorIntensity           { get; set; } // 4 bytes
        public float GeometrySize             { get; set; } // 4 bytes
        public float UserColorAsColor         { get; set; } // 4 bytes
         
        public float MaxX { get; set; }                // 4 bytes
        public float MaxY { get; set; }                // 4 bytes
        public float MaxZ { get; set; }                // 4 bytes
        public float MinX { get; set; }                // 4 bytes
        public float MinY { get; set; }                // 4 bytes
        public float MinZ { get; set; }                // 4 bytes

        ///*** Store original LAS variables ***/
        //// LAS Specific variables BEGIN
        public double XScaleFactor { get; set; }        // 8 bytes
        public double YScaleFactor { get; set; }        // 8 bytes
        public double ZScaleFactor { get; set; }        // 8 bytes
        public double XOffset      { get; set; }        // 8 bytes
        public double YOffset      { get; set; }        // 8 bytes
        public double ZOffset      { get; set; }        // 8 bytes
        //// LAS Specific variables END

        public void ReadHeader(string filePath)
        {
            if (filePath != null)
            {
                BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open));

                Version           = reader.ReadUInt32();
                NumberOfPoints    = reader.ReadUInt32();
                Stride            = reader.ReadUInt16();
                OffsetToPointData = reader.ReadUInt16();

                UserStartPositionX = reader.ReadSingle();
                UserStartPositionY = reader.ReadSingle();
                UserStartPositionZ = reader.ReadSingle();

                UserStartRotationW = reader.ReadSingle();
                UserStartRotationX = reader.ReadSingle();
                UserStartRotationY = reader.ReadSingle();
                UserStartRotationZ = reader.ReadSingle();

                AddIntensityValueToColor = reader.ReadInt32();
                IntensityAsColor         = reader.ReadInt32();
                ColorIntensity           = reader.ReadSingle();
                GeometrySize             = reader.ReadSingle();
                UserColorAsColor               = reader.ReadSingle();

                MaxX = reader.ReadSingle();
                MaxY = reader.ReadSingle();
                MaxZ = reader.ReadSingle();
                MinX = reader.ReadSingle();
                MinY = reader.ReadSingle();
                MinZ = reader.ReadSingle();

                XScaleFactor = reader.ReadDouble();
                YScaleFactor = reader.ReadDouble();
                ZScaleFactor = reader.ReadDouble();
                XOffset      = reader.ReadDouble();
                YOffset      = reader.ReadDouble();
                ZOffset      = reader.ReadDouble();

                reader.Close();
            }
        }

        public ushort GetHeaderSizeInBytes()
        {
            ushort sizeInBytes = 0;

            PropertyInfo[] pi = typeof(PCXRHeader).GetProperties();
            foreach (PropertyInfo pinf in pi)
            {
                int s = System.Runtime.InteropServices.Marshal.SizeOf(pinf.PropertyType);
                sizeInBytes += (ushort)s;
            }
            Debug.Log("PCXRHeader size: " + sizeInBytes + "\n");

            return sizeInBytes;
        }

        public byte[] ToByteArray()
        {
            Version = 1;
            int sizeInBytes = GetHeaderSizeInBytes();
            OffsetToPointData = (ushort)sizeInBytes;

            byte[] arr = new byte[sizeInBytes];

            BitConverter.GetBytes(Version).CopyTo(arr,            0);
            BitConverter.GetBytes(NumberOfPoints).CopyTo(arr,     4);
            BitConverter.GetBytes(Stride).CopyTo(arr,             8);
            BitConverter.GetBytes(OffsetToPointData).CopyTo(arr, 10);

            BitConverter.GetBytes(UserStartPositionX).CopyTo(arr, 12);
            BitConverter.GetBytes(UserStartPositionY).CopyTo(arr, 16);
            BitConverter.GetBytes(UserStartPositionZ).CopyTo(arr, 20);
            BitConverter.GetBytes(UserStartRotationW).CopyTo(arr, 24);
            BitConverter.GetBytes(UserStartRotationX).CopyTo(arr, 28);
            BitConverter.GetBytes(UserStartRotationY).CopyTo(arr, 32);
            BitConverter.GetBytes(UserStartRotationZ).CopyTo(arr, 36);

            BitConverter.GetBytes(AddIntensityValueToColor).CopyTo(arr, 40);
            BitConverter.GetBytes(IntensityAsColor).CopyTo(arr,         44);

            BitConverter.GetBytes(ColorIntensity).CopyTo(arr, 48);
            BitConverter.GetBytes(GeometrySize).CopyTo(arr,   52);
            BitConverter.GetBytes(UserColorAsColor).CopyTo(arr,     56);

            BitConverter.GetBytes(MaxX).CopyTo(arr, 60);
            BitConverter.GetBytes(MaxY).CopyTo(arr, 64);
            BitConverter.GetBytes(MaxZ).CopyTo(arr, 68);
            BitConverter.GetBytes(MinX).CopyTo(arr, 72);
            BitConverter.GetBytes(MinY).CopyTo(arr, 76);
            BitConverter.GetBytes(MinZ).CopyTo(arr, 80);

            BitConverter.GetBytes(XScaleFactor).CopyTo(arr, 84);
            BitConverter.GetBytes(YScaleFactor).CopyTo(arr, 92);
            BitConverter.GetBytes(ZScaleFactor).CopyTo(arr, 100);

            BitConverter.GetBytes(XOffset).CopyTo(arr, 108);
            BitConverter.GetBytes(YOffset).CopyTo(arr, 116);
            BitConverter.GetBytes(ZOffset).CopyTo(arr, 124);

            return arr;
        }
    }
}
