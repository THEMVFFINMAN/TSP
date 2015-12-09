using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace TSP
{
    public class CostMatrix
    {
        public int Size { get; private set; }
        public double MaxNonInfiniteDistance { get; private set; }
        public double MinDistance { get; private set; }
        public double DistanceRange { get; private set; }
        public List<int> AllEdgesSortedByDistance { get; private set; }

        private double[][] _innerMatrix;
        private double[][] _innerWorstDistance;
        private double[][] _innerBestDistance;

        public CostMatrix(City[] Cities)
        {
            Size = Cities.Length;
            _innerMatrix = new double[Size][];
            AllEdgesSortedByDistance = new List<int>();
            MaxNonInfiniteDistance = double.MinValue;
            MinDistance = double.MaxValue;
            for (int i = 0; i < Size; i++)
            {
                _innerMatrix[i] = new double[Size];
                for (int j = 0; j < Size; j++)
                {
                    double val = double.PositiveInfinity;
                    if (i != j)
                    {
                        val = Cities[i].costToGetTo(Cities[j]);
                        MinDistance = Math.Min(MinDistance, val);
                        if (val != double.PositiveInfinity)
                        {
                            MaxNonInfiniteDistance = Math.Max(MaxNonInfiniteDistance, val);
                        }

                    }
                    AllEdgesSortedByDistance.Add(i * Size + j);
                    _innerMatrix[i][j] = val;
                }
            }

            AllEdgesSortedByDistance.Sort((a, b) =>
                CostAtEdgeId(a).CompareTo(CostAtEdgeId(b)));

            DistanceRange = MaxNonInfiniteDistance - MinDistance;

            _innerBestDistance = new double[Size][];
            _innerWorstDistance = new double[Size][];
            for (int i = 0; i < Size; i++)
            {
                _innerWorstDistance[i] = new double[Size];
                _innerBestDistance[i] = new double[Size];
                for (int j = 0; j < Size; j++)
                {
                    double minBest = Math.Min(_innerMatrix[i][j], _innerMatrix[j][i]);
                    _innerBestDistance[i][j] = minBest;


                    double maxWorst = Math.Max(_innerMatrix[i][j], _innerMatrix[j][i]);
                    _innerWorstDistance[i][j] = maxWorst;
                }
            }
        }

        public double DistanceTo(int from, int to)
        {
            return _innerMatrix[from][to];
        }

        public double BestDistanceBetween(int cityA, int cityB)
        {
            return _innerBestDistance[cityA][cityB];
        }

        public double WorstDistanceBetween(int cityA, int cityB)
        {
            return _innerWorstDistance[cityA][cityB];
        }

        public int EdgeId(int from, int to)
        {
            return (from * Size) + to;
        }

        public KeyValuePair<int, int> EdgeCoords(int edgeId)
        {
            return new KeyValuePair<int, int>(edgeId / Size, edgeId % Size);
        }

        public double CostAtEdgeId(int edgeId)
        {
            KeyValuePair<int, int> coords = EdgeCoords(edgeId);
            return Cost(coords.Key, coords.Value);
        }

        public double Cost(int from, int to)
        {
            return _innerMatrix[from][to];
        }

        public override string ToString()
        {
            StringBuilder retVal = new StringBuilder();
            retVal.AppendLine(Size + " " + MinDistance + " " + MaxNonInfiniteDistance);
            for (int i = 0; i < Size; i++)
            {
                for (int j = 0; j < Size; j++)
                {
                    retVal.Append(_innerMatrix[i][j].ToString("0000") + ",");
                }
                retVal.AppendLine();
            }
            return retVal.ToString();
        }
    }
}
