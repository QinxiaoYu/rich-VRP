using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
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
        DestroyAndRepair DR = new DestroyAndRepair();
        TwoOpt twoopt = new TwoOpt();
        Relocate relointra = new Relocate();
        BreakTwoRoute breakOneRoute = new BreakTwoRoute();
        VehTypeChangeIntra VTC = new VehTypeChangeIntra();
        StationPosition sp = new StationPosition();
        public int stop_criteria { get; set; }
        public int restart_criteria { get; set; }
        public int perturb_criteria { get; set; }
        public Solution search(Solution solution, StringBuilder sb=null, bool isPrint=true)
        {
            
            Solution new_sol = solution.Copy();
            Solution bst_sol = solution.Copy();

            RemoveSta oper = new RemoveSta();
            bool isIprv = oper.Remove(solution); //删除多余的充电站

            solution.printCheckSolution();

           
            solution = sp.StationExchage(solution, 0.3);//优化充电站
            solution.printCheckSolution();
            double newcost = solution.CalObjCost();
            double iter_bst_cost = newcost;

            double percent_battery = 0.2;
            //int short_route = 4;
            int select_strategy = 0; //0: first improve; 1:best improve
            double change_obj = 0; //决定解是否能退化， 小于等于0时候，解一定不变差；大于0，有可能变差


            int outiters = 0; //迭代计数器
            int cnt_improve_pertur = 0; //小扰动的条件
            int cnt_improve_restart = 0; //大扰动的条件
            int bst_Unimprove = 0;

            while (bst_Unimprove < stop_criteria)
            {
                Solution tmp_sol = solution.Copy();
                iter_bst_cost = tmp_sol.CalObjCost();
                double current_cost = iter_bst_cost;

                solution = relointra.RelocateIntra(solution, select_strategy, true, change_obj);//线路内重定位
                solution.printCheckSolution();
                //double newcost3 = solution.CalObjCost();
                //Console.WriteLine(solution.SolutionIsFeasible().ToString());
                //Console.WriteLine("ObjVal Relocate = " + newcost3.ToString("0.00"));
                solution.SolutionIsFeasible();

                //while (tmp_sol != null)
                //{
                //    tmp_sol = twoopt.intarChange(solution,change_obj);

                //    if (tmp_sol != null)
                //    {
                //        bool isWorse = false;
                //        if (tmp_sol.ObjVal >= current_cost)
                //        {
                //            isWorse = true;
                //        }
                //        if (isWorse)
                //        {
                //            break;
                //        }
                //        solution = tmp_sol.Copy();
                //        current_cost = solution.CalObjCost();
                //        //Console.WriteLine(solution.SolutionIsFeasible().ToString());
                //        //Console.WriteLine("ObjVal 2opt = " + newcost42.ToString("0.00"));

                //    }

                //}


                tmp_sol = solution.Copy();
                while (tmp_sol != null)
                {
                    tmp_sol = new ShiftInter().Shift(solution, 1, select_strategy, change_obj); //线路间交换
                    if (tmp_sol != null)
                    {
                        bool isWorse = false;
                        if (tmp_sol.ObjVal >= current_cost)
                        {
                            isWorse = true;
                        }
                        if (isWorse)
                        {
                            break;
                        }
                        solution = tmp_sol.Copy();
                        current_cost = solution.CalObjCost();
                        //Console.WriteLine(solution.SolutionIsFeasible().ToString());
                        //Console.WriteLine("ObjVal 0-1 shift = " + newcost42.ToString("0.00"));

                    }

                }

                tmp_sol = solution.Copy();
                while (tmp_sol != null)
                {
                    tmp_sol = new ShiftInter().Shift(solution, 2, select_strategy, change_obj); //线路间交换
                    if (tmp_sol != null)
                    {
                        bool isWorse = false;
                        if (tmp_sol.ObjVal >= current_cost)
                        {
                            isWorse = true;
                        }
                        if (isWorse)
                        {
                            break;
                        }
                        solution = tmp_sol.Copy();
                        current_cost = solution.CalObjCost();
                        //Console.WriteLine(solution.SolutionIsFeasible().ToString());
                        //Console.WriteLine("ObjVal 0-2 shift = " + newcost42.ToString("0.00"));
                        if (isWorse)
                        {
                            break;
                        }
                    }

                }

                tmp_sol = solution.Copy();
                while (tmp_sol != null)
                {
                    tmp_sol = new RelocateInter().Relocate(solution, 1, 1, select_strategy, change_obj); //线路间交换
                    if (tmp_sol != null)
                    {
                        bool isWorse = false;
                        if (tmp_sol.ObjVal >= current_cost)
                        {
                            isWorse = true;
                        }
                        if (isWorse)
                        {
                            break;
                        }
                        solution = tmp_sol.Copy();
                        current_cost = solution.CalObjCost();
                        //Console.WriteLine(solution.SolutionIsFeasible().ToString());
                        //Console.WriteLine("ObjVal 1-1 swap = " + newcost42.ToString("0.00"));
                    }

                }
                tmp_sol = solution.Copy();
                while (tmp_sol != null)
                {
                    tmp_sol = new RelocateInter().Relocate(solution, 2, 2, select_strategy, change_obj); //线路间交换
                    if (tmp_sol != null)
                    {
                        bool isWorse = false;
                        if (tmp_sol.ObjVal >= current_cost)
                        {
                            isWorse = true;
                        }
                        if (isWorse)
                        {
                            break;
                        }
                        solution = tmp_sol.Copy();
                        current_cost = solution.CalObjCost();
                        //Console.WriteLine(solution.SolutionIsFeasible().ToString());
                        //Console.WriteLine("ObjVal 2-2 swap = " + newcost42.ToString("0.00"));
                    }

                }


                tmp_sol = solution.Copy();
                while (tmp_sol != null)
                {
                    tmp_sol = new CrossInter().Cross(solution, select_strategy, change_obj); //线路间交换
                    if (tmp_sol != null)
                    {
                        bool isWorse = false;
                        if (tmp_sol.ObjVal >= current_cost)
                        {
                            isWorse = true;
                        }
                        if (isWorse)
                        {
                            break;
                        }
                        solution = tmp_sol.Copy();
                        current_cost = solution.CalObjCost();
                        //Console.WriteLine(solution.SolutionIsFeasible().ToString());
                        //Console.WriteLine("ObjVal CrossInter = " + newcost4.ToString("0.00"));
                    }

                }
                solution.SolutionIsFeasible();
                solution = sp.StationExchage(solution, percent_battery);//优化充电站
                solution.SolutionIsFeasible();
                solution = VTC.ChangeToSVehWithoutCharge(solution);
                solution = DR.DR(solution, 5, 1, 1);//删除短路，不能变差
                solution.SolutionIsFeasible();
                current_cost = solution.CalObjCost();
                //Console.WriteLine(solution.SolutionIsFeasible().ToString());
                //Console.WriteLine("ObjVal Sta = " + newcost6.ToString("0.00"));

                if (current_cost < iter_bst_cost) //局部有优化，继续搜索
                {
                    cnt_improve_pertur = Math.Max(0, cnt_improve_pertur - 1);
                    change_obj = 0;
                    select_strategy = 0;
                    if (current_cost < bst_sol.ObjVal) //比全局优
                    {
                        bst_sol = solution.Copy();
                        bst_Unimprove = 0;
                        cnt_improve_pertur = 0;
                        cnt_improve_restart = 0;
                        select_strategy = 1; //换成first improve
                        change_obj = 0;
                        //change_obj = Math.Max(20, change_obj + 5);
                        Console.WriteLine(outiters + ": bst_sol in LS" + bst_sol.ObjVal.ToString("0.00"));
                        if (isPrint)
                        {
                            bst_sol.PrintResult();
                        }
                    }
                    else
                    {
                        bst_Unimprove++;
                    }
                }
                else //局部没有优化，扰动
                {
                    change_obj = Math.Min(100, change_obj + 20);
                    select_strategy = 0;
                    //cnt_improve_pertur++;
                    cnt_improve_restart++;
                    bst_Unimprove++;
                    if (cnt_improve_pertur > perturb_criteria)
                    {
                        solution = Perturb(solution);
                        cnt_improve_pertur = 0;
                        change_obj = 0;
                        select_strategy = 1;
                    }
                   
                    //cnt_improve_pertur = 0;


                    if (cnt_improve_restart > restart_criteria)
                    {
                        solution = Restart(solution);
                        change_obj = 0;
                        select_strategy = 1;
                        cnt_improve_pertur = 0;
                        cnt_improve_restart = 0;
                    }
                    //percent_battery = Math.Max(0.6, percent_battery + 0.1);
                    //short_route = Math.Max(5, short_route + 1);
                    //select_strategy = rd.Next(2); //bst improve
                    ////change_obj = Math.Max(-100, change_obj - 20);
                }

                outiters++;
                Console.WriteLine(outiters.ToString() + ": " + current_cost.ToString("0.00") + ": Route Numbers = " + solution.Routes.Count.ToString() + "Veh Number = " + solution.fleet.VehFleet.Count.ToString());
                if (sb != null)
                {
                    sb.AppendLine(outiters.ToString() + ": " + current_cost.ToString("0.00") + ": Route Numbers = " + solution.Routes.Count.ToString() + "Veh Number = " + solution.fleet.VehFleet.Count.ToString());
                }
            }

            return bst_sol;
        }

        internal Solution Restart(Solution solution)
        {
            int restart_sel = rd.Next(5);
            switch (restart_sel)
            {
                case 0:
                    solution = DR.DR(solution,6,0,0); //删除充电
                    break;
                case 1:
                    solution = breakOneRoute.Break(solution, 1);
                break;
                case 2:
                    solution = breakOneRoute.Break(solution, 4);
                break;
                case 3:
                    solution = DR.DR(solution, 4, 1, 0); //删除短路
                    break;
                case 4:
                    solution = DR.DR(solution, 6, 2, 0);
                    break;
                default:
                    solution = VTC.ChangeToSVehWithCharge(solution);
                    break;
            }

            return solution;
        }

        internal Solution Perturb(Solution solution)
        {
            return solution;
        }
    }
}
