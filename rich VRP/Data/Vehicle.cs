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
		public int VehId{ get; set; }
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

        public Solution solution;

        public Vehicle(int _typeid)
        {
            this.TypeId = _typeid;
            this.VehId = -1;
            VehRouteList = new List<string>();
        }

        /// <summary>
        /// 已知车型以及车辆id时候，初始一辆车
        /// </summary>
        /// <param name="_typeid"></param>
        /// <param name="_vehid"></param>
		public Vehicle(int _typeid, int _vehid)
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
		public double calculCost()
		{
            ResetCost();
          
			fixed_use_cost = this.solution.problem.VehTypes[TypeId - 1].FixedCost;
			double TransCostRate = this.solution.problem.VehTypes[TypeId-1].VariableCost;
			double ChargeCostRate = this.solution.problem.VehTypes[TypeId-1].ChargeCostRate;
			int Num_Trip_Veh = getNumofVisRoute();
			double WaitCost1 = Problem.WaitCostRate * (Num_Trip_Veh - 1) * Problem.MinWaitTimeAtDepot;
            int num_routes = this.VehRouteList.Count;
            if (num_routes==0)
            {
                Console.WriteLine(this.VehId.ToString());
            }
            for (int i = 0; i<VehRouteList.Count; i++)
            {
                Route cur_route = this.solution.GetRouteByID(VehRouteList[i]);
                int num_nodes = cur_route.RouteList.Count;
                if (num_nodes==2)
                {
                    Console.WriteLine(this.VehId.ToString() + ";" + cur_route.RouteId + ";" + cur_route.RouteIndexofVeh.ToString());
                }
                var VariableCost = cur_route.routeCost(TransCostRate,ChargeCostRate); //计算单条线路上所有可变成本=等待成本2+运输成本+充电成本
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

        private void ResetCost()
        {
            distance = 0;
            tran_cost = 0;
            wait_cost = 0;
            charge_cost = 0;
            charge_cnt = 0;
            total_cost = 0;
            fixed_use_cost = 0;
        }

        //打印一辆车的各种信息
        public string vehCostInf()
		{

			string costInfs = "";
            GetvehRoutesInfo();    
			costInfs = VehId + "," + TypeId + "," + dist_sep + "," + distribute_lea_tm + "," + distribute_arr_tm + "," + distance + "," + tran_cost.ToString("0.00") + "," + charge_cost + "," + wait_cost.ToString("0.00") + "," + fixed_use_cost+","+total_cost.ToString("0.00")+","+charge_cnt;
			return costInfs;
		}

        private void GetvehRoutesInfo()
        {
           
            double dt_veh = double.MaxValue;
            double at_veh = double.MinValue;
            List<string> nodes_id = new List<string>();
            int num_routes = this.getNumofVisRoute();

            foreach (var item in VehRouteList)
            {
                Route cur_route = this.solution.GetRouteByID(item);
                for (int i = 0; i < cur_route.RouteList.Count-1; i++)
                {
                    nodes_id.Add(cur_route.RouteList[i].Info.Id.ToString());
                }
                
                double at_cur = cur_route.GetArrivalTime();
                double dt_cur = cur_route.GetDepartureTime();
                if (dt_cur<dt_veh)
                {
                    dt_veh = dt_cur;
                }
                if (at_cur>at_veh)
                {
                    at_veh = at_cur;
                }
            }
            nodes_id.Add(0.ToString());
            dist_sep = string.Join(";", nodes_id.ToArray());
            distribute_lea_tm = string.Format("{0}:{1}", ((int)dt_veh / 60).ToString(), (dt_veh % 60).ToString());
            distribute_arr_tm = string.Format("{0}:{1}", ((int)at_veh / 60).ToString(), (at_veh % 60).ToString());

        }

        public Vehicle Copy()
        {
            Vehicle v = new Vehicle(this.TypeId, this.VehId);
            foreach (string r_id in this.VehRouteList)
            {
                v.VehRouteList.Add(r_id);
            }
            return v;
        }
	
	}
	/// <summary>
	/// 车队管理类
	/// </summary>
	public class Fleet
	{
        public Solution solution;
		public List<Vehicle> VehFleet;
       
		public Fleet()
		{
			VehFleet = new List<Vehicle> ();
            solution = null;  
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
            int numofVeh = GetNumOfUsedVeh();
            Vehicle veh = new Vehicle(_vehtypeid, numofVeh);
            veh.solution = this.solution;      
            VehFleet.Add(veh);
            return veh;
        }

        /// <summary>
        /// Removes a veh by its ID.
        /// </summary>
        /// <param name="_vehid">Vehid.</param>
        public void removeVeh(int _vehid)
		{
			Vehicle veh2remove = GetVehbyID (_vehid);
			VehFleet.Remove (veh2remove);
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
		public Vehicle GetVehbyID(int _vehid)
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

