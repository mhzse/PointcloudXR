/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;
using System.IO;

namespace PCXRFile
{
    public class PCXRReaderIO
    {
        private uint   _NumberOfPointsToBuffer;
        private string _FilePath;

        private uint   _PointsRead;
        private int    _BufferCurrentPoint;
        private byte[] _ReadBuffer;
        private byte[] _PointsBuffer;
        private int    _BytesToReadIntoPointsBuffer;
        private int    _OffsetInBuffer;

        public PCXRHeader _Header { get; }

        public PCXRReaderIO(string fullName, PCXRHeader header)
        {
            if (File.Exists(fullName))
            {
                _FilePath = fullName;
                _NumberOfPointsToBuffer = header.NumberOfPoints;

                _Header = header;

                _BytesToReadIntoPointsBuffer = (int)(_Header.Stride * sizeof(float) * _NumberOfPointsToBuffer);

                _ReadBuffer = new byte[_BytesToReadIntoPointsBuffer + _Header.GetHeaderSizeInBytes()];

                ReadFile();
            }
            else
            {
                throw new FileNotFoundException(fullName + " not found");
            }
        }

        public byte[] GetByteData()
        {
            return _PointsBuffer;
        }

        private void ReadFile()
        {
            _ReadBuffer = File.ReadAllBytes(_FilePath);

            _PointsBuffer = new byte[_BytesToReadIntoPointsBuffer];

            Buffer.BlockCopy(_ReadBuffer, _Header.OffsetToPointData, _PointsBuffer, 0, _BytesToReadIntoPointsBuffer);

            _BufferCurrentPoint = 0;
        }
    }
}
