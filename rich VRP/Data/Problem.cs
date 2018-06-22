using System;
using System.Collections;
using System.Collections.Generic;

// Analysis disable once CheckNamespace
namespace OP.Data
{
    public static class Problem
    {   //数据集名称
        public static string Abbr { get; set; }
        public static double Tmax { get; set; }
        public static int VehicleNum { get; set; }
        public static Depot StartDepot { get; set; }
        public static Depot EndDepot { get; set; }
        public static List<Customer> Customers { get; set; }
        public static List<Station> Stations { get; set; }
        public static List<NodeInfo> AllNodes { get; set; }
      
        public static List<VehicleType> VehTypes { get; set; }

        static int[,] DistanceBetween { get; set; }
        static int[,] TravelTimeBetween { get; set; }
        public static double[,] AngleBetween { get; set; }

        public static double MinWaitTimeAtDepot { get; set; }
        public static double WaitCostRate { get; set; }
        public static double MinWeight { get; internal set; }
        public static double MinVolume { get; internal set; }

        /// <summary>
        /// 列表记录了每一个点的商户邻域，其商户邻点按照可达性由近至远排序
        /// </summary>
        public static Dictionary<int,int[]> NearDistanceCus;
        /// <summary>
        /// 列表记录了每一个点的充电站邻域，其充电站邻点由近至远排序
        /// </summary>
        public static Dictionary<int, int[]> NearDistanceSta;


        public static void SetNodes(List<NodeInfo> nodes, string abbr, double t_max, int numV, int numD, int numC, int numS)
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
            AngleBetween = new double [NodeNumber, NodeNumber];
            setAngleBetween();
        }

        public static void SetVehicleTypes(List<VehicleType> _types)
        {
            VehTypes = _types;
        }

