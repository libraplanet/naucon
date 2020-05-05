using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace naulib
{
    using NAudio.Wave;
    
    public class AudioData
    {
        public WaveFormat WaveFormat { get; private set; }
        public double[] Samples { get; private set; }

        private static double[] createSampleList(WaveFormat waveFormat, Stream stream)
        {
            List<double> sampleList = new List<double>();

            using(AudioStream aStream = new AudioStream(stream))
            {
                Func<double> read = null;
                switch (waveFormat.Encoding)
                {
                    case WaveFormatEncoding.Pcm:
                        switch (waveFormat.BitsPerSample)
                        {
                            case 8:
                                read = () => aStream.ReadInt8();
                                break;
                            case 16:
                                read = () => aStream.ReadInt16();
                                break;
                            case 24:
                                read = () => aStream.ReadInt24();
                                break;
                            case 32:
                                read = () => aStream.ReadInt32();
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        break;
                    case WaveFormatEncoding.IeeeFloat:
                        switch (waveFormat.BitsPerSample)
                        {
                            case 32:
                                read = () => aStream.ReadFloat32();
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        break;
                }

                if (read != null)
                {
                    try
                    {
                        while (true)
                        {
                            double d = read();
                            sampleList.Add(d);
                        }
                    }
                    catch (EndOfStreamException)
                    {
                    }
                }
            }
            return sampleList.ToArray();
        }

        public AudioData(WaveFormat waveFormat, byte[] buffer)
        {
            using (MemoryStream stream = new MemoryStream(buffer))
            {
                Samples = createSampleList(waveFormat, stream);
                WaveFormat = waveFormat;
            }
        }
        public AudioData(WaveFormat waveFormat, byte[] buffer, int count)
        {
            using (MemoryStream stream = new MemoryStream(buffer, 0, count))
            {
                Samples = createSampleList(waveFormat, stream);
                WaveFormat = waveFormat;
            }
        }
        public AudioData(WaveFormat waveFormat, byte[] buffer, int index, int count)
        {
            using (MemoryStream stream = new MemoryStream(buffer, index, count))
            {
                Samples = createSampleList(waveFormat, stream);
                WaveFormat = waveFormat;
            }
        }
        public AudioData(WaveFormat waveFormat, Stream stream)
        {
            Samples = createSampleList(waveFormat, stream);
            WaveFormat = waveFormat;
        }

        private static double GetLinearValue(double val0, double val1, double vol)
        {
            return val0 + ((val1 - val0) * vol);
        }

        public void ChangeVolume(double scale)
        {
            for (int i = 0; i < Samples.Length; i++)
            {
                Samples[i] *= scale;
            }
        }

        public void Conver(WaveFormat outWaveFormat)
        {
            WaveFormat srcWaveFormat = this.WaveFormat;
            List<double> sampleList = new List<double>(this.Samples);
            //resample
            if (srcWaveFormat.SampleRate != outWaveFormat.SampleRate)
            {
                List<double> bufferList = new List<double>();
                int ch = srcWaveFormat.Channels;
                int samplsParSec = srcWaveFormat.SampleRate * ch;
                float sec = (float)sampleList.Count / (float)samplsParSec;
                int outSampleCnt = ((int)(outWaveFormat.SampleRate * sec));
                int outTotalSampleCnt = outSampleCnt * ch;
                double delta = srcWaveFormat.SampleRate / (double)outWaveFormat.SampleRate;
                Func<double, double, double, double> getLinearValue = (val0, val1, par) =>
                {
                    double dist = val1 - val0;
                    double v = dist * par;
                    return val0 + v;
                };
                for (int i = 0; i < outTotalSampleCnt; i++)
                {
                    int j = i / ch;
                    double k = j * delta;
                    int k0 = (int)Math.Floor(k);
                    int k1 = (int)Math.Ceiling(k);
                    double vol = k % 1.0;
                    int p0 = Math.Min(k0 * ch, srcWaveFormat.SampleRate);
                    int p1 = Math.Min(k1 * ch, srcWaveFormat.SampleRate);
                    int q0 = p0 + (i % ch);
                    int q1 = p1 + (i % ch);
                    double d0 = sampleList[q0];
                    double d1 = sampleList[q1];
                    double d2 = GetLinearValue(d0, d1, vol);
                    bufferList.Add(d2);
                }
                sampleList = bufferList;
            }

            //mod ch
            if (srcWaveFormat.Channels != outWaveFormat.Channels)
            {
                List<double> bufferList = new List<double>();
                switch (srcWaveFormat.Channels)
                {
                    case 1:
                        if (outWaveFormat.Channels == 2)
                        {
                            foreach (double d in sampleList)
                            {
                                bufferList.Add(d);
                                bufferList.Add(d);
                            }
                        }
                        break;
                    case 2:
                        if (outWaveFormat.Channels == 1)
                        {
                            for (int i = 0; i < sampleList.Count; i += 2)
                            {
                                bufferList.Add((sampleList[i + 0] + sampleList[i + 1]) / 2);
                            }
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
                sampleList = bufferList;
            }

            this.Samples = sampleList.ToArray();
            this.WaveFormat = outWaveFormat;
        }

        public byte[] ToBytes()
        {
            using (MemoryStream memStream = new MemoryStream())
            using (AudioStream aStream = new AudioStream(memStream))
            {
                Action<double> write = null;
                switch (WaveFormat.Encoding)
                {
                    case WaveFormatEncoding.Pcm:
                        switch (WaveFormat.BitsPerSample)
                        {
                            case 8:
                                write = (d) => aStream.WriteInt8(d);
                                break;
                            case 16:
                                write = (d) => aStream.WriteInt16(d);
                                break;
                            case 24:
                                write = (d) => aStream.WriteInt24(d);
                                break;
                            case 32:
                                write = (d) => aStream.WriteInt32(d);
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        break;

                    case WaveFormatEncoding.IeeeFloat:
                        switch (WaveFormat.BitsPerSample)
                        {
                            case 32:
                                write = (d) => aStream.AudioWriteFloat32(d);
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        break;
                }

                if (write != null)
                {
                    foreach (double d in Samples)
                    {
                        write(d);
                    }
                }
                memStream.Flush();
                return memStream.ToArray();
            }
        }
    }
}
