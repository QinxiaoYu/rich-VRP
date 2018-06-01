using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OP.Data
{
    public class Solution
    {

        public List<Route> Routes;
        public Problem problem;
        public double ObjVal;
        public Fleet fleet;
        public List<Customer> UnVisitedCus;

        public Solution(Problem _problem)
        {
            Routes = new List<Route>();
            problem = _problem;
            fleet = new Fleet();
            fleet.solution = this;
            ObjVal = 0.0;
        }
        public void AddRoute(Route route)
        {
            //Route newRoute = route.Copy();
            //newRoute.Solution = this;
            Routes.Add(route);
        }

        internal void UpdateTripChainTime()
        {
            foreach (Vehicle veh in fleet.VehFleet)
            {
                int num_trips_veh = veh.getNumofVisRoute();
                if (num_trips_veh>1)
                {
                    for (int i = 1; i < num_trips_veh; i++)
                    {
                        Route cur_route = GetRouteByID(veh.VehRouteList[i]);
                        double new_departure_cur = cur_route.GetEarliestDepartureTime();
                        cur_route.ServiceBeginingTimes[0] = new_departure_cur;
                              
                    }
                }
            }
        }

        public Route GetRouteByID(string route_id)
        {
            foreach (Route route in Routes)
            {
                if (route.RouteId==route_id)
                {
                    return route;
                }
            }
            return null;
        }

        public double TotalDistance()
        {
            double totalDistance = 0;
            foreach (Route route in Routes)
                totalDistance += route.GetRouteLength();
            return totalDistance;
        }



        public string PrintToString()
        {
            string solution = "";
            if (this == null)
            {
                solution += "None";
                solution += "\r\n";
            }
            else
            {
                for (int i = 0; i < Routes.Count; ++i)
                {
                    solution += i.ToString(CultureInfo.InvariantCulture);
                    solution += ") ";
                    solution += Routes[i].PrintToString() + "; ";
                    solution += "(dist: " + ((int)Routes[i].GetRouteLength()).ToString(CultureInfo.InvariantCulture) + ")";
                    solution += "\r\n";
                }
                solution += "\r\n";
                solution += "total distance: " + TotalDistance().ToString(CultureInfo.InvariantCulture);
            }
                      
            return solution;
        }


        public Solution Copy()
        {
            var sol = new Solution(problem);
            foreach (Route route in Routes)
            {
                if (route.RouteList.Count >= 2)
                    sol.AddRoute(route.Copy());
            }
            foreach (Vehicle veh in this.fleet.VehFleet)
            {
                sol.fleet.VehFleet.Add(veh.Copy());
            }            
            return sol;
        }


        public List<Route> Copy(List<Route> routes)
        {
            var newRoutes = new List<Route>(routes.Count);
            newRoutes.AddRange(routes.Select(route => route.Copy()));
            return newRoutes;
        }

        internal void UpdateFirstTripTime()
        {
            foreach (Route trip in this.Routes)
            {
                if (trip.RouteIndexofVeh ==0)
                {
                    trip.UpdateDepartureTime();
                }
                
            }
        }
    }


    
}