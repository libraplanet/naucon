using System;
using System.IO;

namespace naulib
{
    public class AudioStream : IDisposable
    {
        private Stream stream;

        public AudioStream(Stream stream)
        {
            this.stream = stream;
        }

        public void Dispose()
        {
            stream.Dispose();
        }

        #region [reader]
        private static Int32 createInt32LE(byte[] bytes)
        {
            const Int32 MASK = 0x00FFFFFF;
            Int32 ret = 0;
            for (int i = 0; i < 4; i++)
            {
                ret >>= 8;
                if (i < bytes.Length)
                {
                    byte d = bytes[i];
                    ret &= MASK;
                    ret |= ((d << 24) & (~MASK));
                }
            }
            return ret;
        }

        private static Int64 createInt64LE(byte[] bytes)
        {
            const Int64 MASK = 0x00FFFFFFFFFFFFFF;
            Int64 ret = 0;
            for (int i = 0; i < 8; i++)
            {
                if (i < bytes.Length)
                {
                    byte d = bytes[i];
                    ret &= MASK;
                    ret |= ((d << 56) & (~MASK));
                }
                else
                {
                    ret >>= 8;
                }
            }
            return ret;
        }

        private byte[] readBytes(int length)
        {
            byte[] bytes = new byte[length];
            if (stream.Read(bytes, 0, bytes.Length) == bytes.Length)
            {
                return bytes;
            }
            else
            {
                throw new EndOfStreamException();
            }
        }

        public double ReadInt8()
        {
            byte[] bytes = readBytes(1);
            Int64 n = createInt64LE(bytes);
            double f = (double)n / 0x7F;
            return f;
        }

        public double ReadInt16()
        {
            byte[] bytes = readBytes(2);
            Int64 n = createInt64LE(bytes);
            double f = (double)n / 0x7FFF;
            return f;
        }

        public double ReadInt24()
        {
            byte[] bytes = readBytes(3);
            Int64 n = createInt64LE(bytes);
            double f = (double)n / 0x7FFFFF;
            return f;
        }

        public double ReadInt32()
        {
            byte[] bytes = readBytes(4);
            Int64 n = createInt64LE(bytes);
            double f = (double)n / 0x7FFFFFFF;
            return f;
        }

        public double ReadFloat32()
        {
            unsafe
            {
                byte[] bytes = readBytes(4);
                Int32 n = createInt32LE(bytes);
                float f = *((float*)&n);
                return f;
            }
        }

        public double ReadFloat64()
        {
            unsafe
            {
                byte[] bytes = readBytes(8);
                Int64 n = createInt64LE(bytes);
                double d = *((double*)&n);
                return d;
            }
        }
        #endregion

        #region [writer]
        private void writeInt32LE(Int32 val, int length)
        {
            Int32 t = val;
            for (int i = 0; i < length; i++)
            {
                byte d = (byte)(t & 0xFF);
                stream.WriteByte(d);
                t >>= 8;
            }
        }

        private void writeInt64LE(Int64 val, int length)
        {
            Int64 t = val;
            for (int i = 0; i < length; i++)
            {
                byte d = (byte)(t & 0xFF);
                stream.WriteByte(d);
                t >>= 8;
            }
        }

        private Int64 getValue(double val, double scale, Int64 min, Int64 max)
        {
            double v = val * scale;
            Int64 ret = (Int64)v;
            ret = Math.Max(ret, min);
            ret = Math.Min(ret, max);
            return ret;
        }

        public void WriteInt8(double val)
        {
            writeInt64LE(getValue(val, 0x7F, -128, 127), 1);
        }

        public void WriteInt16(double val)
        {
            writeInt64LE(getValue(val, 0x7FFF, -32768, 32767), 2);
        }

        public void WriteInt24(double val)
        {
            writeInt64LE(getValue(val, 0x7FFFFF, -8388608, 8388607), 3);
        }

        public void WriteInt32(double val)
        {
            writeInt64LE(getValue(val, 0x7FFFFFFF, -2147483648, 2147483647), 4);
        }

        public void AudioWriteFloat32(double val)
        {
            unsafe
            {
                Int32 n = *((Int32*)&val);
                writeInt32LE(n, 4);
            }
        }

        public void AudioWriteFloat64(double val)
        {
            unsafe
            {
                Int64 n = *((Int64*)&val);
                writeInt64LE(n, 8);
            }
        }
        #endregion
    }
}
