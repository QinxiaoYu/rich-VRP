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
            fleet.solution = this;
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
            if (num_trips_veh > 1)
            {
                for (int i = 1; i < num_trips_veh; i++)
                {
                    int pos;
                    Route cur_route = GetRouteByID(veh.VehRouteList[i], out pos);
                    double new_departure_cur = cur_route.GetEarliestDepartureTime();
                    cur_route.ServiceBeginingTimes[0] = new_departure_cur;
                }
            }
        }

        /// <summary>
        /// 从当前解中删除一条线路，并且更新车队中这条线路的信息
        /// </summary>
        /// <param name="r"></param>
        internal void Remove(Route r)
        {
            Vehicle veh = fleet.GetVehbyID(r.AssignedVeh.VehId);
            int idx_route_veh = r.RouteIndexofVeh;
            for (int i = idx_route_veh+1 ; i < veh.VehRouteList.Count; i++)
            {
                string nxt_route_id = veh.VehRouteList[i];
                int nxt_route_idx_solution = -1;
                GetRouteByID(nxt_route_id, out nxt_route_idx_solution);
                Routes[nxt_route_idx_solution].RouteIndexofVeh -= 1;
            }

            string veh_id = r.AssignedVeh.VehId;
            int idx_veh_fleet = fleet.GetVehIdxInFleet(veh_id);
            fleet.VehFleet[idx_veh_fleet].VehRouteList.Remove(r.RouteId);
            if (fleet.VehFleet[idx_veh_fleet].VehRouteList.Count==0)
            {
                fleet.VehFleet.RemoveAt(idx_veh_fleet);
            }
            int idx_route_solution = Routes.FindIndex(a => a.RouteId == r.RouteId);
            Console.WriteLine("Remove route id = "+r.RouteId);
            Routes.RemoveAt(idx_route_solution);
            fleet.solution = this;
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
                totalCost += veh.calculCost();
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
            sol.fleet.solution = this;
            sol.fleet.EverUsedVeh = fleet.EverUsedVeh;
            foreach (Vehicle veh in fleet.VehFleet)
            {
                sol.fleet.VehFleet.Add(veh.Copy(sol.fleet.solution));
            }            
            return sol;
        }

        /// <summary>
        /// 递归检查一条路线推迟到达终点后，对下游线路们的影响。如果下游线路都可行，顺便更新了下游线路的时间，返回true; 如果不可行，则整个返回false。
        /// </summary>
        /// <param name="cur_route_pos">当前线路所在位置</param>
        /// <param name="delaytime">延误时长</param>
        /// <returns></returns>
        public bool CheckNxtRoutesFeasible(Solution solution, Vehicle veh, int cur_route_pos, double delaytime)
        {
            if (delaytime <= 0 || cur_route_pos >= veh.getNumofVisRoute() - 1)
            {
                return true;
            }
            bool Feasible = false;
            int pos;
            //递归检查紧邻下游线路的浮动时间
            Route nxt_route = solution.GetRouteByID(veh.VehRouteList[cur_route_pos + 1], out pos);
            Route tmp_nxt_route = nxt_route.Copy();
            for (int i = 0; i < tmp_nxt_route.RouteList.Count; i++)
            {
                if (i == 0)
                {
                    tmp_nxt_route.ServiceBeginingTimes[i] += delaytime;
                }
                else
                {
                    tmp_nxt_route.ServiceBeginingTimes[i] = tmp_nxt_route.ServiceBeginingTimes[i - 1]
                                                          + tmp_nxt_route.RouteList[i - 1].Info.ServiceTime
                                                          + tmp_nxt_route.RouteList[i - 1].TravelDistance(tmp_nxt_route.RouteList[i]);
                }
            }
            if (tmp_nxt_route.IsFeasible())
            {
                if (CheckNxtRoutesFeasible(solution, veh, cur_route_pos + 1, tmp_nxt_route.GetArrivalTime() - nxt_route.GetArrivalTime()))
                {
                    Feasible = true;
                    nxt_route = tmp_nxt_route.Copy();
                }
            }
            return Feasible;
        }


        internal string vehOtherInfo(Solution solution, Vehicle veh)
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
                Route cur_route = solution.GetRouteByID(item, out pos);
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

        private void GetvehRoutesInfo(Solution solution, Vehicle veh)
        {

            double dt_veh = double.MaxValue;
            double at_veh = double.MinValue;
            List<string> nodes_id = new List<string>();
            int num_routes = veh.getNumofVisRoute();

            foreach (var item in veh.VehRouteList)
            {
                int pos;
                Route cur_route = solution.GetRouteByID(item, out pos);
                for (int i = 0; i < cur_route.RouteList.Count - 1; i++)
                {
                    nodes_id.Add(cur_route.RouteList[i].Info.Id.ToString());
                }

                double at_cur = cur_route.GetArrivalTime();
                double dt_cur = cur_route.GetDepartureTime();
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

        //打印一辆车的各种信息
        public string vehCostInf(Vehicle veh)
        {

            string costInfs = "";
            veh.GetvehRoutesInfo();
            costInfs = veh.VehId + "," + veh.TypeId + "," + veh.dist_sep + "," + veh.distribute_lea_tm + "," + veh.distribute_arr_tm + "," + veh.distance + "," + veh.tran_cost.ToString("0.00") + "," + veh.charge_cost + "," + veh.wait_cost.ToString("0.00") + "," + veh.fixed_use_cost + "," + veh.total_cost.ToString("0.00") + "," + veh.charge_cnt;
            return costInfs;
        }

        public double calculCost(Vehicle veh)
        {
            ResetCost(veh);
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
                int pos;
                Route cur_route = solution.GetRouteByID(veh.VehRouteList[i], out pos);
                int num_nodes = cur_route.RouteList.Count;
                if (num_nodes == 2)
                {
                    Console.WriteLine("Empty Route: " + veh.VehId.ToString() + ";" + cur_route.RouteId + ";" + cur_route.RouteIndexofVeh.ToString());
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

        private void ResetCost(Vehicle veh)
        {
            veh.distance = 0;
            veh.tran_cost = 0;
            veh.wait_cost = 0;
            veh.charge_cost = 0;
            veh.charge_cnt = 0;
            veh.total_cost = 0;
            veh.fixed_use_cost = 0;
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
    
}
}
