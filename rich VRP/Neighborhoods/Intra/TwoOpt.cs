using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OP.Data;
using rich_VRP.Constructive;
using System.Collections;

namespace rich_VRP.Neighborhoods.Intra
{
    class TwoOpt
    {
        //进行路径间的交换，活动
        public List<Route> intraChange(Route originRoute, Vehicle vehicle, List<Route> routesList)
        {
            double ccr = Problem.VehTypes[vehicle.TypeId - 1].ChargeCostRate;
            double tcr = Problem.VehTypes[vehicle.TypeId - 1].VariableCost;
            double fct = Problem.VehTypes[vehicle.TypeId - 1].FixedCost;

            Route BestRoute = originRoute.Copy();
            Vehicle temVehicle = vehicle.Copy();
            double bestCost = vehicle.total_cost;
            double origiArrTime = originRoute.GetArrivalTime();

            //当前路径在routelist中的position
            int routeIndexOfVehicle = originRoute.RouteIndexofVeh;

            //产生交换点的路径
            for (int i = 1; i < originRoute.RouteList.Count - 2; i++)
            {
                for (int j = i + 1; j < originRoute.RouteList.Count - 1; j++)
                {
                    double tempCost = vehicle.total_cost;
                    Route tempRoute = originRoute.Copy();
                    AbsNode absNode1 = originRoute.RouteList[i].ShallowCopy();
                    AbsNode absNode2 = originRoute.RouteList[j].ShallowCopy();
                    tempRoute.RemoveAt(i);
                    tempRoute.InsertNode(absNode2, i);
                    tempRoute.RemoveAt(j);
                    tempRoute.InsertNode(absNode1, j);
                    if (!tempRoute.isEqual(BestRoute))//确定生成路径
                    {
                        //首先判断新生成的路径是否可行，然后判断是否满足后续的运输要求，最后判断新生成路径是否成本减小，如果成本减小则更新route,反之不更新
                        if (tempRoute.IsFeasible())
                        {
                            //生成临时的routesList
                            List<Route> temRoutesList = new List<Route>();
                            routesList.ForEach(k => temRoutesList.Add(k));
                            //替换
                            temRoutesList[routeIndexOfVehicle] = tempRoute;
                            //判读是否相等
                            if (routesList.Equals(temRoutesList))
                            {
                                System.Console.WriteLine("出现了浅复制问题");
                            }
                            //判断时间提前还是推迟，如果提前加入等待成本，如果推迟判断是否满足时间窗的要求，如果满足重新计算成本，反之直接跳出
                            double temArrTime = tempRoute.GetArrivalTime();//需要重新计算
                            if (temArrTime < origiArrTime)//如果时间提前
                            {
                                Double waitCost1 = (origiArrTime - temArrTime) * Problem.WaitCostRate;
                                //总成本-原路径的成本+先路径成本+等待成本
                                tempCost = tempCost - originRoute.routecost + tempRoute.routecost + waitCost1;
                            }
                            else
                            {
                                double delayTime = temArrTime - origiArrTime;
                                //判断是否可以满足后续服务，跟新起点开始时间，看后续是否
                                if (vehicle.CheckNxtRoutesFeasible(routeIndexOfVehicle, delayTime, routesList))
                                {

                                    //计算整个routelist的成本
                                    tempCost = Vehicle.calculCost(temRoutesList, vehicle.TypeId);

                                }
                            }

                        }

                    }
                    if (tempCost < bestCost)
                    {
                        //替换路径
                        BestRoute = tempRoute.Copy();

                    }
                }
            }
            //替换在车辆的路径列表
            routesList[routeIndexOfVehicle] = BestRoute;
            return routesList;
        }

        //转入solution的中的fleet,对里面的每一辆车中的路径进行变异
        public Solution intarChange(Solution solution)
        {
            foreach (var vehicle in solution.fleet.VehFleet)
            {
                //对车辆的中的找到vehicle的在solution的所有路径的副本
                List<Route> routesList = new List<Route>();
                //这些路径在solution中的位置
                List<int> routePos = new List<int>();

                //将所有的route从solution中取出
                foreach (var routID in vehicle.VehRouteList)
                {
                    int pos = 0;
                    routesList.Add(solution.GetRouteByID(routID, out pos).Copy());
                    routePos.Add(pos);
                }

                //对副本其中的路径进行路内的交换操作
                for (int i = 0; i < routesList.Count; i++)
                {
                    //routesList[i] = intraChange(routesList[i], routesList, vehicle);
                    routesList = intraChange(routesList[i], vehicle, routesList);
                }

                //将优化的路替换solution中route,修改fleet中的vehicle
                int index = 0;
                foreach (var pos in routePos)
                {
                    //solution.Routes[pos] = routesList[index];
                    solution.Routes[pos].RouteList.Clear();
                    routesList[index].RouteList.ForEach(k => solution.Routes[pos].RouteList.Add(k));
                    index += 1;
                }

            }
            return solution;

        }
    }
}

