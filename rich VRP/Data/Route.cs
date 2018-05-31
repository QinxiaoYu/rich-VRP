using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Collections;

namespace OP.Data
{
    public class Route
    {


        public Problem Problem;
            
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
        /// <summary>
        /// 在初始化中判断这条线路是否还可以插入
        /// </summary>
        bool isOpen = true;

        public Route(Problem problem, VehicleType vehtype)
        {
            Depot startdepot = problem.StartDepot;
            Depot enddepot = problem.EndDepot;
            Problem = problem;
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
        public Route(Problem problem, Vehicle veh)
        {
            Depot startdepot = problem.StartDepot;
            Depot enddepot = problem.EndDepot;
            Problem = problem;
            AssignedVeh = veh;
            
            //Solution = null;
            RouteList = new List<AbsNode>();
            ServiceBeginingTimes = new List<double>();
            battery_level = new List<double>();

            int numRouteofVeh = veh.VehRouteList.Count(); //当前车辆已经行驶的趟数
            this.RouteIndexofVeh = numRouteofVeh;

            AddNode(startdepot);
            AddNode(enddepot);
        }

        internal void UpdateDepartureTime()
        {
            
            double departuretime = this.ServiceBeginingTimes[0] + this.RouteList[0].Info.ServiceTime;
            double arrivetime = departuretime + this.RouteList[0].TravelTime(RouteList[1]);
            double waittime_firstcus = Math.Max(0, ServiceBeginingTimes[1] - arrivetime);
            if (waittime_firstcus>0)
            {
                this.ServiceBeginingTimes[0] = this.ServiceBeginingTimes[0] + waittime_firstcus;
            }
            
        }



        /// <summary>
        /// 初始化一条路径，该路径只包含一个种子顾客
        /// </summary>
        /// <param name="problem"></param>
        /// <param name="seedCustomer">已选的种子顾客</param>
        public Route(Problem problem, Customer seedCustomer)
        {
            Depot startdepot = problem.StartDepot;
            Depot enddepot = problem.EndDepot;
            Problem = problem;
            //Solution = null;
            RouteList = new List<AbsNode>();
            ServiceBeginingTimes = new List<double>();
            battery_level = new List<double>();
            AddNode(startdepot);
            AddNode(seedCustomer);
            AddNode(enddepot);
        }

        /// <summary>
        /// 把一条线路分配给一辆具体的车
        /// </summary>
        /// <param name="veh"></param>
        public void RouteAssign2Veh(Vehicle veh)
        {
            this.AssignedVeh = veh;
            int numRouteofVeh = veh.VehRouteList.Count(); //当前车辆已经行驶的趟数
            this.RouteIndexofVeh = numRouteofVeh;


        }
        /// <summary>
        /// 计算当前路径能从起点出发的最早时刻
        /// </summary>
        /// <returns></returns>
        public double GetEarliestDepartureTime()
        {
            if (this.RouteIndexofVeh==0) //如果该线路为首趟线路，则其最早开始时间既为起点上班时间
            {
                return Problem.StartDepot.Info.ReadyTime;
            }
            else //如果该线路非首趟线路，则其最早开始时间依赖于其前一趟的结束时间
            {
                double ArrivalTimeofpreRoute = this.AssignedVeh.VehRouteList[this.RouteIndexofVeh - 1].GetArrivalTime();
                return ArrivalTimeofpreRoute + Problem.MinWaitTimeAtDepot;
            }
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

        internal int FindGoodStationPosition(int rock_position, out Station goodSta)
        {
            goodSta = null;
            int best_position_sta = -1;
            for (int i = 1; i < rock_position; i++)
            {

            }
            return best_position_sta;
        }
        /// <summary>
        /// 获得某点处的浮动时间=该点处的等待时长
        /// </summary>
        /// <param name="i">该点在线路中的位置</param>
        /// <returns></returns>
        internal double GetFloatTimeAtCus(int i)
        {
            double floattime = 960.0;
            if (RouteList[i].Info.Type==2)//充电站
            {
                double arrivetime = ServiceBeginingTimes[i - 1] + RouteList[i - 1].Info.ServiceTime + RouteList[i - 1].TravelTime(RouteList[i]);
                double servicestarttime = ServiceBeginingTimes[i];
                floattime = servicestarttime - arrivetime;
            }
            return floattime;
        }

        /// <summary>
        /// 插入新节点到路径末尾，并更新路径各节点的服务时间；
        /// </summary>
        /// <param name="newNode">抽象节点，可以是顾客也可是仓库</param>
        private void AddNode(AbsNode newNode)
        {
            //线路上最后一个点
            AbsNode lastCustomer = RouteList.Count == 0 ? newNode : RouteList[RouteList.Count - 1];
            //线路上最后一个点 可以开始游览的时间 （如果到达时间早于时间窗的开始时间，则为时间窗开始时间；否则为实际达到时间）
            double lastServiceTime = RouteList.Count == 0 ? Math.Max(GetEarliestDepartureTime(), newNode.Info.ReadyTime) : ServiceBeginingTimes[ServiceBeginingTimes.Count - 1];
            
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



        internal void InsertCustomer(List<Customer> unroutedCustomers)
        {
            
            for (int i = 0; i < unroutedCustomers.Count; i++)
            {
                Customer cusToInsert = unroutedCustomers.ElementAt(i);
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
            double CapacityVolume = Problem.fleet.GetVehTypebyID(vehType).Volume;
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
            double CapacityWeight = Problem.fleet.GetVehTypebyID(vehType).Weight;
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
                double dis_ij = RouteList[i].TravelDistance(RouteList[i + 1]);
                currentRange -= dis_ij; //达到j点时的剩余里程

                if (currentRange<0) //如果达到j点时的剩余里程小于0，则返回j点所在点位置
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
            double CapacityRange = Problem.fleet.GetVehTypebyID(vehType).MaxRange;
            return CapacityRange;
        }

        /// <summary>
        /// 路径的长度，或服务时间
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
            var r = new Route(Problem, this.AssignedVeh)
            {
                AssignedVehType = this.AssignedVehType,
                RouteList = newRouteList,
                ServiceBeginingTimes = new List<double>(ServiceBeginingTimes),
                battery_level = new List<double>(battery_level),
                RouteIndexofVeh = this.RouteIndexofVeh,

            };
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
            return true;
        }
        
    }
}
