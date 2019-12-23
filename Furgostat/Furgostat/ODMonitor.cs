using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Graph = System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms.DataVisualization.Charting;

namespace Furgostat
{
    public partial class ODMonitor : Form
    {
        List<List<double>> ODplot = new List<List<double>>();
        List<double> Timeplot = new List<double>();
        public List<Graph.Chart> Charts = new List<Graph.Chart>();
        Algorithms algos;
        // Number of Timepoints on the X axis.
        public double RefreshLimit = 1 * 60 * 60;
        public ODMonitor(ref Algorithms _algos)
        {
            algos = _algos;
            this.InitializeComponent1();
            this.ControlBox = false;
            this.ClientSize = new System.Drawing.Size(1200, 900);
            // initalize data arrays
            for (int i = 0; i < Algorithms.TubeCount; ++i)
                ODplot.Add(new List<double>());

            CreateChart(10, 10, 220, 280, "Culture 1");
            CreateChart(250, 10, 220, 280, "Culture 2");
            CreateChart(490, 10, 220, 280, "Culture 3");
            CreateChart(730, 10, 220, 280, "Culture 4");
            CreateChart(970, 10, 220, 280, "Culture 5");

            CreateChart(10, 310, 220, 280, "Culture 6");
            CreateChart(250, 310, 220, 280, "Culture 7");
            CreateChart(490, 310, 220, 280, "Culture 8");
            CreateChart(730, 310, 220, 280, "Culture 9");
            CreateChart(970, 310, 220, 280, "Culture 10");

            CreateChart(10, 610, 220, 280, "Culture 11");
            CreateChart(250, 610, 220, 280, "Culture 12");
            CreateChart(490, 610, 220, 280, "Culture 13");
            CreateChart(730, 610, 220, 280, "Culture 14");
            CreateChart(970, 610, 220, 280, "Culture 15");
        }

