using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OP.Data;
using System.IO;
using rich_VRP.Constructive;

namespace rich_VRP
{
    class Program
    {
        static void Main(string[] args)
        {
            OpProblemReader reader = new OpProblemReader();
            string dir = Directory.GetCurrentDirectory();
            
            Problem problem = reader.Read(dir);
            problem.MinWaitTimeAtDepot = 60;
      

            ///初始化
            Initialization initial = new Initialization(problem);
            Solution ini_solution = initial.initial_construct();
            string result = ini_solution.PrintToString();

            string outfilename = null;
            StringBuilder sb = new StringBuilder();
            outfilename = dir + "//" + "test.txt";
            StreamWriter sw = new StreamWriter(outfilename, true);
            sb.AppendLine(result);
            sw.Write(sb);

        }
    }
}
