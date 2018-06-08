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
            Console.WriteLine(r.RouteId);
            Routes.RemoveAt(idx_route_solution);

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
				result.AppendLine(veh.vehCostInf());
                string otherinfo = veh.vehOtherInfo();
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
