using OP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rich_VRP.Neighborhoods.Intra
{
    class StationPosition
    {
        Fleet fleet;
        public Solution StationExchage(Solution solution, double percent)
        {
            Console.WriteLine(solution.SolutionIsFeasible());
            fleet = solution.fleet;
            int num_vehs = fleet.GetNumOfUsedVeh();           
            for (int i = 0; i < num_vehs; i++) //对当前解中每辆车进行遍历
            {
                Vehicle veh = fleet.VehFleet[i];
                int num_trips = veh.getNumofVisRoute();
              
                for (int j = 0; j < num_trips; j++)  //对某辆车下的每个trip进行遍历
                {
                    int pos;
                    Route r = solution.GetRouteByID(veh.VehRouteList[j], out pos); //定位线路
                    var costs = r.routeCost();
                    double old_obj = costs.Item1 + costs.Item2 + costs.Item3;
                    int cnt_charge = costs.Item4;
                    if (cnt_charge == 0) //如果该trip上没有充电站，跳过
                    {
                        continue;
                    }
                    Route tmp_r = r.Copy(); //对当前解中该线路进行拷贝，将在此拷贝上做改动
                                   
                    tmp_r = tmp_r.InsertSta(cnt_charge, old_obj); //对拷贝路径进行最优的插入充电站遍历
                    //var new_cost = tmp_r.routeCost();
                    //double new_obj = new_cost.Item1 + new_cost.Item2 + new_cost.Item3;
                    double delay = tmp_r.GetArrivalTime() - r.GetArrivalTime();
                    if (solution.CheckNxtRoutesFeasible(veh,tmp_r.RouteIndexofVeh, delay))
                    {
                        tmp_r.AssignedVeh.VehRouteList[tmp_r.RouteIndexofVeh] = tmp_r.RouteId;
                        solution.Routes[pos] = tmp_r.Copy();
                        solution.fleet.VehFleet[i].VehRouteList[tmp_r.RouteIndexofVeh] = tmp_r.RouteId;                    
                    }
                }
            }
            solution.UpdateTripChainTime();
            return solution;
        }
    }
}
