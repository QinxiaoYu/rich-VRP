using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Reflection;
namespace OP.Data
{
    public class Solution
    {

        public List<Route> Routes;
        public double ObjVal;
        public Fleet fleet;
        public List<Customer> UnVisitedCus;

        public Solution()
        {
            Routes = new List<Route>();
            fleet = new Fleet();
            ObjVal = 0.0;
        }
        public void AddRoute(Route route)
        {
            //Route newRoute = route.Copy();
            //newRoute.Solution = this;
            Routes.Add(route);
        }
        /// <summary>
        /// 在已知更新某条路线后，不会对下游线路造成不可行情况下，更新下游线路们的最早发车时间
        /// </summary>
        internal void UpdateTripChainTime()
        {
            foreach (Vehicle veh in fleet.VehFleet)
            {
                UpdateTripChainTime(veh);
            }
        }

        internal void UpdateTripChainTime(Vehicle veh)
        {
            int num_trips_veh = veh.getNumofVisRoute();
            int pos = -1;
            Route cur_route = GetRouteByID(veh.VehRouteList[0], out pos);
            Routes[pos].ServiceBeginingTimes[0] = veh.Early_time;
            double nxt_dt = Routes[pos].GetArrivalTime() + Problem.MinWaitTimeAtDepot;


            if (num_trips_veh > 1)
            {
                for (int i = 1; i < num_trips_veh; i++)
                {                                 
                    Route cur_route1 = GetRouteByID(veh.VehRouteList[i], out pos);
                    Routes[pos].ServiceBeginingTimes[0] = nxt_dt;
                    Routes[pos].UpdateServiceBeginningTimes();
                    nxt_dt = Routes[pos].GetArrivalTime() + Problem.MinWaitTimeAtDepot;
                }
            }
        }

        /// <summary>
        /// 从当前解中删除一条线路，并且更新车队中这条线路的信息
        /// </summary>
        /// <param name="r"></param>
        internal void Remove(Route r)
        {
            //定位当前线路r对应的车
            Vehicle veh = fleet.GetVehbyID(r.AssignedVeh.VehId);
            int idx_route_veh = r.RouteIndexofVeh; //定位当前路r排在车的第几个trip
            double nxt_dt = veh.Early_time;
            if (idx_route_veh > 0)
            {
                string pre_route_id = veh.VehRouteList[idx_route_veh - 1];
                int pre_route_idx_solution = -1;
                GetRouteByID(pre_route_id, out pre_route_idx_solution);
                nxt_dt = Routes[pre_route_idx_solution].GetArrivalTime() + Problem.MinWaitTimeAtDepot;
            }
            for (int i = idx_route_veh+1 ; i < veh.VehRouteList.Count; i++)//更新其后trip的属性
            {
                string nxt_route_id = veh.VehRouteList[i];
                int nxt_route_idx_solution = -1;
                GetRouteByID(nxt_route_id, out nxt_route_idx_solution);//定位其后trip所在解中的位置
                Routes[nxt_route_idx_solution].RouteIndexofVeh -= 1;
                Routes[nxt_route_idx_solution].AssignedVeh.VehRouteList.Remove(r.RouteId);
                Routes[nxt_route_idx_solution].ServiceBeginingTimes[0] = nxt_dt;
                Routes[nxt_route_idx_solution].UpdateServiceBeginningTimes();
                nxt_dt = Routes[nxt_route_idx_solution].GetArrivalTime() + Problem.MinWaitTimeAtDepot;

            }

            string veh_id = r.AssignedVeh.VehId;
            int idx_veh_fleet = fleet.GetVehIdxInFleet(veh_id);
            fleet.VehFleet[idx_veh_fleet].VehRouteList.Remove(r.RouteId);//更新车队中该车所存的routelists
            if (fleet.VehFleet[idx_veh_fleet].VehRouteList.Count==0)//如果该车没有其他路径，则删除该车
            {
                Console.WriteLine("Remove vehicle id =" + fleet.VehFleet[idx_veh_fleet].VehId);
                fleet.VehFleet.RemoveAt(idx_veh_fleet);
            }
            int idx_route_solution = Routes.FindIndex(a => a.RouteId == r.RouteId);
            Console.WriteLine("Remove route id = "+r.RouteId);
            Routes.RemoveAt(idx_route_solution);//从解中删除该路径
          
        }