        public static void SetDistanceIJ(int i, int j, int dis)
        {
            DistanceBetween[i, j] = dis;
        }
        /// <summary>
        /// 两点之间的角度，所有设定的ioj均为逆时针方向的角度，均为[0,360],比如（1，3）= 60 ，则（3，1）= 300 使用时记得要判断
        /// 其中如果是（0，i），能够获取点i到水平线之间的角度
        /// </summary>
        public static void setAngleBetween()
        {
            for (int i = 0; i < AllNodes.Count; i++)
            {
                for (int j = 0; j < AllNodes.Count; j++)
                {
                    double angle1, angle2;
                    if (i == 0)
                    {
                        angle1 = 0;
                    }
                    else
                    {
                        angle1 = Math.Atan2((AllNodes[i].Y - AllNodes[0].Y), (AllNodes[i].X - AllNodes[0].X)) * (180 / Math.PI);
                        if (angle1 < 0)
                        {
                            angle1 = angle1 + 360;
                        }
                    }
                    if (j == 0)
                    {
                        angle2 = 0;
                    }
                    else
                    {
                        angle2 = Math.Atan2((AllNodes[j].Y - AllNodes[0].Y), (AllNodes[j].X - AllNodes[0].X)) * (180 / Math.PI);
                        if (angle2 < 0)
                        {
                            angle2 = angle2 + 360;
                        }
                    }
                    double angle = angle2 - angle1;
                    if (angle < 0)
                    {
                        angle = angle + 360;
                    }
                    setAngleBetweenIJ(i, j, angle);

                }

            }
        }
        public static void setAngleBetweenIJ(int i, int j, double angle)
        {
            AngleBetween[i, j] = angle;
        }
        public static void SetTravelTimeIJ(int i, int j, int tt)
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
            return AngleBetween[i, j];
        }

        public static Customer SearchCusbyId(int id)
        {
            foreach (var customer in Customers)
                if (customer.Info.Id == id)
                    return customer;
            throw new Exception("Customer not found");
        }

        public static Station SearchStaById(int id)
        {
            foreach (var station in Stations)
            {
                if (station.Info.Id == id)
                {
                    return station;
                }
            }
            throw new Exception("Station not found");
        }

        public static VehicleType GetVehTypebyID(int _vehtypeid)
        {
            foreach (VehicleType vehtype in VehTypes)
            {
                if (vehtype.VehTypeID == _vehtypeid)
                {
                    return vehtype;
                }
            }
            return null;
        }

        public static void SetAllNodes()
        {
            AllNodes = new List<NodeInfo> { StartDepot.Info, EndDepot.Info };
            foreach (var customer in Customers)
                AllNodes.Add(customer.Info);
        }

        internal static void SetDistanceANDTravelIJ(int i, int j, int tt_ij, int dis_ij)
        {
            SetDistanceIJ(i, j, dis_ij);
            SetTravelTimeIJ(i, j, tt_ij);

        }
        /// <summary>
        /// 计算每一个点的小邻域，即离它可达的最近的前numNNCus个商户，以及前numNNSta个充电站
        /// </summary>
        /// <param name="_numNNCus"></param>
        /// <param name="_numNNSta"></param>
        public static  void SetNearDistanceCusAndSta(int _numNNCus, int _numNNSta)
        {
            NearDistanceCus = new Dictionary<int, int[]>();
            NearDistanceSta = new Dictionary<int, int[]>();
            for (int i = 0; i < AllNodes.Count; i++)
            {
                var node = AllNodes[i];
                Hashtable neighbours_Distance = new Hashtable();
                for (int j = 0; j < Customers.Count; j++)
                {
                    if (i!=j)
                    {
                        var node_j = Customers[j];
                        double et_i = node.ReadyTime+node.ServiceTime; //从商户i出发的最早可出发时间！=实际出发时间
                        double tt_ij = GetTravelTimeIJ(node.Id,node_j.Info.Id);
                        double at_j = et_i + tt_ij;

                        if (at_j < node_j.Info.DueDate) //可达性
                        {
                            double dis_ij = GetDistanceIJ(node.Id,node_j.Info.Id);
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
                NearDistanceCus.Add(node.Id, NCus_ID);
                neighbours_Distance.Clear();
                
                for (int j = 0; j < Stations.Count; j++)
                {
                    var station_j = Stations[j];
                    double dis_ij = GetDistanceIJ(node.Id, station_j.Info.Id);
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
                NearDistanceSta.Add(node.Id, NSta_ID);
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
        /// 获得某个点的商户小邻域，即离它可达的最近的前一些商户
        /// </summary>
        /// <param name="_cus_id"></param>
        /// <returns></returns>
        public static int[] GetNearDistanceCus(int _node_id)
        {
            return NearDistanceCus[_node_id];
        }
        /// <summary>
        ///  获得某个商户的充电站小邻域，即离它可达的最近的前一些充电站
        /// </summary>
        /// <param name="_cus_id"></param>
        /// <returns></returns>
        public static int[] GetNearDistanceSta(int _cus_id)
        {
            return NearDistanceSta[_cus_id];
        }
    }

    public class VehicleType
    {
        public int VehTypeID { get; set; }
        public string Name { get; set; }
        /// <summary>
        /// 体积
        /// </summary>
        public double Volume { get; set; }
        /// <summary>
        /// 载重
        /// </summary>
        public double Weight { get; set; }
        /// <summary>
        /// 最大行驶里程
        /// </summary>
        public double MaxRange { get; set; }

        /// <summary>
        /// Gets or sets the fixed cost.
        /// </summary>
        /// <value>The fixed cost.</value>
        public double FixedCost { get; set; }

        /// <summary>
        /// Gets or sets the variable cost.
        /// </summary>
        /// <value>The variable cost.</value>
        public double VariableCost { get; set; }

        /// <summary>
        /// Gets or sets the charge time.
        /// </summary>
        /// <value>The charge time.</value>
        public double ChargeTime { get; set; }
        /// <summary>
        /// Gets or sets the charge cost per hour (RMB/min)
        /// </summary>
        public double ChargeCostRate { get; set; }
        /// <summary>
        /// Gets or sets the maximal number of vehicles of this type
        /// </summary>
        public int MaxNum { get; set; }
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

        public double GetAngel(AbsNode destination)
        {
            return Problem.GetAngelIJ(Info.Id, destination.Info.Id);
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

