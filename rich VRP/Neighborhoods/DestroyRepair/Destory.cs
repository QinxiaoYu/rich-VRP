using OP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rich_VRP.Neighborhoods.DestroyRepair
{
    class Destory
    {
        AC AC;
        List<int> angel = new List<int> { 30, 30, 30, 30, 30, 30, 30, 30, 30, 90 };
        public Solution destoryBYcluster(Solution solution)
        {
            AC = new AC(angel);
            if (solution.UnVisitedCus == null)
            {
                solution.UnVisitedCus = new List<Customer>();
            }
            for (int i = solution.Routes.Count - 1; i > 0; i--)//遍历每一条路
            {
                Route route = solution.Routes[i];
                var costs = route.routeCost();
                double old_obj = costs.Item1 + costs.Item2 + costs.Item3;
                int cnt_charge = costs.Item4;
                //route.RemoveAllSta();
                int route_cluster = AC.getRouteCluster(route);
                for (int j = 1; j < route.RouteList.Count - 1; j++)//遍历每一个节点
                {  
                    AbsNode node = route.RouteList[j];
                    if (node.Info.Type==3)
                    {
                        continue;
                    }
                    int cus_cluster = AC.getCluster(node.Info.Id);
                    if (cus_cluster < route_cluster  || cus_cluster > route_cluster  )
                    {
                        route.Remove((Customer)node);
                        solution.UnVisitedCus.Add((Customer)node);
                        Console.WriteLine(node.Info.Id);
                    }
                }

                //Route tmp_r = route.InsertSta(cnt_charge, old_obj); //最优的插入充电站遍历
                int veh_in_fleet_pos = solution.fleet.GetVehIdxInFleet(route.AssignedVeh.VehId);
                solution.fleet.VehFleet[veh_in_fleet_pos].VehRouteList[route.RouteIndexofVeh] = route.RouteId;
                solution.Routes[i] = route.Copy();
                solution.Routes[i].AssignedVeh = solution.fleet.VehFleet[veh_in_fleet_pos];
            }
            return solution;
        }
    }
}
