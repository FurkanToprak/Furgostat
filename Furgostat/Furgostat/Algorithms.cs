using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
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
        public int TimedMorbidostatState = 0;
        public double P;
        public double I;
        public double D;
        public double PID;
    }
    public class Algorithms
    {
        public string Path = @"C:\Users\s135322\Desktop\Furgostat\data\";
        public string FileName;
        public static int TubeCount = 15;
        public double CyclePeriod = 10; // Minutes
        public Boolean IsRunning = false;
        public double[] LowerThreshold = { 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001, 0.001 };
        public double[] MiddleThreshold = { 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15, 0.15 };
        public double[] UpperThreshold = { 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3, 0.3 };
        public double[] GlobalThreshold = { 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2 };
        public List<Tube> Tubes;
        public System.Timers.Timer Cycler;
        Core core;
        public double DrugAdditionTime = 60;
        public double MediaAdditionTime = 60;
        public double MixingTime = 30;
        public double SuctionTime = 60;
        public double PCoefficient = 0.01;
        public double ICoefficient = 0.01;
        public double DCoefficient = 1;
        public int TimedMorbidostatCounter = 0;
        public double[] TubeVolumeSettings = { 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15, 15 }; // mL
        public double[] TubeDrugAConcentrationSettings = { 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10, 10 }; // micromolar
        public double[] TubeDrugBConcentrationSettings = { 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50, 50 }; // micromolar
        public bool[] TubeStatus = { false, false, false, false, false, false, false, false, false, false, false, false, false, false, false };
        public double[] TimedMorbidostatHours = { 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8 };
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
            DateTime CurrentTime = DateTime.Now;
            FileName = Type + "-Data-" + CurrentTime.ToString("yyyy-MMM-dd-HH-mm-ss") + ".csv";
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
                for (int j = Math.Max(0, LastCycleReadings[i].Count - 1 - 5); j < LastCycleReadings[i].Count; ++j)
                {
                    ODFinal += LastCycleReadings[i][j];
                }
                // Possible Bug: CycleReadings[0] can have count of 0.
                ODFinal /= Math.Min(LastCycleReadings[0].Count, 5);
                double D = (ODFinal - Tubes[i].ODFinal) / (CyclePeriod / 60);
                Tubes[i].ODFinal = ODFinal;
                if (Tubes[i].TimedMorbidostatState == 0 && ODFinal < LowerThreshold[i])
                {
                    Tubes[i].Last10Events.Add("Nothing Added");
                    Tubes[i].Phase = "I";
                }
                else if (Tubes[i].TimedMorbidostatState == 0 && 
                    (TimedMorbidostatCounter % ((Int32)(TimedMorbidostatHours[i] * 60 / CyclePeriod)) != 0 || 
                    ODFinal >= LowerThreshold[i] && ODFinal < MiddleThreshold[i]))
                {
                    Tubes[i].Last10Events.Add("Media Added");
                    Tubes[i].Phase = "M";
                    WillMedia.Add(i);
                    double NewVolume = MediaAdditionTime * 1.2 / 60;
                    Tubes[i].DrugConcentration = (Tubes[i].DrugConcentration * TubeVolumeSettings[i]) / (TubeVolumeSettings[i] + NewVolume);
                }
                else if (Tubes[i].TimedMorbidostatState > 0 && 
                    TimedMorbidostatCounter % ((Int32)(TimedMorbidostatHours[i] * 60 / CyclePeriod)) == 0 &&
                    D > 0 &&
                    ODFinal > MiddleThreshold[i])
                {
                    if (++Tubes[i].TimedMorbidostatState == 3)
                        Tubes[i].TimedMorbidostatState = 0;
                    Tubes[i].Last10Events.Add("Drug A Added");
                    Tubes[i].Phase = "A";
                    WillDrugA.Add(i);
                    double NewVolume = DrugAdditionTime * 1.2 / 60;
                    Tubes[i].DrugConcentration = (Tubes[i].DrugConcentration * TubeVolumeSettings[i] + TubeDrugAConcentrationSettings[i] * NewVolume) / (TubeVolumeSettings[i] + NewVolume);
                }
                if (Tubes[i].Last10Events.Count > 10)
                    Tubes[i].Last10Events.RemoveAt(0);
            }
            if(WillMedia.Count > 0)
                core.FillMedia(WillMedia, MediaAdditionTime);
            if(WillDrugA.Count > 0)
                core.FillDrugA(WillDrugA, DrugAdditionTime);
            if(WillMedia.Count + WillDrugA.Count > 0)
                core.AllSuction(SuctionTime, MixingTime);
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
            for (int i = 0; i < LastCycleReadings.Count; ++i)
            {
                Phases.Add(Tubes[i].Phase);
                double ODFinal = 0; // Last 5 OD values.
                for (int j = Math.Max(0, LastCycleReadings[i].Count - 1 - 5); j < LastCycleReadings[i].Count; ++j)
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
                double D = (ODFinal - Tubes[i].ODFinal) / (CyclePeriod / 60);
                double PID = P > 0 ? 1E5 + 0.01 * I + D : -1E5 + 0.01 * I + D;
                Tubes[i].ODFinal = ODFinal;
                Tubes[i].P = P;
                Tubes[i].I = I;
                Tubes[i].D = D;
                Tubes[i].PID = PID;
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
                        double NewVolume = DrugAdditionTime * 1.2 / 60;
                        Tubes[i].DrugConcentration = (Tubes[i].DrugConcentration * TubeVolumeSettings[i] + TubeDrugBConcentrationSettings[i] * NewVolume) / (TubeVolumeSettings[i] + NewVolume);
                    }
                    else
                    {
                        // Drug A:
                        WillDrugA.Add(i);
                        Tubes[i].Last10Events.Add("Drug A Added");
                        Tubes[i].Phase = "A";
                        double NewVolume = DrugAdditionTime * 1.2 / 60;
                        Tubes[i].DrugConcentration = (Tubes[i].DrugConcentration * TubeVolumeSettings[i] + TubeDrugAConcentrationSettings[i] * NewVolume) / (TubeVolumeSettings[i] + NewVolume);
                    }
                }
                else
                {
                    Tubes[i].DrugAllowed = true;
                    WillMedia.Add(i);
                    Tubes[i].Last10Events.Add("Media Added");
                    Tubes[i].Phase = "M";
                    double NewVolume = MediaAdditionTime * 1.2 / 60;
                    Tubes[i].DrugConcentration = (Tubes[i].DrugConcentration * TubeVolumeSettings[i]) / (TubeVolumeSettings[i] + NewVolume);
                }
                if (Tubes[i].Last10Events.Count > 10)
                    Tubes[i].Last10Events.RemoveAt(0);
            }
            if(WillMedia.Count > 0)
                core.FillMedia(WillMedia, MediaAdditionTime);
            if(WillDrugA.Count > 0)
                core.FillDrugA(WillDrugA, DrugAdditionTime);
            if(WillDrugB.Count > 0)
                core.FillDrugB(WillDrugB, DrugAdditionTime);
            if (WillDrugA.Count + WillDrugB.Count + WillMedia.Count > 0)
            {
                core.AllSuction(SuctionTime, MixingTime);
            }
            core.Log("Morbidostat cycle is done.");
            Document(LastCycleReadings, Phases, true);
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
                for (int j = Math.Max(0, LastCycleReadings[i].Count - 1 - 5); j < LastCycleReadings[i].Count; ++j)
                {
                    ODFinal += LastCycleReadings[i][j];
                }
                // Possible Bug: CycleReadings[0] can have count of 0.
                ODFinal /= Math.Min(LastCycleReadings[0].Count, 5);
                Tubes[i].ODFinal = ODFinal;
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
            if(WillMedia.Count > 0)
            {
                core.FillMedia(WillMedia, MediaAdditionTime);
                core.AllSuction(SuctionTime, MixingTime);
            }
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
                for (int j = Math.Max(0, LastCycleReadings[i].Count - 1 - 5); j < LastCycleReadings[i].Count; ++j)
                {
                    ODFinal += LastCycleReadings[i][j];
                }
                // Possible Bug: CycleReadings[0] can have count of 0.
                ODFinal /= Math.Min(LastCycleReadings[0].Count, 5);
                Tubes[i].ODFinal = ODFinal;
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
            if (WillMedia.Count > 0)
            {
                core.FillMedia(WillMedia, MediaAdditionTime);
                core.AllSuction(SuctionTime, MixingTime);
            }
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

        public void Document(List<List<double>> LastCycleReadings, List<string> Phases, bool IsMorbidostat=false)
        {
            List<double> LastODTime = new List<double>();
            for (int i = LastCycleReadings[0].Count; i >= 0 ; --i)
                LastODTime.Add(core.ODTime[core.ODTime.Count - 1 - i]);
            using (StreamWriter sw = File.AppendText(Path + FileName))
            {
                string Add = "";
                // OD Values for the last cycle
                for (int i = 0; i < LastCycleReadings[0].Count; ++i)
                {
                    double CurrentTime = LastODTime[i] - core.ODTime[0];
                    Add += CurrentTime + ",";
                    for (int j = 0; j < LastCycleReadings.Count; ++j)
                    {
                        Add += LastCycleReadings[j][i] + ",";
                    }
                    Add += "\n";
                }
                string Prefix = "";
                for (int i = 0; i <= Tubes.Count; ++i)
                    Prefix += ",";
                // Phases
                Add += Prefix;
                for (int i = 0; i < LastCycleReadings.Count; ++i)
                {
                    Add += Phases[i] + ",";
                }
                // ODFinal Values
                Add += "\n" + Prefix;
                for (int i = 0; i < Tubes.Count; ++i)
                {
                    Add += Tubes[i].ODFinal + ",";
                }
                // If Morbidostat Cycle: Drug Concentrations, P, I, D, & PID
                if(IsMorbidostat)
                {
                    // Drug Concentrations
                    Add += "\n" + Prefix;
                    for (int i = 0; i < Tubes.Count; ++i)
                    {
                        Add += Tubes[i].DrugConcentration + ",";
                    }
                    // P Values
                    Add += "\n" + Prefix;
                    for (int i = 0; i < Tubes.Count; ++i)
                    {
                        Add += Tubes[i].P + ",";
                    }
                    // I Values
                    Add += "\n" + Prefix;
                    for (int i = 0; i < Tubes.Count; ++i)
                    {
                        Add += Tubes[i].I + ",";
                    }
                    // D Values
                    Add += "\n" + Prefix;
                    for (int i = 0; i < Tubes.Count; ++i)
                    {
                        Add += Tubes[i].D + ",";
                    }
                    // PID Values
                    Add += "\n" + Prefix;
                    for (int i = 0; i < Tubes.Count; ++i)
                    {
                        Add += Tubes[i].PID + ",";
                    }
                }
                sw.WriteLine(Add);
            }
        }
        public void UpdateTubeStatus()
        {
            core.TubeLogger.Clear();
            for(int i = 0; i < TubeCount; ++i)
            {
                if (TubeStatus[i])
                    core.TubeLogger.Add(new TubeEntry { TubeNumber = i + 1, Message = String.Join(" -> ", Tubes[i].Last10Events)}); 

            }
        }
    }
    public class TubeEntry
    {
        public int TubeNumber;
        public String Message;
    }

}