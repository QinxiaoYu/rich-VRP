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
        Problem problem;

        List<Customer> unvisitedCus;

        Random rd = new Random();

        public RouteFirstStationSecond(Problem _p)
        {
            problem = _p;
            unvisitedCus = new List<Customer>(problem.Customers);
        }

        /// <summary>
        /// 初始化一个可行解
        /// </summary>
        /// <param name="_numR">预设线路数量，假设都用大车，至少需要的线路数量</param>
        /// <param name="randVeh">预设车型是否纯单一或混合</param>
        /// <returns></returns>
        public Solution Initial_paralle(int _numR,bool randVeh = false)
        {
            Solution solution = new Solution(problem);

            int Num_VehTypes = problem.VehTypes.Count();

            //Step 0. 初始化一些空线路
            if (randVeh == true) //如果使用混合车型,则需要更多线路
            {
                _numR = (int)1.3 * _numR;
                while (_numR>0)
                {
                    int rd_int = rd.Next(Num_VehTypes);
                    VehicleType rd_vt = problem.VehTypes[rd_int]; //随机车型
                    Route route = new Route(problem, rd_vt);
                    solution.AddRoute(route);
                    _numR--;
                }
            }
            else
            {
                while(_numR>0)
                {
                    VehicleType rd_vt = problem.VehTypes[Num_VehTypes - 1]; //大车型
                    Route route = new Route(problem, rd_vt);
                    solution.AddRoute(route);
                    _numR--;
                }
            }

            //Step 0.1 早上第一趟的第一个点，去这些点没有等待时长，先插入路径
            FixFirstCusofRoute(solution);
        
            //Step 1. 将点分配给路线；这一过程只考虑体积、重量、时间窗约束，不考虑充电

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
                VehicleType rd_vt = problem.VehTypes[rd_int]; //随机车型
                Route route = new Route(problem, rd_vt);
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
                VehicleType rd_vt = problem.VehTypes[Num_VehTypes - 1]; //大车型
                Route route = new Route(problem, rd_vt);
                BuildFeasibleRoute(route);
                solution.AddRoute(route);            
            }

            //Step 3. 检查多条线路是否可以指派给同一辆车。
            AssignRoutes2Vehs(solution);

            return solution;
        }

        private void FixFirstCusofRoute(Solution solution)
        {
            List<Customer> tmp_unvisitCus = new List<Customer>(unvisitedCus);
            int cnt = 0; //路径计数器
            foreach (var cus in tmp_unvisitCus)
            {
                int waittime = (int)cus.Info.ReadyTime - problem.StartDepot.TravelTime(cus);
                if (waittime<=this.problem.StartDepot.Info.ReadyTime)
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
        /// 在当前解中为某一商户寻找一个可行的插入位置,不破化当前解的路径结构
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
                //判断这条路线是否open
                if (solution.Routes[i].GetOpen())
                {
                    Route r = solution.Routes[i];
                    double r_v = r.GetTotalVolume();
                    double r_w = r.GetTotalWeight();
                    int num_cus = r.RouteList.Count;
                    var preNode = r.RouteList[num_cus - 2]; //前一个点
                    var nxtNode = r.RouteList[num_cus - 1]; //后一个点

                    double departuretime_pre = r.ServiceBeginingTimes[num_cus - 2] + preNode.Info.ServiceTime;
                    double min_cost_chage = double.MaxValue; //插入一个点后增加的费用
                    Customer best_cus = null;
                    bool NodesLeftInNeighbours = false;
                    if (preNode.Info.Type == 2) //前一个点是商户
                    {
                        int[] Neighbours_id = problem.GetNearDistanceCus(preNode.Info.Id);

                        for (int j = 0; j < Neighbours_id.Count(); j++)
                        {

                            Customer cus2insert = problem.SearchCusbyId(Neighbours_id[j]);
                            //判断该商户是否还在未访问列表
                            if (!unvisitedCus.Exists((Customer c) => c.Info.Id == cus2insert.Info.Id ? true : false))
                            {
                                continue;
                            }
                            NodesLeftInNeighbours = true;
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
                            //最后判断插入之后引起的长度增加量
                            double trans_cost_change = (preNode.TravelDistance(cus2insert) + cus2insert.TravelDistance(nxtNode)
                                - preNode.TravelDistance(nxtNode)) * r.AssignedVehType.VariableCost;

                            //最最后 看一下插入的这个点，他的小邻域里的邻居数量
                            int num_neighbour_cus = problem.GetNearDistanceCus(cus2insert.Info.Id).Count();

                            trans_cost_change = trans_cost_change / num_neighbour_cus; //距离增加越少越好，后续可选小邻居越多越好
                            if (trans_cost_change < min_cost_chage)
                            {
                                min_cost_chage = trans_cost_change;
                                best_cus = cus2insert;
                            }
                        }
                        
                    }
                    if (preNode.Info.Type == 1 || NodesLeftInNeighbours == false)
                    {
                       foreach (var cus2insert in unvisitedCus)
                        {
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
                            //最后判断插入之后引起的长度增加量
                            double trans_cost_change = (preNode.TravelDistance(cus2insert) + cus2insert.TravelDistance(nxtNode)
                                - preNode.TravelDistance(nxtNode)) * r.AssignedVehType.VariableCost;
                            //最最后 看一下插入的这个点，他的小邻域里的邻居数量
                            int num_neighbour_cus = problem.GetNearDistanceCus(cus2insert.Info.Id).Count();

                            trans_cost_change = trans_cost_change / num_neighbour_cus; //距离增加越少越好，后续可选小邻居越多越好

                            if (trans_cost_change < min_cost_chage)
                            {
                                min_cost_chage = trans_cost_change;
                                best_cus = cus2insert;
                            }
                        }

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
