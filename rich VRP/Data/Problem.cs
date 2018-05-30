using System;
using System.Collections;
using System.Collections.Generic;

// Analysis disable once CheckNamespace
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
      
        public Fleet fleet { get; set; }

        static int[,] DistanceBetween { get; set; }
        static int[,] TravelTimeBetween { get; set; }
        public static double[,] AngelBetween { get; set; }

        public List<int[]> NearDistanceCus;
        public List<int[]> NearDistanceSta;

        //public List<int> PriorityTimeCus;


        public double MinWaitTimeAtDepot { get; set; }
        public double WaitCostRate { get; set; }

        public void SetNodes(List<NodeInfo> nodes, string abbr, double t_max, int numV, int numD, int numC, int numS)
        {
            Tmax = t_max;
            VehicleNum = numV;
            Abbr = abbr;
            AllNodes = nodes;
            StartDepot = new Depot(nodes[0]);
            EndDepot = new Depot(nodes[0]);
            Customers = new List<Customer>();
            Stations = new List<Station>();
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
            TravelTimeBetween = new int[NodeNumber, NodeNumber];
        }

        public void SetVehicleTypes(List<VehicleType> _types)
        {
            fleet = new Fleet(_types);
        }

        public void SetDistanceIJ(int i, int j, int dis)
        {
            DistanceBetween[i, j] = dis;
        }

        public void SetTravelTimeIJ(int i, int j, int tt)
        {
            TravelTimeBetween[i, j] = tt;
        }

        public static int GetDistanceIJ(int i, int j)
        {
            return DistanceBetween[i, j];
        }
        public static int GetTravelTimeIJ(int i, int j)
        {
            return TravelTimeBetween[i, j];
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

        internal void SetDistanceANDTravelIJ(int i, int j, int tt_ij, int dis_ij)
        {
            SetDistanceIJ(i, j, dis_ij);
            SetTravelTimeIJ(i, j, tt_ij);

        }
        /// <summary>
        /// 计算每一个商户的小邻域，即离它可达的最近的前numNNCus个商户，以及前numNNSta个充电站
        /// </summary>
        /// <param name="_numNNCus"></param>
        /// <param name="_numNNSta"></param>
        public void SetNearDistanceCusAndSta(int _numNNCus, int _numNNSta)
        {
            NearDistanceCus = new List<int[]>();
            NearDistanceSta = new List<int[]>();
            for (int i = 0; i < Customers.Count; i++)
            {
                var node = Customers[i];
                Hashtable neighbours_Distance = new Hashtable();
                for (int j = 0; j < Customers.Count; j++)
                {
                    if (i!=j)
                    {
                        var node_j = Customers[j];
                        double et_i = node.Info.ReadyTime+node.Info.ServiceTime; //从商户i出发的最早可出发时间！=实际出发时间
                        double tt_ij = node.TravelTime(node_j);
                        double at_j = et_i + tt_ij;

                        if (at_j < node_j.Info.DueDate) //可达性
                        {
                            double dis_ij = node.TravelDistance(node_j);
                            neighbours_Distance.Add(node_j.Info.Id, dis_ij); //按照里程远近判断两点的距离关系
                        }
                    }
                }
                double[] valueArray = new double[neighbours_Distance.Count];
                int[] keyArray = new int[neighbours_Distance.Count];
                neighbours_Distance.Keys.CopyTo(keyArray, 0);
                neighbours_Distance.Values.CopyTo(valueArray, 0);
                Array.Sort(valueArray, keyArray);// 按照value升序排列
                int real_numNNCus = Math.Min(keyArray.Length, _numNNCus);
                int[] NCus_ID = new int[real_numNNCus];
                for (int k = 0; k < real_numNNCus; k++)
                {
                    NCus_ID[k] = (int)keyArray.GetValue(k) ;
                }
                NearDistanceCus.Add(NCus_ID);
                neighbours_Distance.Clear();
                
                for (int j = 0; j < Stations.Count; j++)
                {
                    var station_j = Stations[j];
                    double dis_ij = node.TravelDistance(station_j);
                    neighbours_Distance.Add(station_j.Info.Id, dis_ij);
                }
                double[] valueArray2 = new double[neighbours_Distance.Count];
                int[] keyArray2 = new int[neighbours_Distance.Count];
                neighbours_Distance.Keys.CopyTo(keyArray2, 0);
                neighbours_Distance.Values.CopyTo(valueArray2, 0);
                Array.Sort(valueArray2, keyArray2);// 按照value升序排列
                int real_numNNSta = Math.Min(keyArray2.Length, _numNNSta);
                int[] NSta_ID = new int[real_numNNSta];
                for (int k = 0; k < real_numNNSta; k++)
                {
                    NSta_ID[k] = (int)keyArray2.GetValue(k);
                }
                NearDistanceSta.Add(NSta_ID);
                neighbours_Distance.Clear();
            }
        }
        ///// <summary>
        ///// 预处理，计算从配送中心出发直接到各个商户的等待时间
        ///// </summary>
        //public void SetPriorityTimeCusList()
        //{
        //    for (int i = 0; i < this.Customers.Count; i++)
        //    {
        //        int dis_0i = StartDepot.TravelTime(Customers[i]);
        //        int waittime = (int)Customers[i].Info.ReadyTime - dis_0i;


        //    }
        //}
        /// <summary>
        /// 获得某个商户的商户小邻域，即离它可达的最近的前一些商户
        /// </summary>
        /// <param name="_cus_id"></param>
        /// <returns></returns>
        public int[] GetNearDistanceCus(int _cus_id)
        {
            return NearDistanceCus[_cus_id];
        }
        /// <summary>
        ///  获得某个商户的充电站小邻域，即离它可达的最近的前一些充电站
        /// </summary>
        /// <param name="_cus_id"></param>
        /// <returns></returns>
        public int[] GetNearDistanceSta(int _cus_id)
        {
            return NearDistanceSta[_cus_id];
        }
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

        /// <summary>
        /// 计算行驶距离，用力计算电量
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        public int TravelDistance(AbsNode destination)
        {

            //var xDist = Info.X - destination.Info.X;
            //var yDist = Info.Y - destination.Info.Y;
            //return Math.Sqrt(xDist * xDist + yDist * yDist);
            return Problem.GetDistanceIJ(Info.Id, destination.Info.Id);
        }


        /// <summary>
        /// 计算行驶时间，用来计算时间窗
        /// </summary>
        /// <param name="destination"></param>
        /// <returns></returns>
        public int TravelTime(AbsNode destination)
        {
            return Problem.GetTravelTimeIJ(Info.Id, destination.Info.Id);
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
       
        public Customer(NodeInfo info)
        {
            Info = info;
         
        }
        public override AbsNode ShallowCopy()
        {
            return new Customer(Info);
        }

    }
    

}

