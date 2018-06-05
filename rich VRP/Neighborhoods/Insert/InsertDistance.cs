using OP.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rich_VRP.Neighborhoods.Insert
{
    class InsertDistance
    {
    }

    class InsertSta
    {

        public Route InsertStaInRoute(Route route)
        {
            Route bst_route = null;
            if (route.IsFeasible())
            {
                bst_route = route;
            }
            else
            {
                bool feasible = false;
                while (!feasible)
                {
                    int best_seat = -1;//记录最优位置
                    double min_add_dis = double.MaxValue; //一个无穷大的数
                    for (int i = 1; i < route.RouteList.Count-1; i++)
                    {
                        Route cur_route = route.Copy();
                        Station insert_sta = cur_route.insert_sta(cur_route.RouteList[i]);
                        cur_route.InsertNode(insert_sta, i+1);

                        if (cur_route.ViolationOfTimeWindow() == -1)
                        {
                            double add_dis = insert_sta.TravelDistance(cur_route.RouteList[i]) + insert_sta.TravelDistance(cur_route.RouteList[i + 2])
                            - cur_route.RouteList[i].TravelDistance(cur_route.RouteList[i + 2]);
                            if (add_dis < min_add_dis)
                            {
                                min_add_dis = add_dis;
                                best_seat = i;
                            }
                        }                    
                    }

                    if (best_seat > 0)
                    {
                        Station inserted_sta = route.insert_sta(route.RouteList[best_seat]);
                        route.InsertNode(inserted_sta, best_seat + 1);
                        if (route.IsFeasible())
                        {
                            feasible = true;
                            bst_route = route;
                        } 
                    }
                    else
                    {
                        feasible = true;
                        bst_route = null;
                    }

                }//退出while循环
            }

            return bst_route;
        }

    }
}
