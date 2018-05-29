using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OP.Data;
using rich_VRP.Constructive;
using System.IO;

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
            problem.SetNearDistanceCusAndSta(10, 2); //计算每个商户的小邻域
            Initialization cons = new Initialization(problem);
            Solution sol = cons.initial_construct();
        }
    }
}
