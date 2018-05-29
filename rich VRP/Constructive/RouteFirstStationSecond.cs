using OP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rich_VRP.Constructive
{
    class RouteFirstStationSecond
    {
        Problem problem;
        Fleet fleet;
        List<Customer> unvisitedCus;

        Random rd = new Random();

        public RouteFirstStationSecond(Problem _p)
        {
            problem = _p;
            fleet = problem.fleet;
            unvisitedCus = new List<Customer>(problem.Customers);
        }


        public Solution Initial_paralle(bool randVeh = false)
        {
            Solution solution = new Solution(problem);

            //Step 1. 将点分配给路线；这一过程只考虑体积、重量、时间窗约束，不考虑充电
            while (unvisitedCus.Count > 0)
            {
                int Num_VehTypes = fleet.VehTypes.Count();
                VehicleType rd_vt = fleet.VehTypes[Num_VehTypes - 1]; //大车型
                if (randVeh)
                {
                    int rd_int = rd.Next(Num_VehTypes);
                    rd_vt = fleet.VehTypes[rd_int]; //随机车型
                }
                Route route = new Route(problem, rd_vt);
                solution.AddRoute(route);
                bool flag = true;
                do
                {
                    int idx_route = 0;
                    Customer _nextcus = null;
                    _nextcus = FindBestRoute(solution, out idx_route);
                    if (_nextcus != null)
                    {
                        Route _r = solution.Routes[idx_route];
                        _r.InsertNode(_nextcus, _r.RouteList.Count - 1);
                        unvisitedCus.Remove(_nextcus);
                    }
                    else //当前解中找不到合适的路线 可以继续往里面加入商户，则退出do while，新产生一条线路
                    {
                        flag = false;
                    }
                } while (flag);
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

                int Num_VehTypes = fleet.VehTypes.Count();
                VehicleType rd_vt = fleet.VehTypes[Num_VehTypes - 1]; //大车型
                Route route = new Route(problem, rd_vt);
                BuildFeasibleRoute(route);
                solution.AddRoute(route);            
            }

            //Step 3. 检查多条线路是否可以指派给同一辆车。
            AssignRoutes2Vehs(solution);

            return solution;
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

        private Customer FindBestRoute(Solution solution, out int idx_route)
        {
            throw new NotImplementedException();
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