        public Route GetRouteByID(string route_id, out int pos_inSolution)
        {
            for (int i = 0; i < Routes.Count; i++)
            {
                if (Routes[i].RouteId == route_id)
                {
                    pos_inSolution = i;
                    return Routes[i];
                }
            }
            pos_inSolution = -1;
            return null;
        }

        public double TotalDistance()
        {
            double totalDistance = 0;
            foreach (Route route in Routes)
                totalDistance += route.GetRouteLength();
            return totalDistance;
        }



        public string PrintToString()
        {
            string solution = "";
            if (this == null)
            {
                solution += "None";
                solution += "\r\n";
            }
            else
            {
                for (int i = 0; i < Routes.Count; ++i)
                {
                    solution += i.ToString(CultureInfo.InvariantCulture);
                    solution += ") ";
                    solution += Routes[i].PrintToStringSample() + "; ";
                    solution += "(dist: " + ((int)Routes[i].GetRouteLength()).ToString(CultureInfo.InvariantCulture) + ")";
                    solution += "\r\n";
                }
                solution += "\r\n";
                solution += "total distance: " + TotalDistance().ToString(CultureInfo.InvariantCulture);
            }
                      
            return solution;
        }

        public double CalObjCost()
        {
            //全部成本
            double totalCost = 0;

            foreach (var veh in fleet.VehFleet) //遍历每一个被使用的车辆
            {
                totalCost += calculCost(veh);
            }
            ObjVal = totalCost;
            return totalCost;
        }

        public Solution Copy()
        {
            Solution sol = new Solution();
            sol.ObjVal = ObjVal;
            foreach (Route route in Routes)
            {
                if (route.RouteList.Count >= 2)
                   sol.AddRoute(route.Copy());
            }          
            sol.fleet.EverUsedVeh = fleet.EverUsedVeh;
            foreach (Vehicle veh in fleet.VehFleet)
            {
                sol.fleet.VehFleet.Add(veh.Copy());
            }
            sol.UnVisitedCus = UnVisitedCus;            
            return sol;
        }

        /// <summary>
        /// 递归检查一条路线推迟到达终点后，对下游线路们的影响。如果下游线路都可行，顺便更新了下游线路的时间，返回true; 如果不可行，则整个返回false。
        /// </summary>
        /// <param name="cur_route_pos">当前线路所在位置</param>
        /// <param name="delaytime">延误时长</param>
        /// <returns></returns>
        public bool CheckNxtRoutesFeasible(Vehicle veh, int cur_route_pos, double delaytime)
        {
            if (delaytime <= 0 || cur_route_pos >= veh.getNumofVisRoute() - 1)
            {
                return true;
            }
            bool Feasible = false;
            int pos;
            //递归检查紧邻下游线路的浮动时间
            Route nxt_route = GetRouteByID(veh.VehRouteList[cur_route_pos + 1], out pos); //定位下游线路的在解中的位置
            Route tmp_nxt_route = nxt_route.Copy(); //拷贝，将在此拷贝上做更改
            for (int i = 0; i < tmp_nxt_route.RouteList.Count; i++)
            {
                if (i == 0)
                {
                    tmp_nxt_route.ServiceBeginingTimes[i] += delaytime; //下游线路起点出发时间顺延 delaytime
                }
                else
                {
                    tmp_nxt_route.ServiceBeginingTimes[i] = tmp_nxt_route.ServiceBeginingTimes[i - 1] //下游线路中商户的开始服务时间依次更新
                                                          + tmp_nxt_route.RouteList[i - 1].Info.ServiceTime
                                                          + tmp_nxt_route.RouteList[i - 1].TravelDistance(tmp_nxt_route.RouteList[i]);
                }
            }
            if (tmp_nxt_route.IsFeasible()) //检查服务时间更新后，下游线路是否还可行
            {
                if (CheckNxtRoutesFeasible(veh, cur_route_pos + 1, tmp_nxt_route.GetArrivalTime() - nxt_route.GetArrivalTime()))
                {
                    Feasible = true; //如果下游线路都可行
                    Routes[pos] = tmp_nxt_route.Copy(); //更新解中该条线路
                }
            }
            return Feasible;
        }

