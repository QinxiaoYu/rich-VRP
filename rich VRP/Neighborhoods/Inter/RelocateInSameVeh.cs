//using OP.Data;
//using rich_VRP.Neighborhoods.Intra;
////using rich_VRP.ObjectiveFunc;
//using System;
//using System.Collections.Generic;

//namespace rich_VRP.Neighborhoods.Inter
//{
//    class RelocateInSameRoute
//    {
//        Random rd;

//        public RelocateInSameRoute()
//        {
//            rd = new Random();

//        }
//        /// <summary>
//        /// 将同一辆车上到商户在该车的不同线路上进行再分配
//        /// 主要考虑将一条线路拆成多条
//        /// </summary>
//        /// <param name="solution"></param>
//        /// <param name="select_strategy">选择策略：0:first improvement;1:best improvement </param>
//        /// <returns></returns>
//        public Solution RelocateToBetter(Solution solution, int chain = 1, int select_strategy = 0)
//        {
//            solution.printCheckSolution();
//            Solution bst_sol = null;
//            double bst_obj_change = 0;

//            for (int i = 0; i < solution.fleet.VehFleet.Count; i++) //对只有一条线路的每辆车进行遍历
//            {
//                Vehicle old_veh = solution.fleet.VehFleet[i];
//                if (old_veh.VehRouteList.Count>1)
//                {
//                    continue;
//                }
//                double old_obj_veh = solution.calculCost(old_veh);
//                int pos_route_sol = -1;
//                Route first_trip_veh = solution.GetRouteByID(old_veh.VehRouteList[0],out pos_route_sol);
//                var costs_fst_trip = first_trip_veh.routeCost();
//                double old_route_obj = costs_fst_trip.Item1 + costs_fst_trip.Item2 + costs_fst_trip.Item3;
//                if (costs_fst_trip.Item4<=0) //线路没充电，肯定没有改善空间
//                {
//                    continue;
//                }
//                Route new_route = new Relocate().DeterRelocateInRoute(first_trip_veh);
//                new_route.RemoveAllSta();
//                int pos_EmptyCharge = new_route.ViolationOfRange(); //没有充电情况下，第一个不能到的位置

//                List<AbsNode> RemovedCus = new_route.RouteList.GetRange(pos_EmptyCharge, new_route.RouteList.Count - 1 - pos_EmptyCharge);
//                new_route.Remove(RemovedCus);
//                while (new_route.ViolationOfRange() > 0)
//                {
//                    int num_nodes = new_route.RouteList.Count;
//                    RemovedCus.Insert(0, new_route.RouteList[num_nodes - 2]);
//                    new_route.RemoveAt(num_nodes-2);
//                }
//                var costs_new_route = new_route.routeCost();
//                double new_obj_route = costs_new_route.Item1 + costs_new_route.Item2 + costs_new_route.Item3;
//                double at_fst = new_route.GetArrivalTime();
//                double dt_nxt = at_fst + Problem.MinWaitTimeAtDepot;
//                Route sec_route = new Route(old_veh, dt_nxt);
//                sec_route.InsertCustomer(RemovedCus);
//                if (sec_route.ViolationOfTimeWindow()>0)
//                {
//                    continue;
//                }
//                if (sec_route.ViolationOfRange()>0)
//                {
//                    sec_route.routecost = -1;
//                    sec_route = sec_route.InsertSta(costs_fst_trip.Item4 - 1, double.MaxValue);
//                    if (sec_route.routecost == -1 )
//                    {
//                        continue;
//                    }
//                }
//                double new_obj_veh = sec_route.routecost + new_obj_route + Problem.GetVehTypebyID(old_veh.TypeId).FixedCost+ Problem.WaitCostRate*Problem.MinWaitTimeAtDepot ;

//                if (new_obj_veh < old_obj_veh)
//                {
//                    ///to do
//                    bst_sol.Routes[pos_route_sol] = new_route   
//                }


//            }



//        }
//    }

//    /// <summary>
//    /// 将同一辆车上到商户在该车的不同线路上进行再分配
//    /// 主要考虑将一条线路拆成多条
//    /// </summary>
//    /// <param name="solution"></param>
//    /// <param name="select_strategy">选择策略：0:first improvement;1:best improvement </param>
//    /// <returns></returns>
//    public Solution RelocateToFeasible(Solution solution,int select_strategy = 0)
//    {
//        solution.printCheckSolution();
//        Solution bst_sol = null;
//        double bst_obj_change = 0;

