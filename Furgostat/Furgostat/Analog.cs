using System;
using MccDaq;
using System.Collections.Generic;
using System.Timers;
using System.Threading;
using System.IO;
namespace Furgostat
{
    public class Relaybox
    {
        MccDaq.MccBoard relaybox;
        bool[] currentPumpState = new bool[25];
        public Relaybox(int mcc_id)
        {
            relaybox = new MccDaq.MccBoard(mcc_id);

            for (int i = 1; i <= 24; ++i)
            {
                TurnOff(i);
            }
        }

        public void TurnOn(int index)
        {
            relaybox.DBitOut(MccDaq.DigitalPortType.FirstPortA, index - 1, MccDaq.DigitalLogicState.High);
            currentPumpState[index] = true;
        }

        public void TurnOff(int index)
        {
            relaybox.DBitOut(MccDaq.DigitalPortType.FirstPortA, index - 1, MccDaq.DigitalLogicState.Low);
            currentPumpState[index] = false;
        }
    }
    public class Analog
    {
        MccDaq.MccBoard DaqBoard;
        private int Rate = 500;
        public int HighChan = 14, LowChan = 0;
        int NumPoints = 31744000;
        private Double[] ADData;
        private IntPtr MemHandle;
        public System.Timers.Timer tmrContinuousRead;
        // calibration vectors
        public Double[] p1, p0;
        public string dataPath = @"C:\Users\s135322\Desktop\Furgostat\data";
        public string currentCalibrationDatafile;

        public Analog(int mcc_index)
        {
            DaqBoard = new MccDaq.MccBoard(0);
            ADData = new Double[HighChan - LowChan + 1];
            MemHandle = MccDaq.MccService.WinBufAllocEx(NumPoints);
            // initialize
            p0 = new Double[HighChan + 1]; p1 = new Double[HighChan + 1];
            for (int i = 0; i <= HighChan; ++i)
            { p1[i] = 1; p0[i] = 0; }
        }
        public void readBlank()
        {
            Double[] OD = StartSingleReadingWindow(5, "OD");
            for (int i = 0; i < OD.Length; ++i)
                p0[i] += (-OD[i]);
        }
        public void updateCalibrationVectors(double[] np1, double[] np0, string datafile)
        {
            np1.CopyTo(p1, 0);
            np0.CopyTo(p0, 0);
            currentCalibrationDatafile = datafile;
        }
        public Double[] returnLastRead()
        {
            return ADData;
        }
        public Double[] returnLastCalibratedODValue()
        {
            return convertADtoOD(ADData);
        }
        private Double[] convertADtoOD(Double[] AD) // Calibrations
        {
            Double[] OD = new Double[HighChan - LowChan + 1];
            Double odc;
            for (int i = LowChan; i <= HighChan; ++i)
            {
                odc = AD[i] * p1[i] + p0[i];
                if (odc <= 1e2)
                    OD[i] = odc;
                else
                    OD[i] = (float)-100;
            }
            return OD;
        }
        public Double[] StartSingleReadingWindow(double Time, string OutputFormat = "OD")
        {
            // New DAQBoard for reading.
            DaqBoard = new MccDaq.MccBoard(0);

            MccDaq.ErrorInfo ULStat;
            int FirstPoint, NumChans = HighChan - LowChan + 1, CurIndex, CurCount;
            short Status;
            NumPoints = (int)(Time) * Rate * NumChans;
            MemHandle = MccDaq.MccService.WinBufAllocEx(10 * NumPoints);

            Thread.Sleep(100);
            MccDaq.ScanOptions Options = MccDaq.ScanOptions.ConvertData;
            ULStat = DaqBoard.AInScan(LowChan, HighChan, NumPoints, ref Rate,
                               MccDaq.Range.Bip10Volts, MemHandle, Options);
            DaqBoard.GetStatus(out Status, out CurCount,
                out CurIndex, MccDaq.FunctionType.AiFunction);
            FirstPoint = CurIndex;

            // recently collected data
            int N = FirstPoint + NumChans;
            ushort[] addata = new ushort[N];
            MccDaq.MccService.WinBufToArray(MemHandle, addata, 0, N);

            List<float> channel_data = new List<float>();
            for (int i = 0; i <= HighChan; ++i)
            {
                //sum = 0;
                channel_data.RemoveRange(0, channel_data.Count);
                for (int j = i; j < N; j += NumChans)
                    //sum += addata[j];
                    channel_data.Add(addata[j]);
                // take median voltage value
                channel_data.Sort();
                ADData[i] = channel_data[(Int32)(channel_data.Count / 2)];
                // convert from int to double precision voltage value
                ADData[i] = (ADData[i] - 32768) / (float)3276.8;
            }

            DaqBoard.StopBackground(MccDaq.FunctionType.AiFunction);
            MccDaq.MccService.WinBufFreeEx(MemHandle);
            if (OutputFormat == "OD")
                return convertADtoOD(ADData);
            else
                return ADData;
        }
    }
}