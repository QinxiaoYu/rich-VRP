using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Collections;
using System.Security.Cryptography.X509Certificates;
using System.Net.NetworkInformation;

namespace OP.Data
{
    public class Route
    {   
        /// <summary>
        /// 路径的ID
        /// </summary>
        public string RouteId { get; set; }
        /// <summary>
        /// The vehicle assigned to this route
        /// </summary>
        public Vehicle AssignedVeh { get; set; }

        public VehicleType AssignedVehType { get; set; }
        /// <summary>
        /// 路径上的顾客集合，首尾包含仓库
        /// </summary>
        public List<AbsNode> RouteList { get; set; }
        public int RouteIndexofVeh { get; set; }
        /// <summary>
        /// 各个点的实际服务开始时间
        /// </summary>
        public List<double> ServiceBeginingTimes { get; set; }
        /// <summary>
        /// The time departure from depot
        /// </summary>
        double DepatureTime;
        /// <summary>
        /// The time return to depot
        /// </summary>
        double ArrivalTime;
        /// <summary>
        /// 在某点处剩余电量可行驶的距离，插入新的点后需要更新
        /// </summary>
        public List<double> battery_level { get; set; }

        internal void UpdateServiceBeginningTimes()
        {
            for (int i = 1; i < RouteList.Count; i++)
            {
                ServiceBeginingTimes[i] = Math.Max(RouteList[i].Info.ReadyTime, ServiceBeginingTimes[i-1] + RouteList[i - 1].Info.ServiceTime + RouteList[i - 1].TravelTime(RouteList[i]));
            }
        }

        internal int FindLongestArc()
        {
            int pos = -1;
            int max_dist = int.MinValue;
            for (int i = 1; i < RouteList.Count-2; i++)
            {
                int dist_ij = RouteList[i].TravelDistance(RouteList[i + 1]);
                if (dist_ij>max_dist)
                {
                    max_dist = dist_ij;
                    pos = i + 1;
                }
            }
            return pos;
        }

        /// <summary>
        /// 在初始化中判断这条线路是否还可以插入
        /// </summary>
        bool isOpen = true;
        /// <summary>
        /// 专门用作route.copy
        /// </summary>
        public double routecost;


        public Route()
        {
            Depot startdepot = Problem.StartDepot;
            Depot enddepot = Problem.EndDepot;
            AssignedVehType = null;
            AssignedVeh = null;
            RouteList = new List<AbsNode>();
            ServiceBeginingTimes = new List<double>();
            battery_level = new List<double>();
            
        }

        internal Route ChangeToAnotherType()
        {
            int anotherType = 1;
            if (AssignedVehType.VehTypeID==1)
            {
                anotherType = 2;
            }
            double Range_Gap = AssignedVehType.MaxRange - Problem.GetVehTypebyID(anotherType).MaxRange;
            Route new_route = Copy();
            new_route.routecost = double.MaxValue;
            new_route.AssignedVehType = Problem.GetVehTypebyID(anotherType);
            for (int i = 0; i < new_route.battery_level.Count; i++)
            {
                new_route.battery_level[i] -= Range_Gap;
            }
            return new_route;

        }

        /// <summary>
        /// 批量删除点集
        /// </summary>
        /// <param name="cuslist">Cuslist.</param>
        internal void Remove(List<AbsNode> cuslist)
        {
            foreach (var node in cuslist)
            {
                Remove((Customer)node);
            }
        }