//        int num_route_sol = solution.Routes.Count;
//        for (int i = 0; i < num_route_sol - 1; i++) //第一条路
//        {

//            Route old_ri = solution.Routes[i];
//            Vehicle old_vi = solution.fleet.GetVehbyID(old_ri.AssignedVeh.VehId);
//            double old_obj_ri = solution.calculCost(old_vi);
//            old_ri.RemoveAllSta();
//            for (int j = i + 1; j < num_route_sol; j++) //第二条路
//            {
//                Route old_rj = solution.Routes[j];
//                Vehicle old_vj = solution.fleet.GetVehbyID(old_rj.AssignedVeh.VehId);
//                double old_obj_rj = solution.calculCost(old_vj);

//                if (v_i.VehId == v_j.VehId) //如果两条路属于同一辆车，则不交换
//                {
//                    continue;
//                }
//                r_j.RemoveAllSta();
//                var Conditions = r_i.overlapPercent(r_j);
//                if (Conditions.Item1 <= 0 || Conditions.Item2 > 50) //如果两条路所在扇形区角度差太大， 或者半径相差太大，都不进行交换
//                {
//                    continue;
//                }
//                for (int split1 = 1; split1 < r_i.RouteList.Count - chain1; split1++)
//                {
//                    for (int split2 = 1; split2 < r_j.RouteList.Count - chain2; split2++)
//                    {

//                        Solution new_sol = solution.Copy();
//                        //int[] Neighbor_ri  =Problem.GetNearDistanceCus(r_i.RouteList[split1 - 1].Info.Id);
//                        //int[] Neighbor_rj = Problem.GetNearDistanceCus(r_j.RouteList[split2 - 1].Info.Id);
//                        //int[] Neighbor_r1_p = new int[] { };
//                        //int[] Neighbor_r2_p = new int[] { };

//                        List<AbsNode> route1part = r_i.RouteList.GetRange(split1, chain1);//从split1开始向后取chain个
//                        List<AbsNode> route2part = r_j.RouteList.GetRange(split2, chain2);

//                        //if (chain1 > 0)
//                        //{
//                        //    Neighbor_r1_p = Problem.GetNearDistanceCus(route1part[route1part.Count - 1].Info.Id);
//                        //}
//                        //if (chain2 > 0)
//                        //{
//                        //    Neighbor_r2_p = Problem.GetNearDistanceCus(route2part[route2part.Count - 1].Info.Id);
//                        //}

//                        //if (true)
//                        //{

//                        //}

//                        double duetime_fstNode_r1p2 = chain1 == 0 ? Problem.StartDepot.Info.DueDate : route1part[0].Info.DueDate;
//                        double duetime_fstNode_r2p2 = chain2 == 0 ? Problem.StartDepot.Info.DueDate : route2part[0].Info.DueDate;
//                        double earlytime_lstNode_r1p1 = chain1 == 0 ? Problem.StartDepot.Info.ReadyTime : r_i.ServiceBeginingTimes[split1 - 1] + r_i.RouteList[split1 - 1].Info.ServiceTime + r_i.RouteList[split1 - 1].TravelTime(route1part[0]);
//                        double earlytime_lstNode_r2p1 = chain2 == 0 ? Problem.StartDepot.Info.ReadyTime : r_j.ServiceBeginingTimes[split2 - 1] + r_j.RouteList[split2 - 1].Info.ServiceTime + r_j.RouteList[split2 - 1].TravelTime(route2part[0]);
//                        if (duetime_fstNode_r1p2 < earlytime_lstNode_r2p1 || duetime_fstNode_r2p2 < earlytime_lstNode_r1p1)
//                        {
//                            break;
//                        }
//                        Route copy_ri = r_i.Copy();
//                        Route copy_rj = r_j.Copy();
//                        copy_ri.Remove(route1part);
//                        copy_rj.Remove(route2part);//从路径里删除后半段

//                        for (int k = 0; k < chain2; k++)
//                        {
//                            copy_ri.InsertNode(route2part[k], split1 + k);

//                        }
//                        for (int l = 0; l < chain1; l++)
//                        {
//                            copy_rj.InsertNode(route1part[l], split2 + l);
//                        }