        private void ODMonitor_Load(object sender, EventArgs e)
        {

        }
        private void CreateChart(int XPosition, int YPosition, int Width, int Length, string title)
        {
            const int MaxX = 20;
            // Creating new Graph and labelling it.
            Charts.Add(new Graph.Chart());
            int Index = Charts.Count - 1;
            string Name = Index.ToString();
            // Placing it.
            Charts[Index].Location = new System.Drawing.Point(XPosition, YPosition);
            Charts[Index].Size = new System.Drawing.Size(Width, Length);

            Charts[Index].ChartAreas.Add(Name);
            Charts[Index].Titles.Add(new Graph.Title(title));

            Charts[Index].ChartAreas[Name].AxisX.Minimum = 0;
            Charts[Index].ChartAreas[Name].AxisX.Maximum = MaxX;
            Charts[Index].ChartAreas[Name].AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
            Charts[Index].ChartAreas[Name].AxisX.LabelStyle.Format = "N";
            Charts[Index].ChartAreas[Name].AxisX.MajorGrid.Enabled = false;

            Charts[Index].ChartAreas[Name].AxisY.Minimum = 0;
            Charts[Index].ChartAreas[Name].AxisY.Maximum = 0.01;
            Charts[Index].ChartAreas[Name].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
            Charts[Index].ChartAreas[Name].AxisY.LabelStyle.Format = "0.000";
            Charts[Index].ChartAreas[Name].AxisY.MajorGrid.Enabled = false;

            // Auto-zooming
            Charts[Index].ChartAreas[Name].AxisX.ScaleView.Zoomable = true;
            Charts[Index].ChartAreas[Name].AxisY.ScaleView.Zoomable = true;

            // Auto-scrolling 
            Charts[Index].ChartAreas[Name].CursorX.AutoScroll = true;
            Charts[Index].ChartAreas[Name].CursorY.AutoScroll = true;

            // Allow user selection for Zoom
            Charts[Index].ChartAreas[Name].CursorX.IsUserSelectionEnabled = true;
            Charts[Index].ChartAreas[Name].CursorY.IsUserSelectionEnabled = true;
            Charts[Index].ChartAreas[Name].CursorX.Interval = 0.001;
            Charts[Index].ChartAreas[Name].CursorY.Interval = 0.001;
            Charts[Index].ChartAreas[Name].BackColor = Color.White;

            Charts[Index].Series.Add(Name);
            Charts[Index].Series[Name].ChartType = Graph.SeriesChartType.Line;
            Charts[Index].Series[Name].Color = Color.Black;
            Charts[Index].Series[Name].BorderWidth = 2;

            Controls.Add(this.Charts[Index]);
        }
        public void AddData(double Time, Double[] OD)
        {
            // Check if data is okay
            for (int i = 0; i < 15; ++i)
            {
                if (OD[i] == 0)
                    OD[i] = (Single)(0.00001);
                if (OD[i] >= 1)
                    OD[i] = 1;
                if (Double.IsNaN(OD[i]) || Time < 1)
                    return;
            }

            // Move to next plotting frame
            if (Timeplot.Count > RefreshLimit || Timeplot.Count == 0)
            {
                Timeplot.RemoveRange(0, Timeplot.Count);
                for (int i = 0; i < Algorithms.TubeCount; ++i) ODplot[i].RemoveRange(0, ODplot[i].Count);

                // Rescale axis limits
                for (int i = 0; i < Algorithms.TubeCount; ++i) if (Charts[i].IsHandleCreated)
                    {
                        string istr = i.ToString();
                        Charts[i].Invoke((Action)(() =>
                        {
                            // 10% above the maximum and 10% below the minimum y value.
                            Charts[i].ChartAreas[istr].AxisY.Maximum = OD[i] + Math.Abs(0.1 * OD[i]);
                            Charts[i].ChartAreas[istr].AxisY.Minimum = OD[i] - Math.Abs(0.1 * OD[i]);
                            Charts[i].ChartAreas[istr].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
                            Charts[i].ChartAreas[istr].AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
                            // X minimum is *pretty much* the current time being plotted. X maximum is 2 times current time being plotted or a small constant plus the current time.
                            Charts[i].ChartAreas[istr].AxisX.Maximum = Math.Min(Time * 2, Time + (RefreshLimit / 5));
                            Charts[i].ChartAreas[istr].AxisX.Minimum = Time - 0.01;
                            // Plots data.
                            Charts[i].Series[istr].Points.DataBindXY(Timeplot, ODplot[i]);
                        }));
                    }
            }

            Timeplot.Add(Time);
            for (int i = 0; i < Algorithms.TubeCount; ++i)
                ODplot[i].Add((double)OD[i]);

            for (int i = 0; i < Algorithms.TubeCount; ++i)
            {
                string istr = i.ToString();
                if (Charts[i].IsHandleCreated)
                    Charts[i].Invoke((Action)(() =>
                    {
                        if (Time > 0.9 * Charts[i].ChartAreas[istr].AxisX.Maximum)
                        {
                            Charts[i].ChartAreas[istr].AxisX.Maximum = Math.Min(Time * 2, Time + (RefreshLimit / 5)); ;
                            Charts[i].ChartAreas[istr].AxisX.IntervalAutoMode = IntervalAutoMode.VariableCount;
                        }
                        if (OD[i] >= Charts[i].ChartAreas[istr].AxisY.Maximum)
                        {
                            Charts[i].ChartAreas[istr].AxisY.Maximum = OD[i] + 0.1 * Math.Abs(OD[i]);
                            Charts[i].ChartAreas[istr].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
                        }
                        if (OD[i] <= Charts[i].ChartAreas[istr].AxisY.Minimum)
                        {
                            Charts[i].ChartAreas[istr].AxisY.Minimum = OD[i] - 0.1 * Math.Abs(OD[i]);
                            Charts[i].ChartAreas[istr].AxisY.IntervalAutoMode = IntervalAutoMode.VariableCount;
                        }
                        try
                        {
                            // Plots data
                            Charts[i].Series[istr].Points.DataBindXY(Timeplot, ODplot[i]);
                        }
                        catch (Exception err)
                        {
                            Console.WriteLine(err.Message);
                        }
                    }));
            }
        }
        private void InitializeComponent1()
        {
            this.SuspendLayout();
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(804, 782);
            this.Name = "ODMonitor";
            this.Text = "ODMonitor";
            this.Load += new System.EventHandler(this.ODMonitor_Load);
            this.ResumeLayout(false);
        }
    }
}
