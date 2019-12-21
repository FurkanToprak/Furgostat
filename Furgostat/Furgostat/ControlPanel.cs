using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.IO;
namespace Furgostat
{
    public partial class ControlPanel : Form
    {
        Core core;
        Algorithms algos;
        ODMonitor ODDisplay;
        bool ODOn;
        bool LockOn;
        List<LaserCalibrationFile> LaserCalibrationDataSource;
        BindingList<LaserCalibrationFile> LaserBindingList = new BindingList<LaserCalibrationFile>();
        BindingSource LaserBSource = new BindingSource();
        public ControlPanel(ref Core _core)
        {
            InitializeComponent();
            SystemTime.Start();
            this.core = _core;
            core.GUI = this;
            algos = new Algorithms(ref _core);
            core.algos = algos;
            core.MainLog.CollectionChanged += UpdateLog;
            ODDisplay = new ODMonitor(ref algos);
            core.ODDisplay = ODDisplay;
            ODOn = false;
            LockOn = true;
        }
        delegate void UpdateLogCallback(object sender, NotifyCollectionChangedEventArgs e);
        public void UpdateLog(object sender, NotifyCollectionChangedEventArgs e)
        {
            try
            {
                if (textBox16.InvokeRequired)
                {
                    UpdateLogCallback d = new UpdateLogCallback(UpdateLog);
                    this.Invoke(d, new object[] { sender, e });
                }
                else
                {
                    textBox16.AppendText(core.MainLog[core.MainLog.Count - 1].Time.ToString("yyyy-MM-ddTHH:mm:ssZ")
                        + " " + core.MainLog[core.MainLog.Count - 1].Message + "\n");
                }
            }
            catch
            {
                // skip logging if it interfere with other GUI operations
            }
        }
        delegate string SelectedLaserCalibrationPathD();
        public string SelectedLaserCalibrationPath()
        {
            if (comboBox1.InvokeRequired)
            {
                SelectedLaserCalibrationPathD d = new SelectedLaserCalibrationPathD(SelectedLaserCalibrationPath);
                return this.Invoke(d, new object[] { }).ToString();
            }
            else
            {
                return ((LaserCalibrationFile)comboBox1.SelectedItem).Path.ToString();
            }
        }
        private void ControlPanel_Load(object sender, EventArgs e)
        {
            LaserBSource.DataSource = LaserBindingList;
            comboBox1.DataSource = LaserBSource;

            LaserCalibrationDataSource = new List<LaserCalibrationFile>();
            string[] files = Directory.GetFiles(@"C:\Users\s135322\Desktop\Furgostat\data\calibrations\", "LaserCalibration*.csv");
            for (int i = 0; i < files.Length; ++i)
                LaserBindingList.Add(new LaserCalibrationFile() { Name = Path.GetFileName(files[i]), Path = files[i] });

            this.comboBox1.DisplayMember = "Name";
            this.comboBox1.ValueMember = "Path";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                double Duration = Double.Parse(textBox1.Text);
                List<int> Selected = new List<int>();
                if (textBox12.Text.Contains(":"))
                {
                    string[] Parse = textBox11.Text.Split(':');
                    for (int i = Int32.Parse(Parse[0]); i <= (Int32.Parse(Parse[1])); ++i)
                        Selected.Add(i - 1);
                }
                else
                {
                    Selected.Add(Int32.Parse(textBox11.Text) - 1);
                }
                core.FillDrugA(Selected, Duration);
            }
            catch(FormatException err)
            {
                // Empty box, do nothing.
            }
        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void label16_Click(object sender, EventArgs e)
        {

        }

        private void label22_Click(object sender, EventArgs e)
        {

        }

        private void textBoxParameterDilutionCyclePeriod_TextChanged(object sender, EventArgs e)
        {
            try
            {
                algos.CyclePeriod = Double.Parse(textBoxParameterDilutionCyclePeriod.Text);
                algos.Cycler = new System.Timers.Timer((Int32)algos.CyclePeriod * 60 * 1000);
                if (radioButton4.Checked)
                    algos.Cycler.Elapsed += algos.ChemostatCycle;

                else if (radioButton5.Checked)
                    algos.Cycler.Elapsed += algos.TurbidostatCycle;
                else
                    algos.Cycler.Elapsed += algos.MorbidostatCycle;
            }
            catch(FormatException err)
            {
                // Empty Box, do nothing.
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            string Path = ((LaserCalibrationFile)comboBox1.SelectedItem).Path.ToString();
            string[] lascal = File.ReadAllLines(Path);
            double[] P1 = new double[core.Input.HighChan + 1], P0 = new double[core.Input.HighChan + 1];
            int i = 0;
            foreach (string s in lascal[0].Split('\t'))
                if (s != "")
                {
                    try
                    {
                        P0[i++] = Single.Parse(s);
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err.Message);
                    }
                }
            i = 0;
            foreach (string s in lascal[1].Split('\t'))
                if (s != "")
                {
                    try
                    {
                        P1[i++] = Single.Parse(s);
                    }
                    catch (Exception err)
                    {
                        Console.WriteLine(err.Message);
                    }
                }
            core.Input.updateCalibrationVectors(P1, P0, Path);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void groupBox8_Enter(object sender, EventArgs e)
        {

        }

        private void button14_Click(object sender, EventArgs e)
        {
            textBoxParameterDilutionCyclePeriod.Enabled = LockOn;
            textBoxMediaDilutionTime.Enabled = LockOn;
            textBoxDrugAdditionTime.Enabled = LockOn;
            textBox18.Enabled = LockOn;
            textBox17.Enabled = LockOn;
            textBox21.Enabled = LockOn;
            textBox20.Enabled = LockOn;
            textBox19.Enabled = LockOn;
            LockOn = !LockOn;
        }

        private void textBox21_TextChanged(object sender, EventArgs e)
        {
            try
            {
                algos.LowerThreshold = Double.Parse(textBox21.Text);
            }
            catch(FormatException err)
            {
                // Empty Box, do nothing
            }
        }

        private void textBoxMediaDilutionTime_TextChanged(object sender, EventArgs e)
        {
            try
            {
                algos.MediaAdditionTime = Double.Parse(textBoxMediaDilutionTime.Text);
            }
            catch(FormatException err)
            {
                // Empty Box, do nothing
            }
        }

        private void textBoxDrugAdditionTime_TextChanged(object sender, EventArgs e)
        {
            try
            {
                algos.DrugAdditionTime = Double.Parse(textBoxDrugAdditionTime.Text);
            }
            catch (FormatException err)
            {
                // Empty Box, do nothing
            }
        }

        private void textBox18_TextChanged(object sender, EventArgs e)
        {
            try
            {
                algos.MixingTime = Double.Parse(textBox18.Text);
            }
            catch (FormatException err)
            {
                // Empty Box, do nothing
            }
        }

        private void textBox17_TextChanged(object sender, EventArgs e)
        {
            try
            {
                core.DataCollectionFrequency = Double.Parse(textBox17.Text);
                core.DataCollector = new System.Timers.Timer(core.DataCollectionFrequency * 1000);
                core.DataCollector.Elapsed += core.DataCollector_Tick;
                core.DataCollector.AutoReset = false;
            }
            catch(FormatException err)
            {
                // Empty Box, do nothing.
            }
        }

        private void textBox20_TextChanged(object sender, EventArgs e)
        {
            try
            {
                algos.MiddleThreshold = Double.Parse(textBox20.Text);
            }
            catch(FormatException err)
            {
                // Empty Box, do nothing
            }
        }

        private void textBox19_TextChanged(object sender, EventArgs e)
        {
            try
            {
                algos.UpperThreshold = Double.Parse(textBox19.Text);
            }
            catch (FormatException err)
            {
                // Empty Box, do nothing
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked)
                core.TimeFormat = 0;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked)
                core.TimeFormat = 1;
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked)
                core.TimeFormat = 2;
        }