        /// <summary>
        /// 输出当前解中某一车辆的非赛题需要信息，如路线上每个点的剩余电量、累计行程、载重量、体积
        /// </summary>
        /// <param name="veh"></param>
        /// <returns></returns>
        internal string vehOtherInfo(Vehicle veh)
        {
            VehicleType thisvt = Problem.GetVehTypebyID(veh.TypeId);
            List<int> CurtourLength = new List<int>();
            List<int> CurWaitTime = new List<int>();
            List<int> CurBattery = new List<int>();
            List<double> CurWeight = new List<double>();
            List<double> CurVolumn = new List<double>();
            int AccumutourLenght = 0;
            int AccumuBattery = 0;
            double AccumuWeight = 0;
            double AccumuVolume = 0;

            foreach (var item in veh.VehRouteList)
            {
                int pos;
                Route cur_route = GetRouteByID(item, out pos); //定位线路
                for (int i = 0; i < cur_route.RouteList.Count; i++)
                {
                    if (i == 0)
                    {
                        CurtourLength.Add(0);
                        CurWaitTime.Add(0);
                        CurBattery.Add((int)thisvt.MaxRange);
                        CurWeight.Add(0);
                        CurVolumn.Add(0);
                        AccumutourLenght = 0;
                        AccumuBattery = (int)thisvt.MaxRange;
                        AccumuVolume = 0;
                        AccumuWeight = 0;
                    }
                    else
                    {
                        AccumutourLenght += cur_route.RouteList[i].TravelDistance(cur_route.RouteList[i - 1]); //到该点时到累计行程
                        CurtourLength.Add(AccumutourLenght);
                        int arrivetime = (int)(cur_route.ServiceBeginingTimes[i - 1] + cur_route.RouteList[i - 1].Info.ServiceTime + cur_route.RouteList[i].TravelTime(cur_route.RouteList[i - 1]));
                        CurWaitTime.Add((int)Math.Max(0, cur_route.ServiceBeginingTimes[i] - arrivetime));
                        if (cur_route.RouteList[i].Info.Type == 3) //充电站
                        {
                            AccumuBattery = (int)thisvt.MaxRange;
                        }
                        else
                        {
                            AccumuBattery -= cur_route.RouteList[i].TravelDistance(cur_route.RouteList[i - 1]);
                        }

                        CurBattery.Add(AccumuBattery);
                        AccumuWeight += cur_route.RouteList[i].Info.Weight;
                        AccumuVolume += cur_route.RouteList[i].Info.Volume;
                        CurWeight.Add(AccumuWeight);
                        CurVolumn.Add(AccumuVolume);
                    }

                }
            }
            string Str_CurtourLength = string.Join(";", CurtourLength);
            string Str_CurWaitTime = string.Join(";", CurWaitTime);
            string Str_CurBattery = string.Join(";", CurBattery);
            string Str_CurWeight = string.Join(";", CurWeight);
            string Str_CurVolumn = string.Join(";", CurVolumn);
            string Str_Otherinfo = Str_CurtourLength + "," + Str_CurWaitTime + "," + Str_CurBattery + "," + Str_CurWeight + "," + Str_CurVolumn;
            return Str_Otherinfo;
        }

