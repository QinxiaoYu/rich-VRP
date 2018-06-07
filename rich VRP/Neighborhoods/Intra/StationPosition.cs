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
        public Solution StationExchage(Solution old_sol, double percent)
        {
            Solution new_sol = old_sol.Copy();
            fleet = old_sol.fleet;
            int num_vehs = fleet.GetNumOfUsedVeh();
            //首先，对每辆车进行遍历
            for (int i = 0; i < num_vehs; i++)
            {
                Vehicle veh = fleet.VehFleet[i];
                int num_trips = veh.getNumofVisRoute();
                //其次，对某辆车下的每个trip进行遍历
                for (int j = 0; j < num_trips; j++)
                {
                    int pos;
                    Route r = new_sol.GetRouteByID(veh.VehRouteList[j], out pos);
                    var costs = r.routeCost();
                    double old_obj = costs.Item1 + costs.Item2 + costs.Item3;
                    int cnt_charge = costs.Item4;
                    if (cnt_charge == 0) //如果该trip上没有充电站，跳过
                    {
                        continue;
                    }
                    Route tmp_r = r.Copy();
                                   
                    tmp_r = tmp_r.InsertSta(cnt_charge, old_obj); //对当前路径进行最优的插入充电站遍历
                    //var new_cost = tmp_r.routeCost();
                    //double new_obj = new_cost.Item1 + new_cost.Item2 + new_cost.Item3;
                    double delay = tmp_r.GetArrivalTime() - r.GetArrivalTime();
                    if (veh.CheckNxtRoutesFeasible(tmp_r.RouteIndexofVeh, delay))
                    {
                        new_sol.Routes[pos] = tmp_r.Copy();
                        new_sol.fleet.VehFleet[i].VehRouteList[tmp_r.RouteIndexofVeh] = tmp_r.RouteId;
                        new_sol.fleet.VehFleet[i].solution = new_sol;
                    }
                }
            }
            new_sol.UpdateTripChainTime();
            return new_sol;
        }
    }
}
