using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OP.Data;
using rich_VRP.Constructive;

namespace rich_VRP.Neighborhoods.Intra
{
    class TwoOpt
    {
		//进行路径间的交换，活动
		public static Route intraChange(Route originRoute){
			double ccr = 0;
			double tcr = 0;
			Route tempRoute = originRoute.Copy();
			Route BestRoute = new Route();
			double bestCost = originRoute.totalCost;
			for (int i = 1; i < originRoute.RouteList.Count-3; i++)
			{
				for (int j = i+1; j < originRoute.RouteList.Count-2; j++)
				{
					AbsNode temAbs = originRoute.RouteList[j];
					tempRoute.RouteList[j] = originRoute.RouteList[i];
					tempRoute.RouteList[i] = originRoute.RouteList[j];
					if(tempRoute.IsFeasible())
					{
						var multipleCostInfo = tempRoute.routeCost(tcr,ccr);
						double totalCost = multipleCostInfo.Item1 + multipleCostInfo.Item2 + multipleCostInfo.Item3;
						if (totalCost<bestCost)
						{
							BestRoute = tempRoute.Copy();
						}
					}
				}
			}
			return BestRoute;
		}
		public void intarChange(Fleet fleet)
		{

			foreach (var vehicle in fleet.VehFleet)
			{
				//attain the routes contains nodesinfo, including attain time,leave time, and service window
				var routes = from Route route in Fleet.solution.Routes where route.AssignedVeh.VehId == vehicle.VehId select route;
				foreach (var route in routes)
				{
					route = intraChange(route).Copy();
					
				}


				}

				
				
			}

		}

	}

    
}