        /// <summary>
        /// 获得当前解中某一车辆的赛题需要信息，将这些信息更新到车的属性上
        /// </summary>
        /// <param name="veh"></param>
        private void GetvehRoutesInfo(Vehicle veh)
        {

            double dt_veh = double.MaxValue;
            double at_veh = double.MinValue;
            List<string> nodes_id = new List<string>();
            int num_routes = veh.getNumofVisRoute();

            foreach (var item in veh.VehRouteList)
            {
                int pos;
                Route cur_route = GetRouteByID(item, out pos); //定位线路
                for (int i = 0; i < cur_route.RouteList.Count - 1; i++)
                {
                    nodes_id.Add(cur_route.RouteList[i].Info.Id.ToString());
                }

                double at_cur = cur_route.GetArrivalTime();
                double dt_cur = cur_route.GetDepartureTime();
                if (cur_route.RouteIndexofVeh==0)
                {
                    dt_cur = cur_route.ServiceBeginingTimes[1] - cur_route.RouteList[1].TravelTime(cur_route.RouteList[0]);
                }
                
                if (dt_cur < dt_veh)
                {
                    dt_veh = dt_cur;
                }
                if (at_cur > at_veh)
                {
                    at_veh = at_cur;
                }
            }
            nodes_id.Add(0.ToString());
            veh.dist_sep = string.Join(";", nodes_id.ToArray());
            veh.distribute_lea_tm = string.Format("{0}:{1}", ((int)dt_veh / 60).ToString(), (dt_veh % 60).ToString());
            veh.distribute_arr_tm = string.Format("{0}:{1}", ((int)at_veh / 60).ToString(), (at_veh % 60).ToString());

        }

        /// <summary>
        /// 打印一辆车的各种信息
        /// </summary>
        /// <param name="veh"></param>
        /// <returns></returns>
        public string vehCostInf(Vehicle veh)
        {

            string costInfs = "";
            GetvehRoutesInfo(veh);
            costInfs = veh.VehId + "," + veh.TypeId + "," + veh.dist_sep + "," + veh.distribute_lea_tm + "," + veh.distribute_arr_tm + "," + veh.distance + "," + veh.tran_cost.ToString("0.00") + "," + veh.charge_cost + "," + veh.wait_cost.ToString("0.00") + "," + veh.fixed_use_cost + "," + veh.total_cost.ToString("0.00") + "," + veh.charge_cnt;
            return costInfs;
        }
        /// <summary>
        /// 计算当前解中某一辆车的所有费用，包括固定成本与可变成本
        /// </summary>
        /// <param name="veh"></param>
        /// <returns></returns>
        public double calculCost(Vehicle veh)
        {
            veh.ResetCost();
            int TypeId = veh.TypeId;
            veh.fixed_use_cost = Problem.VehTypes[TypeId - 1].FixedCost;
            double TransCostRate = Problem.VehTypes[TypeId - 1].VariableCost;
            double ChargeCostRate = Problem.VehTypes[TypeId - 1].ChargeCostRate;
            int Num_Trip_Veh = veh.getNumofVisRoute();
            double WaitCost1 = Problem.WaitCostRate * (Num_Trip_Veh - 1) * Problem.MinWaitTimeAtDepot;
            int num_routes = veh.VehRouteList.Count;
            if (num_routes == 0)
            {
                Console.WriteLine("Empty veh:" + veh.VehId.ToString());
                return 0;
            }
            for (int i = 0; i < veh.VehRouteList.Count; i++)
            {
                int pos=0;
                Route cur_route = GetRouteByID(veh.VehRouteList[i], out pos); //定位路线
                int num_nodes = cur_route.RouteList.Count;
                if (num_nodes == 2)
                {
                    Console.WriteLine("Empty Route In Calculate a route objective: " + veh.VehId.ToString() + ";" + cur_route.RouteId + ";" + cur_route.RouteIndexofVeh.ToString());
                }
                var VariableCost = cur_route.routeCost(TransCostRate, ChargeCostRate); //计算单条线路上所有可变成本=等待成本2+运输成本+充电成本
                veh.tran_cost += VariableCost.Item1;
                veh.distance += VariableCost.Item1 / TransCostRate;
                veh.wait_cost += VariableCost.Item2;
                veh.charge_cost += VariableCost.Item3;
                veh.charge_cnt += VariableCost.Item4;


            }
            veh.wait_cost += WaitCost1;
            veh.total_cost = veh.wait_cost + veh.tran_cost + veh.charge_cost + veh.fixed_use_cost;           
            return veh.total_cost;

        }
      

        public List<Route> Copy(List<Route> routes)
        {
            var newRoutes = new List<Route>(routes.Count);
            newRoutes.AddRange(routes.Select(route => route.Copy()));
            return newRoutes;
        }