//                        if (copy_ri.ViolationOfVolume() > 0 || copy_ri.ViolationOfWeight() > 0
//                             || copy_rj.ViolationOfVolume() > 0 || copy_rj.ViolationOfWeight() > 0
//                            )
//                        {
//                            continue;
//                        }
//                        if (copy_ri.ViolationOfTimeWindow() > -1 && copy_rj.ViolationOfTimeWindow() > -1)
//                        {
//                            break;
//                        }
//                        if (copy_ri.ViolationOfRange() > -1)
//                        {
//                            copy_ri = copy_ri.InsertSta(3);
//                        }
//                        if (copy_rj.ViolationOfRange() > -1)
//                        {
//                            copy_rj = copy_rj.InsertSta(3);
//                        }


//                        if (copy_ri.ViolationOfRange() > -1 || copy_ri.ViolationOfTimeWindow() > -1
//                            || copy_rj.ViolationOfRange() > -1 || copy_rj.ViolationOfTimeWindow() > -1) //加入充电站后仍不可行
//                        {
//                            continue;
//                        }
//                        copy_ri.AssignedVeh.VehRouteList[copy_ri.RouteIndexofVeh] = copy_ri.RouteId;
//                        copy_rj.AssignedVeh.VehRouteList[copy_rj.RouteIndexofVeh] = copy_rj.RouteId;
//                        new_sol.Routes[i] = copy_ri.Copy();
//                        new_sol.Routes[j] = copy_rj.Copy();

//                        int idx_vehi_fleet = new_sol.fleet.GetVehIdxInFleet(v_i.VehId);
//                        int idx_vehj_fleet = new_sol.fleet.GetVehIdxInFleet(v_j.VehId);
//                        new_sol.fleet.VehFleet[idx_vehi_fleet].VehRouteList[r_i.RouteIndexofVeh] = copy_ri.RouteId;
//                        new_sol.fleet.VehFleet[idx_vehj_fleet].VehRouteList[r_j.RouteIndexofVeh] = copy_rj.RouteId;
//                        //以上检查发生交换的两条路，自身是否可行
//                        //以下检查这两条路对应的两辆车下的路径链是否可行                   
//                        double delay_i = copy_ri.GetArrivalTime() - r_i.GetArrivalTime();
//                        double delay_j = copy_rj.GetArrivalTime() - r_j.GetArrivalTime();

//                        if (delay_i > 0 && new_sol.CheckNxtRoutesFeasible(new_sol.fleet.VehFleet[idx_vehi_fleet], copy_ri.RouteIndexofVeh, delay_i) == false)
//                        {
//                            continue;
//                        } //下游线路不可行
//                        if (delay_j > 0 && new_sol.CheckNxtRoutesFeasible(new_sol.fleet.VehFleet[idx_vehj_fleet], copy_rj.RouteIndexofVeh, delay_j) == false)
//                        {
//                            continue;
//                        }//下游线路不可行

//                        if (delay_i < 0)
//                        {
//                            new_sol.UpdateTripChainTime(new_sol.fleet.VehFleet[idx_vehi_fleet]);


//                        }
//                        if (delay_j < 0)
//                        {
//                            new_sol.UpdateTripChainTime(new_sol.fleet.VehFleet[idx_vehj_fleet]);

//                        }
//                        //Console.WriteLine(copy_ri.RouteId + "  " + copy_rj.RouteId);
//                        double new_obj_i = new_sol.calculCost(new_sol.fleet.VehFleet[idx_vehi_fleet]);
//                        double new_obj_j = new_sol.calculCost(new_sol.fleet.VehFleet[idx_vehj_fleet]);
//                        //double new_obj = new_sol.CalObjCost();
//                        double obj_change = (old_r1 + old_r2) - (new_obj_i + new_obj_j);
//                        if (obj_change > 20)//如果变好
//                        {
//                            if (select_strategy == 0)//first improvement
//                            {
//                                new_sol.ObjVal = solution.ObjVal - obj_change;
//                                return new_sol.Copy();
//                            }
//                            else
//                            {
//                                if (obj_change > bst_obj_change) //best improvement
//                                {
//                                    bst_obj_change = obj_change;
//                                    bst_sol = new_sol.Copy();

//                                }
//                            }


//                        }
//                    }//结束对第二条路 各个截断位置的遍历
//                }//结束对第一条路 各个截断位置的遍历
//            }//结束对第二条路的枚举
//        }//结束对第一路对枚举

//        if (bst_sol != null)
//        {
//            Console.WriteLine(solution.CalObjCost());
//            Console.WriteLine(bst_sol.CalObjCost());
//            Console.WriteLine(bst_obj_change);
//        }
//        return bst_sol;

//    }
//}
//}
