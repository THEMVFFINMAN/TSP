using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace TSP
{

    class ProblemAndSolver
    {
        public bool UpdateForm = true;
        private class TSPSolution
        {
            /// <summary>
            /// we use the representation [cityB,cityA,cityC] 
            /// to mean that cityB is the first city in the solution, cityA is the second, cityC is the third 
            /// and the edge from cityC to cityB is the final edge in the path.  
            /// You are, of course, free to use a different representation if it would be more convenient or efficient 
            /// for your node data structure and search algorithm. 
            /// </summary>
            public ArrayList
                Route;

            public TSPSolution(ArrayList iroute)
            {
                Route = new ArrayList(iroute);
            }


            /// <summary>
            /// Compute the cost of the current route.  
            /// Note: This does not check that the route is complete.
            /// It assumes that the route passes from the last city back to the first city. 
            /// </summary>
            /// <returns></returns>
            public double costOfRoute()
            {
                // go through each edge in the route and add up the cost. 
                int x;
                City here;
                double cost = 0D;

                for (x = 0; x < Route.Count - 1; x++)
                {
                    here = Route[x] as City;
                    cost += here.costToGetTo(Route[x + 1] as City);
                }

                // go from the last city to the first. 
                here = Route[Route.Count - 1] as City;
                cost += here.costToGetTo(Route[0] as City);
                return cost;
            }
        }

        #region Private members 

        /// <summary>
        /// Default number of cities (unused -- to set defaults, change the values in the GUI form)
        /// </summary>
        // (This is no longer used -- to set default values, edit the form directly.  Open Form1.cs,
        // click on the Problem Size text box, go to the Properties window (lower right corner), 
        // and change the "Text" value.)
        private const int DEFAULT_SIZE = 25;

        private const int CITY_ICON_SIZE = 5;

        // For normal and hard modes:
        // hard mode only
        private const double FRACTION_OF_PATHS_TO_REMOVE = 0.20;

        /// <summary>
        /// the cities in the current problem.
        /// </summary>
        private City[] Cities;
        /// <summary>
        /// a route through the current problem, useful as a temporary variable. 
        /// </summary>
        private ArrayList Route;
        /// <summary>
        /// best solution so far. 
        /// </summary>
        private TSPSolution bssf; 

        /// <summary>
        /// how to color various things. 
        /// </summary>
        private Brush cityBrushStartStyle;
        private Brush cityBrushStyle;
        private Pen routePenStyle;


        /// <summary>
        /// keep track of the seed value so that the same sequence of problems can be 
        /// regenerated next time the generator is run. 
        /// </summary>
        private int _seed;
        /// <summary>
        /// number of cities to include in a problem. 
        /// </summary>
        private int _size;

        /// <summary>
        /// Difficulty level
        /// </summary>
        private HardMode.Modes _mode;

        /// <summary>
        /// random number generator. 
        /// </summary>
        private Random rnd;
        #endregion

        #region Public members
        public int Size
        {
            get { return _size; }
        }

        public int Seed
        {
            get { return _seed; }
        }
        #endregion

        #region Constructors
        public ProblemAndSolver()
        {
            this._seed = 1; 
            rnd = new Random(1);
            this._size = DEFAULT_SIZE;

            this.resetData();
        }

        public ProblemAndSolver(int seed)
        {
            this._seed = seed;
            rnd = new Random(seed);
            this._size = DEFAULT_SIZE;

            this.resetData();
        }

        public ProblemAndSolver(int seed, int size)
        {
            this._seed = seed;
            this._size = size;
            rnd = new Random(seed); 
            this.resetData();
        }
        #endregion

        #region Private Methods

        /// <summary>
        /// Reset the problem instance.
        /// </summary>
        private void resetData()
        {

            Cities = new City[_size];
            Route = new ArrayList(_size);
            bssf = null;

            if (_mode == HardMode.Modes.Easy)
            {
                for (int i = 0; i < _size; i++)
                    Cities[i] = new City(rnd.NextDouble(), rnd.NextDouble());
            }
            else // Medium and hard
            {
                for (int i = 0; i < _size; i++)
                    Cities[i] = new City(rnd.NextDouble(), rnd.NextDouble(), rnd.NextDouble() * City.MAX_ELEVATION);
            }

            HardMode mm = new HardMode(this._mode, this.rnd, Cities);
            if (_mode == HardMode.Modes.Hard)
            {
                int edgesToRemove = (int)(_size * FRACTION_OF_PATHS_TO_REMOVE);
                mm.removePaths(edgesToRemove);
            }
            City.setModeManager(mm);

            cityBrushStyle = new SolidBrush(Color.Black);
            cityBrushStartStyle = new SolidBrush(Color.Red);
            routePenStyle = new Pen(Color.Blue,1);
            routePenStyle.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// make a new problem with the given size.
        /// </summary>
        /// <param name="size">number of cities</param>
        //public void GenerateProblem(int size) // unused
        //{
        //   this.GenerateProblem(size, Modes.Normal);
        //}

        /// <summary>
        /// make a new problem with the given size.
        /// </summary>
        /// <param name="size">number of cities</param>
        public void GenerateProblem(int size, HardMode.Modes mode)
        {
            this._size = size;
            this._mode = mode;
            resetData();
        }

        /// <summary>
        /// return a copy of the cities in this problem. 
        /// </summary>
        /// <returns>array of cities</returns>
        public City[] GetCities()
        {
            City[] retCities = new City[Cities.Length];
            Array.Copy(Cities, retCities, Cities.Length);
            return retCities;
        }

        /// <summary>
        /// draw the cities in the problem.  if the bssf member is defined, then
        /// draw that too. 
        /// </summary>
        /// <param name="g">where to draw the stuff</param>
        public void Draw(Graphics g)
        {
            float width  = g.VisibleClipBounds.Width-45F;
            float height = g.VisibleClipBounds.Height-45F;
            Font labelFont = new Font("Arial", 10);

            // Draw lines
            if (bssf != null)
            {
                // make a list of points. 
                Point[] ps = new Point[bssf.Route.Count];
                int index = 0;
                foreach (City c in bssf.Route)
                {
                    if (index < bssf.Route.Count -1)
                        g.DrawString(" " + index +"("+c.costToGetTo(bssf.Route[index+1]as City)+")", labelFont, cityBrushStartStyle, new PointF((float)c.X * width + 3F, (float)c.Y * height));
                    else 
                        g.DrawString(" " + index +"("+c.costToGetTo(bssf.Route[0]as City)+")", labelFont, cityBrushStartStyle, new PointF((float)c.X * width + 3F, (float)c.Y * height));
                    ps[index++] = new Point((int)(c.X * width) + CITY_ICON_SIZE / 2, (int)(c.Y * height) + CITY_ICON_SIZE / 2);
                }

                if (ps.Length > 0)
                {
                    g.DrawLines(routePenStyle, ps);
                    g.FillEllipse(cityBrushStartStyle, (float)Cities[0].X * width - 1, (float)Cities[0].Y * height - 1, CITY_ICON_SIZE + 2, CITY_ICON_SIZE + 2);
                }

                // draw the last line. 
                g.DrawLine(routePenStyle, ps[0], ps[ps.Length - 1]);
            }

            // Draw city dots
            foreach (City c in Cities)
            {
                g.FillEllipse(cityBrushStyle, (float)c.X * width, (float)c.Y * height, CITY_ICON_SIZE, CITY_ICON_SIZE);
            }
            CustomDraw(g);
        }

        /// <summary>
        ///  return the cost of the best solution so far. 
        /// </summary>
        /// <returns></returns>
        public double costOfBssf ()
        {
            if (bssf != null)
                return (bssf.costOfRoute());
            else
                return -1D; 
        }

        public int checkForDuplicates(ref ArrayList Route, City addition)
        {
            if (Route.Count == 0)
            {
                Route.Add(addition);
                return 0;
            }
            for (int i = 0; i < Route.Count; i++)
            {
                City city = (City)Route[i];
                if (city.X == addition.X && city.Y == addition.Y)
                {
                    return 0;
                }
            }
            Route.Add(addition);
            return 0;
        }

        public void initialBSSF(int X)
        {
            Route = new ArrayList();
            Random rnd = new Random();
            for (int i = 0; i < X; i++)
            {
                while (Route.Count < Cities.Length)
                {
                    int x = rnd.Next(0, Cities.Length);
                    checkForDuplicates(ref Route, Cities[x]);
                }
                if (i == 0 || new TSPSolution(Route).costOfRoute() < bssf.costOfRoute())  
                {
                    bssf = new TSPSolution(Route);
                }
                Route.Clear();
            }
        }

        [Serializable]
        public class partialRoute
        {
            public partialRoute(City[] Cities)
            {
                myList = Cities;
                size = 0;
                Entered = new int[myList.Length];
                Exited = new int[myList.Length];
                for (int i = 0; i < Entered.Length; i++)
                {
                    Entered[i] = -1;
                    Exited[i] = -1;
                }
            }

            public City[] myList;
            public int size;
            public int[] Entered;
            public int[] Exited;
            public int start;
            public int end;

            public ArrayList toArrayList()
            {
                ArrayList myRoute = new ArrayList();
                int count = 0;
                int i = 0;//this.start;
                while (count < myList.Length)
                {
                    myRoute.Add(this.myList[i]);
                    i = Exited[i];//jeff
                    count++;
                }
                return myRoute;
            }

            public void setSize()
            {
                int temp = 0;
                for (int i = 0; i < this.Entered.Length; i++)
                {
                    if (Entered[i] != -1)
                    {
                        temp++;
                    }
                }
                this.size = temp;
            }
        }

        public void reduceCostMatrix(ref double[][] costMatrix, ref double lowerBound)
        {
            for (int row = 0; row < Cities.Length; row++)
            {
                double min = costMatrix[row][0];
                for (int i = 0; i < Cities.Length; i++)
                {
                    if (costMatrix[row][i] == 0)
                    {
                        min = 0;
                        break;
                    }
                    if (costMatrix[row][i] < min)
                    {
                        min = costMatrix[row][i];
                    }
                }
                if (min != 0 && min != Double.PositiveInfinity)
                {
                    for (int j = 0; j < Cities.Length; j++)
                    {
                        costMatrix[row][j] -= min;
                    }
                    lowerBound += min;
                }
            }
            for (int column = 0; column < costMatrix.Length; column++)
            {
                double min = Double.PositiveInfinity;
                for (int i = 0; i < Cities.Length; i++)
                {
                    if (costMatrix[i][column] == 0)
                    {
                        min = 0;
                        break;
                    }
                    if (costMatrix[i][column] < min)
                    {
                        min = costMatrix[i][column];
                    }
                }
                if (min != 0 && min != Double.PositiveInfinity)
                {
                    for (int j = 0; j < Cities.Length; j++)
                    {
                        costMatrix[j][column] -= min;
                    }
                    lowerBound += min;
                }
            }
        }

        public void excludeReduction(ref double[][] costMatrix, ref double lowerBound, int row, int column)
        {
            double min = Double.PositiveInfinity;//costMatrix[0][column];
            for (int i = 0; i < Cities.Length; i++)
            {
                if (costMatrix[i][column] != 0)
                {
                    if (costMatrix[i][column] < min)
                    {
                        min = costMatrix[i][column];
                    }
                }
            }
            lowerBound += min;
            min = Double.PositiveInfinity;//costMatrix[row][0];
            for (int i = 0; i < Cities.Length; i++)
            {
                if (costMatrix[row][i] != 0)
                {
                    if (costMatrix[row][i] < min)
                    {
                        min = costMatrix[row][i];
                    }
                }
            }
            lowerBound += min;
        }

        public void include(ref double[][] costMatrix, ref double lowerBound, ref partialRoute route, int row, int column)
        {
            route.Entered[column] = row;
            route.Exited[row] = column;
            route.setSize();
            route.start = row;
            route.end = column;

            while (route.Exited[route.end] != -1)
            {
                route.end = route.Exited[route.end];
            }
            while (route.Entered[route.start] != -1)
            {
                route.start = route.Entered[route.start];
            }

            if (route.size < Cities.Length - 1)
            {
                while (route.start != column)
                {
                    costMatrix[route.end][route.start] = Double.PositiveInfinity;
                    costMatrix[column][route.start] = Double.PositiveInfinity;
                    route.start = route.Exited[route.start];
                }
            }
            else
            {
                int final = 0;
                for (int i = 0; i < route.Exited.Length; i++)
                {
                    if (route.Exited[i] == -1)
                    {
                        final = i;
                        break;
                    }
                }
                for (int i = 0; i < route.Entered.Length; i++)
                {
                    if (route.Entered[i] == -1)
                    {
                        route.Entered[i] = final;
                        route.Exited[final] = i;
                        break;
                    }
                }
                route.setSize();
                for (int m = 0; m < Cities.Length; m++)
                {
                    for (int n = 0; n < Cities.Length; n++)
                    {
                        costMatrix[m][n] = Double.PositiveInfinity;
                    }
                }
            }
            for (int r = 0; r < Cities.Length; r++)
            {
                costMatrix[r][column] = Double.PositiveInfinity;
            }
            for (int c = 0; c < Cities.Length; c++)
            {
                costMatrix[row][c] = Double.PositiveInfinity;
            }
            costMatrix[column][row] = Double.PositiveInfinity;
            reduceCostMatrix(ref costMatrix, ref lowerBound);
        }

        public void exclude(ref double[][] costMatrix, ref double lowerBound, int row, int column)
        {
            costMatrix[row][column] = Double.PositiveInfinity;
            excludeReduction(ref costMatrix, ref lowerBound, row, column);
        }

        public int addPriority(ref List<Tuple<double[][], double, partialRoute>> priorityQueue, Tuple<double[][], double, partialRoute> addition)
        {
            if (priorityQueue.Count == 0)
            {
                priorityQueue.Add(addition);
                return 0;
            }
            else
            {
                for (int i = 0; i < priorityQueue.Count; i++)
                {
                    if (addition.Item2 < priorityQueue[i].Item2)
                    {
                        priorityQueue.Insert(i, addition);
                        return 0;
                    }
                }
                priorityQueue.Add(addition);
                return 0;
            }
        }

        public int addTemp(ref List<Tuple<double[][], double, partialRoute, double[][], double, partialRoute, double>> tempQueue, Tuple<double[][], double, partialRoute, double[][], double, partialRoute, double> addition)
        {
            if (tempQueue.Count == 0)
            {
                tempQueue.Add(addition);
                return 0;
            }
            else
            {
                for (int i = 0; i < tempQueue.Count; i++)
                {
                    if (addition.Item7 > tempQueue[i].Item7)
                    {
                        tempQueue.Insert(i, addition);
                        return 0;
                    }
                }
                tempQueue.Add(addition);
                return 0;
            }
        }

        public int addCity(ref ArrayList Route, City addition)
        {
            if (Route.Count == 0)
            {
                Route.Add(addition);
                return 0;
            }
            for (int i = 0; i < Route.Count; i++)
            {
                City city = (City)Route[i];
                if (city.X == addition.X && city.Y == addition.Y)
                {
                    return 0;
                }
            }
            Route.Add(addition);
            return 0;
        }

        public static double[][] copyArray(double[][] obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (double[][])formatter.Deserialize(ms);
            }
        }

        public static partialRoute copyArrayList(partialRoute obj)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, obj);
                ms.Position = 0;

                return (partialRoute)formatter.Deserialize(ms);
            }
        }
        
        [TestSuiteSolver.AlgorithmImplementation]
        public void PointClusterSolution()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            CostMatrix costMatrix = new CostMatrix(Cities);
            _costMatrix = costMatrix;
            Stopwatch clusterWatch = new Stopwatch();
            clusterWatch.Start();
            List<CityNodeData> CityData = new List<CityNodeData>();
            _cityNodes = CityData;
            for (int i = 0; i < costMatrix.Size; i++)
            {
                CityData.Add(new CityNodeData(i, costMatrix, Cities));
            }

            //Clustering
            double threshold = costMatrix.DistanceRange * _clusterPercent;
            _threshold = (float)threshold;
            List<CityNodeData> citiesSortedByNumberUnderThreshold = new List<CityNodeData>(CityData);
            citiesSortedByNumberUnderThreshold.Sort((a, b) =>
                (a.AverageWorstDistanceOfBelowThreshold(threshold).CompareTo(
                b.AverageWorstDistanceOfBelowThreshold(threshold))));

            List<List<CityNodeData>> finalClusters = new List<List<CityNodeData>>();
            List<CityNodeData> visited = new List<CityNodeData>(CityData);
            Dictionary<CityNodeData, int> clusterAssignment = new Dictionary<CityNodeData, int>();
            foreach (CityNodeData c in visited)
            {
                clusterAssignment.Add(c, -1);
            }

            while (citiesSortedByNumberUnderThreshold.Count > 0)
            {
                List<CityNodeData> curCluster = new List<CityNodeData>();
                CityNodeData cur = citiesSortedByNumberUnderThreshold[0];
                clusterAssignment[cur] = finalClusters.Count;

                citiesSortedByNumberUnderThreshold.RemoveAt(0);
                visited.Remove(cur);
                curCluster.Add(cur);

                for (int i = 0; i < cur.SortedWorstDistance.Count; i++)
                {
                    bool canAdd = true;
                    CityNodeData curCloseCheck = CityData[cur.SortedWorstDistance[i].CityId];
                    if (!visited.Contains(curCloseCheck))
                    {
                        continue;
                    }
                    foreach (CityNodeData inCluster in curCluster)
                    {
                        double worstDistance = costMatrix.WorstDistanceBetween(inCluster.CityId, curCloseCheck.CityId);
                        if (worstDistance > threshold)
                        {
                            canAdd = false;
                            break;
                        }
                    }

                    if (canAdd)
                    {
                        curCluster.Add(curCloseCheck);
                        visited.Remove(curCloseCheck);
                        citiesSortedByNumberUnderThreshold.Remove(curCloseCheck);
                    }
                }

                finalClusters.Add(curCluster);
            }
            clusterWatch.Stop();
            Console.WriteLine("SAVED: " + (costMatrix.Size - finalClusters.Count));
            _storedClusters = finalClusters;

            List<CityCluster> actualClusters = new List<CityCluster>();
            Dictionary<int, CityCluster> cityToClusterItsIn = new Dictionary<int, CityCluster>();
            foreach (List<CityNodeData> clust in finalClusters)
            {
                CityCluster actualCluster = new CityCluster(clust, CityData, costMatrix);
                actualClusters.Add(actualCluster);
                foreach (int city in actualCluster.ContainedCityIds)
                {
                    cityToClusterItsIn.Add(city, actualCluster);
                }
            }

            
            int startingEdge = -1;
            foreach (int i in costMatrix.AllEdgesSortedByDistance)
            {
                KeyValuePair<int, int> coords = costMatrix.EdgeCoords(i);
                if (!cityToClusterItsIn[coords.Key].Equals(cityToClusterItsIn[coords.Value]))
                {
                    startingEdge = i;
                    break;
                }
            }
            KeyValuePair<int, int> curCoords = costMatrix.EdgeCoords(startingEdge);
            CityCluster curFromCluster = cityToClusterItsIn[curCoords.Key];
            List<int> visitedCities = new List<int>();
            List<CityCluster> clustersVisitedInOrder = new List<CityCluster>();
            List<int> interNodeEdges = new List<int>();

            clustersVisitedInOrder.Add(cityToClusterItsIn[curCoords.Key]);
            visitedCities.AddRange(cityToClusterItsIn[curCoords.Key].ContainedCityIds);
            cityToClusterItsIn[curCoords.Value].IncomingFromEdge(startingEdge);
            cityToClusterItsIn[curCoords.Key].OutgoingOnEdge(startingEdge);
            interNodeEdges.Add(startingEdge);

            while (interNodeEdges.Count < actualClusters.Count - 1)
            {
                curFromCluster = cityToClusterItsIn[curCoords.Value];
                int newEdgeId = curFromCluster.GetShortestValidOutgoingEdgeIgnoringCities(visitedCities);
                
                curCoords = costMatrix.EdgeCoords(newEdgeId);
                cityToClusterItsIn[curCoords.Value].IncomingFromEdge(newEdgeId);
                cityToClusterItsIn[curCoords.Value].OutgoingOnEdge(newEdgeId);

                clustersVisitedInOrder.Add(curFromCluster);
                visitedCities.AddRange(curFromCluster.ContainedCityIds);
                interNodeEdges.Add(newEdgeId);
            }

            CityCluster end = cityToClusterItsIn[curCoords.Value];
            CityCluster start = clustersVisitedInOrder[0];
            int lastEdge = CityCluster.ShortedValidEdgeBetweenClusters(end, start);
            interNodeEdges.Add(lastEdge);

            List<int> allEdges = new List<int>();
            ArrayList sol = new ArrayList();

            foreach (int edge in interNodeEdges)
            {
                KeyValuePair<int, int> coords = costMatrix.EdgeCoords(edge);
                CityCluster target = cityToClusterItsIn[coords.Value];
                allEdges.Add(edge);
                List<int> edgesSolved = target.GreedySolveEdges();
                if (edgesSolved.Count == 0)
                {
                    sol.Add(Cities[target.ContainedCityIds[0]]);
                }
                else
                {
                    for (int i = 0; i < edgesSolved.Count; i++)
                    {
                        int curEdge = edgesSolved[i];
                        KeyValuePair<int, int> innerCoords = costMatrix.EdgeCoords(curEdge);
                        if (i == 0)
                        {
                            sol.Add(Cities[innerCoords.Key]);
                        }
                        sol.Add(Cities[innerCoords.Value]);

                        allEdges.Add(curEdge);
                    }
                }

            }

            bssf = new TSPSolution(sol);
            sw.Stop();
            _interNodeEdges = interNodeEdges;
            _interNodeEdges = null;

            if (UpdateForm)
            {
                Program.MainForm.tbCostOfTour.Text = bssf.costOfRoute() + "";
                Program.MainForm.tbElapsedTime.Text = sw.Elapsed.TotalSeconds + "";
                Program.MainForm.Invalidate();
            }
            
        }

        private List<List<CityNodeData>> _storedClusters;
        private List<CityNodeData> _cityNodes;
        public static float _clusterPercent = 0.1f;
        private float _threshold;
        private List<int> _interNodeEdges;
        private CostMatrix _costMatrix;
        public void CustomDraw(Graphics g)
        {
            if (_storedClusters == null)
            {
                return;
            }

            float width = g.VisibleClipBounds.Width - 45F;
                float height = g.VisibleClipBounds.Height - 45F;

            Color[] clusterColors = new Color[]{Color.Red, Color.Blue, Color.Green, Color.Yellow, Color.Teal, Color.HotPink,
                Color.Fuchsia, Color.Salmon, Color.Tomato, Color.Violet, Color.CadetBlue, Color.DarkOliveGreen, Color.Orange, 
                Color.OrangeRed, Color.Black };
            for (int i = 0; i < _storedClusters.Count; i++)
            {
                List<CityNodeData> curCluster = _storedClusters[i];
                Color col = clusterColors[i % clusterColors.Length];

                Pen p = new Pen(col);
                
                for (int j = 0; j < curCluster.Count; j++)
                {
                    CityNodeData c = curCluster[j];
                    if (j == 0)
                    {
                        g.FillEllipse(p.Brush, (float)c.BaseCity.X * width - CITY_ICON_SIZE, (float)c.BaseCity.Y * height - CITY_ICON_SIZE,
                            CITY_ICON_SIZE * 2, CITY_ICON_SIZE * 2);
                        //g.DrawEllipse(p, (float)c.BaseCity.X * width - CITY_ICON_SIZE - _threshold, (float)c.BaseCity.Y * height - CITY_ICON_SIZE - _threshold,
                         //   CITY_ICON_SIZE * 2 + 2 * _threshold, CITY_ICON_SIZE * 2 + 2 * _threshold);
                    }
                    else
                    {
                        g.FillEllipse(p.Brush, (float)c.BaseCity.X * width, (float)c.BaseCity.Y * height, CITY_ICON_SIZE, CITY_ICON_SIZE);
                        /*g.DrawEllipse(p, (float)c.BaseCity.X * width - CITY_ICON_SIZE - _threshold, (float)c.BaseCity.Y * height - CITY_ICON_SIZE - _threshold,
                            CITY_ICON_SIZE * 2 + 2 * _threshold, CITY_ICON_SIZE * 2 + 2 * _threshold);*/
                    }

                }
            }

            if (_interNodeEdges != null)
            {
                List<Point> ps = new List<Point>();
                foreach (int edgeId in _interNodeEdges)
                {
                    KeyValuePair<int, int> coords = _costMatrix.EdgeCoords(edgeId);
                    
                    Point a = new Point((int)(Cities[coords.Key].X * width) + CITY_ICON_SIZE / 2,
                        (int)(Cities[coords.Key].Y * height) + CITY_ICON_SIZE / 2);
                    Point b = new Point((int)(Cities[coords.Value].X * width) + CITY_ICON_SIZE / 2,
                        (int)(Cities[coords.Value].Y * height) + CITY_ICON_SIZE / 2);
                    g.DrawLine(routePenStyle, a, b);
                }
            }
        }

        /// <summary>
        ///  solve the problem.  This is the entry point for the solver when the run button is clicked
        /// right now it just picks a simple solution. 
        /// </summary>
        [TestSuiteSolver.AlgorithmImplementation]
        public void solveProblem()
        {
            /** Initialize */
            Stopwatch timer = new Stopwatch();
            double lowerBound = 0;
            List<Tuple<double[][], double, partialRoute>> priorityQueue = new List<Tuple<double[][], double, partialRoute>>();
            double[][] costMatrix = new double[Cities.Length][];
            for (int i = 0; i < Cities.Length; i++)
            {
                costMatrix[i] = new double[Cities.Length];
                for (int k = 0; k < Cities.Length; k++)
                {
                    if (i == k)
                    {
                        costMatrix[i][k] = Double.PositiveInfinity;
                    }
                    else
                    {
                        double cost = Cities[i].costToGetTo(Cities[k]);
                        costMatrix[i][k] = cost;
                    }
                }
            }

            /** Reduce Cost Matrix */
            timer.Start();
            reduceCostMatrix(ref costMatrix, ref lowerBound);
            addPriority(ref priorityQueue, new Tuple<double[][], double, partialRoute>(costMatrix, lowerBound, new partialRoute(Cities)));

            /** Initial BSSF */
            Route = new ArrayList();
            Random rnd = new Random();
            for (int i = 0; i < 100; i++)
            {
                while (Route.Count < Cities.Length)
                {
                    int x = rnd.Next(0, Cities.Length);
                    addCity(ref Route, Cities[x]);
                }
                //temp = new TSPSolution(Route);
                if (i == 0 || new TSPSolution(Route).costOfRoute() < bssf.costOfRoute())
                {
                    bssf = new TSPSolution(Route);
                }
                Route.Clear();
            }

            /** Solve Problem */

            int updates = 0;
            int pruned = 0;
            int states = 0;
            int stored = 0;
            Tuple<double, ArrayList> answer = new Tuple<double, ArrayList>(bssf.costOfRoute(), Route);
            List<Tuple<double[][], double, partialRoute, double[][], double, partialRoute, double>> tempQueue = new List<Tuple<double[][], double, partialRoute, double[][], double, partialRoute, double>>();
            while (true) // until bool is true or timer is >= 30
            {
                if (priorityQueue.Count == 0)
                {
                    timer.Stop();
                    if (UpdateForm)
                    {
                        Program.MainForm.tbCostOfTour.Text = " " + bssf.costOfRoute() + "*";
                        Program.MainForm.toolStripTextBox1.Text = " " + updates;
                        Program.MainForm.tbElapsedTime.Text = " " + timer.Elapsed.TotalSeconds;
                        Program.MainForm.Invalidate();
                    }
                    return;
                }
                if (priorityQueue.Count > stored)
                {
                    stored = priorityQueue.Count;
                }

                partialRoute relativePath = copyArrayList(priorityQueue[0].Item3);
                double cost = priorityQueue[0].Item2;
                double[][] matrix = copyArray(priorityQueue[0].Item1);
                priorityQueue.RemoveAt(0);
                if (bssf.costOfRoute() < cost)
                {
                    timer.Stop();
                    pruned += priorityQueue.Count + 1;
                    if (UpdateForm)
                    {
                        Program.MainForm.tbCostOfTour.Text = " " + bssf.costOfRoute() + "*";
                        Program.MainForm.Invalidate();
                        Program.MainForm.toolStripTextBox1.Text = " " + updates;
                        Program.MainForm.tbElapsedTime.Text = " " + timer.Elapsed.TotalSeconds;
                    }
                    return;
                }

                if (relativePath.size == Cities.Length)
                {
                    if (answer.Item1 > cost)
                    {
                        updates++;
                        answer = new Tuple<double, ArrayList>(cost, relativePath.toArrayList());
                        bssf = new TSPSolution(answer.Item2);
                        if (answer.Item1 <= priorityQueue[0].Item2)
                        {
                            timer.Stop();
                            pruned += priorityQueue.Count;
                            if (UpdateForm)
                            {
                                Program.MainForm.tbCostOfTour.Text = " " + bssf.costOfRoute() + "*";
                                Program.MainForm.Invalidate();
                                Program.MainForm.toolStripTextBox1.Text = " " + updates;
                                Program.MainForm.tbElapsedTime.Text = " " + timer.Elapsed.TotalSeconds;
                            }
                            return;
                        }
                    }
                }

                if (timer.Elapsed.TotalSeconds >= 30)
                {
                    timer.Stop();
                    if (UpdateForm)
                    {
                        Program.MainForm.tbCostOfTour.Text = " " + bssf.costOfRoute();
                        Program.MainForm.Invalidate();
                        Program.MainForm.toolStripTextBox1.Text = " " + updates;
                        Program.MainForm.tbElapsedTime.Text = " " + 30;
                    }
                    return;
                }

                for (int i = 0; i < Cities.Length; i++)
                {
                    for (int j = 0; j < Cities.Length; j++)
                    {
                        if (matrix[i][j] == 0)
                        {
                            double[][] matrixOne = copyArray(matrix);
                            double costOne = cost;
                            partialRoute routeInc = copyArrayList(relativePath);
                            include(ref matrixOne, ref costOne, ref routeInc, i, j);

                            double[][] matrixTwo = copyArray(matrix);
                            double costTwo = cost;
                            partialRoute routeExc = copyArrayList(relativePath);
                            exclude(ref matrixTwo, ref costTwo, i, j);

                            double diff = costTwo - costOne - cost;

                            addTemp(ref tempQueue, new Tuple<double[][], double, partialRoute, double[][], double, partialRoute, double>(matrixOne, costOne, routeInc, matrixTwo, costTwo, routeExc, diff));
                        }
                    }
                }
                if (tempQueue.Count != 0)
                {
                    if (tempQueue[0].Item2 < bssf.costOfRoute())
                    {
                        states++;
                        Tuple<double[][], double, partialRoute> Include = new Tuple<double[][], double, partialRoute>(tempQueue[0].Item1, tempQueue[0].Item2, tempQueue[0].Item3);
                        addPriority(ref priorityQueue, Include);
                    }
                    else
                    {
                        pruned++;
                    }
                    if (tempQueue[0].Item5 < bssf.costOfRoute())
                    {
                        states++;
                        Tuple<double[][], double, partialRoute> Exculde = new Tuple<double[][], double, partialRoute>(tempQueue[0].Item4, tempQueue[0].Item5, tempQueue[0].Item6);
                        addPriority(ref priorityQueue, Exculde);
                    }
                    else
                    {
                        pruned++;
                    }
                    tempQueue.Clear();
                }
            }
        }

        /// <summary>
        /// solve the problem in the greediest way.
        /// </summary>
        [TestSuiteSolver.AlgorithmImplementation]
        public void pickGreedySolution()
        {
            // first node will be the starting & ending point
            int x = 0;
            Route = new ArrayList();

            Stopwatch timer = new Stopwatch();
            timer.Start();

            Route.Add(Cities[x]);
            // this is the greedy solution. we start at 0 and don't return to 0 until we're all done.
            int[] visitedCities = new int[Cities.Length];
            for (int i = 0; i < visitedCities.Length; i++) visitedCities[i] = 0;
            do
            {
                // go through every city not visited and make note of the shortest one
                Double shortestDistance = Double.PositiveInfinity;
                int closestCity = -1;
                for (int i = 1; i < Cities.Length; i++)
                {
                    // if the city is already visited or isn't connected (distance == Double.PositiveInfinity), skip it
                    if (visitedCities[i] == 1) continue;
                    // if it's smaller than the best distance so far, make note of it.
                    Double distance = Cities[x].costToGetTo(Cities[i]);
                    if (distance < shortestDistance)
                    {
                        shortestDistance = distance;
                        closestCity = i;
                    }
                }
                // if by this point the closest city is still -1, it's visited every city and needs to return to the beginning.
                if (closestCity == -1) x = 0;
                else
                {
                    x = closestCity;
                    visitedCities[x] = 1;
                    Route.Add(Cities[closestCity]);
                }
            } while (x != 0);

            timer.Stop();

            // call this the Best Solution So Far. bssf is the route that will be drawn by the Draw method.
            bssf = new TSPSolution(Route);

            if (UpdateForm)
            {
                // update the cost of the tour.
                Program.MainForm.tbCostOfTour.Text = " " + bssf.costOfRoute();
                // update count of tours found. (Greedy stops when it finds the first one)
                if (bssf.costOfRoute() > 0)
                    Program.MainForm.toolStripTextBox1.Text = "" + 1;
                else
                    Program.MainForm.toolStripTextBox1.Text = "" + 0;
                //update time taken to find the tour.
                Program.MainForm.tbElapsedTime.Text = "" + (timer.Elapsed.TotalMilliseconds / 1000);
                // do a refresh.
                Program.MainForm.Invalidate();
            }
        }

        /// <summary>
        /// solve the problem in the randomiest way.
        /// </summary>
        [TestSuiteSolver.AlgorithmImplementation]
        public void pickRandomSolution()
        {
            // first node will be the starting & ending point
            int x = 0;
            Route = new ArrayList();
            Route.Add(Cities[x]);
            // this is the random solution. we start at 0, pick random cities out of the hat, and don't return to 0 until we're all done.
            List<int> hat = new List<int>();
            for (int i = 1; i < Cities.Length; i++) hat.Add(i);
            x = -1;
            Random r = new Random();
            while (x != 0)
            {
                // pick a random city that hasn't been visited
                if (hat.Count == 0) x = 0;
                else
                {
                    x = hat[r.Next(0, hat.Count)];
                    hat.Remove(x);
                    Route.Add(Cities[x]);
                }
            }
            // call this the Best Solution So Far. bssf is the route that will be drawn by the Draw method.
            bssf = new TSPSolution(Route);

            if (UpdateForm)
            {
                // update the cost of the tour.
                Program.MainForm.tbCostOfTour.Text = " " + bssf.costOfRoute();
                // do a refresh.
                Program.MainForm.Invalidate();
            }
        }

        public void initializeFarthestInsertion(ref List<int> subTour)
        {
            // arbitrarily pick a city
            Random rdm = new Random();
            int i = rdm.Next(0, Cities.Length);

            subTour.Add(i);
            int farthestCity = 0;
            double farthestDist = 0;
            // find farthest city
            for (int x = 0; x < Cities.Length; x++)
            {
                if (Cities[i].costToGetTo(Cities[x]) != Double.PositiveInfinity 
                    && Cities[i].costToGetTo(Cities[x]) > farthestDist)
                {
                    farthestDist = Cities[i].costToGetTo(Cities[x]);
                    farthestCity = x;
                }
            }
            // initialize the subTour
            subTour.Add(farthestCity);
        }

        public void findFarthestCity(ref List<int> subTour, ref int farthestCity)
        {
            double max_dist = Double.NegativeInfinity;
            for (int k = 0; k < Cities.Length; k++)
            {
                // find a cities that isn't part of the subTour
                if (subTour.Contains(k) == false)
                {
                    double min_dist = Double.PositiveInfinity;
                    for (int i = 0; i < subTour.Count; i++)
                    {
                        // findind city k's shortest distance of all the cities in subTour, 
                        // tells us it's true distance from subTour
                        if (Cities[i].costToGetTo(Cities[k]) != Double.PositiveInfinity 
                            && Cities[i].costToGetTo(Cities[k]) < min_dist)
                        {
                            min_dist = Cities[i].costToGetTo(Cities[k]);
                            // if k's min distance is greater than the 'so far' greatest distance,
                            // k becomes the new farthest city
                            if (min_dist > max_dist) 
                            {
                                max_dist = min_dist;
                                farthestCity = k;
                            }
                        }
                    }
                }
            }
        }

        public void findInsertionPoint(ref List<int> subTour, ref int farthestCity)
        {
            int insertionPoint = 0;
            double optimalDelta = Double.PositiveInfinity;
            for (int i = 0; i < subTour.Count; i++) // look for the best place to put city
            {
                if (i == subTour.Count - 1)
                {
                    double minDelta = Double.PositiveInfinity;
                    minDelta = Cities[subTour[i]].costToGetTo(Cities[farthestCity])
                        + Cities[farthestCity].costToGetTo(Cities[subTour[0]])
                        - Cities[subTour[i]].costToGetTo(Cities[subTour[0]]);
                    if (minDelta < optimalDelta) // above finds the insertion point with min cost increase
                    {
                        optimalDelta = minDelta;
                        insertionPoint = 0;
                    }
                }
                else
                {
                    double minDelta = Double.PositiveInfinity;
                    minDelta = Cities[subTour[i]].costToGetTo(Cities[farthestCity])
                        + Cities[farthestCity].costToGetTo(Cities[subTour[i + 1]])
                        - Cities[subTour[i]].costToGetTo(Cities[subTour[i + 1]]);
                    if (minDelta < optimalDelta) // above finds the insertion point with min cost increase
                    {
                        optimalDelta = minDelta;
                        insertionPoint = i + 1;
                    }
                }
            }
            subTour.Insert(insertionPoint, farthestCity);
        }

        public void finalizeRoute(ref List<int> subTour, ref int updates)
        {
            double tourCost = 0;
            ArrayList route = new ArrayList();

            for (int i = 0; i < subTour.Count; i++)
            {
                if (i == subTour.Count -1)
                {
                    tourCost += Cities[subTour[i]].costToGetTo(Cities[subTour[0]]);
                    route.Add(Cities[subTour[i]]);
                }
                else
                {
                    tourCost += Cities[subTour[i]].costToGetTo(Cities[subTour[i + 1]]);
                    route.Add(Cities[subTour[i]]);
                }
            }

            if (subTour.Count == Cities.Length && tourCost < bssf.costOfRoute() )
            {
                bssf = new TSPSolution(route);
                updates++;
            }
            else
                return;
        }

        [TestSuiteSolver.AlgorithmImplementation]
        public void specialSolution()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            initialBSSF(10);

            List<int> subTour = new List<int>();
            int updates = 0;
            int farthestCity = 0;

            //for (int i = 0; i < 10; i++)
            //{
                initializeFarthestInsertion(ref subTour);
                while (timer.Elapsed.TotalSeconds <= 30 && subTour.Count != Cities.Length)
                {
                    //Console.WriteLine(subTour.Count + " " + Cities.Length);
                    findFarthestCity(ref subTour, ref farthestCity);
                    findInsertionPoint(ref subTour, ref farthestCity);
                    if (subTour.Count == Cities.Length)
                    {
                        break;
                    }
                    if (timer.Elapsed.TotalSeconds >= 30)
                        break;
                }
                finalizeRoute(ref subTour, ref updates);
                
            //}

            timer.Stop();
            if (UpdateForm)
            {
                Program.MainForm.tbCostOfTour.Text = " " + bssf.costOfRoute();
                Program.MainForm.tbElapsedTime.Text = " " + timer.Elapsed.TotalSeconds;
                Program.MainForm.toolStripTextBox1.Text = " " + updates;
                Program.MainForm.Invalidate();
            }
        }
        #endregion
    }

}
