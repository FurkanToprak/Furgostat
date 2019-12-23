using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
namespace Furgostat
{
    public class Tube
    {
        public double DrugConcentration;
        public String Phase;
        public bool DrugAllowed;
        public List<double> PTracker;
        public List<String> Last10Events;
        public double ODFinal;
    }
    public class Algorithms
    {
        public static int TubeCount = 15;
        public double CyclePeriod = 10; // Minutes
        public Boolean IsRunning = false;
        public Double NewVolume = 10;
        public string Path = "/"; // TODO
        public string FileName;
        public double[] LowerThreshold = { 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001 };
        public double[] MiddleThreshold = { 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15 };
        public double[] UpperThreshold = { 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3 };
        public double[] GlobalThreshold = { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
        public List<Tube> Tubes;
        public System.Timers.Timer Cycler;
        Core core;
        public double DrugAdditionTime = 60;
        public double MediaAdditionTime = 60;
        public double MixingTime;
        public double PCoefficient = 0.01;
        public double ICoefficient = 0.01;
        public double DCoefficient = 1;
        public int TimedMorbidostatCounter = 0;
        public double TimedMorbidostatHours = 8;
        public double[] TubeVolumeSettings = { 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15 }; // mL
        public double[] TubeDrugAConcentrationSettings = { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 }; // micromolar
        public double[] TubeDrugBConcentrationSettings = { 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50 }; // micromolar
        public bool[] TubeStatus = { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };
        public ObservableCollection<TubeEntry> TubeLogger = new ObservableCollection<TubeEntry>();
        public Algorithms(ref Core _core)
        {
            Tubes = new List<Tube>();
            core = _core;
        }
        public void StartCycle(String Type)
        {
            // Protects against starting cycle without ending previous.
            if (IsRunning)
                return;
            IsRunning = true;
            // Cycles every 'CyclePeriod' minutes.
            Cycler = new System.Timers.Timer((Int32)CyclePeriod * 60 * 1000);
            if (Type == "Morbidostat")
            {
                Cycler.Elapsed += new System.Timers.ElapsedEventHandler(MorbidostatCycle);
            }
            else if (Type == "Turbidostat")
            {
                Cycler.Elapsed += new System.Timers.ElapsedEventHandler(TurbidostatCycle);
            }
            else if(Type == "Chemostat") // Chemostat
            {
                Cycler.Elapsed += new System.Timers.ElapsedEventHandler(ChemostatCycle);
            }
            else if(Type == "Timed Morbidostat")
            {
                TimedMorbidostatCounter = 0;
                Cycler.Elapsed += new System.Timers.ElapsedEventHandler(TimedMorbidostatCycle);
            }
            for (int i = 0; i < TubeCount; ++i)
            {
                Tube Culture = new Tube();
                Culture.DrugAllowed = true;
                Culture.Phase = "I";
                Culture.PTracker = new List<Double>();
                Culture.Last10Events = new List<String>();
                Culture.Last10Events.Add("Started");
                Culture.DrugConcentration = 0;
                Culture.ODFinal = 0;
                Tubes.Add(Culture);
            }
            Cycler.Start();
            FileName = Path + Type + "-" + DateTime.Now.ToString("yyyy-MMM-dd-HH-mm-ss") + ".csv";
            core.Log(Type + " has started.");
            UpdateTubeStatus();
        }
        public void TimedMorbidostatCycle(object state, System.Timers.ElapsedEventArgs e)
        {
            core.Log("Timed Morbidostat cycle is active.");
            // Seperate tubes into those that will recieve drug A and media
            List<int> WillDrugA = new List<int>();
            List<int> WillMedia = new List<int>();
            List<List<double>> LastCycleReadings = core.LastCycleReadings();
            List<string> Phases = new List<string>();
            for (int i = 0; i < Tubes.Count; ++i)
            {
                Phases.Add(Tubes[i].Phase);
                double ODFinal = 0;
                for (int j = Math.Max(0, -1 - 5); j < LastCycleReadings[i].Count; ++j)
                {
                    ODFinal += LastCycleReadings[i][j];
                }
                // Possible Bug: CycleReadings[0] can have count of 0.
                ODFinal /= Math.Min(LastCycleReadings[0].Count, 5);
                if (ODFinal < LowerThreshold[i])
                {
                    Tubes[i].Last10Events.Add("Nothing Added");
                    Tubes[i].Phase = "I";
                }
                else if (TimedMorbidostatCounter % (Int32)(TimedMorbidostatHours * 60 / CyclePeriod) != 0 || ODFinal >= LowerThreshold[i] && ODFinal < MiddleThreshold[i])
                {
                    Tubes[i].Last10Events.Add("Media Added");
                    Tubes[i].Phase = "M";
                    WillMedia.Add(i);
                    Tubes[i].DrugConcentration = (Tubes[i].DrugConcentration * TubeVolumeSettings[i]) / (TubeVolumeSettings[i] + NewVolume);
                }
                else if (TimedMorbidostatCounter % (Int32)(TimedMorbidostatHours * 60 / CyclePeriod) == 0)
                {
                    Tubes[i].Last10Events.Add("Drug A Added");
                    Tubes[i].Phase = "A";
                    WillDrugA.Add(i);
                    Tubes[i].DrugConcentration = (Tubes[i].DrugConcentration * TubeVolumeSettings[i] + TubeDrugAConcentrationSettings[i] * NewVolume) / (TubeVolumeSettings[i] + NewVolume);
                    //System.Timers.Timer Administer = //
                }

            }
            core.FillMedia(WillMedia, MediaAdditionTime);
            core.FillDrugA(WillDrugA, DrugAdditionTime);
            core.AllSuction(MediaAdditionTime * 1.2 / 1.5, MixingTime);
            core.Log("Timed Morbidostat cycle is done.");
            Document(LastCycleReadings, Phases);
            TimedMorbidostatCounter++;
            UpdateTubeStatus();
        }
        public void MorbidostatCycle(object state, System.Timers.ElapsedEventArgs e)
        {
            core.Log("Morbidostat cycle is active.");
            // Seperate tubes into those that will recieve drug A, drug B, and media
            List<int> WillDrugA = new List<int>();
            List<int> WillDrugB = new List<int>();
            List<int> WillMedia = new List<int>();
            List<List<double>> LastCycleReadings = core.LastCycleReadings();
            List<string> Phases = new List<string>();
            for (int i = 0; i < Tubes.Count; ++i)
            {
                Phases.Add(Tubes[i].Phase);
                double ODFinal = 0; // Last 5 OD values.
                for (int j = Math.Max(0, LastCycleReadings.Count - 1 - 5); j < LastCycleReadings[i].Count; ++j)
                {
                    ODFinal += LastCycleReadings[i][j];
                }
                // Possible Bug: CycleReadings[0] can have count of 0.
                ODFinal /= Math.Min(LastCycleReadings[0].Count, 5); // Mean
                double P = ODFinal - MiddleThreshold[i];
                Tubes[i].PTracker.Add(P);
                if (Tubes[i].PTracker.Count > 5)
                    Tubes[i].PTracker.RemoveAt(0);
                double I = 0;
                for (int j = 0; j < Tubes[i].PTracker.Count; ++j)
                {
                    I += Tubes[i].PTracker[j];
                }
                double ODInitial = 0; // First 5 OD values.
                for (int j = 0; j < Math.Min(LastCycleReadings[i].Count, 5); ++j)
                {
                    ODInitial += LastCycleReadings[i][j];
                }
                ODInitial /= Math.Min(LastCycleReadings[0].Count, 5);
                double D = (ODFinal - Tubes[i].ODFinal) / (CyclePeriod / 60);
                Tubes[i].ODFinal = ODFinal;
                double PID = P > 0 ? 1E5 + 0.01 * I + D : -1E5 + 0.01 * I + D;
                if(ODFinal > GlobalThreshold[i])
                {
                    Tubes[i].Last10Events.Add("Nothing Added; Optical Error.");
                    Tubes[i].Phase = "I";
                }
                else if (ODFinal < LowerThreshold[i])
                {
                    Tubes[i].Last10Events.Add("Nothing Added");
                    Tubes[i].Phase = "I";
                }
                else if (Tubes[i].DrugAllowed && PID > 0)
                {
                    Tubes[i].DrugAllowed = false;
                    if (Tubes[i].DrugConcentration >= 0.6 * TubeDrugAConcentrationSettings[i])
                    {
                        // Drug B:
                        WillDrugB.Add(i);
                        Tubes[i].Last10Events.Add("Drug B Added");
                        Tubes[i].Phase = "B";
                        Tubes[i].DrugConcentration = (Tubes[i].DrugConcentration * TubeVolumeSettings[i] + TubeDrugBConcentrationSettings[i] * NewVolume) / (TubeVolumeSettings[i] + NewVolume);
                    }
                    else
                    {
                        // Drug A:
                        WillDrugA.Add(i);
                        Tubes[i].Last10Events.Add("Drug A Added");
                        Tubes[i].Phase = "A";
                        Tubes[i].DrugConcentration = (Tubes[i].DrugConcentration * TubeVolumeSettings[i] + TubeDrugAConcentrationSettings[i] * NewVolume) / (TubeVolumeSettings[i] + NewVolume);
                    }
                }
                else
                {
                    Tubes[i].DrugAllowed = true;
                    WillMedia.Add(i);
                    Tubes[i].Last10Events.Add("Media Added");
                    Tubes[i].Phase = "M";
                    Tubes[i].DrugConcentration = (Tubes[i].DrugConcentration * TubeVolumeSettings[i]) / (TubeVolumeSettings[i] + NewVolume);
                }
                if (Tubes[i].Last10Events.Count > 10)
                    Tubes[i].Last10Events.RemoveAt(0);
            }
            core.FillMedia(WillMedia, MediaAdditionTime);
            core.FillDrugA(WillDrugA, DrugAdditionTime);
            core.FillDrugB(WillDrugB, DrugAdditionTime);
            core.AllSuction(MediaAdditionTime * 1.2 / 1.5, MixingTime);
            core.Log("Morbidostat cycle is done.");
            Document(LastCycleReadings, Phases);
            UpdateTubeStatus();
        }
        public void TurbidostatCycle(object state, System.Timers.ElapsedEventArgs e)
        {
            core.Log("Turbidostat cycle is active.");
            List<int> WillMedia = new List<int>();
            List<string> Phases = new List<string>();
            List<List<double>> LastCycleReadings = core.LastCycleReadings();
            for (int i = 0; i < Tubes.Count; ++i)
            {
                Phases.Add(Tubes[i].Phase);
                double ODFinal = 0;
                for (int j = Math.Max(0, -1 - 5); j < LastCycleReadings[i].Count; ++j)
                {
                    ODFinal += LastCycleReadings[i][j];
                }
                // Possible Bug: CycleReadings[0] can have count of 0.
                ODFinal /= Math.Min(LastCycleReadings[0].Count, 5);
                if (ODFinal > GlobalThreshold[i])
                {
                    Tubes[i].Last10Events.Add("Nothing Added; Optical Error.");
                    Tubes[i].Phase = "I";
                }
                else if (ODFinal > MiddleThreshold[i] && ODFinal < UpperThreshold[i])
                {
                    WillMedia.Add(i);
                    Tubes[i].Phase = "M";
                    Tubes[i].Last10Events.Add("Media Added");
                }
                else
                {
                    Tubes[i].Phase = "I";
                    Tubes[i].Last10Events.Add("Nothing Added");
                }
                if (Tubes[i].Last10Events.Count > 10)
                    Tubes[i].Last10Events.RemoveAt(0);
            }
            core.FillMedia(WillMedia, MediaAdditionTime);
            core.AllSuction(MediaAdditionTime * 1.2 / 1.5, MixingTime);
            core.Log("Turbidostat cycle is done.");
            Document(LastCycleReadings, Phases);
            UpdateTubeStatus();
        }
        public void ChemostatCycle(object state, System.Timers.ElapsedEventArgs e)
        {
            core.Log("Chemostat cycle is active.");
            List<int> WillMedia = new List<int>();
            List<string> Phases = new List<string>();
            List<List<double>> LastCycleReadings = core.LastCycleReadings();
            for (int i = 0; i < Tubes.Count; ++i)
            {
                Phases.Add(Tubes[i].Phase);
                double ODFinal = 0;
                for (int j = Math.Max(0, -1 - 5); j < LastCycleReadings[i].Count; ++j)
                {
                    ODFinal += LastCycleReadings[i][j];
                }
                // Possible Bug: CycleReadings[0] can have count of 0.
                ODFinal /= Math.Min(LastCycleReadings[0].Count, 5);
                if (ODFinal > GlobalThreshold[i])
                {
                    Tubes[i].Last10Events.Add("Nothing Added; Optical Error.");
                    Tubes[i].Phase = "I";
                }
                else if (ODFinal > LowerThreshold[i] && ODFinal < UpperThreshold[i])
                {
                    WillMedia.Add(i);
                    Tubes[i].Phase = "M";
                    Tubes[i].Last10Events.Add("Media Added");
                }
                else
                {
                    Tubes[i].Phase = "I";
                    Tubes[i].Last10Events.Add("Nothing Added");
                }
                if (Tubes[i].Last10Events.Count > 10)
                    Tubes[i].Last10Events.RemoveAt(0);
            }
            core.FillMedia(WillMedia, MediaAdditionTime);
            core.AllSuction(MediaAdditionTime * 1.2 / 1.5, MixingTime);
            core.Log("Chemostat cycle is done.");
            Document(LastCycleReadings, Phases);
            UpdateTubeStatus();
        }
        public void StopCycle(String Type)
        {
            // Protects against stopping cycles that aren't running.
            if (!IsRunning)
                return;
            IsRunning = false;
            core.Log(Type + " has finished.");
            Cycler.Stop();
        }

        public void Document(List<List<double>> LastCycleReadings, List<string> Phases)
        {
            using (StreamWriter sw = File.AppendText(FileName))
            {
                string Add = "";
                for (int i = 0; i < LastCycleReadings[0].Count; ++i)
                {
                    sw.WriteLine();
                    for (int j = 0; j < LastCycleReadings.Count; ++j)
                    {
                        Add += LastCycleReadings[j][i] + ",";
                    }
                    for (int j = 0; j < LastCycleReadings.Count; ++j)
                    {
                        Add += Phases[j];
                    }
                }
                sw.WriteLine(Add);
            }
        }
        public void UpdateTubeStatus()
        {
            TubeLogger.Clear();
            for(int i = 0; i < TubeCount; ++i)
            {
                if (TubeStatus[i])
                    TubeLogger.Add(new TubeEntry { TubeNumber = i + 1, Message = String.Join(" -> ", Tubes[i].Last10Events)}); 

            }
        }
    }
    public class TubeEntry
    {
        public int TubeNumber;
        public String Message;
    }

}