        private void textBox15_TextChanged(object sender, EventArgs e)
        {
            try
            {
                algos.Volume = Double.Parse(textBox15.Text);
            }
            catch(FormatException)
            {
                // Empty Box, do nothing.
            }
        }

        private void textBox14_TextChanged(object sender, EventArgs e)
        {
            try
            {
                algos.DrugAStockConcentration = Double.Parse(textBox14.Text);
            }
            catch(FormatException err)
            {
                // Empty Box, do nothing.
            }
        }

        private void textBox13_TextChanged(object sender, EventArgs e)
        {
            try
            {
                algos.DrugBStockConcentration = Double.Parse(textBox13.Text);
            }
            catch(FormatException err)
            {
                // Empty box, do nothing
            }
        }

        private void textBox7_TextChanged(object sender, EventArgs e)
        {

        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button12_Click(object sender, EventArgs e)
        {
            core.DataCollector.Start();
            core.ReinitializeODReader.Start();
            if (radioButton4.Checked)
                algos.StartCycle("Chemostat");
            else if (radioButton5.Checked)
                algos.StartCycle("Turbidostat");
            else if (radioButton6.Checked)
                algos.StartCycle("Morbidostat");
            else if (radioButton7.Checked)
                algos.StartCycle("Timed Morbidostat");
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button13_Click(object sender, EventArgs e)
        {
            if (radioButton4.Checked)
                algos.StopCycle("Chemostat");
            else if (radioButton5.Checked)
                algos.StopCycle("Turbidostat");
            else if (radioButton6.Checked)
                algos.StopCycle("Morbidostat");
            else if (radioButton7.Checked)
                algos.StopCycle("Timed Morbidostat");
            core.DataCollector.Stop();
            core.ReinitializeODReader.Stop();
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void button8_Click(object sender, EventArgs e)
        {
            int RelayboxNum = Int32.Parse(textBox7.Text);
            int Channel = Int32.Parse(textBox8.Text);
            core.Relays[RelayboxNum - 1].TurnOff(Channel);
            core.Log("Relaybox " + RelayboxNum.ToString() + " relay " + Channel.ToString() + " is off.");
        }

        private void button7_Click(object sender, EventArgs e)
        {
            int RelayboxNum = Int32.Parse(textBox7.Text);
            int Channel = Int32.Parse(textBox8.Text);
            core.Relays[RelayboxNum - 1].TurnOn(Channel);
            core.Log("Relaybox " + RelayboxNum.ToString() + " relay " + Channel.ToString() + " is on.");
        }

        private void textBox8_TextChanged(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void textBox9_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox10_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox11_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox12_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                for (int i = 1; i <= 24; ++i)
                {
                    core.Relaybox1.TurnOff(i);
                    core.Relaybox2.TurnOff(i);
                }
                core.Log("All valves are off.");
            }
            catch
            {
                core.Log("ALERT! Cannot turn off the valves.");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                for (int i = 1; i <= 24; ++i)
                {
                    core.Relaybox1.TurnOn(i);
                    core.Relaybox2.TurnOn(i);
                }
                core.Log("All valves are on.");
            }
            catch
            {
                core.Log("ALERT! Cannot turn on the valves.");
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                core.Relays[core.Suction[0]].TurnOn(core.Suction[1]);
                core.Log("Suction is started.");

                Thread.Sleep((Int32)(Double.Parse(textBox4.Text) * 1000));

                core.Relays[core.Suction[0]].TurnOff(core.Suction[1]);
                core.Log("Suction is stopped.");
            }
            catch(FormatException err)
            {
                // Empty Box, do nothing
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                double Duration = Double.Parse(textBox3.Text);
                List<int> Selected = new List<int>();
                if (textBox12.Text.Contains(":"))
                {
                    string[] Parse = textBox10.Text.Split(':');
                    for (int i = Int32.Parse(Parse[0]); i <= (Int32.Parse(Parse[1])); ++i)
                        Selected.Add(i - 1);
                }
                else
                {
                    Selected.Add(Int32.Parse(textBox10.Text) - 1);
                }
                core.FillDrugB(Selected, Duration);
            }
            catch(FormatException err)
            {
                // Do nothing, Empty box
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                double Duration = Double.Parse(textBox2.Text);
                List<int> Selected = new List<int>();
                if (textBox12.Text.Contains(":"))
                {
                    string[] Parse = textBox12.Text.Split(':');
                    for (int i = Int32.Parse(Parse[0]); i <= (Int32.Parse(Parse[1])); ++i)
                        Selected.Add(i - 1);
                }
                else
                {
                    Selected.Add(Int32.Parse(textBox12.Text) - 1);
                }
                core.FillMedia(Selected, Duration);
            }
            catch(FormatException err)
            {
                // Empty box, do nothing
            }
            
        }

        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void label10_Click(object sender, EventArgs e)
        {

        }

        private void label9_Click(object sender, EventArgs e)
        {

        }

        private void label8_Click(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        private void groupBox5_Enter(object sender, EventArgs e)
        {

        }

        private void radioButton6_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void button9_Click(object sender, EventArgs e)
        {
            if(ODOn)
            {
                ODDisplay.Hide();
            }
            else
            {
                ODDisplay.Show();
            }
            ODOn = !ODOn;
        }

        private void textBox16_TextChanged(object sender, EventArgs e)
        {

        }

        private void groupBox6_Enter(object sender, EventArgs e)
        {

        }

        private void button11_Click(object sender, EventArgs e)
        {
            LaserCalibrator lasercal = new LaserCalibrator(ref core);
            lasercal.Show();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            // Turn on LED/PDs, and measure OD
            core.Relays[core.LEDPDSwitch[0]].TurnOn(core.LEDPDSwitch[1]);
            core.Input.readBlank();
            core.Relays[core.LEDPDSwitch[0]].TurnOff(core.LEDPDSwitch[1]);


            LaserCalibrationFile autocal = new LaserCalibrationFile() { 
                Name = Path.GetFileName(core.Input.currentCalibrationDatafile), 
                Path = core.Input.currentCalibrationDatafile };
            LaserBindingList.Add(autocal);
            comboBox1.SelectedItem = autocal;
        }

        private void groupBox7_Enter(object sender, EventArgs e)
        {

        }

        private void label24_Click(object sender, EventArgs e)
        {

        }

        private void label23_Click(object sender, EventArgs e)
        {

        }

        private void label25_Click(object sender, EventArgs e)
        {

        }

        private void label26_Click(object sender, EventArgs e)
        {

        }

        private void label19_Click(object sender, EventArgs e)
        {

        }

        private void label11_Click(object sender, EventArgs e)
        {

        }

        private void label20_Click(object sender, EventArgs e)
        {

        }

        private void label12_Click(object sender, EventArgs e)
        {

        }

        private void label13_Click(object sender, EventArgs e)
        {

        }

        private void label14_Click(object sender, EventArgs e)
        {

        }

        private void label15_Click(object sender, EventArgs e)
        {

        }

        private void label16_Click_1(object sender, EventArgs e)
        {

        }

        private void SystemTime_Tick(object sender, EventArgs e)
        {
            label17.Text = DateTime.Now.ToLongTimeString();
        }

        private void labelSystemTime_Click(object sender, EventArgs e)
        {
            
        }

        private void label17_Click(object sender, EventArgs e)
        {

        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label18.Text = "Tube Number " + trackBar1.Value;
            try
            {
                textBox5.Text = String.Join("\n", algos.Tubes[trackBar1.Value].Last10Events);
            }
            catch(ArgumentOutOfRangeException err)
            {
                textBox5.Text = "Experiment Not Started. Start an experiment to view the status of a tube.";
            }
        }

        private void label18_Click(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {

        }

        private void radioButton7_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
    public class LaserCalibrationFile
    {
        public string Name { get; set; }
        public string Path { get; set; }
    }
}
