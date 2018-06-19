using OP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using rich_VRP.Neighborhoods;

namespace rich_VRP.Neighborhoods.DestroyRepair
{
    class Repair
    {
        //每个点和每个路径都有一个属性，所在扇形区域的id
        //遍历customer pool中的每一个点，插入到相邻扇形区域里的路径
        //插入之前，先判断该路径是否饱和（重量、体积、等待时间、返回仓库的时间）
        //遍历所有相邻扇形区域内的不饱和路径的所有位置，插入该点
        //判断插入是否可行（包括对后续路径的影响）
        //若可行，计算插入成本cost（可以有不同的评价标准）
        //选择最优的位置插入
        //若没有可以插入的位置，现有的车新的路径；若还不行，新产生车，新路径。

        List<int> angel = new List<int> { 30, 30, 30, 30, 30, 30, 30, 30, 30, 90 };
        AC AC;
        Fleet fleet;
        Random rand = new Random();//随机operter


        public Solution InsertCusToSolution(Solution old_solution)
        {
            Solution solution = old_solution.Copy();
            fleet = solution.fleet;
            bool inserted = false;
            int pos_inSolution = -1;
            AC = new AC(angel);
            foreach (Customer customer in solution.UnVisitedCus)
            {
                int Cus_cluster = AC.getCluster(customer.Info.Id);
                double best_cost = double.MaxValue; //一个无穷大的数
                Route best_insert_route = null;

                for (int i = 0; i < solution.Routes.Count - 1; i++)
                {
                    Route route = solution.Routes[i];
                    int Route_cluster = AC.getRouteCluster(route);
                    double TransCostRate = Problem.GetVehTypebyID(route.AssignedVeh.TypeId).VariableCost;//行驶费率
                    double ChargeCostRate = Problem.GetVehTypebyID(route.AssignedVeh.TypeId).ChargeCostRate;//行驶费率
                    var VariableCost = route.routeCost(TransCostRate, ChargeCostRate);
                    double cost_before_insert = VariableCost.Item1 + VariableCost.Item2 + VariableCost.Item3;
                    if (Route_cluster >= Cus_cluster  && Route_cluster <= Cus_cluster )
                    {
                        if (!route.IsSaturated())
                        {
                            Route cur_route = InsertCusToRoute(route, customer, solution, out inserted);
                            if (inserted)
                            {
                                var VariableCost_1 = cur_route.routeCost(TransCostRate, ChargeCostRate);
                                double cost_after_insert = VariableCost_1.Item1 + VariableCost_1.Item2 + VariableCost_1.Item3;
                                double Insert_cost = cost_after_insert - cost_before_insert;
                                if (Insert_cost < best_cost)
                                {
                                    best_cost = Insert_cost;
                                    best_insert_route = route;
                                    pos_inSolution = i;
                                }
                            }
                        }
                    }
                }
                if (best_insert_route != null)
                {
                    Route inserted_route = InsertCusToRoute(best_insert_route, customer, solution, out inserted);


                    int veh_in_fleet_pos = solution.fleet.GetVehIdxInFleet(best_insert_route.AssignedVeh.VehId);
                    solution.fleet.VehFleet[veh_in_fleet_pos].VehRouteList[best_insert_route.RouteIndexofVeh] = inserted_route.RouteId;
                    solution.Routes[pos_inSolution] = inserted_route.Copy();
                    solution.Routes[pos_inSolution].AssignedVeh = solution.fleet.VehFleet[veh_in_fleet_pos];

                }
                else
                {

                    for (int i = 0; i < solution.fleet.VehFleet.Count - 1; i++)//现有车辆产生一条路径服务该任务
                    {
                        Vehicle veh = solution.fleet.VehFleet[i];
                        int numRoutesVeh = veh.getNumofVisRoute();
                        string last_routeID = veh.VehRouteList[numRoutesVeh - 1];
                        Route last_route = solution.GetRouteByID(last_routeID, out pos_inSolution);
                        double overwork_time = last_route.GetArrivalTime();//车结束所有任务的时间
                        double DueDate = customer.Info.DueDate;
                        if (overwork_time + Problem.MinWaitTimeAtDepot + customer.TravelTime(last_route.RouteList[0]) < DueDate)
                        {
                            Route newRoute = new Route(veh, overwork_time + Problem.MinWaitTimeAtDepot);
                            Route cur_newRoute = InsertCusToRoute(newRoute, customer, solution, out inserted);
                            if (inserted)
                            {
                                newRoute = cur_newRoute;
                                newRoute.AssignedVeh.VehRouteList.Add(newRoute.RouteId);
                                veh.addRoute2Veh(newRoute);//将路径加入到vehicle中
                                solution.AddRoute(newRoute);
                                break;
                            }
                        }
                    }
                }
                while (!inserted)////新产生一辆车服务该任务
                {
                    Vehicle veh = null;
                    int type = rand.Next(0, 2) + 1; //随机产生一辆车（类型随机） 
                    veh = fleet.addNewVeh(type);
                    Route newRoute = new Route(veh, 480);
                    Route cur_newRoute = InsertCusToRoute(newRoute, customer, solution, out inserted);
                    newRoute = cur_newRoute;
                    newRoute.AssignedVeh.VehRouteList.Add(newRoute.RouteId);
                    veh.addRoute2Veh(newRoute);//将路径加入到vehicle中
                    solution.AddRoute(newRoute);
                }
            }
            return solution;
        }



