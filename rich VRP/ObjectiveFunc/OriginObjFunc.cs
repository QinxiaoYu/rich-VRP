﻿using OP.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace rich_VRP.ObjectiveFunc
{
    class OriginObjFunc
    {
        public Problem problem;
        public Fleet fleet;

        public double CalObjCost (Solution solution,StringBuilder sb = null)
        {
            problem = solution.problem;
            fleet = solution.fleet;
           
            //1.车辆使用成本
            double FixedCost = 0;
            foreach (var vehtype in problem.VehTypes) 
            {
                int Num_VehsofType = fleet.GetVehsOfType(vehtype.VehTypeID).Count; //每种车型使用的车辆数
                FixedCost += vehtype.FixedCost * Num_VehsofType;
            }
            
            double VariableCost = 0;
            //2. 车辆等待成本，包含 1)多次往返车辆在配送中心的等待成本;2)在商户处的等待成本。（在充电站不算等待成本）
            //double WaitCost = 0;
            //3. 车辆运输成本
            //double TransCost = 0;
            //4. 车辆充电成本
            //double ChargeCost = 0;

            foreach (var veh in fleet.VehFleet) //遍历每一个被使用的车辆
            {
                int Num_Trip_Veh = veh.getNumofVisRoute();
                double WaitCost1 = problem.WaitCostRate * (Num_Trip_Veh - 1)*problem.MinWaitTimeAtDepot;
                
                for (int i = 0; i < veh.VehRouteList.Count; i++)
                {
                    Route ri_veh = solution.GetRouteByID(veh.VehRouteList[i]);
                    double VariableCost_Veh = CalObjVarCost(ri_veh,sb); //计算单条线路上所有可变成本=等待成本2+运输成本+充电成本
                    VariableCost += VariableCost_Veh;       
                }
                VariableCost += WaitCost1;
            }

            return FixedCost+VariableCost;
        }
        /// <summary>
        /// 计算一条路径上的所有可变成本=商户出等待成本+运输成本+充电成本
        /// </summary>
        /// <param name="route"></param>
        /// <returns></returns>
        public double CalObjVarCost(Route route,StringBuilder sb=null)
        {  
            //2. 车辆等待成本，包含 2)在商户处的等待成本。（??在充电站不算等待成本）
            double WaitCost = 0;
            //3. 车辆运输成本
            double TransCost = 0;
            double TransCostRate = problem.GetVehTypebyID(route.AssignedVeh.TypeId).VariableCost;
            //4. 车辆充电成本
            double ChargeCost = 0;
            double ChargeCostRate = problem.GetVehTypebyID(route.AssignedVeh.TypeId).ChargeCostRate;
            for (int i = 1; i < route.RouteList.Count; i++)
            {
                //等待成本
                double AT_i = route.ServiceBeginingTimes[i - 1] + route.RouteList[i - 1].Info.ServiceTime + route.RouteList[i - 1].TravelTime(route.RouteList[i]);
                double WT_i = Math.Max(route.ServiceBeginingTimes[i] - AT_i, 0);
                WaitCost += WT_i * problem.WaitCostRate;
                

                //运输成本
                double Distance_ij = route.RouteList[i - 1].TravelDistance(route.RouteList[i]);
                TransCost += TransCostRate * Distance_ij;

                //充电成本
                if (route.RouteList[i].Info.Type == 3)
                {
                    ChargeCost += ChargeCostRate * route.RouteList[i].Info.ServiceTime;
                }

            }
            string txt = string.Format("vehid = {0}, waitcost ={1}, transcost = {2}, chargecost ={3} \n", route.AssignedVeh.VehId.ToString(), WaitCost.ToString(), TransCost.ToString(), ChargeCost.ToString(), (WaitCost + TransCost + ChargeCost).ToString());
            sb.AppendLine(txt);
            Console.WriteLine("vehid = {0}, waitcost ={1}, transcost = {2}, chargecost ={3} \n", route.AssignedVeh.VehId.ToString(), WaitCost.ToString(), TransCost.ToString(), ChargeCost.ToString(), (WaitCost + TransCost + ChargeCost).ToString());
            return WaitCost + TransCost + ChargeCost;
        }

    }
}
