using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace TSP
{
    public class TestSuiteSolver
    {
        public int ReasonableTime { get; set; }
        public string FileName { get; set; }
        private List<MethodInfo> _implementedMethods;
        private TestSuiteProgress _testSuiteProgress;

        const string RANDOM_NAME = "pickRandomSolution";

        private BackgroundWorker work, timerWorker;
        public TestSuiteSolver()
        {
            getMethodImplementations();
            Log("Discovered {0} algorithm implementations", _implementedMethods.Count);
            foreach (MethodInfo m in _implementedMethods)
            {
                Log(m.Name);
            }
        }

        /// <summary>
        /// Test suite syntax is:
        /// [
        /// reasonableTime
        /// numCities numIterations algorithmType(ALL to run all types) difficulty(Easy, Normal, Hard) storeHighscores(True, False)
        /// ]
        /// </summary>
        public void RunTestSuite()
        {
            FileName = launchOpenDialog();

            if (FileName != null)
            {
                work = new BackgroundWorker();
                work.DoWork += work_DoWork;
                work.ProgressChanged += work_ProgressChanged;
                work.WorkerReportsProgress = true;

                _testSuiteProgress = new TestSuiteProgress();
                _testSuiteProgress.Show();

                timerWorker = new BackgroundWorker();
                timerWorker.DoWork += timerWorker_DoWork;
                timerWorker.ProgressChanged += timerWorker_ProgressChanged;
                timerWorker.WorkerReportsProgress = true;
                work.RunWorkerAsync();
                timerWorker.RunWorkerAsync();
            }
        }

        void timerWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            _testSuiteProgress.UpdateTime(e.ProgressPercentage);
        }

        void timerWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            int currentTime = 0;
            BackgroundWorker worker = sender as BackgroundWorker;
            while(true)
            {
                Thread.Sleep(1000);
                currentTime++;
                worker.ReportProgress(currentTime);
            }
            
        }

        void work_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker worker = sender as BackgroundWorker;
            doSolveWithFilename(FileName, worker, e);
        }

        private void doSolveWithFilename(string fileName, BackgroundWorker worker, DoWorkEventArgs e)
        {
            List<TestSuiteEntry> testSuites = processTestSuite(fileName, worker);
            TotalTestResults results = runAllTestSuites(testSuites, worker);
            writeCSV(fileName, results);
        }

        private void writeCSV(string testFileName, TotalTestResults results)
        {
            List<string> methodOrder = new List<string>();
            methodOrder.Add(RANDOM_NAME);
            foreach (MethodInfo inf in _implementedMethods)
            {
                string name = inf.Name;
                if (!methodOrder.Contains(name))
                {
                    methodOrder.Add(name);
                }
            }

            string header = String.Format(",{0}", RANDOM_NAME);
            foreach (String s in methodOrder)
            {
                if (!s.Equals(RANDOM_NAME))
                {
                    header += ("," + s + ",,");
                }
            }

            StringBuilder secondHeader = new StringBuilder();
            secondHeader.Append("# Cities,Path Length");
            for (int i = 1; i < methodOrder.Count; i++)
            {
                string curMethod = methodOrder[i];
                secondHeader.Append(",Time (sec),Path Length,% Improvement");
            }

            StringBuilder bodyFill = new StringBuilder();
            
            List<int> sortedCities = results.SortedNumberCities();
            foreach (int numCities in sortedCities)
            {
                double randomDistance = results.GetValues(numCities, RANDOM_NAME)[0].Distance;
                bodyFill.Append(numCities + "," + randomDistance);
                foreach (string curMethod in methodOrder)
                {
                    SingleTestResult result = results.GetValues(numCities, curMethod)[0];
                    double increase = result.Distance - randomDistance;
                    double percentChange = (increase % randomDistance) * 100;
                    bodyFill.Append("," + result.TimeInSeconds + "," + result.Distance + "," + percentChange);
                }
                bodyFill.AppendLine();
            }


            DateTime now = DateTime.Now;
            string format = "_MM_dd_yy__HH_mm_ss";
            string dateTimeAppend = now.ToString(format);
            string newFileName = Path.GetFileNameWithoutExtension(testFileName) + dateTimeAppend + ".csv";
            using (StreamWriter f = new StreamWriter(newFileName))
            {
                f.WriteLine(header);
                f.WriteLine(secondHeader);
                f.WriteLine(bodyFill.ToString());
            }
        }

        void work_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            TestSuiteProgressData prog = e.UserState as TestSuiteProgressData;
            _testSuiteProgress.UpdateVisuals(prog);
        }

        private string launchOpenDialog()
        {
            Log("Choosing test suite");
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = Directory.GetCurrentDirectory();
            openFileDialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                return openFileDialog.FileName;
            }
            return null;
        }

        private List<TestSuiteEntry> processTestSuite(string fileName, BackgroundWorker worker)
        {
            List<TestSuiteEntry> retVal = new List<TestSuiteEntry>();
            string testSuiteName = fileName;
            Log("Test suite is {0}", testSuiteName);

            using (FileStream f = new FileStream(testSuiteName, FileMode.Open))
            {
                using (StreamReader sr = new StreamReader(f))
                {
                    ReasonableTime = int.Parse(sr.ReadLine());
                    Log("Reasonable time for this test suite is {0}", ReasonableTime);
                    string line = null;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string[] split = line.Split(' ');
                        if (split.Length < 5)
                        {
                            Log("ERROR: Malformed test suite entry{0}", line);
                            continue;
                        }

                        int numCities = int.Parse(split[0]);
                        int numIterations = int.Parse(split[1]);
                        string algorithmType = split[2];
                        string difficulty = split[3];
                        bool storeHighScores = bool.Parse(split[4]);
                        retVal.Add(new TestSuiteEntry(numCities, numIterations, algorithmType, difficulty, storeHighScores));
                    }
                }
            }
            return retVal;
        }

        private TotalTestResults runAllTestSuites(List<TestSuiteEntry> tests, BackgroundWorker worker)
        {
            int maxProgress = 0;
            foreach (TestSuiteEntry t in tests)
            {
                if (t.AlgorithmType.Equals("ALL"))
                {
                    maxProgress += (t.NumberOfIterations * _implementedMethods.Count);
                }
                else
                {
                    maxProgress += t.NumberOfIterations;
                }
            }
            TotalTestResults totalResults = new TotalTestResults();
            TestSuiteProgressData progressData = new TestSuiteProgressData(ReasonableTime, 1, 1, 1, maxProgress);
            progressData.MaxTestProgress = tests.Count;
            progressData.CurrentTestProgress = 0;
            worker.ReportProgress(0, progressData);
            for (int testIndex = 0; testIndex < tests.Count; testIndex++ )
            {
                TestSuiteEntry curTest = tests[testIndex];
                Log("Running test suite: {0}: {1}/{2}", curTest.ToString(), (testIndex+1), tests.Count);
                progressData.CurrentTest = curTest.ToString();
                progressData.MaxIterationProgress = curTest.NumberOfIterations;
                progressData.CurrentIterationProgress = 0;
                progressData.MaxMethodProgress = _implementedMethods.Count;
                progressData.CurrentMethodProgress = 0;
                progressData.CurrentTestProgress++;
                worker.ReportProgress(progressData.CompletionPercentage, progressData);

                if (curTest.AlgorithmType.Equals("ALL"))
                {
                    foreach (MethodInfo method in _implementedMethods)
                    {
                        progressData.CurrentMethodProgress++;
                        progressData.CurrentMethod = method.Name;
                        worker.ReportProgress(progressData.CompletionPercentage, progressData);
                        SingleTestResult average = runAllTestIterations(curTest, method, worker, progressData);
                        totalResults.StoreResult(curTest.NumberCities, method.Name, average);
                    }
                }
                else
                {
                    MethodInfo chosenMethod = null;
                    foreach (MethodInfo method in _implementedMethods)
                    {
                        if (method.Name.Equals(curTest.AlgorithmType))
                        {
                            chosenMethod = method;
                        }
                    }
                    if (chosenMethod != null)
                    {
                        progressData.MaxMethodProgress = 1;
                        progressData.CurrentMethodProgress++;
                        progressData.CurrentMethod = chosenMethod.Name;
                        worker.ReportProgress(progressData.CompletionPercentage, progressData);
                        SingleTestResult value = runAllTestIterations(curTest, chosenMethod, worker, progressData);
                        totalResults.StoreResult(curTest.NumberCities, chosenMethod.Name, value);
                    }
                    else
                    {
                        Log("Chosen Algorithm: {0} does not exist!", curTest.AlgorithmType);
                    }
                }
                
            }
            return totalResults;
        }

        private SingleTestResult runAllTestIterations(TestSuiteEntry curTest, MethodInfo implementation,
            BackgroundWorker worker, TestSuiteProgressData progressData)
        {
            List<SingleTestResult> _allTestResults = new List<SingleTestResult>();

            for (int i = 0; i < curTest.NumberOfIterations; i++)
            {
                Log("Running {0} iteration: {1}/{2}", implementation.Name, (i + 1), curTest.NumberOfIterations);
                progressData.CurrentIterationProgress++;
                progressData.CurrentMaxProgress++;
                worker.ReportProgress(progressData.CompletionPercentage, progressData);
                Random random = new Random();
                int seed = random.Next(1000); // 3-digit random number
                SingleTestResult thisRun = runTestIteration(seed, curTest.NumberCities, implementation,
                    worker, progressData);
                _allTestResults.Add(thisRun);
                Log("Result was {0}", thisRun);
            }
            double averageTime = 0;
            double averageDistance = 0;
            foreach (SingleTestResult s in _allTestResults)
            {
                averageTime += s.TimeInSeconds;
                averageDistance += s.Distance;
            }
            averageTime /= curTest.NumberOfIterations;
            averageDistance /= curTest.NumberOfIterations;

            SingleTestResult averageTestResult = new SingleTestResult(averageDistance, averageTime);
            Log("Average test result was {0}", averageTestResult.ToString());
            progressData.MostRecentSolution = String.Format("{0} Number of cities with function {1} and {2} iterations" +
                " took {3} seconds with cost of {4}", curTest.NumberCities, implementation.Name, curTest.NumberOfIterations,
                averageTestResult.TimeInSeconds, averageTestResult.Distance);
            worker.ReportProgress(progressData.CompletionPercentage, progressData);
            return averageTestResult;
        }

        private SingleTestResult runTestIteration(int seed, int numCities, MethodInfo implementation,
            BackgroundWorker worker, TestSuiteProgressData progressData)
        {
            ProblemAndSolver ps = new ProblemAndSolver(seed, numCities);
            ps.UpdateForm = false;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            implementation.Invoke(ps, new object[]{});
            sw.Start();
            return new SingleTestResult(ps.costOfBssf(), sw.ElapsedMilliseconds / 1000d);
        }

        
        private void getMethodImplementations()
        {
            _implementedMethods = new List<MethodInfo>();
            MethodInfo[] allMethods = typeof(ProblemAndSolver).GetMethods();
            foreach (MethodInfo method in allMethods)
            {
                object[] attributes = method.GetCustomAttributes(false);
                foreach (object att in attributes)
                {
                    if (att.GetType().Equals(typeof(AlgorithmImplementation)))
                    {
                        _implementedMethods.Add(method);
                        break;
                    }
                }
            }
        }

        public void Log(string l)
        {
            Console.WriteLine("Test Suite Solver - " + l);
        }

        public void Log(string l, params object[] args)
        {
            Log(String.Format(l, args));
        }




        public class AlgorithmImplementation : Attribute
        {

        }

        public class SingleTestResult
        {
            public double Distance { get; set; }
            public double TimeInSeconds { get; set; }
            public bool TimeOut { get; set; }

            public SingleTestResult(double Distance, double TimeInSeconds)
            {
                this.Distance = Distance;
                this.TimeInSeconds = TimeInSeconds;
                this.TimeOut = false;
            }

            public SingleTestResult(double Distance, double MaxTime, bool TimeOut)
            {
                this.Distance = Distance;
                this.TimeInSeconds = MaxTime;
                this.TimeOut = TimeOut;
            }

            public override string ToString()
            {
                return Distance + " " + TimeInSeconds;
            }
        }

        public class TestSuiteProgressData
        {
            public int MaxTime { get; set; }
            public string CurrentTest { get; set; }

            public int CurrentCitySize { get; set; }
            public string CurrentMethod { get; set; }


            public int MaxTestProgress { get; set; }
            public int CurrentTestProgress { get; set; }

            public int MaxMethodProgress { get; set; }
            public int CurrentMethodProgress { get; set; }

            public int MaxIterationProgress { get; set; }
            public int CurrentIterationProgress { get; set; }

            public int MaxProgress { get; set; }
            public int CurrentMaxProgress { get; set; }

            public string MostRecentSolution { get; set; }

            public int CompletionPercentage { get { return (int)(CurrentMaxProgress / (float)MaxProgress); } }

            public TestSuiteProgressData(int MaxTime, int MaxTestProgress, int MaxMethodProgress,
                int MaxIterationProgress, int MaxProgress)
            {
                this.MaxTime = MaxTime;
                this.CurrentTest = CurrentTest;
                this.MaxTestProgress = MaxTestProgress;
                this.MaxMethodProgress = MaxMethodProgress;
                this.MaxIterationProgress = MaxIterationProgress;
                this.MaxProgress = MaxProgress;

                CurrentTestProgress = 0;
                CurrentMaxProgress = 0;
                CurrentIterationProgress = 0;
                CurrentMaxProgress = 0;

                MostRecentSolution = "---Starting Test Suite---";
            }
        }

        public class TotalTestResults
        {
            private Dictionary<int, Dictionary<string, List<SingleTestResult>>> _cityToFunctionToResults;

            public TotalTestResults()
            {
                _cityToFunctionToResults = new Dictionary<int, Dictionary<string, List<SingleTestResult>>>();
            }

            public List<int> SortedNumberCities()
            {
                List<int> retVal = new List<int>(_cityToFunctionToResults.Keys);
                retVal.Sort();
                return retVal;
            }

            public void StoreResult(int numCities, string methodName, SingleTestResult result)
            {
                if (!_cityToFunctionToResults.ContainsKey(numCities))
                {
                    _cityToFunctionToResults.Add(numCities, new Dictionary<string, List<SingleTestResult>>());
                }
                Dictionary<string, List<SingleTestResult>> functionToResults = _cityToFunctionToResults[numCities];
                if (!functionToResults.ContainsKey(methodName))
                {
                    functionToResults.Add(methodName, new List<SingleTestResult>());
                }
                functionToResults[methodName].Add(result);
            }

            public bool HasValues(int numCities, string methodName)
            {
                if (_cityToFunctionToResults.ContainsKey(numCities))
                {
                    if (_cityToFunctionToResults[numCities].ContainsKey(methodName))
                    {
                        return true;
                    }
                }
                return false;
            }

            public List<SingleTestResult> GetValues(int numCities, string methodName)
            {
                List<SingleTestResult> retVal = new List<SingleTestResult>();
                if (HasValues(numCities, methodName))
                {
                    retVal.AddRange(_cityToFunctionToResults[numCities][methodName]);
                }
                return retVal;
            }
        }

        public class TestSuiteEntry
        {
            public int NumberCities { get; set; }
            public int NumberOfIterations { get; set; }
            public string AlgorithmType { get; set; }
            HardMode.Modes Difficulty { get; set; }
            public bool StoreHighScores { get; set; }

            public TestSuiteEntry(int numCities, int numIterations, string algorithmType, 
                string difficulty, bool storeHighScores)
            {
                NumberCities = numCities;
                NumberOfIterations = numIterations;
                AlgorithmType = algorithmType;
                Difficulty = HardMode.getMode(difficulty);
                StoreHighScores = storeHighScores;

            }

            public override string ToString()
            {
                return NumberCities + " " + NumberOfIterations + " " + 
                    AlgorithmType + " " + Difficulty + " " + StoreHighScores;
            }
        }
    }
}
