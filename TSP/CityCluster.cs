using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSP
{
    public class CityCluster
    {
        private List<CityNodeData> _containedCities;
        private List<CityNodeData> _allCities;
        private CostMatrix _costMatrix;

        private Dictionary<int, List<int>> _incomingEdgesByCitySortedByDistance;
        private Dictionary<int, List<int>> _outgoingEdgesByCitySortedByDistance;

        private List<int> _incomingEdgesSortedByDistance;
        private List<int> _outgoingEdgesSortedByDistance;

        private int _cityEntryPoint;
        private int _cityExitPoint;

        public CityCluster(List<CityNodeData> Cities, List<CityNodeData> AllCities, CostMatrix costMatrix)
        {
            _containedCities = new List<CityNodeData>(Cities);
            _allCities = new List<CityNodeData>(AllCities);
            _costMatrix = costMatrix;

            _cityEntryPoint = -1;
            _cityExitPoint = -1;

            _incomingEdgesSortedByDistance = new List<int>();
            _outgoingEdgesSortedByDistance = new List<int>();
            _incomingEdgesByCitySortedByDistance = new Dictionary<int, List<int>>();
            _outgoingEdgesByCitySortedByDistance = new Dictionary<int, List<int>>();

            foreach (CityNodeData innerCity in _containedCities)
            {
                int innerCityId = innerCity.CityId;

                foreach (CityNodeData outerCity in AllCities)
                {
                    if (!_containedCities.Contains(outerCity))
                    {
                        int outerCityId = outerCity.CityId;
                        if (!_incomingEdgesByCitySortedByDistance.ContainsKey(outerCityId))
                        {
                            _incomingEdgesByCitySortedByDistance.Add(outerCityId, new List<int>());
                        }
                        if (!_outgoingEdgesByCitySortedByDistance.ContainsKey(outerCityId))
                        {
                            _outgoingEdgesByCitySortedByDistance.Add(outerCityId, new List<int>());
                        }
                        int incomingId = costMatrix.EdgeId(outerCityId, innerCityId);
                        int outgoingId = costMatrix.EdgeId(innerCityId, outerCityId);
                        _incomingEdgesSortedByDistance.Add(incomingId);
                        _incomingEdgesByCitySortedByDistance[outerCityId].Add(incomingId);
                        _outgoingEdgesSortedByDistance.Add(outgoingId);
                        _outgoingEdgesByCitySortedByDistance[outerCityId].Add(outgoingId);

                    }
                }
            }

            _incomingEdgesSortedByDistance.Sort((a, b) =>
                costMatrix.CostAtEdgeId(a).CompareTo(costMatrix.CostAtEdgeId(b))
                );
            _outgoingEdgesSortedByDistance.Sort((a, b) =>
                costMatrix.CostAtEdgeId(a).CompareTo(costMatrix.CostAtEdgeId(b)));

        }

        public override string ToString()
        {
            StringBuilder retVal = new StringBuilder();
            retVal.AppendLine(String.Format("CLUSTER OF SIZE: {0}", _containedCities.Count));
            retVal.AppendLine("INCOMING");
            foreach (int i in _incomingEdgesSortedByDistance)
            {
                retVal.Append(_costMatrix.CostAtEdgeId(i) + ",");
            }
            retVal.AppendLine();
            retVal.AppendLine("OUTGOING");
            foreach (int i in _outgoingEdgesSortedByDistance)
            {
                retVal.Append(_costMatrix.CostAtEdgeId(i) + ",");
            }


            return retVal.ToString();
        }
    }
}
