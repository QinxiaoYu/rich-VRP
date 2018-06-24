using OP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rich_VRP.Neighborhoods.Remove
{
    class RemoveSta
    {
        public bool Remove(Solution solution)
        {
            bool isImprove = false;
            for (int i = 0; i < solution.Routes.Count; i++)
            {
                Route impr_route = RemoveStaInRoute(solution.Routes[i]);
                int pos_veh_fleet = solution.fleet.GetVehIdxInFleet(solution.Routes[i].AssignedVehID);
                if (impr_route != null)
                {
                    isImprove = true;
                    solution.Routes[i] = impr_route.Copy();
                    solution.fleet.VehFleet[pos_veh_fleet].VehRouteList[impr_route.RouteIndexofVeh] = impr_route.RouteId;
                   
                }
                    
            }
            if (isImprove)
            {
                solution.UpdateTripChainTime();
            }
            return isImprove;
        }

        private Route RemoveStaInRoute(Route route)
        {
            Route bst_route = null;
           
            bool flag = true;
            while (flag)
            {
                for (int i = 1; i < route.RouteList.Count; i++)
                {
                    if (i == route.RouteList.Count - 1)
                    {
                        flag = false;
                        break;
                    }
                    if (route.RouteList[i].Info.Type == 3)
                    {
                        Route tmp_r = route.Copy();
                        tmp_r.Remove((Station)route.RouteList[i]);
                        if (tmp_r.IsFeasible())
                        {
                            route = tmp_r;

                            bst_route = tmp_r.Copy();
                            break;
                        }
                    }
                }
            }
            
            return bst_route;
        }

    }
}
