/*
    Author: Mikael Hertz (mikael.hertz@gmail.com)
*/
using System;
using System.IO;
using Win32FileIO;

namespace PCXRFile
{
    public class PCXRWriterBuffered
    {
        private PCXRHeader   _Header;
        private string       _FullName;
        private BinaryWriter _Writer;
        private FileStream   _StreamWriter;
        private int          _NumberOfPointsToBuffer = 1000;
        private int          _PointsWritten = 0;
        private uint         _PointsBuffered = 0;
        private byte[]       _Buffer;
        private uint         _NumberOfPoints;

        private uint _BufferSpill = 0;
        private bool _UseBuffer   = true;

        private WinFileIO _WFIO;
        private const int _BufferSize = 1000000;

        public PCXRWriterBuffered(string fullName, PCXRHeader header)
        {
            _FullName = fullName;
            _NumberOfPoints = header.NumberOfPoints;
            _Header = header;

            if (File.Exists(fullName))
            {
                File.Delete(fullName);
            }

            _NumberOfPointsToBuffer = (int)_NumberOfPoints-1;
            int bufferSize = _NumberOfPointsToBuffer * _Header.Stride * sizeof(float);
            _Buffer = new byte[bufferSize];
            _BufferSpill = _NumberOfPoints % (uint)_NumberOfPointsToBuffer;
            if (_NumberOfPoints <= _NumberOfPointsToBuffer)
            {
                _UseBuffer = false;
            }

            _StreamWriter = File.Create(fullName);
            
            
            WriteHeader();
        }

        public void WritePointArray(byte[] points_buffer)
        {
            if (File.Exists(_FullName))
            {
                Close();
                File.Delete(_FullName);
            }
            _WFIO = new WinFileIO();
            _WFIO.OpenForWriting(_FullName);
            byte[] header_bytes = _Header.ToByteArray();
            _WFIO.PinBuffer(header_bytes);
            _WFIO.WriteBlocks(header_bytes.Length);

            _WFIO.PinBuffer(points_buffer);
            _WFIO.WriteBlocks(points_buffer.Length);

            _WFIO.Close();
        }

        public void WritePointOptimized(PCXRPoint point)
        {
            point.ToByteArray().CopyTo(_Buffer, _PointsBuffered * _Header.Stride * sizeof(float));
            _PointsBuffered++;

            if (_PointsBuffered == _NumberOfPointsToBuffer)
            {
                _PointsBuffered = 0;
                _PointsWritten += _NumberOfPointsToBuffer;
            }
        }

        public void WritePoint(PCXRPoint point)
        {
            if ((_UseBuffer == true) && (_PointsWritten == (_NumberOfPoints - _BufferSpill)))
            {
                _UseBuffer = false;
            }

            if (_UseBuffer)
            {
                if (_PointsBuffered < (_NumberOfPointsToBuffer-1))
                {
                    point.ToByteArray().CopyTo(_Buffer, _PointsBuffered * _Header.Stride * sizeof(float));
                    _PointsBuffered++;
                    return;
                }
                else
                {
                    try
                    {
                        point.ToByteArray().CopyTo(_Buffer, _PointsBuffered * _Header.Stride * sizeof(float));
                        _PointsBuffered++;

                        _StreamWriter.Write(_Buffer, 0, _NumberOfPointsToBuffer * _Header.Stride * sizeof(float));
                        _PointsWritten += _NumberOfPointsToBuffer;
                    }
                    catch (Exception e)
                    {
                        Console.Write(e.Message);
                    }
                    _PointsBuffered = 0;
                    return;
                }
            }
            else
            {
                byte[] pointBytes = point.ToByteArray();
                _StreamWriter.Write(pointBytes, 0, pointBytes.Length);
                _PointsWritten++;
                return;
            }
        }

        private void WriteHeader()
        {
            int hSize = _Header.GetHeaderSizeInBytes();
            byte[] hBytes = _Header.ToByteArray();
            _StreamWriter.Write(hBytes, 0, hSize);
        }

        public void Close()
        {
            _StreamWriter.Close();
        }
    }
}
