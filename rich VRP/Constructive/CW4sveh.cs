using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OP.Data;

namespace rich_VRP.Constructive
{
    class CW4sveh
    {
      
        Fleet fleet;
        Random rand = new Random();//随机operter
        List<Customer> unrouted_Cus = new List<Customer>();
        List<Station> charge_sta = new List<Station>();
        public CW4sveh()//把带初始化的问题传进来
        {
                   
        }


        public Solution initial_construct()
        {
            Solution solution = new Solution();
            fleet = solution.fleet;
            var unroute_cus = new List<Customer>(Problem.Customers); //没有访问的点
            Vehicle veh = null;
            List<int> BigCustomers = new List<int>();// new List<int> { 154,608,657,718,745,822};
            int type = 1; //默认小车
            Route newRoute = null;
            while (unroute_cus.Count > 0)
            {
                //int type = rand.Next(0, 2) + 1; //随机产生一辆车（类型随机）
                if (BigCustomers.Count>0)
                {

                    Customer bigCus = Problem.SearchCusbyId(BigCustomers[0]);
                    type = 2;
                    veh = fleet.addNewVeh(type);
                    newRoute = new Route(veh);
                    newRoute.InsertNode(bigCus, 1);
                    if (newRoute.RouteList.Count>2)
                    {
                        BigCustomers.RemoveAt(0);
                        unroute_cus.Remove(bigCus);
                    }
                }
                else
                {
                    type = 1;
                    veh = fleet.addNewVeh(type);
                    newRoute = new Route(veh); ////////产生一条该车的路径,已经把车分配给了路径
                }
                         
                double earliest_departure_time = newRoute.GetEarliestDepartureTime();//该路径的最早出发时间
                //只要新产生路径的最早出发时间小于最晚时间限制就可以为其分配customer
                while (earliest_departure_time < veh.Late_time)
                {
                    newRoute = BIA(newRoute, unroute_cus, out unroute_cus);
                    if (newRoute.RouteList.Count > 2)//此路线插入customer了
                    {
                        veh.addRoute2Veh(newRoute);//将路径加入到vehicle中
                        solution.AddRoute(newRoute);
                        
                        if (unroute_cus.Count == 0)//是否还有未插入的点
                        {
                            earliest_departure_time = veh.Late_time;
                        }
                        else
                        {
                            newRoute = new Route( veh);
                            //newRoute.RouteAssign2Veh(veh);//将路径分配给该车
                            earliest_departure_time = newRoute.GetEarliestDepartureTime();
                        }
                    }
                    else
                    {
                        earliest_departure_time = veh.Late_time;
                    }
                }
                int a = fleet.GetNumOfUsedVeh();
                Console.WriteLine(a);
                
            }
            solution.UpdateFirstTripTime();
            return solution;
        }



        /// <summary>
        /// best insert althgram
        /// </summary>
        /// <param name="route"></param>
        /// <param name="unroute_cus"></param>
        /// <param name="left_unroute_cus"></param>
        /// <returns></returns>
        public Route BIA(Route route, List<Customer> unroute_cus, out List<Customer> left_unroute_cus)
        {
            double violation_volume = route.ViolationOfVolume();//若不违反返回0
            double violation_weight = route.ViolationOfVolume();//若不违反返回0
            //有总的行驶里程约束吗？？？？

            double insert_feasible = violation_volume + violation_weight;//只有体积和重量限制没有违反才能继续往路径里插入新的点
            Route best_route = route.Copy();
           
            while (insert_feasible == 0)
            {
                double best_change_obj = double.MaxValue; //一个无穷大的数
                var old_costs = best_route.routeCost();
                double old_obj = old_costs.Item1 + old_costs.Item2 + old_costs.Item3;
                double alefa = rand.NextDouble(); //产生0~1的随机数，评价标准的参数          
                bool inserted = false;//记录本次循环是否插入了点
                Customer inserted_cus = null;//最终确定要插入的点
                Route cur_route_i = best_route.Copy();
                for (int i = 0; i < unroute_cus.Count; i++)
                {
                    Customer insert_cus = unroute_cus[i];
                    int num_cus = cur_route_i.RouteList.Count;
                    for (int j = 1; j < cur_route_i.RouteList.Count; j++)//第一个位置和最后一个位置不能插入
                    {
                        Route cur_route_j = cur_route_i.Copy();
                        cur_route_j.InsertNode(insert_cus, j);//插入
                        
                        ///////////////插入电站:在插入点前、后、前和后或者都不插入四种情况////////////////////////////////////
                        
                        Station after_sta = cur_route_j.insert_sta(insert_cus);//若要在insert_cus后插入电站，应该插入哪个？
                        AbsNode after_sta1 = null;
                        if (j == num_cus - 1)
                        {
                            after_sta1 = Problem.EndDepot;
                        }
                        else
                        {
                            after_sta1 = cur_route_j.insert_sta(cur_route_j.RouteList[j + 1]);//若要在insert_cus后一个客户后面插入电站，应该插入哪个？
                        }
                        
                        Station before_sta = cur_route_j.insert_sta(cur_route_j.RouteList[j - 1]);//若要在insert_cus前插入电站，应该插入哪个？
                        ///判断是否需要在insert-cus后插入充电站
                        ///如果剩余电量能够维持车辆行驶至下custoner后再充电
                        double after_dis = insert_cus.TravelDistance(cur_route_j.RouteList[j + 1]) + cur_route_j.RouteList[j + 1].TravelDistance(after_sta1);
                        if (cur_route_j.battery_level[j] < after_dis)//如果剩余电量不能坚持到下次充电
                        {
                            cur_route_j.InsertNode(after_sta, j + 1);//在insert-cus后插入电站
                           
                        }

                        ///判断是否需要在插入点前插入充电站
                        //如果剩余电量不能保证下次抵达充电桩,就在insert_cus前插入充电站
                        double before_dis = insert_cus.TravelDistance(cur_route_j.RouteList[j - 1]) + insert_cus.TravelDistance(after_sta);
                        if (cur_route_j.battery_level[j - 1] < before_dis)
                        {
                            cur_route_j.InsertNode(before_sta, j);//在insert-cus前插入电站
                           
                        }


                        ////////////////选择最优的一次插入////////////////////////////////////////////////////
                        if (cur_route_j.IsFeasible())//如果插入customer和相应的station后满足所有约束
                        {
                            var new_costs = cur_route_j.routeCost();
                            double new_obj = new_costs.Item1 + new_costs.Item2 + new_costs.Item3; 
                            double probability = (new_obj - old_obj - best_change_obj) / best_change_obj; //当(new_obj-old_obj)最小时，得到一个确定性初始解
                            double rd = rand.NextDouble()/2;
                            if (probability< 0)
                            {
                                best_change_obj = new_obj-old_obj;
                                best_route = cur_route_j.Copy();
                                inserted = true;
                                inserted_cus = insert_cus;
                            }                          
                        }

                    }
                }
                if (!inserted)//如果在本次循环中没有插入新的点，说明该路径接近饱和，退出while循环
                {
                    insert_feasible = 1;
                }
                else
                {
                    unroute_cus.Remove(inserted_cus);
                    violation_volume = best_route.ViolationOfVolume();//若不违反返回0
                    violation_weight = best_route.ViolationOfVolume();//若不违反返回0
                    insert_feasible = violation_volume + violation_weight;//只有体积和重量限制没有违反才能继续往路径里插入新的点
                }
            }

            left_unroute_cus = unroute_cus;
            return best_route;
        }


    }
}
