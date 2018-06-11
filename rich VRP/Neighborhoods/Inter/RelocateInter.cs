using OP.Data;
//using rich_VRP.ObjectiveFunc;
using System;
using System.Collections.Generic;

namespace rich_VRP.Neighborhoods.Inter
{
    class RelocateInter
    {
        Random rd;

        public RelocateInter()
        {
            rd = new Random();

        }
        /// <summary>
        /// 两条线路上点的交换操作，如r1=abcd, r2 = ABCD,交换后变为abDc和ABdC
        /// 其中参数：角度差-10，半径差50
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="select_strategy">选择策略：0:first improvement;1:best improvement </param>
        /// <returns></returns>
        public Solution Relocate(Solution solution, int chain = 1, int select_strategy=0)
        {
            //Console.WriteLine(solution.PrintToString());
            Solution bst_sol = null;
            double old_obj = solution.ObjVal;
            double bst_obj_change = 0;
            //Console.WriteLine("=====solution========");
            //Console.WriteLine(solution.PrintToString());
            //Console.WriteLine("=====solution in fleet========");
            //Console.WriteLine(solution.fleet.solution.PrintToString());
            //Console.WriteLine("=====solution in vehicle=======");
            //Console.WriteLine(solution.fleet.VehFleet[0].solution.PrintToString());
                   

            int num_route_sol = solution.Routes.Count;
            for (int i = 0; i < num_route_sol - 1; i++) //第一条路
            {
                Route r_i = solution.Routes[i].Copy();
                //double old_ri_obj = r_i.AssignedVeh.calculCost();
                r_i.RemoveAllSta();
                for (int j = i + 1; j < num_route_sol; j++) //第二条路
                {
                    Route r_j = solution.Routes[j].Copy();
                    //double old_rj_obj = r_j.AssignedVeh.calculCost();
                    if (r_i.AssignedVeh.VehId == r_j.AssignedVeh.VehId) //如果两条路属于同一辆车，则不交换
                    {
                        continue;
                    }
                    r_j.RemoveAllSta();
                    var Conditions = r_i.overlapPercent(r_j);
                    if (Conditions.Item1<=-10 || Conditions.Item2>50) //如果两条路所在扇形区角度差太大， 或者半径相差太大，都不进行交换
                    {
                        continue;
                    }
                    for (int split1 = 1; split1 < r_i.RouteList.Count - chain; split1++)
                    {
                        for (int split2 = 1; split2 < r_j.RouteList.Count - chain; split2++)
                        {
                           
                            Solution new_sol = solution.Copy();
                            List<AbsNode> route1part = r_i.RouteList.GetRange(split1, chain);//从split1开始向后取chain个
                            List<AbsNode> route2part = r_j.RouteList.GetRange(split2,  chain);

                            double duetime_fstNode_r1p2 = route1part[0].Info.DueDate;
                            double duetime_fstNode_r2p2 = route2part[0].Info.DueDate;
                            double earlytime_lstNode_r1p1 = r_i.ServiceBeginingTimes[split1 - 1] + r_i.RouteList[split1 - 1].Info.ServiceTime + r_i.RouteList[split1 - 1].TravelTime(route1part[0]);
                            double earlytime_lstNode_r2p1 = r_j.ServiceBeginingTimes[split2 - 1] + r_j.RouteList[split2 - 1].Info.ServiceTime + r_j.RouteList[split2 - 1].TravelTime(route2part[0]);
                            if (duetime_fstNode_r1p2<earlytime_lstNode_r2p1 || duetime_fstNode_r2p2<earlytime_lstNode_r1p1)
                            {
                                break;
                            }
                            Route copy_ri = r_i.Copy();
                            Route copy_rj = r_j.Copy();
                            copy_ri.Remove(route1part);
                            copy_rj.Remove(route2part);//从路径里删除后半段

                            for (int k = 0; k < chain; k++)
                            {
                                copy_ri.InsertNode(route1part[k], split1+k);
                                copy_rj.InsertNode(route2part[k], split2 + k);
                            }

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
                            new_sol.Routes[i] = copy_ri;
                            new_sol.Routes[j] = copy_rj;
                            Vehicle new_veh_i = new_sol.fleet.GetVehbyID(r_i.AssignedVeh.VehId);
                            Vehicle new_veh_j = new_sol.fleet.GetVehbyID(r_j.AssignedVeh.VehId);
                            int idx_vehi_fleet = new_sol.fleet.GetVehIdxInFleet(new_veh_i.VehId);
                            int idx_vehj_fleet = new_sol.fleet.GetVehIdxInFleet(new_veh_j.VehId);
                            new_sol.fleet.VehFleet[idx_vehi_fleet].VehRouteList[r_i.RouteIndexofVeh] = copy_ri.RouteId;                          
                            new_sol.fleet.VehFleet[idx_vehj_fleet].VehRouteList[r_j.RouteIndexofVeh] = copy_rj.RouteId;                     
                            //以上检查发生交换的两条路，自身是否可行
                            //以下检查这两条路对应的两辆车下的路径链是否可行                   
                            double delay_i = copy_ri.GetArrivalTime() - r_i.GetArrivalTime();
                            double delay_j = copy_rj.GetArrivalTime() - r_j.GetArrivalTime();
                          
                            if (delay_i>0 && new_sol.CheckNxtRoutesFeasible(new_veh_i,copy_ri.RouteIndexofVeh,delay_i)==false)
                            {
                                continue;
                            } //下游线路不可行
                            if (delay_j>0 && new_sol.CheckNxtRoutesFeasible(new_veh_j,copy_rj.RouteIndexofVeh,delay_j)==false)
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
                            Console.WriteLine(copy_ri.PrintToStringSample() + "  " + copy_rj.PrintToStringSample());
                            //double new_obj_i = new_veh_i.calculCost();
                            //double new_obj_j = new_veh_j.calculCost();
                            double new_obj = new_sol.CalObjCost();
                            double obj_change = old_obj-new_obj;
                            if (obj_change>0)//如果变好
                            {
                                if (select_strategy == 0)//first improvement
                                {
                                    return new_sol;
                                }
                                else
                                {
                                    if (obj_change > bst_obj_change) //best improvement
                                    {
                                        bst_obj_change = obj_change;
                                        bst_sol = new_sol.Copy();

                                    }
                                }                         
                                
                                
                            }
                        }//结束对第二条路 各个截断位置的遍历
                    }//结束对第一条路 各个截断位置的遍历
                }//结束对第二条路的枚举
            }//结束对第一路对枚举

            if (bst_sol != null)
            {
                Console.WriteLine(solution.CalObjCost());
                Console.WriteLine(bst_sol.CalObjCost());
                Console.WriteLine(bst_obj_change);
            }
            return bst_sol;
            
        }
    }
}
