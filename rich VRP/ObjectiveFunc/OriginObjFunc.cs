//using OP.Data;
//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace rich_VRP.ObjectiveFunc
//{
//    class OriginObjFunc
//    {
//        public Fleet fleet;

//        public double CalObjCost (Solution solution)
//        {
           
//            fleet = solution.fleet;

//			//全部成本
//			double totalCost = 0;
          
//            foreach (var veh in fleet.VehFleet) //遍历每一个被使用的车辆
//            {
//                veh.solution = solution;
//				totalCost += veh.calculCost();
//            }
//            solution.ObjVal = totalCost;
//            return totalCost;
//        }

//    }
//}
