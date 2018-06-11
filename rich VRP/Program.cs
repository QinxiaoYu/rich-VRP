using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OP.Data;

using System.IO;
using rich_VRP.Constructive;
//using rich_VRP.ObjectiveFunc;
using rich_VRP.Neighborhoods.Remove;
using rich_VRP.Neighborhoods.Intra;
using rich_VRP.Constructive.rich_VRP.Constructive;
using rich_VRP.Neighborhoods.DestroyRepair;
using rich_VRP.Neighborhoods.Inter;

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
            outfilename = dir + "//" + "test610.txt";
            StreamWriter sw = new StreamWriter(outfilename, true);
            for (int i = 0; i < 1000; i++)
            {
                sb.Clear();
                sb.AppendLine("============== " + i.ToString() + " ===============");
                //CW4sveh initial = new CW4sveh(); //这个效果次之
                CWObjFunc initial = new CWObjFunc(); //这个效果最差
                //Initialization initial = new Initialization(); //这个效果最好
                Solution ini_solution = initial.initial_construct();
                //OriginObjFunc evaluate = new OriginObjFunc();
                double cost = ini_solution.CalObjCost();
                Console.WriteLine("ObjVal 0 = " + cost.ToString("0.00"));
                //ini_solution.PrintResult();
                sb.AppendLine(cost.ToString("0.00") + ": Route Numbers = " + ini_solution.Routes.Count.ToString() + "Veh Number = " + ini_solution.fleet.VehFleet.Count.ToString());
                //sb.AppendLine(result);

                RemoveSta oper = new RemoveSta();
                bool isIprv = oper.Remove(ini_solution); //删除多余的充电站
                sb.AppendLine("====RemoveSta=====");
                double newcost = ini_solution.CalObjCost();

                Console.WriteLine("ObjVal 1 = " + newcost.ToString("0.00"));

                StationPosition sp = new StationPosition();
                ini_solution = sp.StationExchage(ini_solution, 0.3);//优化充电站
                double newcost2 = ini_solution.CalObjCost();
                Console.WriteLine("ObjVal 2 = " + newcost2.ToString("0.00"));
                int outiters = 5;
                while (outiters>0)
                {
                    DestroyAndRepair DR = new DestroyAndRepair();
                    ini_solution = DR.DR(ini_solution, 4);
                    Console.WriteLine("ObjDR = "+ini_solution.ObjVal.ToString("0.00"));
                    //ini_solution = DR.DestroyShortRoute(ini_solution, 5);
                    //ini_solution = DR.DestroyWasteRoute(ini_solution, 0.2);
                    //ini_solution = DR.DestroyAfternoonNodes(ini_solution, 780, 0.2);
                    //ini_solution = DR.Repair(ini_solution);

                    //ini_solution = new TwoOpt().intarChange(ini_solution);
                    //double newcost31 = ini_solution.CalObjCost();
                    //Console.WriteLine("ObjVal 2opt = " + newcost31.ToString("0.00"));

                    ini_solution = new Relocate().RelocateIntra(ini_solution,true);//线路内重定位
                    double newcost3 = ini_solution.CalObjCost();
                    Console.WriteLine("ObjVal Relocate = " + newcost3.ToString("0.00"));

                    if (newcost3 > 296000) break ;

                    double newcost4 = 0;
                    Solution tmp_sol = ini_solution.Copy();
                    while (tmp_sol!=null)
                    {
                        tmp_sol = new CrossInter().Cross(ini_solution); //线路间交换
                        if (tmp_sol!=null)
                        {
                            ini_solution = tmp_sol.Copy();
                            newcost4 = ini_solution.CalObjCost();
                            Console.WriteLine("ObjVal 4 = " + newcost4.ToString("0.00"));
                        }
                        
                    }
                    ini_solution = DR.DR(ini_solution, 4);
                    Console.WriteLine("ObjDR =" + ini_solution.ObjVal.ToString("0.00"));

                    ini_solution = new Relocate().RelocateIntra(ini_solution, true);//线路内重定位
                    double newcost5 = ini_solution.CalObjCost();
                    Console.WriteLine("ObjVal 5 = " + newcost5.ToString("0.00"));
                    //double newcost5 = evaluate.CalObjCost(ini_solution);
                    //Console.WriteLine("ObjVal 5 = " + newcost5.ToString("0.00"));
                    outiters--;
             
                    sb.AppendLine(outiters.ToString()+": "+ newcost5.ToString("0.00") + ": Route Numbers = " + ini_solution.Routes.Count.ToString() + "Veh Number = " + ini_solution.fleet.VehFleet.Count.ToString());
                    //sb.AppendLine(newcost.ToString("0.00"));
                    //sb.AppendLine(ini_solution.PrintToString());

                }
                ini_solution = new TwoOpt().intarChange(ini_solution);
                double newcost32 = ini_solution.CalObjCost();
                Console.WriteLine("ObjVal 2opt = " + newcost32.ToString("0.00"));
                if (newcost32 < 285000)
                {
                    ini_solution.PrintResult();
                    Console.WriteLine(ini_solution.PrintToString());
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