        public Route InsertCusToRoute(Route route, Customer customer, Solution solution, out bool inserted)
        {

            bool insert = false;
            int num_cus = route.RouteList.Count;
            double best_cost = double.MaxValue; //一个无穷大的数
            Route best_route = route;
            for (int i = 1; i < route.RouteList.Count; i++)
            {
                Route cur_route = route.Copy();

                cur_route.InsertNode(customer, i);//插入
                double add_distance = customer.TravelDistance(cur_route.RouteList[i - 1]) + customer.TravelDistance(cur_route.RouteList[i + 1])
                                              - cur_route.RouteList[i - 1].TravelDistance(cur_route.RouteList[i + 1]);//增加的距离（dik + dkj - dij）

                //==========================充电站的判断=============
                Station after_sta = cur_route.insert_sta(customer);//若要在insert_cus后插入电站，应该插入哪个？
                AbsNode after_sta1 = null;
                if (i == num_cus - 1)
                {
                    after_sta1 = Problem.EndDepot;
                }
                else
                {
                    after_sta1 = cur_route.insert_sta(cur_route.RouteList[i + 1]);//若要在insert_cus后一个客户后面插入电站，应该插入哪个？
                }

                Station before_sta = cur_route.insert_sta(cur_route.RouteList[i - 1]);//若要在insert_cus前插入电站，应该插入哪个？                                                                
                double after_dis = customer.TravelDistance(cur_route.RouteList[i + 1]) + cur_route.RouteList[i + 1].TravelDistance(after_sta1);
                if (cur_route.battery_level[i] < after_dis)//如果剩余电量不能坚持到下次充电
                {
                    cur_route.InsertNode(after_sta, i + 1);//在insert-cus后插入电站
                    add_distance += after_sta.TravelDistance(customer) + after_sta.TravelDistance(cur_route.RouteList[i + 2])
                                    - customer.TravelDistance(cur_route.RouteList[i + 2]); //插入电站后增加的行驶距离
                }
                ///判断是否需要在插入点前插入充电站
                //如果剩余电量不能保证下次抵达充电桩,就在insert_cus前插入充电站
                double before_dis = customer.TravelDistance(cur_route.RouteList[i - 1]) + customer.TravelDistance(after_sta);
                if (cur_route.battery_level[i - 1] < before_dis)
                {
                    cur_route.InsertNode(before_sta, i);//在insert-cus前插入电站
                    add_distance += before_sta.TravelDistance(cur_route.RouteList[i - 1]) + before_sta.TravelDistance(customer)
                                    - customer.TravelDistance(cur_route.RouteList[i - 1]);//插入电站后增加的行驶距离
                }


                //==========================选择最优的位置=============
                double delay = cur_route.GetArrivalTime() - route.GetArrivalTime();

                if (cur_route.IsFeasible())//如果插入customer和相应的station后满足所有约束
                {
                    Vehicle veh = cur_route.AssignedVeh;

                    if (solution.CheckNxtRoutesFeasible(veh, cur_route.RouteIndexofVeh, delay))//如果下游路径也可行
                    {
                        double TransCostRate = Problem.GetVehTypebyID(route.AssignedVeh.TypeId).VariableCost;//行驶费率
                        double add_waittime = cur_route.GetWaitTime() - route.GetWaitTime();
                        double cost = TransCostRate * add_distance + Problem.WaitCostRate * add_waittime;//评价插入质量的标准
                        if (cost < best_cost)
                        {
                            best_cost = cost;
                            best_route = cur_route;
                            insert = true;
                        }
                    }
                }
            }

            inserted = insert;
            return best_route;
        }



