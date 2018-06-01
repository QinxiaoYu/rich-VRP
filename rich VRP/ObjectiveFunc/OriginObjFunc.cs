using OP.Data;
using System;
using System.Collections.Generic;



namespace rich_VRP.ObjectiveFunc
{
    class OriginObjFunc
    {
        public Problem problem;
        public Fleet fleet;

        public double CalObjCost (Solution solution)
        {
            problem = solution.Problem;
            fleet = problem.fleet;

			//全部成本
			double totalCost = 0;
          
            foreach (var veh in fleet.VehFleet) //遍历每一个被使用的车辆
            {
				totalCost += veh.calculCost();
            }

            return totalCost;
        }
      

    }
}
