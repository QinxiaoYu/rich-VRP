using OP.Data;
using System;
using System.Collections.Generic;

namespace rich_VRP.Neighborhoods.Inter
{
    class CrossInter
    {
        Random rd;

        public CrossInter()
        {
            rd = new Random();

        }

        public bool Cross(Solution solution)
        {
            bool isImpr = false;
            Solution bst_sol = solution.Copy();
            double bst_obj_change = double.MaxValue;

            int num_route_sol = solution.Routes.Count;
            for (int i = 0; i < num_route_sol - 1; i++)
            {
                Route r_i = solution.Routes[i].Copy();
                double old_ri_obj = r_i.AssignedVeh.calculCost();
                r_i.RemoveAllSta();
                for (int j = i + 1; j < num_route_sol; j++)
                {
                    Route r_j = solution.Routes[j].Copy();
                    double old_rj_obj = r_j.AssignedVeh.calculCost();
                    if (r_i.AssignedVeh.VehId == r_j.AssignedVeh.VehId) //如果两条路属于同一辆车，则不交换
                    {
                        continue;
                    }
                    r_j.RemoveAllSta();
                    for (int split1 = 1; split1 < r_i.RouteList.Count - 1; split1++)
                    {
                        for (int split2 = 1; split2 < r_j.RouteList.Count - 1; split2++)
                        {
                            if (split1 == 1 && split2 == 1)
                            {
                                continue;
                            }
                            Solution new_sol = solution.Copy();
                            List<AbsNode> route1part2 = r_i.RouteList.GetRange(split1, r_i.RouteList.Count - split1);
                            List<AbsNode> route2part2 = r_i.RouteList.GetRange(split2, r_j.RouteList.Count - split2);

                            Route copy_ri = r_i.Copy();
                            Route copy_rj = r_j.Copy();
                            copy_ri.Remove(route1part2);
                            copy_rj.Remove(route2part2);//从路径里删除后半段

                            copy_ri.InsertCustomer(route2part2);//添加另一条路径的后半段进来,不可能产生空路线
                            copy_rj.InsertCustomer(route1part2);

                            if (copy_ri.ViolationOfVolume() <= 0 && copy_ri.ViolationOfWeight() <= 0
                                && copy_rj.ViolationOfVolume() <= 0 && copy_rj.ViolationOfWeight() <= 0
                                && copy_ri.ViolationOfTimeWindow() > -1 && copy_rj.ViolationOfTimeWindow() > -1)
                            {
                                continue;
                            }
                            if (copy_ri.ViolationOfRange() > -1)
                            {
                                copy_ri = copy_ri.InsertSta(3);
                            }
                            if (copy_rj.ViolationOfRange() > -1)
                            {
                                copy_rj = copy_rj.InsertSta(3);
                            }


                            if (copy_ri.ViolationOfRange() > -1 || copy_ri.ViolationOfTimeWindow() > -1
                                || copy_rj.ViolationOfRange() > -1 || copy_rj.ViolationOfTimeWindow() > -1) //加入充电站后仍不可行
                            {
                                continue;
                            }
                            //以上检查发生交换的两条路，自身是否可行
                            //以下检查这两条路对应的两辆车下的路径链是否可行

                            Vehicle new_veh_i = new_sol.fleet.GetVehbyID(r_i.AssignedVeh.VehId);
                            Vehicle new_veh_j = new_sol.fleet.GetVehbyID(r_j.AssignedVeh.VehId);

                            double delay_i = copy_ri.GetArrivalTime() - r_i.GetArrivalTime();
                            double delay_j = copy_rj.GetArrivalTime() - r_j.GetArrivalTime();

                            int idx_vehi_fleet = new_sol.fleet.GetVehIdxInFleet(new_veh_i.VehId);
                            int idx_vehj_fleet = new_sol.fleet.GetVehIdxInFleet(new_veh_j.VehId);

                            if (delay_i>0 && new_veh_i.CheckNxtRoutesFeasible(copy_ri.RouteIndexofVeh,delay_i)==false)
                            {
                                continue;
                            } //下游线路不可行
                            if (delay_j>0 && new_veh_j.CheckNxtRoutesFeasible(copy_rj.RouteIndexofVeh,delay_j)==false)
                            {
                                continue;
                            }//下游线路不可行

                            if (delay_i < 0)
                            {
                                new_sol.UpdateTripChainTime(new_veh_i);                             


                            }
                            if (delay_j < 0)
                            {
                                new_sol.UpdateTripChainTime(new_veh_j);

                            }

                            new_sol.fleet.VehFleet[idx_vehi_fleet].VehRouteList[r_i.RouteIndexofVeh] = copy_ri.RouteId;
                            new_sol.fleet.VehFleet[idx_vehi_fleet].solution = new_sol;
                            new_sol.fleet.VehFleet[idx_vehj_fleet].VehRouteList[r_j.RouteIndexofVeh] = copy_rj.RouteId;
                            new_sol.fleet.VehFleet[idx_vehj_fleet].solution = new_sol;
                            new_sol.fleet.solution = new_sol;

                            double new_obj_i = new_veh_i.calculCost();
                            double new_obj_j = new_veh_j.calculCost();
                            double obj_change = (old_ri_obj + old_rj_obj) - (new_obj_i + new_obj_j);
                            if (obj_change>0)//如果变好
                            {
                                if (obj_change<bst_obj_change)
                                {
                                    bst_obj_change = obj_change;
                                    bst_sol = new_sol;
                                    isImpr = true;
                                }
                            }


                        }//结束对第二条路 各个截断位置的遍历
                    }//结束对第一条路 各个截断位置的遍历
                }//结束对第二条路的枚举
            }//结束对第一路对枚举


            solution = bst_sol;
            return isImpr;
        }
    }
}
