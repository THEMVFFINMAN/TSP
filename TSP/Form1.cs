using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace TSP
{
    public partial class mainform : Form
    {
        private ProblemAndSolver CityData;

        public mainform()
        {
            InitializeComponent();

            CityData = new ProblemAndSolver();
            this.tbSeed.Text = CityData.Seed.ToString();
        }

        /// <summary>
        /// overloaded to call the redraw method for CityData. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SetClip(new Rectangle(0,0,this.Width, this.Height - this.toolStrip1.Height-35));
            CityData.Draw(e.Graphics);
        }

        private void SetSeed()
        {
            if (Regex.IsMatch(this.tbSeed.Text, "^[0-9]+$"))
            {
                this.toolStrip1.Focus();
                CityData = new ProblemAndSolver(int.Parse(this.tbSeed.Text));
                this.Invalidate();
            }
            else
                MessageBox.Show("Seed must be an integer.");
        }

        private HardMode.Modes getMode()
        {
            return HardMode.getMode(cboMode.Text);
        }

        private int getProblemSize()
        {
            if (Regex.IsMatch(this.tbProblemSize.Text, "^[0-9]+$"))
            {
                return Int32.Parse(this.tbProblemSize.Text);
            }
            else
            {
                MessageBox.Show("Problem size must be an integer.");
                return 20;
            };
        }

        // not necessarily a new problem but resets the state using the specified settings
        private void reset()
        {
            this.SetSeed(); // also resets the CityData variable

            int size = getProblemSize();
            HardMode.Modes mode = getMode();

            CityData.GenerateProblem ( size, mode );
        }


#region GUI Event Handlers

        private void Form1_Resize(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void tbSeed_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                this.reset();
//                this.SetSeed();
        }

#endregion // Event Handlers

        private void Form1_Load(object sender, EventArgs e)
        {
            // use the parameters in the GUI controls
            this.reset();
        }

        private void tbProblemSize_Leave(object sender, EventArgs e)
        {
            this.reset();
        }

        private void tbProblemSize_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                this.reset();
        }

        private void cboMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.reset();
        }

        private void newProblem_Click(object sender, EventArgs e)
        {
            if (Regex.IsMatch(this.tbProblemSize.Text, "^[0-9]+$"))
            {
                Random random = new Random();
                int seed = int.Parse(tbSeed.Text);
                
                this.reset();
                
                this.Invalidate(); 
            }
            else
            {
                MessageBox.Show("Problem size must be an integer.");
            };
        }

        private void randomProblem_Click(object sender, EventArgs e)
        {
            if (Regex.IsMatch(this.tbProblemSize.Text, "^[0-9]+$"))
            {
                Random random = new Random();
                int seed = random.Next(1000); // 3-digit random number
                this.tbSeed.Text = "" + seed;

                this.reset();

                this.Invalidate();
            }
            else
            {
                MessageBox.Show("Problem size must be an integer.");
            };
        }

        // DEFAULT - this algorithm came with the download. it's pretty much garbage.
        private void dToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.reset();
            CityData.solveProblem();
        }

        // GREEDY
        private void greedyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.reset();
            CityData.pickGreedySolution();
        }

        // BRANCH AND BOUND - this is for our individual solutions, we don't need it for our group project unless someone wants to be a completionist.
        private void bBToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.reset();
            CityData.solveProblem();
        }

        // RANDOM
        private void randomToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.reset();
            CityData.pickRandomSolution();
        }

        // GROUP'S CUSTOM ALGORITHM
        private void yourTSPToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.reset();
            //CityData.specialSolution();
            ProblemAndSolver._clusterPercent = (float)numericUpDown1.Value;
            CityData.PointClusterSolution();
        }

        private void AlgorithmMenu2_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            AlgorithmMenu2.Text = e.ClickedItem.Text;
            AlgorithmMenu2.Tag = e.ClickedItem;
        }

        private void AlgorithmMenu2_ButtonClick_1(object sender, EventArgs e)
        {
            if (AlgorithmMenu2.Tag != null)
            {
                (AlgorithmMenu2.Tag as ToolStripMenuItem).PerformClick();
            }
            else
            {
                AlgorithmMenu2.ShowDropDown();
            }
        }

        TestSuiteSolver solver = new TestSuiteSolver();

        private void button1_Click(object sender, EventArgs e)
        {
            solver.RunTestSuite();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            float result;
            float.TryParse(((TextBox)sender).Text, out result);
            ProblemAndSolver._clusterPercent = result;
        }

        private void doCluster_Click(object sender, EventArgs e)
        {
            ProblemAndSolver._clusterPercent = (float)numericUpDown1.Value;
            CostMatrix costMatrix;
            List<CityNodeData> cityNodeData;
            List<CityCluster> cityClusters;
            Dictionary<int, CityCluster> cityToClusterItsIn;
            CityData.DoClusters(out costMatrix, out cityNodeData, out cityClusters, out cityToClusterItsIn);
            Invalidate();
        }

        private void doClusterSolve_Click(object sender, EventArgs e)
        {
            CostMatrix costMatrix;
            List<CityNodeData> cityNodeData;
            List<CityCluster> cityClusters;
            Dictionary<int, CityCluster> cityToClusterItsIn;
            List<int> interNodeEdges;
            CityData.DoClusters(out costMatrix, out cityNodeData, out cityClusters, out cityToClusterItsIn);
            CityData.SolveExternalClusters(costMatrix, cityNodeData, cityClusters, cityToClusterItsIn,
                out interNodeEdges);
            Invalidate();
        }

    }
}