using OP.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rich_VRP.Constructive
{
    class ReadInitialSolution
    {
        public string resultpath { get; set; }
        public ReadInitialSolution(string _resultpath)
        {
            resultpath = _resultpath;
        }
        public Solution initial_construct()
        {
            Solution sol = new Solution();

            using (StreamReader Dreader = new StreamReader(resultpath))
            {
                string str_Dis = string.Empty;
                str_Dis = Dreader.ReadLine(); //表头
                str_Dis = Dreader.ReadLine();
                while (!string.IsNullOrWhiteSpace(str_Dis))
                {
                    string[] paras = str_Dis.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    string v_num = paras[0];
                    int vtype = int.Parse(paras[1]);
                    string routelists = paras[2];
                    string[] departuretime = paras[3].Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries); ;

                    //Vehicle veh = new Vehicle(vtype);
                    Vehicle veh = sol.fleet.addNewVeh(vtype);
                    double dt = double.Parse(departuretime[0]) * 60 + double.Parse(departuretime[1]);
                    double nxt_dt = dt;
                    Route route = null;
                    string[] nodes = routelists.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < nodes.Length; i++)
                    {
                        if (int.Parse(nodes[i]) == 0 && i == nodes.Length - 1)
                        {
                            veh.addRoute2Veh(route);
                            Console.WriteLine(route.RouteId + ":" + route.IsFeasible().ToString());
                            sol.AddRoute(route.Copy());
                            continue;
                           
                        }
                        if (int.Parse(nodes[i]) == 0 && i == 0)
                        {
                            route = new Route(veh, nxt_dt);
                            continue;
                            
                        }
                        if (int.Parse(nodes[i])==0)
                        {
                            veh.addRoute2Veh(route);
                            Console.WriteLine(route.RouteId+":" + route.IsFeasible().ToString());
                            sol.AddRoute(route.Copy());
                            nxt_dt = route.GetArrivalTime() + Problem.MinWaitTimeAtDepot;
                            route = new Route(veh, nxt_dt);
                            continue;
                        }
                        if (int.Parse(nodes[i]) < 1001)
                        {
                            route.InsertNode(Problem.SearchCusbyId(int.Parse(nodes[i])), route.RouteList.Count - 1);                  
                            continue;
                        }
                        if (int.Parse(nodes[i]) > 1000)
                        {
                            route.InsertNode(Problem.SearchStaById(int.Parse(nodes[i])),route.RouteList.Count-1);
                            continue;
                        }
                    }
                    str_Dis = Dreader.ReadLine();
                }
            }
            return sol;
        }
    }
}
