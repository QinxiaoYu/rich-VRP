using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace OP.Data
{


	public class Vehicle 
	{
        //////////////////////新增方法与属性/////////////////////////
        public double Late_time = 1440;//车辆最晚结束时间（分钟）24：00
        public double Early_time = 480;//车辆最早开始时间（分钟）8：00
        ///////////////////////////////////////////////////////////

        /// <summary>
        /// Gets or sets the type identifier.
        /// </summary>
        /// <value>The VehType identifier.</value>
        public int TypeId { get; set; }
		/// <summary>
		/// Gets or sets the veh identifier.
		/// </summary>
		/// <value>The veh identifier.</value>
		public string VehId{ get; set; }
        /// <summary>
        /// 某辆车跑过的所有路线的集合
        /// </summary>
		public List<String> VehRouteList;
		/// <summary>
		/// 
		/// </summary>
		/// <value>The dist sep.</value>
		public string dist_sep { get; set; }
		public string distribute_lea_tm { get; set; }
		public string distribute_arr_tm { get; set; }
		public double distance { get; set; }
		public double tran_cost { get; set; }
		public double charge_cost { get; set; }
		public double wait_cost { get; set; }
		public double total_cost { get; set; }
		public int charge_cnt { get; set; }
		public double fixed_use_cost { get; set;}

        //public Solution solution;

        /// <summary>
        /// 初始化一辆车的对象，该车只有车型，没有id
        /// </summary>
        /// <param name="_typeid"></param>
        public Vehicle(int _typeid)
        {
            this.TypeId = _typeid;
            this.VehId = "AbsVEH";
            VehRouteList = new List<string>();
        }

        /// <summary>
        /// 已知车型以及车辆id时候，初始一辆车
        /// </summary>
        /// <param name="_typeid"></param>
        /// <param name="_vehid"></param>
		public Vehicle(int _typeid, string _vehid)
		{
			this.TypeId = _typeid;
			this.VehId = _vehid;
			VehRouteList = new List<string>();
		}

		public void addRoute2Veh(Route _r)
		{
			this.VehRouteList.Add (_r.RouteId);
			 
			///to do 
			///update some infomation about this veh, e.g, the arrival time to the depot and the departure time from depots
			 
		}

		public int getNumofVisRoute()
		{
			return VehRouteList.Count;
		}
		//计算一辆车的各种成本
		
       
        public bool CheckNxtRoutesFeasible(int cur_route_pos, double delaytime, List<Route> routesList)
        {
            if (delaytime <= 0 || cur_route_pos >= getNumofVisRoute() - 1)
            {
                return true;
            }
            bool Feasible = false;
            int pos;
            //递归检查紧邻下游线路的浮动时间
            //Route nxt_route = solution.GetRouteByID(VehRouteList[cur_route_pos + 1], out pos);
            Route nxt_route = routesList[cur_route_pos + 1];
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
                if (CheckNxtRoutesFeasible(cur_route_pos + 1, tmp_nxt_route.GetArrivalTime() - nxt_route.GetArrivalTime(),routesList))
                {
                    Feasible = true;
                    nxt_route = tmp_nxt_route.Copy();
                }
            }
            return Feasible;
        }

        //public Vehicle Copy()
        //{
        //    Vehicle v = new Vehicle(this.TypeId, this.VehId);
        //    foreach (string r_id in this.VehRouteList)
        //    {
        //        v.VehRouteList.Add(r_id);
        //        //v.solution = this.solution;
        //    }
        //    //v.solution = solution;
        //    return v;
        //}

        public Vehicle Copy()
        {
            Vehicle v = new Vehicle(this.TypeId, this.VehId);
            foreach (string r_id in this.VehRouteList)
            {
                v.VehRouteList.Add(r_id);
                //v.solution = this.solution;
            }
            v.dist_sep = this.dist_sep;
            v.distribute_lea_tm = this.distribute_lea_tm;
            v.distribute_arr_tm = this.distribute_arr_tm;
            v.distance = this.distance;
            v.tran_cost = this.tran_cost;
            v.charge_cnt = this.charge_cnt;
            v.wait_cost = this.wait_cost;
            v.total_cost = this.total_cost;
            v.fixed_use_cost = this.fixed_use_cost;
            return v;
        }

        //指定给routes,给定vehicleType下计算成本
        public static double calculCost(List<Route> routes, int VehicleType)
        {
            double fixed_use_cost = Problem.VehTypes[VehicleType - 1].FixedCost;
            double TransCostRate = Problem.VehTypes[VehicleType - 1].VariableCost;
            double ChargeCostRate = Problem.VehTypes[VehicleType - 1].ChargeCostRate;
            double tran_cost = 0;
            double distance = 0;
            double wait_cost = 0;
            double charge_cost = 0;
            double charge_cnt = 0;
            double total_cost = 0;

            int Num_Trip_Veh = routes.Count;
            double WaitCost1 = Problem.WaitCostRate * (Num_Trip_Veh - 1) * Problem.MinWaitTimeAtDepot;
            int num_routes = routes.Count;
            if (num_routes == 0)
            {
                //Console.WriteLine(this.VehId.ToString());
            }
            for (int i = 0; i < num_routes; i++)
            {
                Route cur_route = routes[i];
                int num_nodes = cur_route.RouteList.Count;
                if (num_nodes == 2)
                {
                    //Console.WriteLine(this.VehId.ToString() + ";" + cur_route.RouteId + ";" + cur_route.RouteIndexofVeh.ToString());
                }
                var VariableCost = cur_route.routeCost(TransCostRate, ChargeCostRate); //计算单条线路上所有可变成本=等待成本2+运输成本+充电成本
                tran_cost += VariableCost.Item1;
                distance += VariableCost.Item1 / TransCostRate;
                wait_cost += VariableCost.Item2;
                charge_cost += VariableCost.Item3;
                charge_cnt += VariableCost.Item4;


            }
            wait_cost += WaitCost1;
            total_cost = wait_cost + tran_cost + charge_cost + fixed_use_cost;
            return total_cost;

        }
	
	}
	/// <summary>
	/// 车队管理类
	/// </summary>
	public class Fleet
	{
        //public Solution solution;
		public List<Vehicle> VehFleet;
        public int EverUsedVeh;
       
		public Fleet()
		{
			VehFleet = new List<Vehicle> ();
            //solution = null;
            EverUsedVeh = 0;
		}

		public int GetNumOfUsedVeh()
		{
			return VehFleet.Count;
		}
        /// <summary>
        /// Adds the new vehicle to VehFleet.
        /// </summary>
        /// <param name="_vehtypeid">VehTypeid.</param>
        public Vehicle addNewVeh(int _vehtypeid)
        {
            EverUsedVeh += 1;
            String veh_id = _vehtypeid.ToString() + "-" + EverUsedVeh.ToString();
            Vehicle veh = new Vehicle(_vehtypeid, veh_id);
            //veh.solution = this.solution;      
            VehFleet.Add(veh);
            return veh;
        }

        /// <summary>
        /// Removes a veh by its ID.
        /// </summary>
        /// <param name="_vehid">Vehid.</param>
        public void removeVeh(string _vehid)
		{
			Vehicle veh2remove = GetVehbyID (_vehid);
			VehFleet.Remove(veh2remove);
		}

        public int GetVehIdxInFleet(string veh_id)
        {
            for (int i = 0; i < VehFleet.Count; i++)
            {
                if (VehFleet[i].VehId==veh_id)
                {
                    return i;
                }
            }
            return -1;
        }

		/// <summary>
		/// Gets the number of vehicle of a particular type.
		/// </summary>
		/// <returns>The number of veh of type.</returns>
		/// <param name="_vehtypeid">VehTypeid.</param>
		public int GetNumOfVehOfType(int _vehtypeid)
		{
			int count = 0;
			foreach (var veh in VehFleet) {
				if (_vehtypeid==veh.TypeId) 
				{
					count++;
				}
			}
			return count;
		}
        /// <summary>
        /// 获得某个车型对应的所有车辆
        /// </summary>
        /// <param name="_vehtypeid">车型id</param>
        /// <returns></returns>
        public List<Vehicle> GetVehsOfType(int _vehtypeid)
        {
            List<Vehicle> vehs = new List<Vehicle>();
            foreach (var veh in VehFleet)
            {
                if (_vehtypeid == veh.TypeId)
                {
                    vehs.Add(veh);
                }
            }
            return vehs;
        }

		/// <summary>
		/// Gets the vehicle by its ID.
		/// </summary>
		/// <returns>vehicle</returns>
		/// <param name="_vehid">Veh's id.</param>
		public Vehicle GetVehbyID(string _vehid)
		{
			foreach (Vehicle veh in VehFleet) {
				if (veh.VehId == _vehid) {
					return veh;
				}
			}
			return null;
		}

        public Fleet Copy()
            {
            Fleet f = new Fleet();
            foreach (Vehicle veh in this.VehFleet)
            { 
                f.VehFleet.Add(veh.Copy());
            }
            return f;
        }

	}
}

