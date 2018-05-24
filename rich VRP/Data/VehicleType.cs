using System;
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
		public int TypeId;
		public List<Route> VehRouteList;

		public Vehicle(int _typeid)
		{
			this.TypeId = _typeid;
			VehRouteList = null;
		}

		public int addRoute2Veh(Route _r)
		{
			this.VehRouteList.Add (_r);
			///to do 
			///update some infomation about this veh, e.g, the arrival time to the depot and the departure time from depots
			 
		}

		public int getNumof
	
	}
}