        /// <summary>
        /// 一条线路最多充电3次
        /// </summary>
        /// <param name="cnt_charge"></param>
        /// <param name="old_obj"></param>
        /// <returns></returns>
        internal Route InsertSta(int cnt_charge, double old_obj =0)
        {
            Route bst_route = this.Copy();
            bst_route.routecost = old_obj;
            Route tmp_r = this.Copy();
            if (cnt_charge>3)
            {
                throw new Exception("一条路上不能多于3个充电站");
            }
            if (old_obj==0)
            {
                var costs = this.routeCost();
                old_obj = costs.Item1 + costs.Item2 + costs.Item3;
            }

            for (int k = RouteList.Count - 1; k > 0; k--)
            {
                if (RouteList[k].Info.Type == 3)//充电站
                {
                   
                    tmp_r.RemoveAt(k);
                }
            }
                       
            bool isFeasible = false;
            //1个充电站    
            for (int i = 0; i < tmp_r.RouteList.Count-1; i++)
            {
                Route tmp_route_i = tmp_r.Copy();
                bool Finded = tmp_route_i.insert_sta_between(i, i + 1);
                if (Finded == false || tmp_route_i.ViolationOfRange()>-1 || tmp_route_i.ViolationOfTimeWindow()>-1)
                {
                    continue;
                }
                var new_costs = tmp_route_i.routeCost();
                double new_obj = new_costs.Item1 + new_costs.Item2 + new_costs.Item3;
                if (new_obj < old_obj)
                {
                    bst_route = tmp_route_i.Copy();
                    if (bst_route.AssignedVeh.VehRouteList.Count!=0)
                    {
                        bst_route.AssignedVeh.VehRouteList[bst_route.RouteIndexofVeh] = bst_route.RouteId;
                    }        
                    bst_route.routecost = new_obj;                            
                    old_obj = new_obj;
                    isFeasible = true;
                }
            }
            if (isFeasible == true || cnt_charge == 1)
            {
                return bst_route;
            }

            //2个充电站
            for (int i = 0; i < tmp_r.RouteList.Count-1; i++)
            {
                Route tmp_route_i = tmp_r.Copy();
                tmp_route_i.insert_sta_between(i, i + 1);

                for (int j = i+2; j < tmp_route_i.RouteList.Count-1; j++)
                {
                    Route tmp_route_j = tmp_route_i.Copy();
                    bool isFinded = tmp_route_j.insert_sta_between(j, j + 1);
                    if (isFinded ==false || tmp_route_j.ViolationOfRange()>-1 || tmp_route_j.ViolationOfTimeWindow()>-1)
                    {
                        continue;
                    }
                    var new_costs = tmp_route_j.routeCost();
                    double new_obj = new_costs.Item1 + new_costs.Item2 + new_costs.Item3;
                    if (new_obj < old_obj)
                    {
                        bst_route = tmp_route_j.Copy();
                        if (bst_route.AssignedVeh.VehRouteList.Count != 0)
                        {
                            bst_route.AssignedVeh.VehRouteList[bst_route.RouteIndexofVeh] = bst_route.RouteId;
                        }
                      
                        bst_route.routecost = new_obj;
                        old_obj = new_obj;
                        isFeasible = true;
                    }
                }
            }
            if (isFeasible == true || cnt_charge == 2)
            {
                return bst_route;
            }

            //3个充电站
            for (int i = 0; i < tmp_r.RouteList.Count-1; i++)
            {
                Route tmp_route_i = tmp_r.Copy();
                tmp_route_i.insert_sta_between(i, i + 1);
                for (int j = i+2; j < tmp_route_i.RouteList.Count-1; j++)
                {
                    Route tmp_route_j = tmp_route_i.Copy();
                    tmp_route_j.insert_sta_between(j, j + 1);
                    for (int k = j+2; k <tmp_route_j.RouteList.Count-1 ; k++)
                    {
                        Route tmp_route_k = tmp_route_j.Copy();
                        bool isFinded = tmp_route_k.insert_sta_between(k, k + 1);
                        if (isFinded ==false || tmp_route_k.ViolationOfRange() > -1 || tmp_route_k.ViolationOfTimeWindow() > -1)
                        {
                            continue;
                        }
                        var new_costs = tmp_route_k.routeCost();
                        double new_obj = new_costs.Item1 + new_costs.Item2 + new_costs.Item3;
                        if (new_obj < old_obj)
                        {
                            bst_route = tmp_route_k.Copy();
                            if (bst_route.AssignedVeh.VehRouteList.Count != 0)
                            {
                                bst_route.AssignedVeh.VehRouteList[bst_route.RouteIndexofVeh] = bst_route.RouteId;
                            }
                            bst_route.routecost = new_obj;
                            old_obj = new_obj;
                            isFeasible = true;
                        }
                    }
                }
            }
            return bst_route;

        }

        internal void RemoveAllSta()
        {
            Route copy_route = this.Copy();
            foreach (AbsNode node in copy_route.RouteList)
            {
                if (node.Info.Type==3)
                {
                    Remove((Station)node);
                }
            }
        }

        /// <summary>
        /// 在路径的相邻两点之间插入充电站，该充电站距离这两个点的距离和最近
        /// </summary>
        /// <param name="i"></param>
        /// <param name="j"></param>
        internal bool insert_sta_between(int i, int j)
        {
            bool isFindSta = true;
            var Node_i = this.RouteList[i];
            var Node_j = this.RouteList[j];
            List<int> NeighborSta_i = new List<int>();
            List<int> NeighborSta_j = new List<int>();

            if (Node_i.Info.Type==2)
            {
                NeighborSta_i = Problem.GetNearDistanceSta(Node_i.Info.Id).ToList();
            }
            if (Node_j.Info.Type == 2)
            {
                NeighborSta_j = Problem.GetNearDistanceSta(Node_j.Info.Id).ToList();
            }

            List<int> NeighborSta_ij = NeighborSta_i.Intersect(NeighborSta_j).ToList(); 
            if (NeighborSta_ij.Count==0)
            {
                NeighborSta_ij = NeighborSta_i.Union(NeighborSta_j).ToList();
            }

            double battery_i = this.battery_level[i];

            Station bst_sta = null;
            double min_dist_ij = double.MaxValue;
            foreach (int sta_id in NeighborSta_ij)
            {
                Station sta = Problem.SearchStaById(sta_id);
                if (Node_i.TravelDistance(sta)<battery_i)
                {
                    double dist_ij = Node_i.TravelDistance(sta) + sta.TravelDistance(Node_j);
                    if (dist_ij < min_dist_ij)
                    {
                        bst_sta = sta;
                        min_dist_ij = dist_ij;
                    }
                }          
            }
            if (bst_sta == null)
            {
                isFindSta = false;
                return isFindSta;
            }
            this.InsertNode(bst_sta, j);
            return isFindSta;
        }

