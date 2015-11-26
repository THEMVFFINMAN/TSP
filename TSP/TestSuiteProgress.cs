using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace TSP
{
    public partial class TestSuiteProgress : Form
    {
        private string _mostRecentSolution = "";
        public TestSuiteProgress()
        {
            InitializeComponent();
        }

        public void UpdateTime(int time)
        {
            timeLabel.Text = time + "";
        }

        public void UpdateVisuals(TestSuiteSolver.TestSuiteProgressData data)
        {
            maxTimeBox.Text = data.MaxTime + "";
            currentTestBox.Text = data.CurrentTest;
            currentFunctionBox.Text = data.CurrentMethod;

            progressBar1.Maximum = data.MaxProgress;
            progressBar1.Value = data.CurrentMaxProgress;

            testProgressBox.Text = String.Format("{0}/{1}", data.CurrentTestProgress, data.MaxTestProgress);
            functionProgressBox.Text = String.Format("{0}/{1}", data.CurrentMethodProgress, data.MaxMethodProgress);
            iterationProgressBox.Text = String.Format("{0}/{1}", data.CurrentIterationProgress, data.MaxIterationProgress);
            totalProgressBox.Text = String.Format("{0}/{1}", data.CurrentMaxProgress, data.MaxProgress);

            if (_mostRecentSolution == null || !_mostRecentSolution.Equals(data.MostRecentSolution))
            {
                reportBox.AppendText(data.MostRecentSolution + Environment.NewLine);
                _mostRecentSolution = data.MostRecentSolution;
            }
        }
    }
}
