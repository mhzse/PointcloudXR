/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System.IO;

namespace PCXRFile
{
    public class PCXRWriter
    {
        private PCXRHeader _Header;
        private string _FullName;
        private BinaryWriter _Writer;

        public PCXRWriter(string fullName, PCXRHeader header)
        {
            _FullName = fullName;
            _Header = header;
            _Writer = new BinaryWriter(File.Open(_FullName, FileMode.Create));
            WriteHeader();
        }

        private void WriteHeader()
        {
            _Writer.Write(_Header.NumberOfPoints);
            _Writer.Write(_Header.Stride);
            _Writer.Write(_Header.OffsetToPointData);

            _Writer.Write(_Header.UserStartPositionX);
            _Writer.Write(_Header.UserStartPositionY);
            _Writer.Write(_Header.UserStartPositionZ);

            _Writer.Write(_Header.UserStartRotationW);
            _Writer.Write(_Header.UserStartRotationX);
            _Writer.Write(_Header.UserStartRotationY);
            _Writer.Write(_Header.UserStartRotationZ);
            
            _Writer.Write(_Header.AddIntensityValueToColor);
            _Writer.Write(_Header.ColorIntensity);
            _Writer.Write(_Header.GeometrySize);
            _Writer.Write(_Header.UserColorAsColor);

            _Writer.Write(_Header.MaxX);
            _Writer.Write(_Header.MaxY);
            _Writer.Write(_Header.MaxZ);
            _Writer.Write(_Header.MinX);
            _Writer.Write(_Header.MinY);
            _Writer.Write(_Header.MinZ);
        }

        public void WritePoint(PCXRPoint point)
        {
            _Writer.Write(point.x);
            _Writer.Write(point.y);
            _Writer.Write(point.z);
            _Writer.Write(point.red);
            _Writer.Write(point.green);
            _Writer.Write(point.blue);
            _Writer.Write(point.alpha);
            _Writer.Write(point.deleted);
            _Writer.Write(point.selected);
            _Writer.Write(point.intensity_normalized);
            _Writer.Write(point.classification);
            _Writer.Write(point.id);
        }

        public void Close()
        {
            _Writer.Close();
        }
    }
}