        public Route(VehicleType vehtype)
        {
            Depot startdepot = Problem.StartDepot;
            Depot enddepot = Problem.EndDepot;
            AssignedVehType = vehtype;
            AssignedVeh = null;
            RouteList = new List<AbsNode>();
            ServiceBeginingTimes = new List<double>();
            battery_level = new List<double>();

            AddNode(startdepot);
            AddNode(enddepot);
        }



        /// <summary>
        /// 在已知分配给哪辆车的前提下，初始化一条路径
        /// </summary>
        /// <param name="problem"></param>
        /// <param name="veh">为该路径分配的车辆</param>
        public Route(Vehicle veh, double earliestDepartureTime)
        {
            Depot startdepot = Problem.StartDepot;
            Depot enddepot = Problem.EndDepot;
            AssignedVeh = veh;
            AssignedVehType = Problem.VehTypes[veh.TypeId-1];
            
            //Solution = null;
            RouteList = new List<AbsNode>();
            ServiceBeginingTimes = new List<double>();
            battery_level = new List<double>();

            int numRouteofVeh = veh.VehRouteList.Count(); //当前车辆已经行驶的趟数
            this.RouteIndexofVeh = numRouteofVeh;

            //ServiceBeginingTimes.Add(earliestDepartureTime);

            AddNode(startdepot);
            ServiceBeginingTimes[0] = earliestDepartureTime;
            AddNode(enddepot);
        }

        /// <summary>
        /// 判断该路径是否饱和（重量、体积、等待时间、返回仓库的时间）
        /// </summary>
        /// <returns></returns>
        public bool IsSaturated()
        {
            bool saturated = false;
            if (GetTotalVolume()< GetRouteVolumeCap() && GetTotalWeight()<GetRouteWeightCap())
            {
                saturated = true;
            }
            return saturated;
        }
   

        /// <summary>
        /// 该方法仅适用于一天的首班线路,最后输出结果时候再用
        /// 根据线路第一个商户的服务开始时间，往回推起点的出发时间
        /// </summary>
        internal void UpdateDepartureTime()
        {          
            double departuretime = ServiceBeginingTimes[0] + RouteList[0].Info.ServiceTime;
            double arrivetime = departuretime + RouteList[0].TravelTime(RouteList[1]);
            double waittime_firstcus = Math.Max(0, ServiceBeginingTimes[1] - arrivetime);
            if (waittime_firstcus>0)
            {
                ServiceBeginingTimes[0] = ServiceBeginingTimes[0] + waittime_firstcus;
            }
            
        }

        /// <summary>
        /// 把一条线路分配给一辆具体的车
        /// </summary>
        /// <param name="veh"></param>
        public void RouteAssign2Veh(Vehicle veh)
        {
            this.AssignedVeh = veh;
            this.AssignedVeh.VehRouteList[this.RouteIndexofVeh] = this.RouteId;

        }

        /// <summary>
        /// 在初始化中，设置一条线路已经处于不能再添加商户的状态
        /// </summary>
        public void SetClosed()
        {
            this.isOpen = false;
        }
        /// <summary>
        /// 在初始化中，判断一条线路是否处于还可以添加商户的状态
        /// </summary>
        /// <returns></returns>
        public bool GetOpen()
        {
            return this.isOpen;
        }

        /// <summary>
        /// 计算当前路径达到终点的时刻
        /// </summary>
        /// <returns></returns>
        public double GetArrivalTime()
        {
            int numNodesinRoute = this.RouteList.Count();
            this.ArrivalTime = ServiceBeginingTimes[numNodesinRoute - 1];
            return ArrivalTime;
        }

        public double GetDepartureTime()
        {
            this.DepatureTime = ServiceBeginingTimes[0];
            return DepatureTime;
        }

        /// <summary>
        /// 获得某点处的浮动时间=该点处的等待时长
        /// </summary>
        /// <param name="i">该点在线路中的位置</param>
        /// <returns></returns>
        internal double GetFloatTimeAtCus(int i)
        {
            double floattime = 960.0;
            if (RouteList[i].Info.Type==2)//商户
            {
                double duetime = RouteList[i].Info.DueDate;
                double servicestarttime = ServiceBeginingTimes[i];
                floattime = duetime - servicestarttime;
            }
            return floattime;
        }

