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


        public DestroyAndRepair()
        {
        }
        /// <summary>
        /// 如果一条路线上商户的数量小于threshold_node个，则删除此线路
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
                    solution.Routes.RemoveAt(i);
                    int veh_id = r.AssignedVeh.VehId;
                    int idx_veh = solution.fleet.GetVehIdxInFleet(veh_id);
                    solution.fleet.VehFleet[idx_veh].VehRouteList.Remove(r.RouteId);
                    if (solution.fleet.VehFleet[idx_veh].getNumofVisRoute() == 0)
                    {
                        solution.fleet.removeVeh(veh_id);
                    }
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

        public Solution DestroyWasteRoute(Solution solution, double percent)
        {
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
                    solution.Routes.RemoveAt(i);
                    int veh_id = r.AssignedVeh.VehId;
                    int idx_veh = solution.fleet.GetVehIdxInFleet(veh_id);
                    solution.fleet.VehFleet[idx_veh].VehRouteList.Remove(r.RouteId);
                    if (solution.fleet.VehFleet[idx_veh].getNumofVisRoute() == 0)
                    {
                        solution.fleet.removeVeh(veh_id);
                    }
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

        public Solution DestroyAfternoonNodes(Solution solution, double cuttingpoint)
        {
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
                if (totalVolume < cuttingpoint * Problem.VehTypes[r.AssignedVeh.TypeId - 1].Volume
                    || totalWeight < cuttingpoint * Problem.VehTypes[r.AssignedVeh.TypeId - 1].Weight)
                {
                    solution.Routes.RemoveAt(i);
                    int veh_id = r.AssignedVeh.VehId;
                    int idx_veh = solution.fleet.GetVehIdxInFleet(veh_id);
                    solution.fleet.VehFleet[idx_veh].VehRouteList.Remove(r.RouteId);
                    if (solution.fleet.VehFleet[idx_veh].getNumofVisRoute() == 0)
                    {
                        solution.fleet.removeVeh(veh_id);
                    }
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
    }
}
