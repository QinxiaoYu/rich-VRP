using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace OP.Data
{
    public class Problem
    {   //数据集名称
        public string Abbr { get; set; }
        public double Tmax { get; set; }
        public int VehicleNum { get; set; }
        public Depot StartDepot { get; set; }
        public Depot EndDepot { get; set; }
        public List<Customer> Customers { get; set; }

        public List<Station> Stations { get; set; }
        public List<NodeInfo> AllNodes { get; set; }

        public int Threashold { get; set; }

        public static int[,] DistanceBetween { get; set; }
        public static double[,] AngelBetween { get; set; }




        public void SetNodes(List<NodeInfo> nodes, string abbr, double t_max, int numV, int numD, int numC, int numS)
        {
            Tmax = t_max;
            VehicleNum = numV;
            Abbr = abbr;
            AllNodes = nodes;
            StartDepot = new Depot(nodes[0]);
            EndDepot = new Depot(nodes[1]);
            Customers = new List<Customer>();
            for (var i = numD; i < numC + 1; ++i)
            {
                Customers.Add(new Customer(nodes[i]));
            }
            for (int i = numC + 1; i < AllNodes.Count; i++)
            {
                Stations.Add(new Station(nodes[i]));
            }
            int NodeNumber = AllNodes.Count;
            DistanceBetween = new int[NodeNumber, NodeNumber];
        }

        public void SetDistanceIJ(int i, int j, int dis)
        {
            DistanceBetween[i, j] = dis;
        }



        public static double GetDistanceIJ(int i, int j)
        {
            return DistanceBetween[i, j];
        }
        public static double GetAngelIJ(int i, int j)
        {
            return AngelBetween[i, j];
        }

        public Customer SearchbyId(int id)
        {
            foreach (var customer in Customers)
                if (customer.Info.Id == id)
                    return customer;
            throw new Exception("Customer not found");
        }


        public void SetAllNodes()
        {
            AllNodes = new List<NodeInfo> { StartDepot.Info, EndDepot.Info };
            foreach (var customer in Customers)
                AllNodes.Add(customer.Info);
        }

        public class NodeInfo
        {
            public int Id { get; set; }
            public int Type { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double Weight { get; set; }
            public double Volume { get; set; }
            public double ReadyTime { get; set; }
            public double DueDate { get; set; }
            public double ServiceTime { get; set; }
        }



        public abstract class AbsNode
        {
            public NodeInfo Info;


            public double Distance(AbsNode destination)
            {

                //var xDist = Info.X - destination.Info.X;
                //var yDist = Info.Y - destination.Info.Y;
                //return Math.Sqrt(xDist * xDist + yDist * yDist);
                return Problem.GetDistanceIJ(Info.Id, destination.Info.Id);
            }

            public double TravelTime(AbsNode destination)
            {
                return Distance(destination);
            }


            public virtual AbsNode ShallowCopy()
            {
                throw new Exception("You cannot copy abstract node");
            }


        }



        public class Depot : AbsNode
        {
            public Depot(NodeInfo info)
            {
                Info = info;
            }


            public override AbsNode ShallowCopy()
            {
                return new Depot(Info);
            }
        }

        public class Station : AbsNode
        {
            public Station(NodeInfo info)
            {
                Info = info;
            }

            public override AbsNode ShallowCopy()
            {
                return new Station(Info);
            }
        }



        public class Customer : AbsNode
        {
            public Route Route { get; set; }

            public Customer(NodeInfo info)
            {
                Info = info;
                Route = null;
            }


            public override AbsNode ShallowCopy()
            {
                return DeepCopy();
            }

            public Customer DeepCopy()
            {
                return new Customer(Info)
                {
                    Route = Route
                };
            }


            /// <summary>
            /// Find the position that customer in route.
            /// </summary>
            /// <returns>The position of customer in route.</returns>
            public int Index()
            {
                for (var i = 0; i < Route.RouteList.Count; ++i)
                    if (Route.RouteList[i].Info.Id == Info.Id)
                        return i;
                return -1;
            }
        }
    }
}
