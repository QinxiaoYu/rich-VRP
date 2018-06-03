using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OP.Data;

using System.IO;
using rich_VRP.Constructive;
using rich_VRP.ObjectiveFunc;
using rich_VRP.Neighborhoods.Remove;

namespace rich_VRP
{
    class Program
    {
        static void Main(string[] args)
        {
            OpProblemReader reader = new OpProblemReader();
            string dir = Directory.GetCurrentDirectory();
            
            Problem problem = reader.Read(dir);
            Problem.MinWaitTimeAtDepot = 60; //在配送中心的最少等待时间 
            Problem.WaitCostRate = 0.4;
            //problem.SetNearDistanceCusAndSta(10, 2); //计算每个商户的小邻域
            string outfilename = null;
            StringBuilder sb = new StringBuilder();
            outfilename = dir + "//" + "test_0603.txt";
            StreamWriter sw = new StreamWriter(outfilename, true);
            for (int i = 0; i < 100; i++)
            {
                sb.Clear();
                sb.AppendLine("============== "+ i.ToString() + " ===============");
                Initialization initial = new Initialization(problem);
                Solution ini_solution = initial.initial_construct();
                OriginObjFunc evaluate = new OriginObjFunc();
                double cost = evaluate.CalObjCost(ini_solution);
                //ini_solution.PrintResult();


                sb.AppendLine(cost.ToString("0.00")+": Route Numbers = "+ini_solution.Routes.Count.ToString()+"Veh Number = "+ini_solution.fleet.VehFleet.Count.ToString());
                //sb.AppendLine(result);
               
                RemoveSta oper = new RemoveSta();
                bool isIprv = oper.Remove(ini_solution);
                if (isIprv)
                {
                    sb.AppendLine("====RemoveSta=====");
                    double newcost = evaluate.CalObjCost(ini_solution);
                    if (newcost<306194)
                    {
                        ini_solution.PrintResult();
                    }
                   
                    sb.AppendLine(newcost.ToString("0.00") + ": Route Numbers = " + ini_solution.Routes.Count.ToString() + "Veh Number = " + ini_solution.fleet.VehFleet.Count.ToString());
                    //sb.AppendLine(newcost.ToString("0.00"));
                    //sb.AppendLine(ini_solution.PrintToString());
                   
                }
                sw.Write(sb);
                sw.Flush();
            }
            ///初始化
                       
            sw.Flush();
            sw.Close();


          
        }
    }
}