        /// <summary>
        /// 插入新节点到路径末尾，并更新路径各节点的服务时间；
        /// </summary>
        /// <param name="newNode">抽象节点，可以是顾客也可是仓库</param>
        public void AddNode(AbsNode newNode)
        {
            //线路上最后一个点
            AbsNode lastCustomer = RouteList.Count == 0 ? newNode : RouteList[RouteList.Count - 1];
            //线路上最后一个点 可以开始游览的时间 （如果到达时间早于时间窗的开始时间，则为时间窗开始时间；否则为实际达到时间）
            double lastServiceTime = RouteList.Count == 0 ? Problem.StartDepot.Info.ReadyTime : ServiceBeginingTimes[ServiceBeginingTimes.Count - 1];
            
            //新景点 可以开始游览的时间
            double serviceBegins = NextServiceBeginTime(newNode, lastCustomer, lastServiceTime);
            //线路上最后一个点的剩余电量
            double lastRemainBattery = RouteList.Count == 0 ? this.GetRouteRangeCap() : battery_level[battery_level.Count - 1];
            //新点 的剩余电量 
            double RemainBattery = NextRemainBattery(newNode, lastCustomer, lastRemainBattery);
            RouteList.Add(newNode);
            ServiceBeginingTimes.Add(serviceBegins);
            battery_level.Add(RemainBattery);
            UpdateId();
        }

        private double NextRemainBattery(AbsNode newNode, AbsNode lastCustomer, double lastRemainBattery)
        {
            double travelDistance = lastCustomer.TravelDistance(newNode); //两点之间用电量=行驶距离
            double remainBattery = lastRemainBattery - travelDistance;

            if (newNode.Info.Type == 3) //新点是一个充电站
            {
                remainBattery = this.GetRouteRangeCap();
            }         
            return remainBattery;
        }

        internal int getNumofCus()
        {
            int cnt = 0;
            foreach (var node in RouteList)
            {
                if (node.Info.Type==2)
                {
                    cnt++;
                }
            }
            return cnt;
        }

        internal void InsertCustomer(List<AbsNode> unroutedCustomers)
        {
            
            for (int i = 0; i < unroutedCustomers.Count; i++)
            {
                Customer cusToInsert = (Customer)unroutedCustomers.ElementAt(i);
                InsertNode(cusToInsert, this.RouteList.Count - 1);
            }            

        }


        /// <summary>
        /// 在路径中任意位置插入新点（商家或充电站）
        /// </summary>
        /// <param name="newCustomer">待插入的新点</param>
        /// <param name="position">插入位置</param>
        public void InsertNode(AbsNode newNode, int position)
        {
            if (newNode.Info.Type==2)
            {
                newNode = (Customer)newNode;
            }
            if (newNode.Info.Type == 3)
            {
                newNode = (Station)newNode;
            }           
            RouteList.Insert(position, newNode);
            ServiceBeginingTimes.Insert(position,0);
            battery_level.Insert(position, 0);
            //更新插入顾客及其之后顾客的服务开始时间与电量
            for (int i = position; i < RouteList.Count; ++i)
            {
                double newTime = NextServiceBeginTime(RouteList[i], RouteList[i - 1], ServiceBeginingTimes[i - 1]);
                ServiceBeginingTimes[i] = newTime;
                double newBattery = NextRemainBattery(RouteList[i], RouteList[i - 1], battery_level[i - 1]);
                battery_level[i] = newBattery;
            }
            UpdateId();
        }


        /// <summary>
        /// 删除某一位置上的顾客，更新服务时间
        /// </summary>
        /// <param name="position">要删除的位置</param>
        public void RemoveAt(int position)
        {
            RouteList.RemoveAt(position);
            ServiceBeginingTimes.RemoveAt(position);
            battery_level.RemoveAt(position);
            for (int i = position; i < RouteList.Count; ++i)
            {
                double newTime = NextServiceBeginTime(RouteList[i], RouteList[i - 1], ServiceBeginingTimes[i - 1]);
                ServiceBeginingTimes[i] = newTime;
                double newBattery = NextRemainBattery(RouteList[i], RouteList[i - 1], battery_level[i - 1]);
                battery_level[i] = newBattery;
            }

            UpdateId();
        }

        public void Remove(Customer cus)
        {
            for (var i = 0; i < this.RouteList.Count; ++i)
                if (this.RouteList[i].Info.Id == cus.Info.Id)
                    RemoveAt(i);

        }

        public void Remove(Station station)
        {
            for (var i = 0; i < this.RouteList.Count; ++i)
                if (this.RouteList[i].Info.Id == station.Info.Id)
                    RemoveAt(i);

        }