        internal void UpdateFirstTripTime()
        {
            foreach (Route trip in this.Routes)
            {
                if (trip.RouteIndexofVeh ==0)
                {
                    trip.UpdateDepartureTime();
                }

            }
        }

        public bool SolutionIsFeasible()
        {
            foreach (Route route in Routes)
            {
                if (!route.IsFeasible())
                {
                    int range =  route.ViolationOfRange();
                    int time = route.ViolationOfTimeWindow();
                    double weight = route.ViolationOfWeight();
                    double volume = route.ViolationOfVolume();
                    string txt = string.Format("Range: {0}; Time:{1}; Weight: {2}; Volume:{3}",range,time,weight.ToString("0.00"),volume.ToString("0.00")); 
                    System.Console.WriteLine(route.RouteId+" is not feasible. "+txt);
                    
                    return false;
                   
                }
            }
            return true;
        }
		public void PrintResult()
		{
			StringBuilder result = new StringBuilder("");//初始化空的可变长字符串
			String[] columns = { "trans_code", "vehicle_type", "dist_seq", "distribute_lea_tm", "distribute_arr_tm", "distance", "trans_cost", "charge_cost", "wait_cost", "fixed_use_cost", "total_cost", "charge_cnt" };
            //create trans_code_dict that containts routeID
            StringBuilder result_otherinfo = new StringBuilder();
            string[] columns_otherinfo = { "acc_range","waittime","acc_battery","acc_weight","acc_volumn"};
			string title = String.Join(",", columns);
            string title_otherinfo = String.Join(",", columns_otherinfo);
            result.AppendLine(title);
            result_otherinfo.AppendLine(title_otherinfo);
			foreach (var veh in fleet.VehFleet)
			{				
				result.AppendLine(vehCostInf(veh));
                string otherinfo = vehOtherInfo(veh);
                result_otherinfo.AppendLine(otherinfo);
            }
			//string result_s = result.ToString();
			//生成文件名称
			//获取当前时间

			DateTime time = DateTime.Now;
			string path =  ".//reslut" + time.Month.ToString() + time.Day.ToString() + time.Hour.ToString()+time.Minute.ToString()+time.Second.ToString()+time.Millisecond.ToString() + ".csv";
            string path_otherinfo = ".//other_reslut" + time.Month.ToString() + time.Day.ToString() + time.Hour.ToString() + time.Minute.ToString() + time.Second.ToString() + time.Millisecond.ToString() + ".csv";
			using (System.IO.StreamWriter file = new System.IO.StreamWriter(path))
			{
				file.Write(result);
                file.Flush();
                file.Close();
			}
            using (System.IO.StreamWriter file_ohterinfo = new System.IO.StreamWriter(path_otherinfo))
            {
                file_ohterinfo.Write(result_otherinfo);
                file_ohterinfo.Flush();
                file_ohterinfo.Close();
            }
        }
        public void printCheckSolution()
        {
            foreach (Route route in Routes)
            {
                string txt = "";
                txt += route.RouteId + "  " + route.AssignedVeh.VehId + " ";
                //Console.Write(route.RouteId + "  " + route.AssignedVeh.VehId + " ");
                bool isInVeh = false;
                foreach (string routeid in route.AssignedVeh.VehRouteList)
                {
                    if (routeid == route.RouteId)
                    {
                        isInVeh = true;
                    }
                    txt += routeid + "  ";
                    //Console.Write(routeid + "  ");
                }
                if (isInVeh == false)
                {
                    txt += "Route not in Veh.Routes=======\n";
                    Console.Write(txt);
                }
             
            }

            foreach (Vehicle veh in fleet.VehFleet)
            {
                string txt_veh = "";
                foreach (string routeid in veh.VehRouteList)
                {
                    int pos = -1;
                    GetRouteByID(routeid, out pos);
                    if (pos == -1)
                    {
                        txt_veh += veh.VehId + "   " + routeid + "not in solution\n";
                        Console.Write(txt_veh);
                    }
                }
            }
        }     
    
}
}
