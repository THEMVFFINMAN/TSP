using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSP
{
    public class CityCluster
    {
        public List<int> ContainedCityIds;
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

            ContainedCityIds = new List<int>();
            foreach (CityNodeData city in _containedCities)
            {
                ContainedCityIds.Add(city.CityId);
            }
        }

        public int GetShortestValidIncomingEdgeFromCity(int CityId)
        {
            List<int> incomingFromCity = _incomingEdgesByCitySortedByDistance[CityId];
            int proposedEdge = incomingFromCity[0];
            KeyValuePair<int, int> coords = _costMatrix.EdgeCoords(proposedEdge);
            _cityEntryPoint = coords.Value;
            return proposedEdge;
        }

        public void IncomingFromEdge(int edgeId)
        {
            _cityEntryPoint = _costMatrix.EdgeCoords(edgeId).Value;
        }

        public void OutgoingOnEdge(int edgeId)
        {
            _cityExitPoint = _costMatrix.EdgeCoords(edgeId).Key;
        }

        public int GetShortestValidOutgoingEdgeIgnoringCities(List<int> ToIgnore)
        {
            foreach (int edgeId in _outgoingEdgesSortedByDistance)
            {
                KeyValuePair<int, int> coords = _costMatrix.EdgeCoords(edgeId);
                //Makes sure we haven't already visited the city, 
                //and we don't have an incoming to the same city in here
                if (!ToIgnore.Contains(coords.Value) && 
                    (_cityEntryPoint != coords.Key || _containedCities.Count == 1))
                {
                    _cityExitPoint = coords.Key;
                    return edgeId;
                }
            }
            return -1;
        }

        public static int ShortedValidEdgeBetweenClusters(CityCluster from, CityCluster to)
        {
            foreach (int edgeId in from._outgoingEdgesSortedByDistance)
            {
                KeyValuePair<int, int> coords = from._costMatrix.EdgeCoords(edgeId);
                if (from._cityEntryPoint != coords.Key || from._containedCities.Count == 1)
                {
                    //If valid to leave from here
                    if (to.ContainedCityIds.Contains(coords.Value))
                    {
                        //If target has this city
                        if (to._cityExitPoint != coords.Value || to._containedCities.Count == 1)
                        {
                            //If target city is valid
                            from.OutgoingOnEdge(edgeId);
                            to.IncomingFromEdge(edgeId);
                            return edgeId;
                        }
                    }
                }
                
            }

            return -1;
        }

        public List<int> GreedySolveEdges()
        {
            if (_cityEntryPoint == -1 || _cityExitPoint == -1)
            {
                throw new Exception("OZ NOZ!");
            }
            List<int> retVal = new List<int>();
            List<int> cities = new List<int>();
            cities.Add(_cityEntryPoint);
            CityNodeData cur = _allCities[_cityEntryPoint];
            
            List<CityNodeData> notVisited = new List<CityNodeData>(_containedCities);
            notVisited.Remove(cur);

            while (notVisited.Count > 0)
            {
                CityNodeData target = null;
                double minDistance = double.MaxValue;
                foreach (CityNodeData c in notVisited)
                {
                    double checkDistance = _costMatrix.DistanceTo(cur.CityId, c.CityId);
                    if (checkDistance < minDistance && 
                        (notVisited.Count == 1 || c.CityId != _cityExitPoint))
                    {
                        minDistance = checkDistance;
                        target = c;
                    }
                }
                if (target != null)
                {
                    cities.Add(target.CityId);
                    retVal.Add(_costMatrix.EdgeId(cur.CityId, target.CityId));
                    notVisited.Remove(target);
                }
                cur = target;
            }
            


            return retVal;
        }

        /*public override string ToString()
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
        }*/
    }
}
