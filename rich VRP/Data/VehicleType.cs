﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace OP.Data
{
	public class VehicleType
	{
		public int VehTypeID { get; set; }
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
		public double MaxRange { get; set;}	

		/// <summary>
		/// Gets or sets the fixed cost.
		/// </summary>
		/// <value>The fixed cost.</value>
		public double FixedCost { get; set;}

		/// <summary>
		/// Gets or sets the variable cost.
		/// </summary>
		/// <value>The variable cost.</value>
		public double VariableCost {get;set;}

		/// <summary>
		/// Gets or sets the charge time.
		/// </summary>
		/// <value>The charge time.</value>
		public double ChargeTime { get; set;}
	}

	public class Vehicle 
	{
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

		public List<Route> VehRouteList;

		public Vehicle(int _typeid, int _vehid)
		{
			this.TypeId = _typeid;
			this.VehId = _vehid;
			VehRouteList = null;
		}

		public int addRoute2Veh(Route _r)
		{
			this.VehRouteList.Add (_r);
			///to do 
			///update some infomation about this veh, e.g, the arrival time to the depot and the departure time from depots
			 
		}

		public int getNumofVisRoute()
		{
			return VehRouteList.Count;
		}
	
	}
	/// <summary>
	/// 车队管理类
	/// </summary>
	public class Fleet
	{

		public List<Vehicle> VehFleet;

		public Fleet()
		{
			VehFleet = new List<Vehicle> ();
		}

		public int GetNumOfUsedVeh()
		{
			return VehFleet.Count;
		}
		/// <summary>
		/// Adds the new vehicle to VehFleet.
		/// </summary>
		/// <param name="_vehtypeid">VehTypeid.</param>
		public void addNewVeh(int _vehtypeid)
		{
			int numofVeh = GetNumOfUsedVeh;
			Vehicle veh = new Vehicle(_vehtypeid, numofVeh);
			VehFleet.Add (veh);		
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




	}
}

