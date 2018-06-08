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
using rich_VRP.Neighborhoods.Intra;
using rich_VRP.Constructive.rich_VRP.Constructive;
using rich_VRP.Neighborhoods.DestroyRepair;

namespace rich_VRP
{
    class Program
    {
        static void Main(string[] args)
        {
            OpProblemReader reader = new OpProblemReader();
            string dir = Directory.GetCurrentDirectory();
            reader.Read(dir);
            Problem.MinWaitTimeAtDepot = 60; //在配送中心的最少等待时间 
            Problem.WaitCostRate = 0.4;
            Problem.SetNearDistanceCusAndSta(10, 10); //计算每个商户的小邻域
            string outfilename = null;
            StringBuilder sb = new StringBuilder();
            outfilename = dir + "//" + "test.txt";
            StreamWriter sw = new StreamWriter(outfilename, true);
            for (int i = 0; i < 100; i++)
            {
                sb.Clear();
                sb.AppendLine("============== " + i.ToString() + " ===============");
                //CW4sveh initial = new CW4sveh(); 
                CWObjFunc initial = new CWObjFunc(); //这个效果最差
                //Initialization initial = new Initialization();
                Solution ini_solution = initial.initial_construct();
                OriginObjFunc evaluate = new OriginObjFunc();
                double cost = evaluate.CalObjCost(ini_solution);
                //ini_solution.PrintResult();


                sb.AppendLine(cost.ToString("0.00") + ": Route Numbers = " + ini_solution.Routes.Count.ToString() + "Veh Number = " + ini_solution.fleet.VehFleet.Count.ToString());
                //sb.AppendLine(result);

                RemoveSta oper = new RemoveSta();
                bool isIprv = oper.Remove(ini_solution);
                if (isIprv)
                {
                    sb.AppendLine("====RemoveSta=====");
                    double newcost = evaluate.CalObjCost(ini_solution);

                    Console.WriteLine("ObjVal 1 = " + newcost.ToString("0.00"));

                    StationPosition sp = new StationPosition();
                    ini_solution = sp.StationExchage(ini_solution, 0.3);
                    double newcost2 = evaluate.CalObjCost(ini_solution);
                    Console.WriteLine("ObjVal 2 = " + newcost2.ToString("0.00"));

                    DestroyAndRepair DR = new DestroyAndRepair();
                    ini_solution = DR.DestroyShortRoute(ini_solution, 5);
                    ini_solution = DR.DestroyWasteRoute(ini_solution, 0.2);
                    ini_solution = DR.DestroyAfternoonNodes(ini_solution, 780, 0.2);
                    ini_solution = DR.Repair(ini_solution);

                    double newcost3 = evaluate.CalObjCost(ini_solution);
                    Console.WriteLine("ObjVal 3 = " + newcost3.ToString("0.00"));

                    if (newcost < 340000)
                    {
                        ini_solution.PrintResult();
                        Console.WriteLine(ini_solution.PrintToString());
                    }

                    sb.AppendLine(newcost3.ToString("0.00") + ": Route Numbers = " + ini_solution.Routes.Count.ToString() + "Veh Number = " + ini_solution.fleet.VehFleet.Count.ToString());
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

