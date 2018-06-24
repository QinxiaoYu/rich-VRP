using System;
using OP.Data;
using System.Collections.Generic;
using rich_VRP.Constructive.rich_VRP.Constructive;
using rich_VRP.Neighborhoods;

namespace rich_VRP.Constructive
{
    public class ClusterFirstRouteSecond
    {
        Random rd = new Random();
        List<Customer> unrouteed_cus = new List<Customer>();
        public int cluster_strategy = 1;
        public double time_threshold = 720;

        public Solution initial_construct(int cus_threshold = 20)
        {
            Solution solution = new Solution();
            solution.UnVisitedCus = new List<Customer>(Problem.Customers);
            Initialization cw_ini = new Initialization();
            LocalSearch ls = new LocalSearch();
            ls.stop_criteria = 10;
            ls.restart_criteria = 10;
            while(solution.UnVisitedCus.Count>0)
            {
                if (cluster_strategy==1)
                {
                    unrouteed_cus = Utility.FindCusByAngle(cus_threshold, solution.UnVisitedCus);
                }
                if (cluster_strategy ==2)
                {
                    unrouteed_cus = Utility.FindCusByRadians(cus_threshold, solution.UnVisitedCus);
                }
                if (cluster_strategy == 3)
                {
                    unrouteed_cus = Utility.FindCusByAngleAndRadians(cus_threshold, solution.UnVisitedCus);
                }
                if (cluster_strategy == 4)
                {
                      unrouteed_cus = Utility.FindCusByTime(time_threshold, solution.UnVisitedCus);
                }
                foreach (var cus in unrouteed_cus)
                {
                    solution.UnVisitedCus.RemoveAll((Customer obj) => obj.Info.Id == cus.Info.Id);
                }
                Solution part_sol = cw_ini.initial_construct(unrouteed_cus);
                double old_obj = part_sol.CalObjCost();      
                        
                part_sol = ls.search(part_sol,null,false);
                Console.WriteLine("part_sol before / after ls: " + old_obj.ToString("0.00") + "  "+ part_sol.CalObjCost().ToString("0.00"));
                part_sol.PrintResult();
                solution.Merge(part_sol);

                unrouteed_cus.Clear();
            }
            return solution;
        }
    }
}
