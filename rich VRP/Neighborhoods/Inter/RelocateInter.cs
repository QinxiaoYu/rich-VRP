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
        /// 其中参数：角度差-10，半径差50，目标函数值下降超过20
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="select_strategy">选择策略：0:first improvement;1:best improvement </param>
        /// <returns></returns>
        public Solution Relocate(Solution solution, int chain1 = 1, int chain2=1, int select_strategy=0, double change_obj_threshold = 20)
        {
            solution.printCheckSolution();
            //Console.WriteLine(solution.PrintToString());
            Solution bst_sol = null;
            //double old_obj = solution.ObjVal;
            double bst_obj_change = -double.MaxValue;
            //Console.WriteLine("=====solution========");
            //Console.WriteLine(solution.PrintToString());
            //Console.WriteLine("=====solution in fleet========");
            //Console.WriteLine(solution.fleet.solution.PrintToString());
            //Console.WriteLine("=====solution in vehicle=======");
            //Console.WriteLine(solution.fleet.VehFleet[0].solution.PrintToString());
                   
            int num_route_sol = solution.Routes.Count;
            int r1_rand_start = rd.Next(num_route_sol-1);
            for (int i = r1_rand_start; i < num_route_sol - 1; i++) //第一条路
            {
               
                Route r_i = solution.Routes[i].Copy();
                Vehicle v_i = solution.fleet.GetVehbyID(solution.Routes[i].AssignedVeh.VehId);
                double old_r1 = solution.calculCost(v_i);
                //double old_ri_obj = r_i.AssignedVeh.calculCost();
                r_i.RemoveAllSta();
                for (int j = i + 1; j < num_route_sol; j++) //第二条路
                {
                    Route r_j = solution.Routes[j].Copy();
                    Vehicle v_j = solution.fleet.GetVehbyID(solution.Routes[j].AssignedVeh.VehId);
                    solution.printCheckSolution();
                    double old_r2 = solution.calculCost(v_j);
                    //double old_rj_obj = r_j.AssignedVeh.calculCost();
                    if (v_i.VehId == v_j.VehId) //如果两条路属于同一辆车，则不交换
                    {
                        continue;
                    }
                    r_j.RemoveAllSta();
                    var Conditions = r_i.overlapPercent(r_j);
                    if (Conditions.Item1<=0 || Conditions.Item2>50) //如果两条路所在扇形区角度差太大， 或者半径相差太大，都不进行交换
                    {
                        continue;
                    }
                    for (int split1 = 1; split1 < r_i.RouteList.Count - chain1; split1++)
                    {
                        for (int split2 = 1; split2 < r_j.RouteList.Count - chain2; split2++)
                        {
                           
                            Solution new_sol = solution.Copy();
                            //int[] Neighbor_ri  =Problem.GetNearDistanceCus(r_i.RouteList[split1 - 1].Info.Id);
                            //int[] Neighbor_rj = Problem.GetNearDistanceCus(r_j.RouteList[split2 - 1].Info.Id);
                            //int[] Neighbor_r1_p = new int[] { };
                            //int[] Neighbor_r2_p = new int[] { };
                           
                            List<AbsNode> route1part = r_i.RouteList.GetRange(split1, chain1);//从split1开始向后取chain个
                            List<AbsNode> route2part = r_j.RouteList.GetRange(split2,  chain2);

                            //if (chain1 > 0)
                            //{
                            //    Neighbor_r1_p = Problem.GetNearDistanceCus(route1part[route1part.Count - 1].Info.Id);
                            //}
                            //if (chain2 > 0)
                            //{
                            //    Neighbor_r2_p = Problem.GetNearDistanceCus(route2part[route2part.Count - 1].Info.Id);
                            //}

                            //if (true)
                            //{

                            //}

                            double duetime_fstNode_r1p2 = chain1==0?Problem.StartDepot.Info.DueDate: route1part[0].Info.DueDate;
                            double duetime_fstNode_r2p2 = chain2==0? Problem.StartDepot.Info.DueDate : route2part[0].Info.DueDate;
                            double earlytime_lstNode_r1p1 = chain1 == 0 ? Problem.StartDepot.Info.ReadyTime: r_i.ServiceBeginingTimes[split1 - 1] + r_i.RouteList[split1 - 1].Info.ServiceTime + r_i.RouteList[split1 - 1].TravelTime(route1part[0]);
                            double earlytime_lstNode_r2p1 = chain2 == 0 ? Problem.StartDepot.Info.ReadyTime:r_j.ServiceBeginingTimes[split2 - 1] + r_j.RouteList[split2 - 1].Info.ServiceTime + r_j.RouteList[split2 - 1].TravelTime(route2part[0]);
                            if (duetime_fstNode_r1p2<earlytime_lstNode_r2p1 || duetime_fstNode_r2p2<earlytime_lstNode_r1p1)
                            {
                                break;
                            }
                            Route copy_ri = r_i.Copy();
                            Route copy_rj = r_j.Copy();
                            copy_ri.Remove(route1part);
                            copy_rj.Remove(route2part);//从路径里删除后半段

                            for (int k = 0; k < chain2; k++)
                            {
                                copy_ri.InsertNode(route2part[k], split1+k);
                                
                            }
                            for (int l = 0; l < chain1; l++)
                            {
                                copy_rj.InsertNode(route1part[l], split2 + l);
                            }

                            if (copy_ri.ViolationOfVolume() > 0 || copy_ri.ViolationOfWeight() > 0
                                 || copy_rj.ViolationOfVolume() > 0 || copy_rj.ViolationOfWeight() > 0
                                )
                            {
                                continue;
                            }
                            if (copy_ri.ViolationOfTimeWindow() > -1 || copy_rj.ViolationOfTimeWindow() > -1)
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
                            copy_ri.AssignedVeh.VehRouteList[copy_ri.RouteIndexofVeh] = copy_ri.RouteId;
                            copy_rj.AssignedVeh.VehRouteList[copy_rj.RouteIndexofVeh] = copy_rj.RouteId;
                            new_sol.Routes[i] = copy_ri.Copy();
                            new_sol.Routes[j] = copy_rj.Copy();
                          
                            int idx_vehi_fleet = new_sol.fleet.GetVehIdxInFleet(v_i.VehId);
                            int idx_vehj_fleet = new_sol.fleet.GetVehIdxInFleet(v_j.VehId);
                            new_sol.fleet.VehFleet[idx_vehi_fleet].VehRouteList[r_i.RouteIndexofVeh] = copy_ri.RouteId;                          
                            new_sol.fleet.VehFleet[idx_vehj_fleet].VehRouteList[r_j.RouteIndexofVeh] = copy_rj.RouteId;                     
                            //以上检查发生交换的两条路，自身是否可行
                            //以下检查这两条路对应的两辆车下的路径链是否可行                   
                            double delay_i = copy_ri.GetArrivalTime() - r_i.GetArrivalTime();
                            double delay_j = copy_rj.GetArrivalTime() - r_j.GetArrivalTime();
                          
                            if (delay_i>0 && new_sol.CheckNxtRoutesFeasible(new_sol.fleet.VehFleet[idx_vehi_fleet], copy_ri.RouteIndexofVeh,delay_i)==false)
                            {
                                continue;
                            } //下游线路不可行
                            if (delay_j>0 && new_sol.CheckNxtRoutesFeasible(new_sol.fleet.VehFleet[idx_vehj_fleet], copy_rj.RouteIndexofVeh,delay_j)==false)
                            {
                                continue;
                            }//下游线路不可行

                            if (delay_i < 0)
                            {
                                new_sol.UpdateTripChainTime(new_sol.fleet.VehFleet[idx_vehi_fleet]);                             


                            }
                            if (delay_j < 0)
                            {
                                new_sol.UpdateTripChainTime(new_sol.fleet.VehFleet[idx_vehj_fleet]);

                            }
                            //Console.WriteLine(copy_ri.RouteId + "  " + copy_rj.RouteId);
                            double new_obj_i = new_sol.calculCost(new_sol.fleet.VehFleet[idx_vehi_fleet]);
                            double new_obj_j = new_sol.calculCost(new_sol.fleet.VehFleet[idx_vehj_fleet]);
                            //double new_obj = new_sol.CalObjCost();
                            double obj_change = (old_r1+old_r2)-(new_obj_i+new_obj_j);
                            if (obj_change> change_obj_threshold)//如果变好
                            {
                                if (select_strategy == 0)//first improvement
                                {
                                    new_sol.ObjVal = solution.ObjVal - obj_change;
                                    return new_sol.Copy();
                                }
                                else
                                {
                                    if (obj_change > bst_obj_change) //best improvement
                                    {
                                        bst_obj_change = obj_change;
                                        bst_sol = new_sol.Copy();
                                        bst_sol.ObjVal = solution.ObjVal - obj_change;

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
