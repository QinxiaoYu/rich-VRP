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
            Random rd = new  Random();
            OpProblemReader reader = new OpProblemReader();
            string dir = Directory.GetCurrentDirectory();
            reader.Read(dir);
            Problem.MinWaitTimeAtDepot = 60; //在配送中心的最少等待时间 
            Problem.WaitCostRate = 0.4;
            Problem.SetNearDistanceCusAndSta(20, 5); //计算每个商户的小邻域
            string outfilename = null;
            StringBuilder sb = new StringBuilder();
            outfilename = dir + "//" + "test618.txt";
            StreamWriter sw = new StreamWriter(outfilename, true);
            for (int i = 0; i < 10000; i++)
            {
                sb.Clear();
                sb.AppendLine("============== " + i.ToString() + " ===============");
                //CW4sveh initial = new CW4sveh(); //这个效果次之
                //CWObjFunc initial = new CWObjFunc(); //这个效果最差
                //Initialization initial = new Initialization(); //这个效果最好
                ReadInitialSolution initial = new ReadInitialSolution(@"C:\Users\user\Desktop\Good Solution\reslut61403229739.csv");


                Solution ini_solution = initial.initial_construct();
                Console.WriteLine(ini_solution.SolutionIsFeasible().ToString());
                //OriginObjFunc evaluate = new OriginObjFunc();
                ini_solution.printCheckSolution();
                double cost = ini_solution.CalObjCost();
                Console.WriteLine("ObjVal 0 = " + cost.ToString("0.00"));
                //ini_solution.PrintResult();
                sb.AppendLine(cost.ToString("0.00") + ": Route Numbers = " + ini_solution.Routes.Count.ToString() + "Veh Number = " + ini_solution.fleet.VehFleet.Count.ToString());
                //sb.AppendLine(result);

                RemoveSta oper = new RemoveSta();
                bool isIprv = oper.Remove(ini_solution); //删除多余的充电站
                sb.AppendLine("====RemoveSta=====");
                ini_solution.printCheckSolution();
                double newcost = ini_solution.CalObjCost();
                Console.WriteLine(ini_solution.SolutionIsFeasible().ToString());
                Console.WriteLine("ObjVal 1 = " + newcost.ToString("0.00"));

                StationPosition sp = new StationPosition();
                ini_solution = sp.StationExchage(ini_solution, 0.3);//优化充电站
                ini_solution.printCheckSolution();
                double newcost2 = ini_solution.CalObjCost();
                Console.WriteLine(ini_solution.SolutionIsFeasible().ToString());
                Console.WriteLine("ObjVal 2 = " + newcost2.ToString("0.00"));

                if (newcost2 > 298000)
                {
                    continue;
                }

                Solution bst_sol = ini_solution.Copy();

                double percent_battery = 0.2;
                int short_route = 4;
                int select_strategy = 1; //0: first improve; 1:best improve
                double change_obj = 0;
                DestroyAndRepair DR = new DestroyAndRepair();
                TwoOpt twoopt = new TwoOpt();
                Relocate relointra = new Relocate();
                BreakTwoRoute breakOneRoute = new BreakTwoRoute();
                int outiters = 20;
                while (outiters > 0)
                {
                    
                    ini_solution = DR.DR(ini_solution, short_route);
                    ini_solution.printCheckSolution();
                    Console.WriteLine(ini_solution.SolutionIsFeasible().ToString());
                    Console.WriteLine("ObjDR = " + ini_solution.ObjVal.ToString("0.00"));

                    ini_solution = relointra.RelocateIntra(ini_solution, 1, true);//线路内重定位
                    ini_solution.printCheckSolution();
                    double newcost3 = ini_solution.CalObjCost();
                    Console.WriteLine(ini_solution.SolutionIsFeasible().ToString());
                    Console.WriteLine("ObjVal Relocate = " + newcost3.ToString("0.00"));

                    //if (newcost3 >300000) break;
                    Solution tmp_sol = ini_solution.Copy();
                   
                    double newcost42 = tmp_sol.ObjVal;

                    while (tmp_sol != null)
                    {
                        tmp_sol = twoopt.intarChange(ini_solution);
                                          
                        if (tmp_sol != null)
                        {
                            bool isWorse = false;
                            if (tmp_sol.ObjVal >= newcost42)
                            {
                                isWorse = true;
                            }
                            ini_solution = tmp_sol.Copy();
                            newcost42 = ini_solution.CalObjCost();
                            Console.WriteLine(ini_solution.SolutionIsFeasible().ToString());
                            Console.WriteLine("ObjVal 2opt = " + newcost42.ToString("0.00"));
                            if (isWorse)
                            {
                                break;
                            }
                        }

                    }

                    
                    //tmp_sol = ini_solution.Copy();
                    while (tmp_sol != null)
                    {
                        tmp_sol = new ShiftInter().Shift(ini_solution,1,select_strategy,change_obj); //线路间交换
                        if (tmp_sol != null)
                        {
                            bool isWorse = false;
                            if (tmp_sol.ObjVal >= newcost42)
                            {
                                isWorse = true;
                            }
                            ini_solution = tmp_sol.Copy();
                            newcost42 = ini_solution.CalObjCost();
                            Console.WriteLine(ini_solution.SolutionIsFeasible().ToString());
                            Console.WriteLine("ObjVal 0-1 shift = " + newcost42.ToString("0.00"));
                            if (isWorse)
                            {
                                break;
                            }
                        }

                    }
           
                    //tmp_sol = ini_solution.Copy();
                    while (tmp_sol != null)
                    {
                        tmp_sol = new ShiftInter().Shift(ini_solution, 2, select_strategy, change_obj); //线路间交换
                        if (tmp_sol != null)
                        {
                            bool isWorse = false;
                            if (tmp_sol.ObjVal >= newcost42)
                            {
                                isWorse = true;
                            }
                            ini_solution = tmp_sol.Copy();
                            newcost42 = ini_solution.CalObjCost();
                            Console.WriteLine(ini_solution.SolutionIsFeasible().ToString());
                            Console.WriteLine("ObjVal 0-2 shift = " + newcost42.ToString("0.00"));
                            if (isWorse)
                            {
                                break;
                            }
                        }

                    }

                    //tmp_sol = ini_solution.Copy();
                    while (tmp_sol != null)
                    {
                        tmp_sol = new RelocateInter().Relocate(ini_solution, 1, 1,select_strategy,change_obj); //线路间交换
                        if (tmp_sol != null)
                        {
                            bool isWorse = false;
                            if (tmp_sol.ObjVal>=newcost42)
                            {
                                isWorse = true;
                            }
                            ini_solution = tmp_sol.Copy();
                            newcost42 = ini_solution.CalObjCost();
                            Console.WriteLine(ini_solution.SolutionIsFeasible().ToString());
                            Console.WriteLine("ObjVal 1-1 swap = " + newcost42.ToString("0.00"));
                            if (isWorse)
                            {
                                break;
                            }
                        }

                     }
                        tmp_sol = ini_solution.Copy();
                        while (tmp_sol != null)
                        {
                            tmp_sol = new RelocateInter().Relocate(ini_solution, 2, 2,select_strategy, change_obj); //线路间交换
                            if (tmp_sol != null)
                            {
                                bool isWorse = false;
                                if (tmp_sol.ObjVal >= newcost42)
                                {
                                    isWorse = true;
                                }
                                ini_solution = tmp_sol.Copy();
                                newcost42 = ini_solution.CalObjCost();
                                Console.WriteLine(ini_solution.SolutionIsFeasible().ToString());
                                Console.WriteLine("ObjVal 2-2 swap = " + newcost42.ToString("0.00"));
                                if (isWorse)
                                {
                                    break;
                                }
                            }

                        }

                    
                    tmp_sol = ini_solution.Copy();
                    double newcost4 = tmp_sol.ObjVal;
                    while (tmp_sol != null)
                    {
                        tmp_sol = new CrossInter().Cross(ini_solution,select_strategy, change_obj); //线路间交换
                        if (tmp_sol != null)
                        {
                            bool isWorse = false;
                            if (tmp_sol.ObjVal >= newcost4)
                            {
                                isWorse = true;
                            }
                            ini_solution = tmp_sol.Copy();
                            newcost4 = ini_solution.CalObjCost();
                            Console.WriteLine(ini_solution.SolutionIsFeasible().ToString());
                            Console.WriteLine("ObjVal CrossInter = " + newcost4.ToString("0.00"));
                            if (isWorse)
                            {
                                break;
                            }
                        }

                    }

                    ini_solution = sp.StationExchage(ini_solution, percent_battery);//优化充电站
                    ini_solution.printCheckSolution();
                    double newcost6 = ini_solution.CalObjCost();
                    Console.WriteLine(ini_solution.SolutionIsFeasible().ToString());
                    Console.WriteLine("ObjVal Sta = " + newcost6.ToString("0.00"));

                   


                    if (ini_solution.ObjVal<bst_sol.ObjVal)
                    {
                        bst_sol = ini_solution.Copy();
                        outiters++;
                        select_strategy = 1; //换成first improve
                        change_obj = 0;
                        //change_obj = Math.Max(20, change_obj + 5);
                    }
                    else
                    {
                        percent_battery = Math.Max(0.6, percent_battery + 0.1);
                        short_route = Math.Max(5, short_route + 1);
                        select_strategy = rd.Next(2); //bst improve
                        change_obj = Math.Max(-200, change_obj - 20);

                    }
                    ini_solution = breakOneRoute.Break(ini_solution, 1);
                    outiters--;

                    sb.AppendLine(outiters.ToString() + ": " + bst_sol.ObjVal.ToString("0.00") + ": Route Numbers = " + ini_solution.Routes.Count.ToString() + "Veh Number = " + ini_solution.fleet.VehFleet.Count.ToString());
                    sw.Write(sb);
                    sw.Flush();
                    sb.Clear();
                    //sb.AppendLine(newcost.ToString("0.00"));
                    //sb.AppendLine(ini_solution.PrintToString());

                }
                
                if (bst_sol.ObjVal < 295000)
                {
                    bst_sol.PrintResult();
                    Console.WriteLine(bst_sol.PrintToString());
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

