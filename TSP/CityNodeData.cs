using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace TSP
{
    public class CityNodeData
    {
        public int CityId { get; private set; }
        public City BaseCity { get; private set; }
        public double MinWorstDistance { get; private set; }
        public double MinBestDistance { get; private set; }
        public double MaxWorstDistance { get; private set; }
        public double MaxBestDistance { get; private set; }

        public List<CityDistancePair> SortedBestDistance { get; private set; }
        public List<CityDistancePair> SortedWorstDistance { get; private set; }
        public List<CityDistancePair> SortedOutgoingDistance { get; private set; }

        public double MeanWorstDistance { get; private set; }
        public double MeanBestDistance { get; private set; }
        public double SDWorstDistance { get; private set; }
        public double SDBestDistance { get; private set; }
        public double WorstProximityScore { get; private set; }
        public double BestProximityScore { get; private set; }

        public CityNodeData(int CityId, CostMatrix CostMatrix, City[] Cities)
        {
            this.CityId = CityId;
            this.BaseCity = Cities[CityId];

            MinWorstDistance = double.MaxValue;
            MinBestDistance = double.MaxValue;
            MaxWorstDistance = double.MinValue;
            MaxBestDistance = double.MinValue;

            SortedBestDistance = new List<CityDistancePair>();
            SortedWorstDistance = new List<CityDistancePair>();
            SortedOutgoingDistance = new List<CityDistancePair>();

            MeanWorstDistance = 0;
            MeanBestDistance = 0;

            int worstCount = 0;
            int bestCount = 0;

            WorstProximityScore = 0;
            BestProximityScore = 0;

            for (int j = 0; j < CostMatrix.Size; j++)
            {
                if (CityId != j)
                {
                    double WorstDistance = CostMatrix.WorstDistanceBetween(CityId, j);
                    double BestDistance = CostMatrix.BestDistanceBetween(CityId, j);
                    double Distance = CostMatrix.DistanceTo(CityId, j);

                    if (WorstDistance != double.PositiveInfinity)
                    {
                        MeanWorstDistance += WorstDistance;
                        MinWorstDistance = Math.Min(MinWorstDistance, WorstDistance);
                        MaxWorstDistance = Math.Max(MaxWorstDistance, WorstDistance);
                        SortedWorstDistance.Add(new CityDistancePair(j, WorstDistance));
                        WorstProximityScore += (CostMatrix.DistanceRange - WorstDistance);
                        worstCount++;
                    }

                    if (BestDistance != double.PositiveInfinity)
                    {
                        MeanBestDistance += BestDistance;
                        MinBestDistance = Math.Min(MinBestDistance, BestDistance);
                        MaxBestDistance = Math.Max(MaxBestDistance, BestDistance);
                        SortedBestDistance.Add(new CityDistancePair(j, BestDistance));
                        BestProximityScore += (CostMatrix.DistanceRange - BestDistance);
                        bestCount++;
                    }
                    SortedOutgoingDistance.Add(new CityDistancePair(j, Distance));
                }
            }

            MeanWorstDistance /= (worstCount);
            MeanBestDistance /= (bestCount);

            SDWorstDistance = 0;
            SDBestDistance = 0;

            worstCount = 0;
            bestCount = 0;

            for (int j = 0; j < CostMatrix.Size; j++)
            {
                if (CityId != j)
                {
                    double WorstDistance = CostMatrix.WorstDistanceBetween(CityId, j);
                    double BestDistance = CostMatrix.BestDistanceBetween(CityId, j);

                    if (WorstDistance != double.PositiveInfinity)
                    {
                        SDWorstDistance += Math.Pow(WorstDistance - MeanWorstDistance, 2);
                        worstCount++;
                    }
                    if (BestDistance != double.PositiveInfinity)
                    {
                        SDBestDistance += Math.Pow(BestDistance - MeanBestDistance, 2);
                        bestCount++;
                    }
                }
            }

            SDWorstDistance /= (worstCount);
            SDBestDistance /= (bestCount);
            SDWorstDistance = Math.Sqrt(SDWorstDistance);
            SDBestDistance = Math.Sqrt(SDBestDistance);

            SortedBestDistance.Sort();
            SortedWorstDistance.Sort();
            SortedOutgoingDistance.Sort();

        }

        public int NumberCitiesBelowWorstThreshold(double threshold)
        {
            int num = 0;
            foreach (CityDistancePair p in SortedWorstDistance)
            {
                if (p.Distance > threshold)
                {
                    break;
                }
                num++;
            }
            return num;
        }

        public int NumberCitiesBelowBestThreshold(double threshold)
        {
            int num = 0;
            foreach (CityDistancePair p in SortedBestDistance)
            {
                if (p.Distance > threshold)
                {
                    break;
                }
                num++;
            }
            return num;
        }

        public double AverageWorstDistanceOfBelowThreshold(double threshold)
        {
            double running = 0;
            int count = 0;
            foreach (CityDistancePair p in SortedWorstDistance)
            {
                if (p.Distance > threshold)
                {
                    break;
                }
                else
                {
                    count++;
                    running += p.Distance;
                }
            }
            if (count == 0)
            {
                return double.MaxValue;
            }
            return running / count;
        }

        public double AverageBestDistanceOfBelowThreshold(double threshold)
        {
            double running = 0;
            int count = 0;
            foreach (CityDistancePair p in SortedBestDistance)
            {
                if (p.Distance > threshold)
                {
                    break;
                }
                else
                {
                    count++;
                    running += p.Distance;
                }
            }
            if (count == 0)
            {
                return double.MaxValue;
            }
            return running / count;
        }

        public class CityDistancePair : IComparable<CityDistancePair>
        {
            public int CityId { get; private set; }
            public double Distance { get; private set; }

            public CityDistancePair(int CityId, double Distance)
            {
                this.CityId = CityId;
                this.Distance = Distance;
            }

            public int CompareTo(CityDistancePair other)
            {
                return Distance.CompareTo(other.Distance);
            }

            public override string ToString()
            {
                return CityId + " " + Distance + " ";
            }
        }
    }
}