        /// <summary>
        /// 修复，将未服务商户插回部分解中，此方法返回的解不一定比原解费用低
        /// </summary>
        /// <param name="solution"></param>
        /// <returns></returns>
        public Solution RepairToFeasible(Solution solution)
        {
            Random rand = new Random();
            while (solution.UnVisitedCus.Count > 0)
            {
                int rd = rand.Next(0, solution.UnVisitedCus.Count);//随机选一个未服务商户
                Customer cus = solution.UnVisitedCus[rd];
                int pos_route = -1; //第几条路
                int pos = FindBstPosition(solution, cus, out pos_route); //路的第几个位置
                if (pos_route != -1) //能找到一条路
                {
                    solution.Routes[pos_route].InsertNode(cus, pos);
                    Route nr = solution.Routes[pos_route];
                    solution.Routes[pos_route].AssignedVeh.VehRouteList[nr.RouteIndexofVeh] = nr.RouteId;
                    int idx_veh = solution.fleet.GetVehIdxInFleet(nr.AssignedVeh.VehId);
                    solution.fleet.VehFleet[idx_veh].VehRouteList[nr.RouteIndexofVeh] = nr.RouteId;
                    Console.WriteLine(solution.SolutionIsFeasible().ToString());
                }
                else
                {
                    int type = 2;
                    Vehicle veh = solution.fleet.addNewVeh(type); //先生成辆车
                    int pos_veh_fleet = solution.fleet.GetVehIdxInFleet(veh.VehId);    //车在车队中的位置              
                    Route newRoute = new Route(veh, veh.Early_time); //再生成个路线
                    newRoute.InsertNode(cus, 1);
                    veh.addRoute2Veh(newRoute);//把路分配给车

                    if (newRoute.ViolationOfRange() > 0)
                    {
                        newRoute = newRoute.InsertSta(3, double.MaxValue);
                    }

                    solution.AddRoute(newRoute); //把路添加到解里
                    solution.fleet.VehFleet[pos_veh_fleet].VehRouteList[newRoute.RouteIndexofVeh] = newRoute.RouteId;//更新车队中此车的线路集合

                }
                solution.UnVisitedCus.Remove(cus);
            }
            //solution.UpdateFirstTripTime();
            //solution.UpdateTripChainTime();
            Console.WriteLine(solution.SolutionIsFeasible().ToString());
            return solution;
        }

        private int FindBstPosition(Solution solution, Customer customer, out int idx_route)
        {
            idx_route = -1;
            int idx_pos_route = -1;
            double min_obj_change = double.MaxValue;
            for (int i = 0; i < solution.Routes.Count; i++)
            {
                Route route = solution.Routes[i];
                Vehicle veh = route.AssignedVeh;
                double v_route = route.GetTotalVolume();
                if (v_route + customer.Info.Volume > route.AssignedVehType.Volume)
                {
                    continue;
                }
                double w_route = route.GetTotalWeight();
                if (w_route + customer.Info.Weight > route.AssignedVehType.Weight)
                {
                    continue;
                }
                var costs = route.routeCost();
                double old_obj = costs.Item1 + costs.Item2 + costs.Item3;
                for (int j = 1; j < route.RouteList.Count; j++)
                {
                    double floattime_j = route.GetFloatTimeAtCus(j);
                    if (floattime_j > customer.Info.ServiceTime) //某点有浮动时间，才有可能往其前面加入商户
                    {
                        Route tmp_r = solution.Routes[i].Copy();
                        tmp_r.InsertNode(customer, j);
                        if (tmp_r.IsFeasible()) //可行
                        {
                            double delay = tmp_r.GetArrivalTime() - route.GetArrivalTime();
                            if (solution.CheckNxtRoutesFeasible(veh, tmp_r.RouteIndexofVeh, delay))
                            {
                                var newcosts = tmp_r.routeCost();
                                double new_obj = newcosts.Item1 + newcosts.Item2 + newcosts.Item3;
                                double obj_change = new_obj - old_obj;
                                if (obj_change < min_obj_change)
                                {
                                    idx_pos_route = j;
                                    idx_route = i;
                                }
                            }
                        }
                    }

                }
            }
            return idx_pos_route;
        }


    }
}
