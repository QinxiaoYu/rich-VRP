using OP.Data;
//using rich_VRP.ObjectiveFunc;
using System;
using System.Collections.Generic;

public class BreakTwoRoute
{
    Random rd;
    public BreakTwoRoute()
    {
        rd = new Random();
    }
    /// <summary>
    /// 对这辆车的这条路进行拆分
    /// </summary>
    /// <param name="solution"></param>
    /// <param name="break_strategy">拆分方法1: 断开路径中距离最远的边 break_strategy = 1
                                    //拆分方法2: 一没电就回配送中心 break_strategy = 2
                                    //拆分方法3: 12点左右回到配送中心 break_stategy = 3</param>
    /// <returns></returns>
    public Solution Break(Solution solution, int break_strategy)
    {
        Solution new_sol = solution.Copy();
        for (int i = 0; i < solution.fleet.VehFleet.Count; i++)
        {
            Vehicle old_v = solution.fleet.VehFleet[i];
            if (old_v.getNumofVisRoute() > 1)
            {
                continue;
            }
            int pos_route_sol = -1;
            Route old_r = solution.GetRouteByID(old_v.VehRouteList[0], out pos_route_sol);
            Route copy_old_r = old_r.Copy();
            copy_old_r.RemoveAllSta();
            if (copy_old_r.RouteList.Count < 4) //只有1个客户，没法break
            {
                continue;
            }
            //对这辆车的这条路进行拆分
            //拆分方法1: 断开路径中距离最远的边 break_strategy = 1
            //拆分方法2: 一没电就回配送中心 break_strategy = 2
            //拆分方法3: 12点左右回到配送中心 break_stategy = 3
            if (break_strategy == 1)
            {
                Vehicle new_veh = old_v.Copy();
                new_veh.VehRouteList.Clear();
                //定位最长边所在位置，不能是第一条也不能是最后一条边，返回的是断点位置，如断第二条边，则返回2
                int pos_longest_arc = copy_old_r.FindLongestArc();
                Route new_r1 = new Route(new_veh, new_veh.Early_time);
                for (int j = 1; j < pos_longest_arc; j++)
                {
                    new_r1.InsertNode(copy_old_r.RouteList[j], j);
                }
                if (new_r1.battery_level[new_r1.RouteList.Count - 1] < 0)
                {
                    new_r1 = new_r1.InsertSta(3, double.MaxValue);
                    //找不到可行的插入充电站方案，时间窗违反
                    if (new_r1.routecost > 300000)
                    {
                        //throw new Exception("IMPOSSIBLE！！！该路需要3个以上充电站");
                        continue;
                    }
                }
                new_r1.RouteIndexofVeh = 0;
                //生成第二条路
                double nxt_dt = new_r1.GetArrivalTime() + Problem.MinWaitTimeAtDepot;
                if (nxt_dt > copy_old_r.RouteList[pos_longest_arc].Info.DueDate)
                {
                    continue;
                }
                Route new_r2 = new Route(new_veh, nxt_dt);
                int tmp_count = 1;
                for (int j = pos_longest_arc; j < copy_old_r.RouteList.Count - 1; j++)
                {
                    new_r2.InsertNode(copy_old_r.RouteList[j], tmp_count);
                    tmp_count++;
                }
                if (new_r2.ViolationOfTimeWindow()>-1)
                {
                    continue;
                }
                if (new_r2.battery_level[new_r2.RouteList.Count - 1] < 0)
                {
                    new_r2 = new_r2.InsertSta(3, double.MaxValue);
                    //找不到可行的插入充电站方案，时间窗违反
                    if (new_r2.routecost > 300000)
                    {
                        continue;
                        //throw new Exception("IMPOSSIBLE！！！该路需要3个以上充电站");
                    }
                }
                new_r2.RouteIndexofVeh = 1;

                new_veh.addRoute2Veh(new_r1);
                new_veh.addRoute2Veh(new_r2);
                new_r1.RouteAssign2Veh(new_veh);
                new_r2.RouteAssign2Veh(new_veh);
                //对new_sol做更改,veh位置不变，route删除
                int pos_route_newsol = -1;
                Route r = new_sol.GetRouteByID(old_r.RouteId,out pos_route_newsol);
                new_sol.Routes.RemoveAt(pos_route_newsol);
                new_sol.AddRoute(new_r1);
                new_sol.AddRoute(new_r2);
                new_sol.fleet.VehFleet[i] = new_veh.Copy();
                Console.WriteLine(new_sol.SolutionIsFeasible().ToString());
            }
            //拆分方法2: 一没电就回配送中心 break_strategy = 2
            if (break_strategy  == 2)
            {
                int num_cus = copy_old_r.RouteList.Count - 2;
                Vehicle new_veh = old_v.Copy();
                new_veh.VehRouteList.Clear();
                int tmp_count = 1;
                double tmp_battery = copy_old_r.AssignedVehType.MaxRange;
                double nxt_dt = new_veh.Early_time;
                bool isSucc = true;
                Route new_r = new Route(new_veh, nxt_dt);
                List<Route> list_routes_changed = new List<Route>();
                while (tmp_count<=num_cus)
                {
                    int nxt_pos = new_r.RouteList.Count - 1;
                    double nxt_charge = new_r.RouteList[nxt_pos-1].TravelDistance(copy_old_r.RouteList[tmp_count]);
                    if (tmp_battery-nxt_charge>0)
                    {
                        new_r.InsertNode(copy_old_r.RouteList[i], nxt_pos);
                        tmp_count++;

                    }
                    //跑到下一个将回不到充电站，就此结束此线路
                    else
                    {
                        //如果此线路违反时间窗，也不行
                        if (new_r.ViolationOfTimeWindow()>-1)
                        {
                            isSucc = false;
                            break;
                        }
                        Route copy_new_r = new_r.Copy();
                        new_veh.addRoute2Veh(copy_new_r);
                        copy_new_r.RouteAssign2Veh(new_veh);
                        list_routes_changed.Add(copy_new_r);
                        //下一趟开始时间窗不可行，也不行
                        nxt_dt = copy_new_r.GetArrivalTime() + Problem.MinWaitTimeAtDepot;
                        if (nxt_dt> copy_old_r.RouteList[tmp_count].Info.DueDate)
                        {
                            isSucc = false;
                            break;
                        }
                        new_r = new Route(new_veh, nxt_dt);
                    }
                }
                if (isSucc)
                {
                    new_sol.Routes.RemoveAt(pos_route_sol);
                    new_sol.Routes.AddRange(list_routes_changed);
                    new_sol.fleet.VehFleet[i] = new_veh.Copy();
                }else
                {
                    continue;
                }
            }
            ////拆分方法3: 12点左右回到配送中心 break_stategy = 3
            //if (break_strategy == 3)
            //{

            //}

            ////拆分方法4: 遍历插入配送中心

        }
        new_sol.printCheckSolution();
        return new_sol;

    }
}