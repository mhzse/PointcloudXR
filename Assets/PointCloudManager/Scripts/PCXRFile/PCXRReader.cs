/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;
using System.IO;

namespace PCXRFile
{
    public class PCXRReader
    {
        private FileStream _FileStream;
        private uint _NumberOfPointsToBuffer;
        private string _FilePath;

        private uint _PointsRead;
        private int _BufferCurrentPoint;
        private byte[] _Buffer;
        private int _BytesToReadIntoBuffer;
        private int _OffsetInBuffer;
        public PCXRHeader _Header { get; }

        private long _TotalReadTime = 0;

        public PCXRReader(string fullName, uint numberOfPointsToBuffer)
        {
            if (File.Exists(fullName))
            {
                _FilePath = fullName;
                _NumberOfPointsToBuffer = numberOfPointsToBuffer;

                _Header = new PCXRHeader();
                _Header.ReadHeader(fullName);

                _BytesToReadIntoBuffer = (int)(_Header.Stride * sizeof(float) * _NumberOfPointsToBuffer);
                _FileStream = File.OpenRead(_FilePath);
                _Header.OffsetToPointData = 92;
                _FileStream.Seek(_Header.OffsetToPointData, SeekOrigin.Begin);

                _Buffer = new byte[_BytesToReadIntoBuffer];

                FillBuffer();
            }
            else
            {
                throw new FileNotFoundException(fullName + " not found");
            }
        }

        public PCXRPoint GetNextPoint()
        {
            bool pointReturned = false;
            PCXRPoint point;
            
            if (_PointsRead <= _Header.NumberOfPoints)
            {
                if (_BufferCurrentPoint < _NumberOfPointsToBuffer)
                {
                    pointReturned = true;
                    point = ReadPoint();
                    return point;
                }
                else
                {
                    // Fill buffer
                    FillBuffer();
                    point = ReadPoint();
                    pointReturned = true;
                    return point;
                }
            }
            
            if(!pointReturned)
            {
                Exception ex = new Exception("No more points in data");
                throw ex;
            }

            point = new PCXRPoint();
            return point;
        }

        private PCXRPoint ReadPoint()
        {
            PCXRPoint point = new PCXRPoint();

            _OffsetInBuffer = _BufferCurrentPoint * _Header.Stride * sizeof(float);

            point.x = BitConverter.ToSingle(_Buffer, _OffsetInBuffer + 0);
            point.y = BitConverter.ToSingle(_Buffer, _OffsetInBuffer + 4);
            point.z = BitConverter.ToSingle(_Buffer, _OffsetInBuffer + 8);

            point.red = BitConverter.ToSingle(_Buffer, _OffsetInBuffer + 12);
            point.green = BitConverter.ToSingle(_Buffer, _OffsetInBuffer + 16);
            point.blue = BitConverter.ToSingle(_Buffer, _OffsetInBuffer + 20);
            point.alpha = BitConverter.ToSingle(_Buffer, _OffsetInBuffer + 24);

            point.deleted = BitConverter.ToSingle(_Buffer, _OffsetInBuffer + 28);
            point.selected = BitConverter.ToSingle(_Buffer, _OffsetInBuffer + 32);

            point.intensity_normalized = BitConverter.ToSingle(_Buffer, _OffsetInBuffer + 36);
            point.classification = BitConverter.ToSingle(_Buffer, _OffsetInBuffer + 40);

            point.id = BitConverter.ToSingle(_Buffer, _OffsetInBuffer + 44);

            _BufferCurrentPoint++;
            _PointsRead++;

            return point;
        }

        private void FillBuffer()
        {
            _FileStream.Read(_Buffer, 0, _BytesToReadIntoBuffer);
            _BufferCurrentPoint = 0;
        }

        public void Close()
        {
            _FileStream.Close();
        }
    }
}
