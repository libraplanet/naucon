using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace naucon
{
    using NAudio.Wave;
    using NAudio.CoreAudioApi;
    using NAudio.Wave.Compression;
    using NAudio.MediaFoundation;

    using naulib;

    class Program
    {
        enum WaveInType
        {
            WaveIn,
            WasapiLoppback
        }


        private static void WriteToEnd(Stream inStream, Stream outStream, int seg)
        {
            byte[] buf = new byte[seg];
            int cnt;
            while ((cnt = inStream.Read(buf, 0, seg)) > 0)
            {
                outStream.Write(buf, 0, cnt);
            }
        }

        private static void WriteToEnd(IWaveProvider inStream, Stream outStream, int seg)
        {
            byte[] buf = new byte[seg];
            int cnt;
            while ((cnt = inStream.Read(buf, 0, seg)) > 0)
            {
                outStream.Write(buf, 0, cnt);
            }
        }

        private static void message(string s)
        {
            Console.Error.WriteLine(s);
        }

        static void Main(string[] args)
        {
            int iSampleRate = 41000;
            int iCh = 2;
            int iBits = 16;
            int iVol = 100;
            WaveInType waveInType = WaveInType.WaveIn;
            bool isHead = true;
            bool isHelp = false;
            bool isTest = false;
            List<string> errorList = new List<string>();

            //read args
            {
                if (args != null)
                {
                    string sw = null;
                    for (int i = 0; i < args.Length; i++)
                    {
                        string arg = args[i];
                        if (string.IsNullOrWhiteSpace(sw))
                        {
                            switch (arg)
                            {
                                case "-d":
                                case "-r":
                                case "-c":
                                case "-b":
                                case "-v":
                                    sw = arg;
                                    break;
                                case "-N":
                                    isHead = false;
                                    break;
                                case "-test":
                                    isTest = true;
                                    break;
                                case "--h":
                                case "--v":
                                    isHelp = true;
                                    break;
                                default:
                                    errorList.Add(string.Format("arg[{0}] : illegal option \"{1}\"", new object[] { i, arg }));
                                    break;
                            }
                        }
                        else
                        {
                            Action<Action> Exec = (action) =>
                            {
                                try
                                {
                                    action();
                                }
                                catch (Exception e)
                                {
                                    errorList.Add(string.Format("arg[{0}] : illegal param \"{2}\" at \"{1}\"", new object[] { i, sw, arg }));
                                    errorList.Add(e.ToString());
                                }
                            };
                            switch (sw)
                            {
                                case "-d":
                                    switch (arg)
                                    {
                                        case "wasapiloopback":
                                            waveInType = WaveInType.WasapiLoppback;
                                            break;
                                        case "wavein":
                                            waveInType = WaveInType.WaveIn;
                                            break;
                                        default:
                                            errorList.Add(string.Format("arg[{0}] : illegal param \"{2}\" at \"{1}\"", new object[] { i, sw, arg }));
                                            break;
                                    }
                                    break;
                                case "-r":
                                    Exec(() => iSampleRate = int.Parse(arg));
                                    ;
                                    break;
                                case "-c":
                                    Exec(() => iCh = int.Parse(arg));
                                    break;
                                case "-b":
                                    Exec(() => iBits = int.Parse(arg));
                                    break;
                                case "-v":
                                    Exec(() => iVol = int.Parse(arg));
                                    break;
                            }
                            sw = null;
                        }
                    }
                }
            }

            if (isHead)
            {
                message("naucon v0.0.0.0.0.1");
                message("auther takumi.");
                message("copyright libraplanet.");
                message("license n/a");
                message("");
                if (!isHelp)
                {
                    message("parameter:");
                    message(string.Format("  sampling rale  {0} Hz", new object[] { iSampleRate }));
                    message(string.Format("  ch             {0} ch", new object[] { iCh }));
                    message(string.Format("  bits           {0} bit", new object[] { iBits }));
                    message(string.Format("  capture device {0}", new object[] { waveInType }));
                    message(string.Format("  vol            {0}", new object[] { iVol }));
                    message("");
                }
            }
            //start
            if (errorList.Count > 0)
            {
                foreach (string s in errorList)
                {
                    message(s);
                }
                message("");
            }
            else if (isHelp)
            {
                //help
                message("usage: naucon [[option] [param]]...");
                message("");
                message("options and pamrams");
                message("-d [wavein | wasapiloopback]  mode of capture device.");
                message("                              WaveIn or WASAPI Loopback.");
                message("-r [n]                        sampling rate.");
                message("                                e.g.) 441000");
                message("-c [n]                        channels.");
                message("                                e.g.) 2");
                message("-b [n]                        bits per sample.");
                message("                                e.g.) 16");
                message("-v [n]                        volume. 100 = 100%");
                message("                                e.g.) 16");
                message("-N                            no output head message.");
                message("-test                         argument test (no recording).");
                message("--h                           view help.");
                message("--v                           view version.");
                message("");
            }
            else
            {

                object mutex = new object();
                bool isActive = true;
                IWaveIn waveIn;
                WaveFormat outWaveFormat = new WaveFormat(iSampleRate, iBits, iCh);

                //init
                {
                    switch (waveInType)
                    {
                        case WaveInType.WasapiLoppback:
                            waveIn = new WasapiLoopbackCapture();
                            break;
                        case WaveInType.WaveIn:
                        default:
                            WaveCallbackInfo callback = WaveCallbackInfo.FunctionCallback();
                            waveIn = new WaveIn(callback);
                            waveIn.WaveFormat = outWaveFormat;
                            break;
                    }
                }

                if (isHead)
                {
                    message("output format:");
                    message(string.Format("  sampling rale  {0} Hz", new object[] { outWaveFormat.SampleRate }));
                    message(string.Format("  ch             {0} ch", new object[] { outWaveFormat.Channels }));
                    message(string.Format("  bits           {0} bit", new object[] { outWaveFormat.BitsPerSample }));
                    message(string.Format("  encoding       {0}", new object[] { outWaveFormat.Encoding }));
                    message("");
                }

                //event
                {
                    waveIn.DataAvailable += (sender, e) =>
                    {
                        lock (mutex)
                        {

                            if (WaveFormat.Equals(waveIn.WaveFormat, outWaveFormat) && (iVol == 100))
                            {
                                using (Stream consoleStream = Console.OpenStandardOutput())
                                {
                                    consoleStream.Write(e.Buffer, 0, e.BytesRecorded);
                                }
                            }
                            else
                            {
                                byte[] data;
                                AudioData audio = new AudioData(waveIn.WaveFormat, e.Buffer, e.BytesRecorded);

                                if (iVol != 100)
                                {
                                    audio.ChangeVolume(iVol / 100.0);
                                }

                                audio.Conver(outWaveFormat);
                                data = audio.ToBytes();
                                if ((data != null) && (data.Length > 0))
                                {
                                    using (Stream consoleStream = Console.OpenStandardOutput())
                                    {
                                        consoleStream.Write(data, 0, data.Length);
                                    }
                                }
                            }
                        }
                    };

                    waveIn.RecordingStopped += (sender, e) =>
                    {
                        lock (mutex)
                        {
                            isActive = false;
                        }
                    };
                }


                if (!isTest)
                {
                    waveIn.StartRecording();
                    while (true)
                    {
                        lock (mutex)
                        {
                            if (isActive)
                            {
                                Thread.Sleep(1);
                            }
                            else
                            {
                                return;
                            }
                        }
                    }
                }
            }
        }
    }
}
