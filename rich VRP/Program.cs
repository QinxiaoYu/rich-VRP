using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OP.Data;

using System.IO;
using rich_VRP.Constructive;
using rich_VRP.ObjectiveFunc;

namespace rich_VRP
{
    class Program
    {
        static void Main(string[] args)
        {
            OpProblemReader reader = new OpProblemReader();
            string dir = Directory.GetCurrentDirectory();
            
            Problem problem = reader.Read(dir);
            problem.MinWaitTimeAtDepot = 60; //在配送中心的最少等待时间 
            //problem.SetNearDistanceCusAndSta(10, 2); //计算每个商户的小邻域

            ///初始化
            Initialization initial = new Initialization(problem);
            Solution ini_solution = initial.initial_construct();

           

            string result = ini_solution.PrintToString();
            string outfilename = null;
            StringBuilder sb = new StringBuilder();
            outfilename = dir + "//" + "test_4.txt";
            StreamWriter sw = new StreamWriter(outfilename, true);

            OriginObjFunc evaluate = new OriginObjFunc();
            double cost = evaluate.CalObjCost(ini_solution);
            sb.AppendLine(cost.ToString("0.00"));

            sb.AppendLine(result);

            
            

            sw.Write(sb);


          
        }
    }
}