        /// <summary>
        /// 获取某个点的近邻
        /// </summary>
        /// <param name="v_index">目标点在路径中的下标</param>
        /// <returns>其他点（不包括Depot）从小到大排列</returns>
        internal List<Customer> getNeighborhood(Customer v)
        {
            List<Customer> neighborhood = new List<Customer>();
            Hashtable ht = new Hashtable();      
            
            for (int i = 1; i < this.RouteList.Count-2; i++)
            {
                Customer neighbor = (Customer)this.RouteList[i];
                if (neighbor.Info.Id != v.Info.Id)
                {              
                    double distance = v.TravelDistance(neighbor);
                    ht.Add(neighbor, distance);
                }
            }

            double[] valueArray = new double[ht.Count];
            Customer[] keyArray = new Customer[ht.Count];
            ht.Keys.CopyTo(keyArray, 0);
            ht.Values.CopyTo(valueArray, 0);
            Array.Sort(valueArray, keyArray);//默认升序排列
            neighborhood.AddRange(keyArray.ToList());

            return neighborhood;
        }


        /// <summary>
        /// 笼统的判读路径是否可行(除电量之外)
        /// </summary>
        /// <returns>不可行：对某一个顾客：服务时间>最晚可接受时间or对路径：容量超限</returns>
        public bool IsFeasible_except_bat()
        {
            double vV = ViolationOfVolume();
            if (vV > 0) return false;
            double vW = ViolationOfWeight();
            if (vW > 0) return false;
            int vTW = ViolationOfTimeWindow();
            if (vTW > -1) return false;
            return true;
        }



        /// <summary>
        /// 笼统的判读路径是否可行
        /// </summary>
        /// <returns>不可行：对某一个顾客：服务时间>最晚可接受时间or对路径：容量超限</returns>
        public bool IsFeasible()
        {
            double vV = ViolationOfVolume();
            if (vV > 0) return false;
            double vW = ViolationOfWeight();
            if (vW > 0) return false;
            int vR = ViolationOfRange();
            if (vR > -1) return false;
            int vTW = ViolationOfTimeWindow();
            if (vTW > -1) return false;

            return true;
        }
        /// <summary>
        /// 判断是否违反体积约束
        /// </summary>
        /// <returns>0：不违反; >0:违反的量</returns>
        public double ViolationOfVolume()
        {
            double Violation = 0;
            double CurrentVolume = GetTotalVolume();
            double CapacityVolume = GetRouteVolumeCap();
            Violation = Math.Max(0, CurrentVolume - CapacityVolume);
            return Violation;
        }
        /// <summary>
        /// 获得当前路径上的所有货物的总体积
        /// </summary>
        /// <returns></returns>
        public double GetTotalVolume()
        {
            double CurrentVolume = 0;        
            for (int i = 0; i < RouteList.Count; i++)
            {
                if (RouteList[i].Info.Type == 2) //商家
                {
                    CurrentVolume += RouteList[i].Info.Volume;
                }
            }
            return CurrentVolume;
        }
        /// <summary>
        /// 获得当前路径的体积上限（车的体积容量）
        /// </summary>
        /// <returns></returns>
        public double GetRouteVolumeCap()
        {
            int vehType = this.AssignedVeh.TypeId;
            double CapacityVolume = Problem.GetVehTypebyID(vehType).Volume;
            return CapacityVolume;
        }
        /// <summary>
        /// 判断是否违反重量约束
        /// </summary>
        /// <returns>0：不违反; >0:违反的量</returns>
        public double ViolationOfWeight()
        {
            double Violation = 0;
            double CurrentWeight = GetTotalWeight();
            double CapacityWeight = GetRouteWeightCap();
            if (CurrentWeight-CapacityWeight>-0.000001)
            {
                Violation = CurrentWeight - CapacityWeight;
            }
            Violation = Math.Max(0, CurrentWeight - CapacityWeight);
            return Violation;
        }
        /// <summary>
        /// 获得当前路径上的所有货物的总体积
        /// </summary>
        /// <returns></returns>
        public double GetTotalWeight()
        {
            double CurrentWeight = 0;
            for (int i = 0; i < RouteList.Count; i++)
            {
                if (RouteList[i].Info.Type == 2) //商家
                {
                    CurrentWeight += RouteList[i].Info.Weight;
                }
            }
            return CurrentWeight;
        }
        public double GetRouteWeightCap()
        {
            int vehType = this.AssignedVeh.TypeId;
            double CapacityWeight = Problem.GetVehTypebyID(vehType).Weight;
            return CapacityWeight;
        }
        /// <summary>
        /// 判断这条路径是否有里程允许范围内，不能到达的点。
        /// </summary>
        /// <returns>如果有, 则返回第一个不能到的点的位置；否则返回-1.</returns>
        public int ViolationOfRange()
        {
            int VioPosition = -1;
            double CapacityRange = GetRouteRangeCap(); //获取服务该路径的车辆的最大行驶里程
            double currentRange = CapacityRange; //车辆离开某点时的剩余里程
            for (int i = 0; i < RouteList.Count-1; i++)
            {
                //if (battery_level[i] < 0)
                //{
                //    return i;
                //} 
                double dis_ij = RouteList[i].TravelDistance(RouteList[i + 1]);
                currentRange -= dis_ij; //达到j点时的剩余里程

                if (currentRange < 0) //如果达到j点时的剩余里程小于0，则返回j点所在点位置
                {
                    return i + 1;
                }

                if (RouteList[i + 1].Info.Type == 3)//j点是一个充电站
                {
                    currentRange = CapacityRange; //在j点充满电出发
                }
            }
            return VioPosition; 
        }

