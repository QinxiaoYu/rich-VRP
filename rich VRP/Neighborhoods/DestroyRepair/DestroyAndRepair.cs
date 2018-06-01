using OP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rich_VRP.Neighborhoods.DestroyRepair
{
    class DestroyAndRepair
    {
        public Solution solution;
        public Fleet fleet;
        public DestroyAndRepair(Solution _solution)
        {
            solution = _solution.Copy();
            fleet = solution.Problem.fleet.Copy();
            solution.UnVisitedCus = new List<Customer>();
        }
        /// <summary>
        /// 如果一条路线上商户的数量小于threshold_node个，则删除此线路
        /// </summary>
        /// <param name="threshold_node"></param>
        public void DestroyShortRoute(int threshold_node)
        {

        }

    }
}
