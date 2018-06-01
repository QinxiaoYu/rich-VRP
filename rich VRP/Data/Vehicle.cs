using System;
using System.Collections;
using System.Collections.Generic;

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
		public List<string> VehRouteList;

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