        public double GetRouteRangeCap()
        {
            int vehType = this.AssignedVeh.TypeId;
            double CapacityRange = Problem.GetVehTypebyID(vehType).MaxRange;
            return CapacityRange;
        }

        /// <summary>
        /// 路径的行驶长度
        /// </summary>
        /// <returns></returns>
        public double GetRouteLength()
        {
            double totalDist = 0;
            for (int i = 0; i < RouteList.Count - 1; ++i)
                totalDist += RouteList[i].TravelDistance(RouteList[i + 1]);
            return totalDist;
        }
        /// <summary>
        /// 判断是否有不满足时间窗的点.
        /// </summary>
        /// <returns>如果有，则返回第一个违反点所在位置，否则返回-1.</returns>
        public int ViolationOfTimeWindow()
        {
            for (int i = 0 ; i < RouteList.Count-1; ++i)
            {
                double ArrivalTimeAtj = ServiceBeginingTimes[i]
                                        + RouteList[i].Info.ServiceTime
                                                      + RouteList[i].TravelTime(RouteList[i + 1]);
                if (ArrivalTimeAtj>RouteList[i+1].Info.DueDate)
                {
                    return i + 1;
                }                  
            }
            return -1;
        }

        /// <summary>
        /// 下一个点的实际服务开始时间
        /// </summary>
        /// <param name="newCustomer">下一个点</param>
        /// <param name="prevCustomer">当前点</param>
        /// <param name="prevTime">当前点实际服务开始时间</param>
        /// <returns>下一个点的实际服务开始时间</returns>
        public double NextServiceBeginTime(AbsNode newCustomer, AbsNode prevCustomer, double prevTime)
        {
            double travelTime = prevCustomer.TravelTime(newCustomer);
            double serviceTime = prevCustomer.Info.ServiceTime;  //起终点的servicetime=0
            double readyTime = newCustomer.Info.ReadyTime;
            return Math.Max(readyTime, prevTime + serviceTime + travelTime);
        }


        internal double GetWaitTime()
        {
            double waittime = 0;
            for (int i = 1; i < this.RouteList.Count - 1; i++)
            {
                double arrivetime = ServiceBeginingTimes[i - 1] + RouteList[i - 1].Info.ServiceTime + RouteList[i - 1].TravelTime(RouteList[i]);
                double servicestarttime = ServiceBeginingTimes[i];
                if (arrivetime<servicestarttime)
                {
                    waittime += servicestarttime - arrivetime;
                }
            }
            return waittime;
        }
        /// <summary>
        /// 线路上每个点的等待时间，包括起终点但默认为0.（在起点的1个小时充电+装货时间不算等待时间）  
        /// </summary>
        /// <returns></returns>
        internal List<double> GetWaitTimeAtNode()
        {
            List<double> list_waittime = new List<double>();
            list_waittime.Add(0); //  起点    
            for (int i = 1; i < this.RouteList.Count - 1; i++)
            {
                double arrivetime = ServiceBeginingTimes[i - 1] + RouteList[i - 1].Info.ServiceTime + RouteList[i - 1].TravelTime(RouteList[i]);
                double servicestarttime = ServiceBeginingTimes[i];

               
                if (arrivetime < servicestarttime)
                {
                    list_waittime.Add(servicestarttime - arrivetime);
                }
                else
                {
                    list_waittime.Add(0);
                }
            }
            list_waittime.Add(0);
            return list_waittime;
        }

       
        /// <summary>
        /// which station should be inesrt after a certain customer?
        /// </summary>
        /// <param name="cus"></param>
        /// <returns></returns>
        public Station insert_sta(AbsNode node)
        {
            int min_distance = int.MaxValue;
            Station sta = null;
            foreach (var station in Problem.Stations)
            {
                if (station.Info.Id == node.Info.Id)
                {
                    continue;
                }
                int distance = node.TravelDistance(station);
                if (distance < min_distance)
                {
                    min_distance = distance;
                    sta = station;
                }
            }

            return sta;
        }



