using OP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rich_VRP.Neighborhoods.DestroyRepair
{
    class DestroyAndRepair
    {
        Random rand = new Random();

        public Solution DR(Solution solution, int minCusNum, int selectstrategy = 0)
        {
            Solution tmp_sol = solution.Copy();
            tmp_sol = DestroyShortRoute(tmp_sol, minCusNum);
            tmp_sol = RepairToFeasible(tmp_sol);
            double obj = tmp_sol.CalObjCost();
            if (obj<solution.ObjVal)
            {
                solution = tmp_sol;
                solution.ObjVal = obj;
            }
            return solution;
        }

        /// <summary>
        /// 如果一条路线上点的数量小于threshold_node个，则删除此线路
        /// </summary>
        /// <param name="threshold_node"></param>
        public Solution DestroyShortRoute(Solution solution, int threshold_node)
        {
            Solution new_sol = solution.Copy();
            if (solution.UnVisitedCus == null)
            {
                solution.UnVisitedCus = new List<Customer>();
            }
            for (int i = new_sol.Routes.Count - 1; i > 0; i--)
            {
                Route r = new_sol.Routes[i];
                if (r.RouteList.Count < threshold_node)
                {
                    solution.Remove(r);             
                    foreach (AbsNode cus in r.RouteList)
                    {
                        if (cus.Info.Type == 2)
                        {
                            solution.UnVisitedCus.Add((Customer)cus);
                        }
                    }

                }
            }
            //foreach (var veh in solution.fleet.VehFleet)
            //{
            //    Console.WriteLine(veh.VehId);
            //}
            return solution;
        }
        /// <summary>
        /// 如线路利用率低，即重量或体积小于百分之percent，则删除此线路
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="percent"></param>
        /// <returns></returns>
        public Solution DestroyWasteRoute(Solution solution, double percent)
        {
            foreach (var veh in solution.fleet.VehFleet)
            {
                Console.WriteLine("in Waste  "+veh.VehId);
            }
            Solution new_sol = solution.Copy();
            if (solution.UnVisitedCus == null)
            {
                solution.UnVisitedCus = new List<Customer>();
            }
            for (int i = new_sol.Routes.Count - 1; i > 0; i--)
            {
                Route r = new_sol.Routes[i];
                double totalWeight = r.GetTotalWeight();
                double totalVolume = r.GetTotalVolume();
                if (totalVolume < percent * Problem.VehTypes[r.AssignedVeh.TypeId - 1].Volume
                    || totalWeight < percent * Problem.VehTypes[r.AssignedVeh.TypeId - 1].Weight)
                {
                    solution.Remove(r);
                    foreach (AbsNode cus in r.RouteList)
                    {
                        if (cus.Info.Type == 2)
                        {
                            solution.UnVisitedCus.Add((Customer)cus);
                        }
                    }

                }
            }
            return solution;
        }

        /// <summary>
        /// To do....
        /// 删除线路中部分下午的商户,商户的最早开始时间晚于cuttingpoint，即认为是下午服务的商户
        /// 如果线路没有充电，且回到配送中心剩余电量小于百分之percent,则这条线路上不删任何点
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="cuttingpoint"></param>
        /// <param name="percent">回到配送中心的剩余电量百分比</param>
        /// <returns></returns>
        public Solution DestroyAfternoonNodes(Solution solution, double cuttingpoint, double percent)
        {
            if (solution.UnVisitedCus == null)
            {
                solution.UnVisitedCus = new List<Customer>();
            }

            for (int i = 0; i < solution.fleet.VehFleet.Count; i++)
            {
                Vehicle veh = solution.fleet.VehFleet[i];
                int numRoutesVeh = veh.getNumofVisRoute();
                for (int j = numRoutesVeh-1; j > 0; j--)
                {
                    string route_id = veh.VehRouteList[j];
                    int pos_route_sol = -1;
                    Route route = solution.GetRouteByID(route_id,out pos_route_sol);
                    double departuretime = route.GetDepartureTime(); //路线的出发时间
                    if (departuretime>cuttingpoint) //如果出发时间晚于cuttingpoing，则把这条路整个删除
                    {
                        solution.Remove(route);
                        foreach (AbsNode cus in route.RouteList)
                        {
                            if (cus.Info.Type == 2)
                            {
                                solution.UnVisitedCus.Add((Customer)cus);
                            }
                        }
                    }
                    else
                    {
                        var costs = route.routeCost();
                        int cnt_charge = costs.Item4;                        
                        double battery = route.battery_level.Last();
                        if (cnt_charge == 0 && battery < percent * Problem.VehTypes[veh.TypeId - 1].MaxRange)
                        {
                            continue;
                        }
                        Route tmp_route = route.Copy();
                        for (int k = tmp_route.RouteList.Count - 2; k >0; k--)
                        {
                            AbsNode node = tmp_route.RouteList[k];
                            if (node.Info.Type==3 || node.Info.ReadyTime>cuttingpoint) //倒序删除说有充电站及下午的商户
                            {
                                if (node.Info.Type==3)
                                {
                                    route.Remove((Station)node);
                                }
                                if (node.Info.Type==2)
                                {
                                    route.Remove((Customer)node);
                                    solution.UnVisitedCus.Add((Customer)node);
                                }

                            }                      
                        }
                        if (!route.IsFeasible())
                        {
                            route =  route.InsertSta(3);
                        }
                        solution.Routes[pos_route_sol] = route;
                        solution.fleet.VehFleet[i].VehRouteList[j] = route.RouteId;
                    }
                }//结束对一辆车下所有线路的遍历
            }//结束对一辆车的遍历         
            return solution;
        }
        /// <summary>
        /// 修复，将未服务商户插回部分解中，此方法返回的解不一定比原解费用低
        /// </summary>
        /// <param name="solution"></param>
        /// <returns></returns>
        public Solution RepairToFeasible(Solution solution)
        {
            while (solution.UnVisitedCus.Count>0)
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
                }
                else
                {
                    int type = 2;
                    Vehicle veh = solution.fleet.addNewVeh(type); //先生成辆车
                    int pos_veh_fleet = solution.fleet.GetVehIdxInFleet(veh.VehId);    //车在车队中的位置              
                    Route newRoute = new Route(veh,veh.Early_time); //再生成个路线
                    newRoute.InsertNode(cus, 1);
                    veh.addRoute2Veh(newRoute);//把路分配给车
                    
                    if (newRoute.ViolationOfRange()>0)
                    {
                        newRoute = newRoute.InsertSta(3,double.MaxValue);
                    }
                    
                    solution.AddRoute(newRoute); //把路添加到解里
                    solution.fleet.VehFleet[pos_veh_fleet].VehRouteList[newRoute.RouteIndexofVeh] = newRoute.RouteId;//更新车队中此车的线路集合
                    
                }
                solution.UnVisitedCus.Remove(cus);
            }
            solution.UpdateFirstTripTime();
            solution.UpdateTripChainTime();
            Console.WriteLine(solution.SolutionIsFeasible().ToString());
            return solution;
        }

        public Solution RepairToBetter(Solution solution)
        {
            Solution bst_sol = solution.Copy();

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
                    int idx_veh = solution.fleet.GetVehIdxInFleet(nr.AssignedVeh.VehId);
                    solution.fleet.VehFleet[idx_veh].VehRouteList[nr.RouteIndexofVeh] = nr.RouteId;
                }
                else
                {
                    int type = 2;
                    Vehicle veh = solution.fleet.addNewVeh(type);
                    Route newRoute = new Route(veh,veh.Early_time);
                    newRoute.InsertNode(cus, 1);
                    if (newRoute.ViolationOfRange() > 0)
                    {
                        newRoute = newRoute.InsertSta(3, double.MaxValue);
                    }
                    solution.AddRoute(newRoute);
                    veh.VehRouteList.Add(newRoute.RouteId);
                }
                solution.UnVisitedCus.Remove(cus);
            }
            solution.UpdateFirstTripTime();
            solution.UpdateTripChainTime();
            double new_obj = solution.CalObjCost();
            if (solution.ObjVal<bst_sol.ObjVal)
            {
                return solution;
            }
            else
            {
                return bst_sol;
            }
           
        }
        /// <summary>
        /// 在当前解中为某一商户寻找一个可行的插入位置,不破坏当前解的路径结构
        /// </summary>
        /// <param name="solution">当前解</param>
        /// <param name="customer">商户</param>
        /// <param name="idx_pos_route">输出插入路线上的位置</param>
        /// <returns>插入的路线，找到能使插入后该路线成本增加最小的方案</returns>
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
                            if (solution.CheckNxtRoutesFeasible(veh,tmp_r.RouteIndexofVeh, delay))
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
