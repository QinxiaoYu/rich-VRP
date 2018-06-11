using OP.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rich_VRP.Constructive
{
    class RouteFirstStationSecond
    {
        List<Customer> unvisitedCus;
        Random rd = new Random();
        public RouteFirstStationSecond()
        {
            unvisitedCus = new List<Customer>(Problem.Customers);
        }

        /// <summary>
        /// 初始化一个可行解
        /// </summary>
        /// <param name="_numR">预设线路数量，假设都用大车，至少需要的线路数量</param>
        /// <param name="randVeh">预设车型是否纯单一或混合</param>
        /// <returns></returns>
        public Solution Initial_paralle(int _numR,bool randVeh = false)
        {
            Solution solution = new Solution();

            int Num_VehTypes = Problem.VehTypes.Count();

            //Step 0. 初始化一些空线路
            if (randVeh == true) //如果使用混合车型,则需要更多线路
            {
                _numR = (int)1.3 * _numR;
                while (_numR>0)
                {
                    int rd_int = rd.Next(Num_VehTypes);
                    VehicleType rd_vt = Problem.VehTypes[rd_int]; //随机车型
                    Route route = new Route(rd_vt);
                    solution.AddRoute(route);//只把线路加入解中，还未分配具体车辆
                    _numR--;
                }
            }
            else
            {
                while(_numR>0)
                {
                    VehicleType rd_vt = Problem.VehTypes[Num_VehTypes - 1]; //大车型
                    Route route = new Route(rd_vt);
                    solution.AddRoute(route);
                    _numR--;
                }
            }

            //Step 0.1 早上第一趟的第一个点，去这些点没有等待时长，先插入路径
            FixFirstCusofRoute(solution);
        
            //Step 1. 将点分配给路线；分为三种情况
            //（1）去第一个商户比较近，如百分之30电量内，则该线路默认为短线路，用小车，只跑附近点，装满就回去
            //（2）去第一个商户比较远，如已占用百分之50电量，则该线路默认为长线路，用大车，可以充电
            //（3) 中间区域商户，随机。。。           

            while (unvisitedCus.Count > 0)
            {                              
                bool flag = true;
                do
                {
                  
                    bool isEnought = true; //标记当前解中的路径是否能够容纳所有商户
                    isEnought = FindBestRoute(solution); 
                    if (isEnought == false)
                    {
                        flag = false;
                    }            
                } while (flag);
                int rd_int = rd.Next(Num_VehTypes);
                VehicleType rd_vt = Problem.VehTypes[rd_int]; //随机车型
                Route route = new Route(rd_vt);
                solution.AddRoute(route);
            }
            //Step 2. 对每条线路，考虑里程约束，加入充电站；加入充电站后，将不满足时间窗的商户重新放回unvisitedCus。
            for (int i = 0; i < solution.Routes.Count; i++)
            {
                Route tmp_r = solution.Routes[i];
                int first_rock = tmp_r.ViolationOfRange();
                while (first_rock > -1)
                {
                    RepairRouteByInstSta(tmp_r);
                }
            }
            //Step 3. 针对被退回的商户，寻找合适的插入线路和位置，此时的插入要考虑所有约束条件。
            //首先，检查是否可以在当前解中插入这些被退回商户
            List<Customer> still_unvisitedcus = new List<Customer>(unvisitedCus);
            foreach (var cus in unvisitedCus)
            {
                int idx_route = -1;
                int idx_pos_route = -1;
                idx_route = FindPosition(solution, cus, out idx_pos_route);//在当前解中找一个可以容身的地方
                if (idx_pos_route != -1) //在当前解中有其容身的地方
                {
                    solution.Routes[idx_route].InsertNode(cus, idx_pos_route);
                    still_unvisitedcus.Remove(cus);
                }
            }
            unvisitedCus = still_unvisitedcus;
            //之后，若还有未访问的商户，为他们新建路线
            while (unvisitedCus.Count>0)
            {
                VehicleType rd_vt = Problem.VehTypes[Num_VehTypes - 1]; //大车型
                Route route = new Route(rd_vt);
                BuildFeasibleRoute(route);
                solution.AddRoute(route);            
            }

            //Step 3. 检查多条线路是否可以指派给同一辆车。
            AssignRoutes2Vehs(solution);

            return solution;
        }

        /// <summary>
        /// 固定早上要服务的第一个商户，去此商户没有等待时长
        /// </summary>
        /// <param name="solution">Solution.</param>
        private void FixFirstCusofRoute(Solution solution)
        {
            List<Customer> tmp_unvisitCus = new List<Customer>(unvisitedCus);
            int cnt = 0; //路径计数器
            foreach (Customer cus in tmp_unvisitCus)
            {
                int waittime = (int)cus.Info.ReadyTime - Problem.StartDepot.TravelTime(cus); //从商户开始时间往回跑，到达起点的时间
                if (waittime<=Problem.StartDepot.Info.ReadyTime) //到达起点的时间如果小于起点的上班时间，则去上述商户没有等待时长
                {
                    solution.Routes[cnt].InsertNode(cus, 1);
                    cnt++;
                    unvisitedCus.Remove(cus);
                    if (cnt>=solution.Routes.Count)
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 在当前解中为某一商户寻找一个可行的插入位置,不破坏当前解的路径结构
        /// </summary>
        /// <param name="solution">当前解</param>
        /// <param name="customer">商户</param>
        /// <param name="idx_pos_route">输出插入路线上的位置</param>
        /// <returns>插入的路线，一找到可行方案就返回，不进行比较</returns>
        private int FindPosition(Solution solution, Customer customer, out int idx_pos_route)
        {
            int idx_route = -1;
            idx_pos_route = -1;
            for (int i = 0; i < solution.Routes.Count; i++)
            {
                Route route = solution.Routes[i];
                double v_route = route.GetTotalVolume();
                if (v_route+customer.Info.Volume>route.AssignedVehType.Volume)
                {
                    continue;
                }
                double w_route = route.GetTotalWeight();
                if (w_route+customer.Info.Weight>route.AssignedVehType.Weight)
                {
                    continue;
                }
                for (int j = 1; j < route.RouteList.Count; j++)
                {
                    double floattime_j = route.GetFloatTimeAtCus(j);
                    if (floattime_j>customer.Info.ServiceTime) //某点有浮动时间，才有可能往其前面加入商户
                    {
                        Route tmp_r = solution.Routes[i].Copy();
                        tmp_r.InsertNode(customer, j);
                        if (tmp_r.ViolationOfTimeWindow()==-1 && tmp_r.ViolationOfRange()==-1) //可行
                        {
                            idx_pos_route = j;
                            idx_route = i;
                            return idx_route;
                        }
                    }

                }           
            }
            return idx_route;
        }

        private void AssignRoutes2Vehs(Solution solution)
        {
            throw new NotImplementedException();
        }

        private void BuildFeasibleRoute(Route route)
        {
            throw new NotImplementedException();
        }

        private bool FindBestRoute(Solution solution)
        {
         
            int num_route = solution.Routes.Count;
            Hashtable ht = new Hashtable();
            for (int i = 0; i < num_route; i++)
            {
                Route r = solution.Routes[i];
                double dis_depot_fstNode = Problem.StartDepot.TravelDistance(r.RouteList[1]);
                if (r.RouteList.Count==3) //当前线路上已经有一个点，由这个点确定这条线路是用大车还是小车
                {
                    
                    if (dis_depot_fstNode<30)
                    {
                        r.ChangeToVehType(1);
                    }
                    if (dis_depot_fstNode>60)
                    {
                        r.ChangeToVehType(2);
                    }
                }
                double r_v = r.GetTotalVolume(); //当前体积
                double r_w = r.GetTotalWeight(); //当前重量
                double r_l = r.GetRouteLength(); //当前长度
                //判断这条路线是否open
                while (solution.Routes[i].GetOpen())
                {
                    Route tmp_r = solution.Routes[i];
                    int num_cus = r.RouteList.Count;
                    var preNode = r.RouteList[num_cus - 2]; //前一个点
                    var nxtNode = r.RouteList[num_cus - 1]; //后一个点
                    double departuretime_pre = r.ServiceBeginingTimes[num_cus - 2] + preNode.Info.ServiceTime; //从前一个点的出发时间

                    if (dis_depot_fstNode < 30) //第一个点是近距离商户,该线路默认用小车，不充电
                    {
                        if (tmp_r.battery_level[num_cus-1]<tmp_r.AssignedVehType.MaxRange*0.1) //电量已不足，线路结束
                        {
                            solution.Routes[i].SetClosed();
                            break;
                        }
                        int[] Neighbours_id = Problem.GetNearDistanceCus(preNode.Info.Id); //从近至远排序的其他商户
                        //检查插入哪个商户
                        for (int j = 0; j < Neighbours_id.Count(); j++)
                        {

                            Customer cus2insert = Problem.SearchCusbyId(Neighbours_id[j]);
                            //判断该商户是否还在未访问列表
                            if (!unvisitedCus.Exists((Customer c) => c.Info.Id == cus2insert.Info.Id ? true : false))
                            {
                                continue;
                            }
                            //首先判断重量、体积超限否
                            if (cus2insert.Info.Volume + r_v > r.AssignedVehType.Volume ||
                                cus2insert.Info.Weight + r_w > r.AssignedVehType.Weight)
                            {
                                continue;
                            }
                            //其次判断时间窗满足否                        
                            double at_cus = departuretime_pre + preNode.TravelTime(cus2insert);
                            if (at_cus > cus2insert.Info.DueDate)
                            {
                                continue;
                            }
                            //再次判断插入之后引起的长度增加量
                            double trans_change = preNode.TravelDistance(cus2insert) + cus2insert.TravelDistance(nxtNode)
                                - preNode.TravelDistance(nxtNode);
                            if (r_l + trans_change > r.AssignedVehType.MaxRange) // 超过里程，则排在后面都邻居不可能更优
                            {
                                r.SetClosed();
                                break; //不充电的话，邻点列表中其后节点都将违反里程约束
                            }
                            solution.Routes[i].InsertNode(cus2insert, r.RouteList.Count - 1); //将邻点插入路径           
                            unvisitedCus.Remove(cus2insert);
                            r_w += cus2insert.Info.Weight;
                            r_v += cus2insert.Info.Volume;
                            r_l += trans_change;
                            break; //已选择一个点插入线路，扩展下一个点
                        }
                        
                    }

                    if (dis_depot_fstNode > 60) //第一个商户是远距离商户，默认用大车，可以充电，装的越多越好
                    {
                        int[] Neighbours_id = Problem.GetNearDistanceCus(preNode.Info.Id); //从近至远排序的其他商户

                        for (int j = 0; j < Neighbours_id.Count(); j++)
                        {

                            Customer cus2insert = Problem.SearchCusbyId(Neighbours_id[j]);
                            //判断该商户是否还在未访问列表
                            if (!unvisitedCus.Exists((Customer c) => c.Info.Id == cus2insert.Info.Id ? true : false))
                            {
                                continue;
                            }
                            //判断重量、体积超限否
                            if (cus2insert.Info.Volume + r_v > r.AssignedVehType.Volume ||
                                cus2insert.Info.Weight + r_w > r.AssignedVehType.Weight)
                            {
                                continue;
                            }
                            //判断时间窗满足否                        
                            double at_cus = departuretime_pre + preNode.TravelTime(cus2insert);
                            if (at_cus > cus2insert.Info.DueDate) //不充电情况下，到达该商户仍不能满足其时间窗约束
                            {
                                continue;
                            }
                            //之后判断剩余电量能否到达其邻点商户j
                            double battery_ij = preNode.TravelDistance(cus2insert); //从i到j需要到电量
                            double battery_i = r.battery_level[num_cus - 2]; //在i点剩余电量（当前电量）
                            double battery_j = battery_i - battery_ij;//不充电情况下到j点的剩余电量
                            int sta_j_id = Problem.GetNearDistanceSta(cus2insert.Info.Id)[0];//离j点到最近的充电站
                            Station station = Problem.SearchStaById(sta_j_id);
                            double battery_j_sta = cus2insert.TravelDistance(station);//从j点到最近充电站所需电量
                            if (battery_j-battery_j_sta>0) //在i点后不充电可安全到达j
                            {
                                solution.Routes[i].InsertNode(cus2insert, r.RouteList.Count - 1); //将邻点插入路径中
                                unvisitedCus.Remove(cus2insert);
                                r_w += cus2insert.Info.Weight;
                                r_v += cus2insert.Info.Volume;
                                break; //已选择一个点插入线路，扩展下一个点
                            }else //插入j点前需要先充电
                            {
                                Route copy_r = tmp_r.Copy();
                                copy_r.InsertNode(cus2insert, copy_r.RouteList.Count - 1); //先暂时把j插入到路径中
                                //再检查能否在j点前找到一个充电站
                                bool isOKCharge = copy_r.insert_sta_between(copy_r.RouteList.Count - 2, copy_r.RouteList.Count - 1);
                                if (isOKCharge && copy_r.ViolationOfTimeWindow()==-1) //如果能找到充电站，且不违反时间窗约束
                                {
                                    solution.Routes[i]=copy_r; //将j点插入路径中
                                    unvisitedCus.Remove(cus2insert);
                                    r_w += cus2insert.Info.Weight;
                                    r_v += cus2insert.Info.Volume;
                                }else //找不到，尝试下一个邻点
                                {
                                    continue;
                                }
                            }

                        }//结束对邻点对遍历

                    }//远距离大车情况结束



                    if (preNode !=2 )
                    {

                    }

                    if (best_cus!=null)
                    {
                        ht.Add(i, best_cus);
                    }
                    else //当前路径不能再扩充了
                    {
                        r.SetClosed();
                    }                 
                }
            }//end for routes 循环

            if (ht.Count==0)
            {
                return false;
            }
            else
            {
                foreach (int idx_route in ht.Keys)
                {
                    Customer cus = (Customer)ht[idx_route];
                    if (unvisitedCus.Exists((Customer c) => c.Info.Id == cus.Info.Id ? true : false))
                    {
                        solution.Routes[idx_route].InsertNode(cus, solution.Routes[idx_route].RouteList.Count - 1);
                        unvisitedCus.Remove(cus);
                    }
                }
                return true;
            }
        }

        private void RepairRouteByInstSta(Route route)
        {
            Route tmp_r = route;
            int rock_position = tmp_r.ViolationOfRange(); //在里程内，第一个不能到的商户
            while (rock_position >-1)
            {
                //先检查rock_position之前有没有等待时间可以提前充电
                Station goodSta = null;
                int sta_position = tmp_r.FindGoodStationPosition(rock_position,out goodSta);
                if (sta_position == -1) //没有可选的充电方案，则删除该商户
                {                   
                    unvisitedCus.Add((Customer)tmp_r.RouteList[rock_position]);
                    tmp_r.RemoveAt(rock_position);
                }
                else //有可选方案，加入充电站
                {
                    tmp_r.InsertNode(goodSta, sta_position);
                }
                rock_position = tmp_r.ViolationOfRange();
            }
        }
    }
}
