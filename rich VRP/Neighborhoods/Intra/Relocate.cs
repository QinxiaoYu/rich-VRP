using OP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rich_VRP.Neighborhoods.Intra
{
    class Relocate
    {
        Random rd;
        public Relocate ()
        {
            rd = new Random();
        }
        public Solution RelocateIntra(Solution solution, bool rand = false)
        {
            Fleet fleet = solution.fleet;
            int num_veh = fleet.GetNumOfUsedVeh();
            for (int i = 0; i < num_veh; i++)
            {
                Vehicle veh = fleet.VehFleet[i];
                int num_routes_veh = veh.getNumofVisRoute();
                for (int j = 0; j < num_routes_veh; j++)
                {
                    string route_id = veh.VehRouteList[j];
                    int pos_route_sol = -1;
                    Route route = solution.GetRouteByID(route_id, out pos_route_sol);
                    Route tmp_r = route.Copy();
                    if (rand)
                    {
                        tmp_r = RandRelocateIntra(tmp_r);
                    }
                    else
                    {
                        tmp_r = DeterRelocateInRoute(tmp_r);//对当前路径进行重定位遍历     
                    }                                                
                    double delay = tmp_r.GetArrivalTime() - route.GetArrivalTime();
                    if (delay<=0)
                    {
                        solution.UpdateTripChainTime(veh);
                        solution.Routes[pos_route_sol] = tmp_r;
                        solution.fleet.VehFleet[i].VehRouteList[tmp_r.RouteIndexofVeh] = tmp_r.RouteId;
                        solution.fleet.VehFleet[i].solution = solution;
                        solution.fleet.solution = solution;

                    }
                    else if (veh.CheckNxtRoutesFeasible(tmp_r.RouteIndexofVeh, delay))
                    {                       
                        solution.Routes[pos_route_sol] = tmp_r;
                        solution.fleet.VehFleet[i].VehRouteList[tmp_r.RouteIndexofVeh] = tmp_r.RouteId;
                        solution.fleet.VehFleet[i].solution = solution;
                        solution.fleet.solution = solution;            
                    }
                }//结束对同一辆车下每条路的遍历
            }//结束对所有车的遍历
            return solution;
        }



        public Route DeterRelocateInRoute(Route route)
        {
            Route bst_route =route.Copy();          
            var old_costs = route.routeCost();
            double old_obj = old_costs.Item1 + old_costs.Item2 + old_costs.Item3;
            double bst_obj = old_obj;
            int cnt_charge = old_costs.Item4;
            route.RemoveAllSta(); //删除线路上所有充电站
            int num_node_route = route.RouteList.Count;
            Route copy_route = route.Copy();
            

            for (int i = 1; i < num_node_route-1; i++) //遍历线路上的每一个点
            {
                        
                Customer cus = (Customer)route.RouteList[i];
                Route tmp_route = copy_route.Copy();
                tmp_route.RemoveAllSta();
                int pos_cus_route = tmp_route.RouteList.FindIndex(a => a.Info.Id == cus.Info.Id);
                tmp_route.Remove(cus);
                for (int j = 1; j < tmp_route.RouteList.Count-1; j++)
                {
                    if (j==pos_cus_route) //不插回原位置
                    {
                        continue;
                    }
                    Route tmp_route_j = tmp_route.Copy();
                    tmp_route_j.InsertNode(cus, j); //插入新位置
                    if (tmp_route_j.ViolationOfTimeWindow()>-1) //违反时间窗
                    {
                        continue;
                    }
                    tmp_route_j = tmp_route_j.InsertSta(cnt_charge,old_obj); //检查充电站，并返回更优的路线
                    if (tmp_route_j.IsFeasible())
                    {
                        copy_route = tmp_route_j.Copy();
                        bst_route = tmp_route_j.Copy();                       
                    }
                }//结束遍历每一个可插入位置
                old_costs = copy_route.routeCost();
                old_obj = old_costs.Item1 + old_costs.Item2 + old_costs.Item3;
            }// 结束遍历每个待插入点    
            return bst_route;   
        }

        public Route RandRelocateIntra(Route route)
        {
            Route bst_route = route.Copy();
            var old_costs = route.routeCost();
            double old_obj = old_costs.Item1 + old_costs.Item2 + old_costs.Item3;
            double bst_obj = old_obj;
            int cnt_charge = old_costs.Item4;
            route.RemoveAllSta(); //删除线路上所有充电站
            int num_node_route = route.RouteList.Count;
            Route copy_route = route.Copy();
            List<AbsNode> Cus2Relocate = route.RouteList.GetRange(1, num_node_route - 2);

            while (Cus2Relocate.Count>0)
            {
                int rd_cus = rd.Next(Cus2Relocate.Count);
                Customer cus = (Customer)Cus2Relocate[rd_cus];
                Cus2Relocate.RemoveAt(rd_cus);
                Route tmp_route = copy_route.Copy();
                tmp_route.RemoveAllSta();
                int pos_cus_route = tmp_route.RouteList.FindIndex(a => a.Info.Id == cus.Info.Id);
                tmp_route.Remove(cus);
                for (int j = 1; j < tmp_route.RouteList.Count - 1; j++)
                {
                    if (j == pos_cus_route) //不插回原位置
                    {
                        continue;
                    }
                    Route tmp_route_j = tmp_route.Copy();
                    tmp_route_j.InsertNode(cus, j); //插入新位置
                    if (tmp_route_j.ViolationOfTimeWindow() > -1) //违反时间窗
                    {
                        continue;
                    }
                    tmp_route_j = tmp_route_j.InsertSta(cnt_charge, old_obj); //检查充电站，并返回更优的路线
                    if (tmp_route_j.IsFeasible())
                    {
                        copy_route = tmp_route_j.Copy();
                        bst_route = tmp_route_j.Copy();
                    }
                }//结束遍历每一个可插入位置
                old_costs = copy_route.routeCost();
                old_obj = old_costs.Item1 + old_costs.Item2 + old_costs.Item3;
            }// 结束遍历每个待插入点    
            return bst_route;
        }
    }
}
