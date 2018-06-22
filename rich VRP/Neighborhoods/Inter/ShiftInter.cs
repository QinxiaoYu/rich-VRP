using OP.Data;
//using rich_VRP.ObjectiveFunc;
using System;
using System.Collections.Generic;

namespace rich_VRP.Neighborhoods.Inter
{
    class ShiftInter
    {
        Random rd;

        public ShiftInter()
        {
            rd = new Random();

        }
        /// <summary>
        /// 将一条线路上的一些点，移动到另一条线路
        /// 不会产生空线路
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="select_strategy">选择策略：0:first improvement;1:best improvement </param>
        /// <returns></returns>
        public Solution Shift(Solution solution, int chain = 1, int select_strategy = 0, double change_obj_threshold=0)
        {
            solution.printCheckSolution();
            Solution bst_sol = null;
            double bst_obj_change = -double.MaxValue;

            int num_route_sol = solution.Routes.Count;
            for (int i = 0; i < num_route_sol; i++) //第一条路
            {
                solution.printCheckSolution();
                //Console.WriteLine(solution.SolutionIsFeasible());
                Route old_ri = solution.Routes[i].Copy();
                Vehicle old_vi = solution.fleet.GetVehbyID(old_ri.AssignedVeh.VehId); 
                double old_obj_vi = solution.calculCost(old_vi); //第一条路所在车的总费用
                old_ri.RemoveAllSta();
                for (int j = 0; j < num_route_sol; j++) //第二条路
                {
                    if (j==i)
                    {
                        continue;
                    }
                    Route old_rj = solution.Routes[j].Copy();
                    double weight = old_rj.GetTotalWeight();
                    double volume = old_rj.GetTotalVolume();
                    if (weight>old_rj.AssignedVehType.Weight-Problem.MinWeight || volume > old_rj.AssignedVehType.Volume-Problem.MinVolume)
                    {
                        continue;
                    }
                    Vehicle old_vj = solution.fleet.GetVehbyID(old_rj.AssignedVeh.VehId);
                    double old_obj = old_obj_vi; //两条路涉及到两辆车所对应到总费用
                    if (old_vj.VehId!=old_vi.VehId) //如果两条路在同一辆车，则只有一辆车de费用
                    {
                        
                        old_obj += solution.calculCost(old_vj);
                    }


                    old_rj.RemoveAllSta();
                    //var Conditions = old_ri.overlapPercent(old_rj);
                    //if (Conditions.Item1 <= 0 || Conditions.Item2 > 50) //如果两条路所在扇形区角度差太大， 或者半径相差太大，都不进行交换
                    //{
                    //    continue;
                    //}
                    for (int split1 = 1; split1 < old_ri.RouteList.Count - chain; split1++)//将第一条路上到一些点移到第二条路
                    {
                        List<AbsNode> route1part = old_ri.RouteList.GetRange(split1, chain);//从第一条路取chain个商户
                        double weight_chain = 0;
                        double volume_chain = 0;
                        foreach (var cus in route1part)
                        {
                            weight_chain += cus.Info.Weight;
                            volume_chain += cus.Info.Volume;
                        }
                        //重量和体积检查
                        if (old_rj.GetTotalWeight() + weight_chain > old_rj.AssignedVehType.Weight
                           || old_rj.GetTotalVolume() + volume_chain > old_rj.AssignedVehType.Volume)
                        {
                            continue;
                        }
                        for (int split2 = 1; split2 < old_rj.RouteList.Count-1; split2++)//移到第二条路到位置
                        {
                            //时间窗检查
                            double duetime_fstNode_ri = route1part[0].Info.DueDate;
                            double earlytime_lstNode_rj = old_rj.ServiceBeginingTimes[split2 - 1] + old_rj.RouteList[split2 - 1].Info.ServiceTime + old_rj.RouteList[split2 - 1].TravelTime(route1part[0]);
                            if (earlytime_lstNode_rj > duetime_fstNode_ri) //第二条路前面的点到不了，后面的点肯定也到不了
                            {
                                break;
                            }
                           
                            //邻域检查
                            AbsNode rj_preNode = old_rj.RouteList[split2 - 1];
                            AbsNode rj_nxtNode = old_rj.RouteList[split2];

                            int[] Neighbor_rj_preNode = Problem.GetNearDistanceCus(rj_preNode.Info.Id);
                            int ri_fst_isExist = Array.IndexOf(Neighbor_rj_preNode, route1part[0].Info.Id); //判断是否在邻域里
                            if (ri_fst_isExist ==-1)//第一条路链中第一个点不在第二条路插入上点邻域
                            {
                                continue;
                            }
                            int[] Neighbor_ri_lstNode = Problem.GetNearDistanceCus(route1part[chain - 1].Info.Id);
                            int ri_lst_isExist = Array.IndexOf(Neighbor_ri_lstNode, rj_nxtNode.Info.Id);
                            if (ri_lst_isExist == -1)//第二条路插入下点不在第一条路链最后一个点的邻域中
                            {
                                continue;
                            }

                            //以下将对两条路做改动了
                            Route copy_ri = old_ri.Copy();
                            Route copy_rj = old_rj.Copy();
                            copy_ri.Remove(route1part);

                            for (int k = 0; k < chain; k++)
                            {
                                copy_rj.InsertNode(route1part[k], split2 + k);

                            }

                            if (copy_rj.ViolationOfTimeWindow() > -1 || copy_ri.ViolationOfTimeWindow() > -1 )
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

                            //改动后的两条路是可行的，进一步检查是否影响它俩所在的路径链
                            Solution new_sol = solution.Copy();

                            copy_ri.AssignedVeh.VehRouteList[copy_ri.RouteIndexofVeh] = copy_ri.RouteId;
                            copy_rj.AssignedVeh.VehRouteList[copy_rj.RouteIndexofVeh] = copy_rj.RouteId;
                            new_sol.Routes[i] = copy_ri.Copy();
                            new_sol.Routes[j] = copy_rj.Copy();
                            int pos_vehi_fleet = new_sol.fleet.GetVehIdxInFleet(copy_ri.AssignedVeh.VehId);
                            int pos_vehj_fleet = new_sol.fleet.GetVehIdxInFleet(copy_rj.AssignedVeh.VehId);
                            
                            new_sol.fleet.VehFleet[pos_vehi_fleet].VehRouteList[copy_ri.RouteIndexofVeh] = copy_ri.RouteId;
                            new_sol.fleet.VehFleet[pos_vehj_fleet].VehRouteList[copy_rj.RouteIndexofVeh] = copy_rj.RouteId;
                  
                            double delay_i = copy_ri.GetArrivalTime() - old_ri.GetArrivalTime();
                            double delay_j = copy_rj.GetArrivalTime() - old_rj.GetArrivalTime();

                            if (delay_i > 0 && new_sol.CheckNxtRoutesFeasible(new_sol.fleet.VehFleet[pos_vehi_fleet], copy_ri.RouteIndexofVeh, delay_i) == false)
                            {
                                continue;
                            } //下游线路不可行
                            if (delay_j > 0 && new_sol.CheckNxtRoutesFeasible(new_sol.fleet.VehFleet[pos_vehj_fleet], copy_rj.RouteIndexofVeh, delay_j) == false)
                            {
                                continue;
                            }//下游线路不可行

                            if (delay_i < 0)
                            {
                                new_sol.UpdateTripChainTime(new_sol.fleet.VehFleet[pos_vehi_fleet]);


                            }
                            if (delay_j < 0)
                            {
                                new_sol.UpdateTripChainTime(new_sol.fleet.VehFleet[pos_vehj_fleet]);

                            }
                            //Console.WriteLine(copy_ri.RouteId + "  " + copy_rj.RouteId);
                            if (copy_ri.RouteList.Count == 2)
                            {
                                new_sol.Remove(copy_ri);
                            }
                            if (copy_rj.RouteList.Count == 2)
                            {
                                new_sol.Remove(copy_rj);
                            }
                            //Console.WriteLine(copy_ri.RouteId + "  " + copy_rj.RouteId);
                            double new_obj_i = 0;
                            double new_obj_j = 0;
                            Vehicle vi = new_sol.fleet.GetVehbyID(copy_ri.AssignedVeh.VehId);
                            if (vi != null)
                            {
                                new_obj_i = new_sol.calculCost(vi);
                            }
                            Vehicle vj = new_sol.fleet.GetVehbyID(copy_rj.AssignedVeh.VehId);
                            if (vj != null)
                            {
                                new_obj_j = new_sol.calculCost(vj);
                            }

                            double new_obj = 0;
                            if (copy_ri.AssignedVeh.VehId==copy_rj.AssignedVeh.VehId)
                            {
                                new_obj = new_obj_j;
                            }else
                            {                    
                                new_obj = new_obj_i + new_obj_j;
                            }

                            double obj_change = old_obj - new_obj;
                            if (obj_change > change_obj_threshold)//如果变好
                            {

                                if (select_strategy == 0)//first improvement
                                {
                                    new_sol.ObjVal = solution.ObjVal - obj_change;
                                    //Console.WriteLine(new_sol.SolutionIsFeasible().ToString());
                                    return new_sol.Copy();
                                }
                                else
                                {
                                    if (obj_change > bst_obj_change) //best improvement
                                    {
                                        bst_obj_change = obj_change;
                                        new_sol.ObjVal = solution.ObjVal - obj_change;
                                        //Console.WriteLine(new_sol.ObjVal+"    "+ new_sol.CalObjCost());
                                        //Console.WriteLine(new_sol.SolutionIsFeasible().ToString());
                                        bst_sol = new_sol.Copy();
                                        
                                    }
                                }


                            }
                        }//结束对第二条路 各个截断位置的遍历
                    }//结束对第一条路 各个截断位置的遍历
                }//结束对第二条路的枚举
            }//结束对第一路对枚举
            if (bst_sol == null)
            {
                return solution;
            }
            else
            {
                return bst_sol;
            }
        }
    }
}
