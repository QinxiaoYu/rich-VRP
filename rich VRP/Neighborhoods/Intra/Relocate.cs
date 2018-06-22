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
        public Solution RelocateIntra(Solution solution, int select_strategy = 1, bool rand = false)
        {
            //Solution bst_sol = solution.Copy();
            double bst_obj_change = 0;
            Fleet fleet = solution.fleet;
            Solution new_sol = solution.Copy(); 
            //Solution new_sol2 = solution.Copy();
            int num_veh = fleet.GetNumOfUsedVeh();//车的数量
            for (int i = 0; i < num_veh; i++)//对每辆车做遍历
            {
                Vehicle veh = fleet.VehFleet[i]; 
                double old_obj = solution.calculCost(veh); //一辆车的旧成本
                int num_routes_veh = veh.getNumofVisRoute();//车i服务的路线数量
                for (int j = 0; j < num_routes_veh; j++)//遍历车i下的所有路线
                {

                    string route_id = veh.VehRouteList[j];
                    int pos_route_sol = -1;
                    Route route = solution.GetRouteByID(route_id, out pos_route_sol);//定位车i的路线j在解中的位置
                    Route tmp_r = route.Copy();//路线j的拷贝，将对此拷贝做更改
                    if (rand)
                    {
                        tmp_r = RandRelocateIntra(tmp_r);
                        tmp_r.AssignedVeh.VehRouteList[j] = tmp_r.RouteId;
                        new_sol.fleet.VehFleet[i].VehRouteList[j] = tmp_r.RouteId;
                        new_sol.Routes[pos_route_sol] = tmp_r.Copy();
                    }
                    else
                    {
                        tmp_r = DeterRelocateInRoute(tmp_r);//对当前路径进行重定位遍历，返回一个不比原来差的解
                        tmp_r.AssignedVeh.VehRouteList[j] = tmp_r.RouteId;
                        new_sol.fleet.VehFleet[i].VehRouteList[j] = tmp_r.RouteId;
                        new_sol.Routes[pos_route_sol] = tmp_r.Copy();
                    }
                    double delay = tmp_r.GetArrivalTime() - route.GetArrivalTime();
                    if (delay > 0 && new_sol.CheckNxtRoutesFeasible(new_sol.fleet.VehFleet[i], tmp_r.RouteIndexofVeh, delay) == false)
                    {
                        new_sol.fleet.VehFleet[i].VehRouteList[j] = route.RouteId;
                        new_sol.Routes[pos_route_sol] = route; //不可行，再撤销更改
                        continue;
                    }
                    if (delay < 0)
                    {
                        new_sol.UpdateTripChainTime(new_sol.fleet.VehFleet[i]);
                    }
                }

                double new_obj = new_sol.calculCost(new_sol.fleet.VehFleet[i]);
                double obj_change = old_obj - new_obj;
                if (obj_change > 0)
                {
                    if (select_strategy == 0)//first improvement
                    {
                        new_sol.ObjVal = new_sol.ObjVal - obj_change;                        
                    }
                    else
                    {
                        if (obj_change > bst_obj_change) //best improvement
                        {
                            bst_obj_change = obj_change;
                            new_sol.ObjVal = new_sol.ObjVal - bst_obj_change;                          
                        }
                    }
                }
                if (obj_change<=0)//没变好，再变回来...
                {                
                    for (int j = 0; j < num_routes_veh; j++)//遍历车i下的所有路线
                    {
                        string route_id = veh.VehRouteList[j];
                        int pos_route_sol = -1;
                        Route route = solution.GetRouteByID(route_id, out pos_route_sol);//定位车i的路线j在解中的位置
                        new_sol.fleet.VehFleet[i].VehRouteList[j] = route.RouteId;
                        new_sol.Routes[pos_route_sol] = route.Copy(); //不可行，再撤销更改
                    }
                }                          
            }//结束对所有车的遍历
            return new_sol;
        }



        public Route DeterRelocateInRoute(Route route)
        {
            Route bst_route =route.Copy();          
            var old_costs = route.routeCost();
            double old_obj = old_costs.Item1 + old_costs.Item2 + old_costs.Item3; //旧线路的可变成本
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
                tmp_route.Remove(cus);  //删除一个点后的线路
                for (int j = 1; j < tmp_route.RouteList.Count-1; j++)//遍历每一个可插入位置
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
                    tmp_route_j = tmp_route_j.InsertSta(cnt_charge+1,old_obj); //检查充电站，并返回更优的路线
                    if (tmp_route_j.IsFeasible())
                    {
                        old_obj = tmp_route_j.routecost;
                        bst_route = tmp_route_j.Copy();                       
                    }
                }//结束遍历每一个可插入位置
                //copy_route = bst_route.Copy();
               
            }// 结束遍历每个待插入点    
            return bst_route;   
        }

        public Route RandRelocateIntra(Route route)
        {
           
            var old_costs = route.routeCost();
            double old_obj = old_costs.Item1 + old_costs.Item2 + old_costs.Item3;
            Route bst_route = route.Copy();
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
                    if (tmp_route_j.IsFeasible()&&tmp_route_j.routecost<old_obj)
                    {
                        old_obj = tmp_route_j.routecost;
                        bst_route = tmp_route_j.Copy();
                    }
                }//结束遍历每一个可插入位置
                copy_route = bst_route.Copy();
                
            }// 结束遍历每个待插入点    
            return bst_route;
        }
    }
}