        public Route Copy()
        {
            var newRouteList = new List<AbsNode>(RouteList.Count);
            newRouteList.AddRange(RouteList.Select(node => node.ShallowCopy()));
            var r = new Route()
            {
                RouteList = newRouteList,
                ServiceBeginingTimes = new List<double>(ServiceBeginingTimes),
                battery_level = new List<double>(battery_level),
            };
            r.AssignedVehType = this.AssignedVehType;
            r.AssignedVeh = AssignedVeh.Copy();           
            r.RouteIndexofVeh = this.RouteIndexofVeh;
            r.routecost = this.routecost;
            r.UpdateId();
            return r;
        }

     

        public string PrintToString(bool printTime = true, bool printCapacity = false, bool printCapacityUnit = false )
        {
            //string routeText = "";
            //string serviceText = "";
            //string serviceBeginText = "";
            string TourInfo = string.Format("{0} 点 从 {1} 出发：\n", ServiceBeginingTimes[0], RouteList[0].Info.Id);
            for (int i = 1; i < RouteList.Count-1; ++i)
            {
                //routeText += RouteList[i].Info.Name;
                ////serviceText += RouteList[i].Info.ServiceTime.ToString("0.00") + " ";
                //serviceText += ServiceTimes[i].ToString("0.00") + " ";
                //serviceBeginText += ServiceBeginingTimes[i].ToString("0.00") + " ";
                double departuretime = ServiceBeginingTimes[i - 1] + RouteList[i - 1].Info.ServiceTime; 
                double arrivetime = departuretime + RouteList[i - 1].TravelTime(RouteList[i]);
                double waittime = Math.Max(0, ServiceBeginingTimes[i] - arrivetime);
                double servicetime = RouteList[i].Info.ServiceTime;
                TourInfo += string.Format("第{0}站：{1}, 出发时间 {2}, 到达时间 {3}, 等待时间 {4}, 游览时长 {5}, 时间窗：({6},{7}), 类型 {8} \n", i , RouteList[i].Info.Id,departuretime,arrivetime,waittime,servicetime,RouteList[i].Info.ReadyTime,RouteList[i].Info.DueDate, RouteList[i].Info.Type);
                //if (i != RouteList.Count - 1)
                //{
                //    routeText += "-";
                //    serviceText += " ";
                //    serviceBeginText += " ";
                //}
            }
            TourInfo += string.Format("{0} 点 到达 {1} ,旅途结束。\n", ServiceBeginingTimes[RouteList.Count-1],RouteList[RouteList.Count-1].Info.Id);
            TourInfo += string.Format("所用车型: {0} id={1}, 重量 ={2} 体积={3}。\n ",this.AssignedVeh.TypeId.ToString(),this.AssignedVeh.VehId, this.GetTotalWeight().ToString(),this.GetTotalVolume().ToString());
            //if (printTime)
            //{
            //    return (routeText + "\n" + serviceText+"\n");
            //}
            //else
            //{
            //    return routeText;
            //}
            //return (routeText + "," + serviceText);
            return TourInfo;
        }

        public string PrintToStringSample(bool printTime = true, bool printCapacity = false, bool printCapacityUnit = false)
        {
            string routeText = "";
            //string serviceText = "";
            //string serviceBeginText = "";
            
            for (int i = 1; i < RouteList.Count - 1; ++i)
            {
                routeText += RouteList[i].Info.Id;
                ////serviceText += RouteList[i].Info.ServiceTime.ToString("0.00") + " ";
                //serviceText += ServiceTimes[i].ToString("0.00") + " ";
                //serviceBeginText += ServiceBeginingTimes[i].ToString("0.00") + " ";

                if (i != RouteList.Count - 1)
                {
                    routeText += "-";
                    //serviceText += " ";
                    //serviceBeginText += " ";
                }
            }
            

                return routeText;
 
           
            
        }

        private void UpdateId()
        {
            if (RouteList.Count > 2)
                RouteId = RouteList[1].Info.Id.ToString(CultureInfo.InvariantCulture) + "-" +
                          RouteList[RouteList.Count - 2].Info.Id.ToString(CultureInfo.InvariantCulture);
            else
                RouteId = "<EMPTY>";
        }
       

