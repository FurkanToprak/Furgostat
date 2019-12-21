using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.Timers;
using System.IO;
namespace Furgostat
{
    public class Core
    {
        /* Interesting data structure for .NET: https://docs.microsoft.com/en-us/dotnet/api/system.collections.objectmodel.observablecollection-1?view=netframework-4.8
         Used for notification purposes in the GUI.*/
        public ObservableCollection<LogEntry> MainLog = new ObservableCollection<LogEntry>();
        // TODO: Configurable
        public int[] Suction;
        public int[,] MediaRelayIDs;
        public int[,] DrugARelayIDs;
        public int[,] DrugBRelayIDs;
        public int[] LEDPDSwitch;
        // Reinitializes Input every hour to clear buffer
        public System.Timers.Timer ReinitializeODReader;
        public Analog Input;
        public Relaybox Relaybox1 = new Relaybox(1);
        public Relaybox Relaybox2 = new Relaybox(2);
        public Relaybox[] Relays;
        public System.Timers.Timer DataCollector;
        public Stopwatch GlobalWatch = new Stopwatch(); /* Tick Tock */
        public Algorithms algos;
        public double DataCollectionFrequency = 10;
        public int TimeFormat;
        public ControlPanel GUI;
        public List<double> ODTime = new List<double>();
        public ODMonitor ODDisplay;
        List<List<double>> OD = new List<List<double>>();
        double[] CurrentOD;
        //TODO: Customize
        string[] files = Directory.GetFiles(@"C:\Users\s135322\Desktop\Furgostat\data\calibrations\",
                "LaserCalibration*.csv");
        public Core()
        {
            Input = new Analog(0);
            Relays = new Relaybox[] { Relaybox1, Relaybox2 };
            Suction = new int[] { 1, 22 };
            MediaRelayIDs = new int[,] { { 0, 1 }, { 0, 4 }, { 0, 7 }, { 0, 10 }, { 0, 13 }, { 0, 16 }, { 0, 19 }, { 0, 22 }, { 1, 1 }, { 1, 4 }, { 1, 7 }, { 1, 10 }, { 1, 13 }, { 1, 16 }, { 1, 19 } };
            DrugARelayIDs = new int[,] { { 0, 2 }, { 0, 5 }, { 0, 8 }, { 0, 11 }, { 0, 14 }, { 0, 17 }, { 0, 20 }, { 0, 23 }, { 1, 2 }, { 1, 5 }, { 1, 8 }, { 1, 11 }, { 1, 14 }, { 1, 17 }, { 1, 20 } };
            DrugBRelayIDs = new int[,] { { 0, 3 }, { 0, 6 }, { 0, 9 }, { 0, 12 }, { 0, 15 }, { 0, 18 }, { 0, 21 }, { 0, 24 }, { 1, 3 }, { 1, 6 }, { 1, 9 }, { 1, 12 }, { 1, 15 }, { 1, 18 }, { 1, 21 } };
            LEDPDSwitch = new int[] { 1, 24 }; //currently active but circuit is not modified yet
            for (int i = 0; i < Input.HighChan; ++i)
                OD.Add(new List<double>());
            GlobalWatch.Start();
            ReinitializeODReader = new System.Timers.Timer(3600 * 1000);
            ReinitializeODReader.Elapsed += ReinitializeODReader_Tick;
            DataCollector = new System.Timers.Timer(DataCollectionFrequency * 1000);
            DataCollector.Elapsed += DataCollector_Tick;
            DataCollector.AutoReset = false;
        }
        public void AddODData(Double Time, Double[] od)
        {
            ODTime.Add((double)Time);
            for (int i = 0; i < OD.Count; ++i)
            {
                OD[i].Add((double)od[i]);
            }
        }
        public void LoadNewLaserCalibration(string path)
        {
            string[] cal = File.ReadAllLines(path);
            double[] p1 = new double[Input.HighChan + 1], p0 = new double[Input.HighChan + 1];
            int i = 0;
            foreach (string s in cal[0].Split('\t'))
                if (s != "")
                {
                    try
                    {
                        p0[i++] = Single.Parse(s);
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err.Message);
                    }
                }
            i = 0;
            foreach (string s in cal[1].Split('\t'))
                if (s != "")
                {
                    try
                    {
                        p1[i++] = Single.Parse(s);
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err.Message);
                    }
                }
            Input.updateCalibrationVectors(p1, p0, path);
        }

        public void ReinitializeODReader_Tick(object state, ElapsedEventArgs e)
        {
            DataCollector.Stop();
            Input = new Analog(0); //data acquisition board is defined 

            // Load Calibration
            if (GUI != null)
            {
                LoadNewLaserCalibration(GUI.SelectedLaserCalibrationPath());
            }
            // If Uninitialized GUI, Pick Default.
            else
            {
                string[] files = Directory.GetFiles(@"C:\Users\s135322\Desktop\Furgostat\data\calibrations\",
                    "LaserCalibration*.csv");
                LoadNewLaserCalibration(files[files.Length - 1]);
            }
            DataCollector.Start();
            DataCollector.Start();
        }
        public void DataCollector_Tick(object state, ElapsedEventArgs e)
        {
            Double elapsedTime = (GlobalWatch.ElapsedMilliseconds / 1000.0);
            if (TimeFormat == 1)
                elapsedTime /= 60;
            else if (TimeFormat == 2)
                elapsedTime /= (60 * 24);
            // Turn on LED/PDs, and measure OD
            Relays[LEDPDSwitch[0]].TurnOn(LEDPDSwitch[1]);
            CurrentOD = Input.StartSingleReadingWindow(1, "OD");
            Relays[LEDPDSwitch[0]].TurnOff(LEDPDSwitch[1]);
            // Skip failed analog reads
            if (CurrentOD[0] > -10)
                AddODData(elapsedTime, CurrentOD);

            // built-in data streamer
            if (ODDisplay != null && ODTime.Count > 0 && CurrentOD[0] > -10)
            {
                try
                {
                    ODDisplay.AddData(ODTime[ODTime.Count - 1], CurrentOD);
                }
                catch (ArgumentException err)
                {
                    ODDisplay.AddData(ODTime[ODTime.Count - 1], CurrentOD);
                    Console.WriteLine(err.Message);
                }
            }
            else
            {
                Console.WriteLine("AAA");
                Log("AAAA");
                if (ODTime.Count <= 0) // TODO: BUG
                    Log("BBC");
                if (CurrentOD[0] <= -10) // TODO: BUG
                    Log("BBA");
            }
            DataCollector.Start();
        }
        public void Log(string message)
        {
            MainLog.Add(new LogEntry() { Time = DateTime.Now, Message = message });
        }
        public void FillMedia(List<int> CultureId, double Time)
        {
            List<int> cosmetic = new List<int>();
            foreach (int i in CultureId)
            {
                Relays[MediaRelayIDs[i, 0]].TurnOn(MediaRelayIDs[i, 1]);
                cosmetic.Add(i + 1);
            }
            Log("Media is started flowing for cultures" + string.Join(", ", cosmetic));
            Thread.Sleep((Int32)(Time * 1000));
            foreach (int i in CultureId)
            {
                Relays[MediaRelayIDs[i, 0]].TurnOff(MediaRelayIDs[i, 1]);
            }
            Log("Media is stopped for cultures" + string.Join(", ", cosmetic));
        }
        public void AllSuction(double Time, double MixingTime)
        {
            Thread.Sleep((Int32) MixingTime * 1000);
            Relays[Suction[0]].TurnOn(Suction[1]);
            Log("Suction is started for all cultures." + Time);
            Thread.Sleep((Int32)(Time * 1000));
            Relays[Suction[0]].TurnOff(Suction[1]);
            Log("Suction is stopped for all cultures.");
        }
        public void FillDrugA(List<int> CultureId, double Time)
        {
            List<int> Cosmetic = new List<int>();
            foreach (int i in CultureId)
            {
                Relays[DrugARelayIDs[i, 0]].TurnOn(DrugARelayIDs[i, 1]);
                Cosmetic.Add(i + 1);
            }
            Log("Drug A is started flowing for cultures" + string.Join(", ", Cosmetic));

            Thread.Sleep((Int32)(Time * 1000));

            foreach (int i in CultureId)
            {
                Relays[DrugARelayIDs[i, 0]].TurnOff(DrugARelayIDs[i, 1]);
            }
            Log("Drug A is stopped for cultures" + string.Join(", ", Cosmetic));
        }
        public void FillDrugB(List<int> CultureId, double Time)
        {
            List<int> Cosmetic = new List<int>();
            foreach (int i in CultureId)
            {
                Relays[DrugBRelayIDs[i, 0]].TurnOn(DrugBRelayIDs[i, 1]);
                Cosmetic.Add(i + 1);
            }
            Log("Drug B is started flowing for cultures" + string.Join(", ", Cosmetic));

            Thread.Sleep((Int32)(Time * 1000));

            foreach (int i in CultureId)
            {
                Relays[DrugBRelayIDs[i, 0]].TurnOff(DrugBRelayIDs[i, 1]);
            }
            Log("Drug B is stopped for cultures" + string.Join(", ", Cosmetic));
        }
        public List<List<double>> LastCycleReadings()
        {
            List<List<double>> LastCycleOD = new List<List<double>>();
            int N = OD[0].Count;
            for(int i = 0; i < OD.Count; ++i)
            {
                LastCycleOD.Add(new List<double>());
                for(int j = (Int32) (algos.CyclePeriod * 60 / DataCollectionFrequency); j >= 0 ; --j)
                {
                    LastCycleOD[i].Add(OD[i][N - j - 1]);
                }
            }
            return LastCycleOD;
        }
    }
}
public class LogEntry
{
    public string Message { get; set; }
    public DateTime Time { get; set; }
}
public class CalibrationFile
{
    public string Name { get; set; }
    public string Path { get; set; }
}