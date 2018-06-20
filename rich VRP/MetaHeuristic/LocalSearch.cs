using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OP.Data;
using rich_VRP.Neighborhoods.DestroyRepair;
using rich_VRP.Neighborhoods.Inter;
using rich_VRP.Neighborhoods.Intra;
using rich_VRP.Neighborhoods.Remove;

namespace rich_VRP.Neighborhoods
{
    class LocalSearch
    {
        Random rd = new Random();
        public Solution search(Solution solution)
        {
            Solution new_sol = solution.Copy();
            Solution bst_sol = solution.Copy();

            RemoveSta oper = new RemoveSta();
            bool isIprv = oper.Remove(solution); //删除多余的充电站

            solution.printCheckSolution();
            double newcost = solution.CalObjCost();
            //Console.WriteLine(solution.SolutionIsFeasible().ToString());
            //Console.WriteLine("ObjVal 1 = " + newcost.ToString("0.00"));

            StationPosition sp = new StationPosition();
            solution = sp.StationExchage(solution, 0.3);//优化充电站
            solution.printCheckSolution();
            double newcost2 = solution.CalObjCost();
            //Console.WriteLine(solution.SolutionIsFeasible().ToString());
            //Console.WriteLine("ObjVal 2 = " + newcost2.ToString("0.00"));

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

                solution = DR.DR(solution, short_route);
                solution.printCheckSolution();
                //Console.WriteLine(solution.SolutionIsFeasible().ToString());
                //Console.WriteLine("ObjDR = " + solution.ObjVal.ToString("0.00"));

                solution = relointra.RelocateIntra(solution, 1, true);//线路内重定位
                solution.printCheckSolution();
                double newcost3 = solution.CalObjCost();
                //Console.WriteLine(solution.SolutionIsFeasible().ToString());
                //Console.WriteLine("ObjVal Relocate = " + newcost3.ToString("0.00"));

                //if (newcost3 >300000) break;
                Solution tmp_sol = solution.Copy();

                double newcost42 = tmp_sol.ObjVal;

                while (tmp_sol != null)
                {
                    tmp_sol = twoopt.intarChange(solution);

                    if (tmp_sol != null)
                    {
                        bool isWorse = false;
                        if (tmp_sol.ObjVal >= newcost42)
                        {
                            isWorse = true;
                        }
                        solution = tmp_sol.Copy();
                        newcost42 = solution.CalObjCost();
                        //Console.WriteLine(solution.SolutionIsFeasible().ToString());
                        //Console.WriteLine("ObjVal 2opt = " + newcost42.ToString("0.00"));
                        if (isWorse)
                        {
                            break;
                        }
                    }

                }


                //tmp_sol = ini_solution.Copy();
                while (tmp_sol != null)
                {
                    tmp_sol = new ShiftInter().Shift(solution, 1, select_strategy, change_obj); //线路间交换
                    if (tmp_sol != null)
                    {
                        bool isWorse = false;
                        if (tmp_sol.ObjVal >= newcost42)
                        {
                            isWorse = true;
                        }
                        solution = tmp_sol.Copy();
                        newcost42 = solution.CalObjCost();
                        //Console.WriteLine(solution.SolutionIsFeasible().ToString());
                        //Console.WriteLine("ObjVal 0-1 shift = " + newcost42.ToString("0.00"));
                        if (isWorse)
                        {
                            break;
                        }
                    }

                }

                //tmp_sol = ini_solution.Copy();
                while (tmp_sol != null)
                {
                    tmp_sol = new ShiftInter().Shift(solution, 2, select_strategy, change_obj); //线路间交换
                    if (tmp_sol != null)
                    {
                        bool isWorse = false;
                        if (tmp_sol.ObjVal >= newcost42)
                        {
                            isWorse = true;
                        }
                        solution = tmp_sol.Copy();
                        newcost42 = solution.CalObjCost();
                        //Console.WriteLine(solution.SolutionIsFeasible().ToString());
                        //Console.WriteLine("ObjVal 0-2 shift = " + newcost42.ToString("0.00"));
                        if (isWorse)
                        {
                            break;
                        }
                    }

                }

                //tmp_sol = ini_solution.Copy();
                while (tmp_sol != null)
                {
                    tmp_sol = new RelocateInter().Relocate(solution, 1, 1, select_strategy, change_obj); //线路间交换
                    if (tmp_sol != null)
                    {
                        bool isWorse = false;
                        if (tmp_sol.ObjVal >= newcost42)
                        {
                            isWorse = true;
                        }
                        solution = tmp_sol.Copy();
                        newcost42 = solution.CalObjCost();
                        //Console.WriteLine(solution.SolutionIsFeasible().ToString());
                        //Console.WriteLine("ObjVal 1-1 swap = " + newcost42.ToString("0.00"));
                        if (isWorse)
                        {
                            break;
                        }
                    }

                }
                tmp_sol = solution.Copy();
                while (tmp_sol != null)
                {
                    tmp_sol = new RelocateInter().Relocate(solution, 2, 2, select_strategy, change_obj); //线路间交换
                    if (tmp_sol != null)
                    {
                        bool isWorse = false;
                        if (tmp_sol.ObjVal >= newcost42)
                        {
                            isWorse = true;
                        }
                        solution = tmp_sol.Copy();
                        newcost42 = solution.CalObjCost();
                        //Console.WriteLine(solution.SolutionIsFeasible().ToString());
                        //Console.WriteLine("ObjVal 2-2 swap = " + newcost42.ToString("0.00"));
                        if (isWorse)
                        {
                            break;
                        }
                    }

                }


                tmp_sol = solution.Copy();
                double newcost4 = tmp_sol.ObjVal;
                while (tmp_sol != null)
                {
                    tmp_sol = new CrossInter().Cross(solution, select_strategy, change_obj); //线路间交换
                    if (tmp_sol != null)
                    {
                        bool isWorse = false;
                        if (tmp_sol.ObjVal >= newcost4)
                        {
                            isWorse = true;
                        }
                        solution = tmp_sol.Copy();
                        newcost4 = solution.CalObjCost();
                        //Console.WriteLine(solution.SolutionIsFeasible().ToString());
                        //Console.WriteLine("ObjVal CrossInter = " + newcost4.ToString("0.00"));
                        if (isWorse)
                        {
                            break;
                        }
                    }

                }

                solution = sp.StationExchage(solution, percent_battery);//优化充电站
                solution.printCheckSolution();
                double newcost6 = solution.CalObjCost();
                //Console.WriteLine(solution.SolutionIsFeasible().ToString());
                //Console.WriteLine("ObjVal Sta = " + newcost6.ToString("0.00"));




                if (solution.ObjVal < bst_sol.ObjVal)
                {
                    bst_sol = solution.Copy();
                    outiters++;
                    select_strategy = 1; //换成first improve
                    change_obj = 0;
                    //change_obj = Math.Max(20, change_obj + 5);
                    Console.WriteLine(outiters+ ": bst_sol in LS" + bst_sol.ObjVal.ToString("0.00"));
                }
                else
                {
                    percent_battery = Math.Max(0.6, percent_battery + 0.1);
                    short_route = Math.Max(5, short_route + 1);
                    select_strategy = rd.Next(2); //bst improve
                    //change_obj = Math.Max(-100, change_obj - 20);

                }
                solution = breakOneRoute.Break(solution, 1);
                outiters--;


            }

            return bst_sol;
        }
    }
}