        public bool isEqual(Route route2)
        {
         
            if (this.RouteId != route2.RouteId)
            {
                return false;
            }
            if (this.RouteList.Count != route2.RouteList.Count)
            {
                return false;
            }
            for (int i = 1; i < route2.RouteList.Count-1; i++)
            {
                if (this.RouteList[i].Info.Id != route2.RouteList[i].Info.Id)
                {
                    return false;
                }
            }
           return true;}
        /// <summary>
        /// 计算一条线路的可变成本，包括运输费用，等待费用，充电费用
        /// </summary>
        /// <param name="TransCostRate"></param>
        /// <param name="ChargeCostRate"></param>
        /// <returns>返回一个元组（TransCost, WaitCost, ChargeCost, chargeCount）</returns>
		public Tuple<double, double, double, int> routeCost(double TransCostRate=0.0,double ChargeCostRate=0.0)
		{
			double WaitCost = 0;
			double TransCost = 0;
			double ChargeCost = 0;
			int chargeCount = 0;
            if (TransCostRate == 0.0)
            {
                TransCostRate = this.AssignedVehType.VariableCost;
                ChargeCostRate = this.AssignedVehType.ChargeCostRate;
            }
            int fisttrip = RouteIndexofVeh; //判断是否首趟
            for (int i = 1; i<RouteList.Count; i++)
            {
                //等待成本
                double AT_i = ServiceBeginingTimes[i - 1] + RouteList[i - 1].Info.ServiceTime + RouteList[i - 1].TravelTime(RouteList[i]);
				double WT_i = Math.Max(ServiceBeginingTimes[i] - AT_i, 0);
                if (fisttrip==0&&i==1)
                {
                    WaitCost += 0;
                }
                else
                {
                    WaitCost += WT_i * Problem.WaitCostRate;
                }
				
				//运输成本
				double Distance_ij = RouteList[i - 1].TravelDistance(RouteList[i]);
				TransCost += TransCostRate* Distance_ij;

                //充电成本
                if (RouteList[i].Info.Type == 3)
                {
                    ChargeCost += ChargeCostRate* RouteList[i].Info.ServiceTime;
					chargeCount += 1;
                }

            }

			return new Tuple<double, double, double, int>(TransCost, WaitCost, ChargeCost, chargeCount);

			
        }

        public Tuple<double,double,int,int> GetRouteBearing()
        {
            double sAngle = double.MaxValue;
            double lAngle = double.MinValue;
            int sRadian = int.MaxValue;
            int lRadian = int.MinValue;
            for (int i = 1; i < RouteList.Count-1; i++)
            {
                double angle = Problem.GetAngelIJ(0,RouteList[i].Info.Id);
                int radian = Problem.GetDistanceIJ(0, RouteList[i].Info.Id);
                if (angle<sAngle)
                {
                    sAngle = angle;
                }
                if (angle>lAngle)
                {
                    lAngle = angle;
                }
                if (radian<sRadian)
                {
                    sRadian = radian;
                }
                if (radian>lRadian)
                {
                    lRadian = radian;
                }
            }
            return new Tuple<double, double,int,int>(sAngle, lAngle,sRadian,lRadian);
        }

        /// <summary>
        /// 获得两条路线的重叠区域所占百分比 = 重叠角度/（线路1覆盖角度+线路2覆盖角度的并集）,如不相交，返回角度差为负值
        /// </summary>
        /// <returns>The percent.</returns>
        /// <param name="r1">R1.</param>
        public Tuple<double,int> overlapPercent(Route r1)
        {
            double op = 0;
            int dis_radian = 0;
            var r0_bearing = GetRouteBearing(); //线路1对应的扇形区域
            var r1_bearing = r1.GetRouteBearing();//线路2对应的扇形区域

            if (r0_bearing.Item4>r1_bearing.Item4) //线路1的大半径长于线路2的长半径
            {
                dis_radian = Math.Max(0, r0_bearing.Item3 - r1_bearing.Item4); //两条线路的半径差
            }else //反之
            {
                dis_radian = Math.Max(0, r1_bearing.Item3 - r0_bearing.Item4);
            }

            if (r0_bearing.Item1<=r1_bearing.Item1) //线路1的小角度小于线路2的小角度
            {
                double op_area = r0_bearing.Item2 - r1_bearing.Item1; //两条线路的角度差
                if (op_area<=0) //两条线路的扇形没有相交区域
                {
                    return new Tuple<double, int> (op_area,dis_radian);  //没有相交的话，返回相距角度差
                }else
                {
                    double uni_area = Math.Max(r0_bearing.Item2, r1_bearing.Item2) - r0_bearing.Item1;
                    return new Tuple<double, int> (op_area / uni_area,dis_radian); //相交的话，返回相交区域百分比
                }
            }

            if (r1_bearing.Item1<r0_bearing.Item1)
            {
                double op_area = r1_bearing.Item2 - r0_bearing.Item1;
                if (op_area<=0)
                {
                    return new Tuple<double, int>(op_area, dis_radian);
                }
                else
                {
                    double uni_area = Math.Max(r0_bearing.Item2, r1_bearing.Item2) - r1_bearing.Item1;
                    return new Tuple<double, int>(op_area / uni_area, dis_radian);
                }
            }
            return new Tuple<double, int>(op,dis_radian);
        }
        
    }
}
