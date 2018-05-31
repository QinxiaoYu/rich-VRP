using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OP.Data
{
    public class Solution
    {

        public List<Route> Routes;
        public Problem Problem;
        public double ObjVal;

        public Solution(Problem problem)
        {
            Routes = new List<Route>();
            Problem = problem;
            ObjVal = 0.0;
        }
        public void AddRoute(Route route)
        {
            //Route newRoute = route.Copy();
            //newRoute.Solution = this;
            Routes.Add(route);
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
                solution += "total distance: " + TotalDistance().ToString(CultureInfo.InvariantCulture) + " (OBJ: " + (ObjVal).ToString(CultureInfo.InvariantCulture) + ")";
            }
                      
            return solution;
        }


        public Solution Copy()
        {
            var sol = new Solution(Problem);
            foreach (Route route in Routes)
                if(route.RouteList.Count >= 2)
                   sol.AddRoute(route.Copy());
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