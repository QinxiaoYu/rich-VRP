﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace OP.Data
{
    public class Solution
    {
    
        public List<Route> Routes { get; set; }
        public Problem Problem { get; set; }
        public double Phi { get; set; }

        public Solution(Problem problem)
        {
            Routes = new List<Route>();
            Problem = problem;
            Phi = 0.0;
        }
        public void AddRoute(Route route)
        {
            Route newRoute = route.Copy();
            newRoute.Solution = this;
            Routes.Add(newRoute);
        }


        public double TotalDistance()
        {
            double totalDistance = 0;
            foreach (Route route in Routes)
                totalDistance += route.Length();
            return totalDistance;
        }

		public double[] getSolutionInfo()
		{
			double traveltime = 0;
			double loadViol = 0;
			double twViol = 0;
			double watingtime = 0;
			for (int i = 0; i < this.Routes.Count; i++)
			{
				Route r = this.Routes[i];
				double[] routeInfo = r.getRouteInfo ();
				traveltime += routeInfo [0];
				loadViol += routeInfo[1];
				twViol += routeInfo[2];
				watingtime += routeInfo[3];
			}
            
			return new double[] { traveltime, loadViol, twViol, watingtime };
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
                    solution += "(dist: " + ((int)Routes[i].Length()).ToString(CultureInfo.InvariantCulture) + ")";
                    solution += "\r\n";
                }
                solution += "\r\n";
                solution += "total distance: " + TotalDistance().ToString(CultureInfo.InvariantCulture) + " (gain: " + (-1 * Phi).ToString(CultureInfo.InvariantCulture) + ")";
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


        /// <summary>
        /// 提取解中的所有客户
        /// </summary>
        /// <returns></returns>
        public List<Customer> CustomerListCopy()
        {
            var customerList = new List<Customer>();
            foreach (Route route in Routes)
                for (int i = 1; i < route.RouteList.Count - 1; ++i)
                    customerList.Add(((Customer) route.RouteList[i]).DeepCopy());
            return customerList;
        }
    }


    
}