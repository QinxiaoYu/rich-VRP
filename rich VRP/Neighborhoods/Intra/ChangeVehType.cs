using OP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rich_VRP.Neighborhoods.Intra
{
    class VehTypeChangeIntra
    {
        Random rd = new Random();
        /// <summary>
        /// Changes to LV eh.
        /// </summary>
        /// <returns>The to LV eh.</returns>
        /// <param name="sol">Sol.</param>
        //public Solution ChangeToLVeh(Solution sol)
        //{

        //}

        /// <summary>
        /// 遍历所有车，将所有大车上能换成小车的线路都换成小车小路及小车
        /// 该方法返回的解不会变差，路线不变，仅改变车型
        /// </summary>
        /// <returns>非劣解</returns>
        /// <param name="sol">原解</param>
        public Solution ChangeToSVehWithoutCharge(Solution sol)
        {
            VehicleType sVehType = Problem.GetVehTypebyID(1);

            Solution new_sol = sol.Copy();
            foreach (Vehicle veh in sol.fleet.VehFleet)
            {
                if (veh.TypeId == 1)//小车无需转换
                {
                    continue;
                }
                //大车跑的所有线路都能换成小车
                bool AllRouteChange = true;
                foreach (string route_id in veh.VehRouteList)
                {
                    int pos_route_sol = -1;
                    Route old_route = sol.GetRouteByID(route_id, out pos_route_sol);
                    double veh_w = old_route.GetTotalWeight();
                    double veh_v = old_route.GetTotalVolume();
                    double veh_l = old_route.GetRouteLength();
                    double veh_wait = old_route.GetWaitTime();
                    //如果超重，或者时间很紧凑 则不换
                    if (veh_w > sVehType.Weight || veh_v > sVehType.Volume || veh_l > sVehType.MaxRange)
                    {
                        AllRouteChange = false;
                        break;
                    }
                }
                if (AllRouteChange == false) //该车上的路线并不能全转换成小车，
                {
                    continue;
                }
                //肯定能换
                int pos_veh_fleet = sol.fleet.GetVehIdxInFleet(veh.VehId);
                Vehicle new_veh = veh.ChangeToAnotherType();
                for (int i = 0; i < veh.VehRouteList.Count; i++)//修改车上的每条路
                {
                    int pos_route_sol = -1;
                    Route old_route = sol.GetRouteByID(veh.VehRouteList[i], out pos_route_sol);
                    Route new_route = old_route.ChangeToAnotherType();
                    new_route.AssignedVehID = new_veh.VehId;
                    new_veh.VehRouteList[new_route.RouteIndexofVeh] = new_route.RouteId;
                    new_sol.Routes[pos_route_sol] = new_route;
                    new_sol.fleet.VehFleet[pos_veh_fleet] = new_veh;
                }
            }
            new_sol.SolutionIsFeasible();
            return new_sol;
        }
        /// <summary>
        /// 仅针对只有一条线路的大车，当大车重量体积都可用小车时，把大车换成小车，如果里程超出小车范围，去充电， 解可能退化
        /// </summary>
        /// <returns>The to SV eh with charge.</returns>
        /// <param name="sol">Sol.</param>
        public Solution ChangeToSVehWithCharge(Solution sol)
        {
            VehicleType sVehType = Problem.GetVehTypebyID(1);

            Solution new_sol = sol.Copy();
            foreach (Vehicle veh in sol.fleet.VehFleet)
            {
                if (veh.TypeId == 1 || veh.getNumofVisRoute() > 1)//小车无需转换
                {
                    continue;
                }
                //大车跑的所有线路都能换成小车
                bool AllRouteChange = true;
                foreach (string route_id in veh.VehRouteList)
                {
                    int pos_route_sol = -1;
                    Route old_route = sol.GetRouteByID(route_id, out pos_route_sol);
                    double veh_w = old_route.GetTotalWeight();
                    double veh_v = old_route.GetTotalVolume();
                    double veh_l = old_route.GetRouteLength();
                    double veh_wait = old_route.GetWaitTime();
                    //如果超重，或者时间很紧凑 则不换
                    if (veh_w > sVehType.Weight || veh_v > sVehType.Volume || veh_wait < 30)
                    {
                        AllRouteChange = false;
                        break;
                    }
                }
                if (AllRouteChange == false) //该车上的路线并不能全转换成小车，
                {
                    continue;
                }
                //有可能换
                int pos_veh_fleet = sol.fleet.GetVehIdxInFleet(veh.VehId);
                Vehicle new_veh = veh.ChangeToAnotherType(); //大车换成小车
                for (int i = 0; i < veh.VehRouteList.Count; i++)//修改车上的每条路
                {
                    int pos_route_sol = -1;
                    Route old_route = sol.GetRouteByID(veh.VehRouteList[i], out pos_route_sol);
                    Route new_route = old_route.ChangeToAnotherType();
                    new_route.AssignedVehID = new_veh.VehId;
                    if (new_route.ViolationOfRange() > -1) //新线路可能里程超出
                    {
                        new_route = new_route.InsertSta(3, double.MaxValue);
                    }
                    if (new_route.routecost > 30000)
                    {
                        break;
                    }

                    new_veh.VehRouteList[new_route.RouteIndexofVeh] = new_route.RouteId;
                    new_sol.Routes[pos_route_sol] = new_route;
                    new_sol.fleet.VehFleet[pos_veh_fleet] = new_veh;
                }


            }
            new_sol.SolutionIsFeasible();
            return new_sol;
        }

        public Solution ChangeToLVehWithLessCharge(Solution sol)
        {
            VehicleType LVehType = Problem.GetVehTypebyID(2);

            Solution new_sol = sol.Copy();
            foreach (Vehicle veh in sol.fleet.VehFleet)
            {
                if (veh.TypeId == 2 || veh.getNumofVisRoute() > 1)//大车或者车上线路大于1条的跳过
                {
                    continue;
                }
                int pos_route_sol = -1;
                Route old_r = sol.GetRouteByID(veh.VehRouteList[0], out pos_route_sol);
                var costs_old_r = old_r.routeCost();
                int cnt_charge = costs_old_r.Item4;
                double old_l = old_r.GetRouteLength();
                if (cnt_charge == 0 || old_l > cnt_charge * LVehType.MaxRange) //小车没充电 或者充完电所跑里程大于大车里程 跳过
                {
                    continue;
                }
                Vehicle old_v = sol.fleet.GetVehbyID(old_r.AssignedVehID);
                int pos_veh_fleet = sol.fleet.GetVehIdxInFleet(old_v.VehId);
                Vehicle new_v = old_v.ChangeToAnotherType();
                Route new_r = old_r.ChangeToAnotherType();
                new_r = new_r.InsertSta(3, double.MaxValue);
                if (new_r.routecost > 30000) //换成大车后，没有合适的充电方案，impossible
                {
                    continue;
                }
                if (rd.NextDouble()>0.3)
                {
                    continue;
                }
                new_r.RouteAssign2Veh(new_v);
                new_v.VehRouteList[new_r.RouteIndexofVeh] = new_r.RouteId;
                new_sol.Routes[pos_route_sol] = new_r.Copy();
                new_sol.fleet.VehFleet[pos_veh_fleet] = new_v.Copy();
                Console.WriteLine(new_r.RouteId);
            }
            new_sol.SolutionIsFeasible();
            new_sol.printCheckSolution();
            return new_sol;
        }
    }
}