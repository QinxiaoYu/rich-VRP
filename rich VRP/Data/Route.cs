using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Collections;

namespace OP.Data
{
    public class Route
    {
        public Problem Problem;
            
        /// <summary>
        /// 路径的ID
        /// </summary>
        public string RouteId { get; set; }

        
        /// <summary>
        /// 路径上的顾客集合，首尾包含仓库
        /// </summary>
        public List<AbsNode> RouteList { get; set; }
        
        /// <summary>
        /// 路径上各个节点的服务开始时间
        /// </summary>
        public List<double> ServiceBeginingTimes { get; set; }
        public List<double> ServiceTimes { get; set; }
        /// <summary>
        /// 对应的解
        /// </summary>
        public Solution Solution { get; set; }


        /// <summary>
        /// 初始化一条路径，该路径为空
        /// </summary>
        /// <param name="problem"></param>
        public Route(Problem problem)
        {
            Depot startdepot = problem.StartDepot;
            Depot enddepot = problem.EndDepot;
            Problem = problem;
            Solution = null;
            RouteList = new List<AbsNode>();
            ServiceBeginingTimes = new List<double>();
            ServiceTimes = new List<double>();
            AddNode(startdepot);
            AddNode(enddepot);
        }

        /// <summary>
        /// 初始化一条路径，该路径只包含一个种子顾客
        /// </summary>
        /// <param name="problem"></param>
        /// <param name="seedCustomer">已选的种子顾客</param>
        public Route(Problem problem, Customer seedCustomer)
        {
            Depot startdepot = problem.StartDepot;
            Depot enddepot = problem.EndDepot;
            Problem = problem;
            Solution = null;
            RouteList = new List<AbsNode>();
            ServiceBeginingTimes = new List<double>();
            ServiceTimes = new List<double>();
            AddNode(startdepot);
            AddNode(seedCustomer);
            AddNode(enddepot);
        }
        /////// <summary>
        /////// 插入新顾客到路径尾，有问题，无引用
        /////// </summary>
        /////// <param name="newCustomer"></param>
        ////public void AddCustomer(Customer newCustomer)
        ////{
        ////    newCustomer = (Customer)newCustomer.ShallowCopy();
        ////    newCustomer.Route = this;
        ////    AddNode(newCustomer);
        ////}


        /// <summary>
        /// 插入新节点到路径末尾，并更新路径各节点的服务时间；
        /// </summary>
        /// <param name="newNode">抽象节点，可以是顾客也可是仓库</param>
        private void AddNode(AbsNode newNode)
        {
            //线路上最后一个点
            AbsNode lastCustomer = RouteList.Count == 0 ? newNode : RouteList[RouteList.Count - 1];
            //线路上最后一个点 可以开始游览的时间 （如果到达时间小于时间窗的开始时间，则为时间窗开始时间；否则为实际达到时间）
            double lastServiceTime = RouteList.Count == 0 ? newNode.Info.ReadyTime : ServiceBeginingTimes[ServiceBeginingTimes.Count - 1];
            //新景点 可以开始游览的时间
            double serviceBegins = NextServiceBeginTime(newNode, lastCustomer, lastServiceTime);
            RouteList.Add(newNode);
            ServiceBeginingTimes.Add(serviceBegins);
            ServiceTimes.Add(newNode.Info.ServiceTime);
            UpdateId();
        }

        public List<AbsNode> getSubRoute(AbsNode fromNode, AbsNode toNode)
        {
            List<AbsNode> SubCustomers = new List<AbsNode>();
            if (fromNode is Depot && toNode is Depot)
            {
                SubCustomers.Add(RouteList[0]);
                return SubCustomers;
            }
            else
            {
                int from_index = (fromNode is Depot) ? 0 : ((Customer)fromNode).Index();
                int to_index = (toNode is Depot) ? this.RouteList.Count - 1 : ((Customer)toNode).Index();
                if (from_index <= to_index)
                {
                    for (int i = from_index; i <= to_index; i++)
                    {
                        SubCustomers.Add(this.RouteList[i]);
                    }
                }
                else
                {
                    for (int i = from_index; i < this.RouteList.Count; i++)
                    {
                        SubCustomers.Add(this.RouteList[i]);
                    }
                    for (int j = 1; j <= to_index; j++)
                    {
                        SubCustomers.Add(this.RouteList[j]);
                    }
                }


                return SubCustomers;
            }
        }

        internal void InsertCustomer(List<Customer> unroutedCustomers)
        {
            
            for (int i = 0; i < unroutedCustomers.Count; i++)
            {
                Customer cusToInsert = unroutedCustomers.ElementAt(i);
                InsertCustomer(cusToInsert, this.RouteList.Count - 1);
            }            

        }


        /// <summary>
        /// 在路径中任意位置插入新顾客
        /// </summary>
        /// <param name="newCustomer">待插入的新顾客</param>
        /// <param name="position">插入位置</param>
        public void InsertCustomer(Customer newCustomer, int position)
        {
            newCustomer = (Customer)newCustomer.ShallowCopy();
            newCustomer.Route = this;
            RouteList.Insert(position, newCustomer);
            ServiceBeginingTimes.Insert(position,0);
            //更新插入顾客及其之后顾客的服务开始时间
            for (int i = position; i < RouteList.Count; ++i)
            {
                double newTime = NextServiceBeginTime(RouteList[i], RouteList[i - 1], ServiceBeginingTimes[i - 1]);
                ServiceBeginingTimes[i] = newTime;
            }
            ServiceTimes.Insert(position, newCustomer.Info.ServiceTime);
            UpdateId();
        }

        /// <summary>
        /// 删除某一位置上的顾客，更新服务时间
        /// </summary>
        /// <param name="position">要删除的位置</param>
        public void RemoveAt(int position)
        {
            RouteList.RemoveAt(position);
            ServiceBeginingTimes.RemoveAt(position);
            for (int i = position; i < RouteList.Count; ++i)
            {
                double newTime = NextServiceBeginTime(RouteList[i], RouteList[i - 1], ServiceBeginingTimes[i - 1]);
                ServiceBeginingTimes[i] = newTime;
            }
            ServiceTimes.RemoveAt(position);
            UpdateId();
        }

        public void Remove(Customer cus)
        {
            for (var i = 0; i < this.RouteList.Count; ++i)
                if (this.RouteList[i].Info.Id == cus.Info.Id)
                    RemoveAt(i);

        }

        /// <summary>
        /// 获取某个点的近邻
        /// </summary>
        /// <param name="v_index">目标点在路径中的下标</param>
        /// <returns>其他点（不包括Depot）从小到大排列</returns>
        internal List<Customer> getNeighborhood(Customer v)
        {
            List<Customer> neighborhood = new List<Customer>();
            Hashtable ht = new Hashtable();      
            
            for (int i = 1; i < this.RouteList.Count-2; i++)
            {
                Customer neighbor = (Customer)this.RouteList[i];
                if (neighbor.Info.Id != v.Info.Id)
                {              
                    double distance = v.Distance(neighbor);
                    ht.Add(neighbor, distance);
                }
            }

            double[] valueArray = new double[ht.Count];
            Customer[] keyArray = new Customer[ht.Count];
            ht.Keys.CopyTo(keyArray, 0);
            ht.Values.CopyTo(valueArray, 0);
            Array.Sort(valueArray, keyArray);//默认升序排列
            neighborhood.AddRange(keyArray.ToList());

            return neighborhood;
        }


        /// <summary>
        /// 检查路径是否可行
        /// </summary>
        /// <returns>不可行：对某一个顾客：服务时间>最晚可接受时间or对路径：容量超限</returns>
        public bool IsFeasible()
        {
            for (int i = 0; i < RouteList.Count; ++i)
            {
                if (ServiceBeginingTimes[i] + RouteList[i].Info.ServiceTime > RouteList[i].Info.DueDate)
                    return false;
            }

            if (this.Problem.Threashold>0)
            {
                return IsRepeat(this.Problem.Threashold);
            }
            return true;
        }

        public bool IsRepeat(int threashold)
        {
            for (int i = 1; i < RouteList.Count - 1; i++)
            {
                int type_i = RouteList[i].Info.Type;
                int cnt_repeat = 1;
                for (int j = i + 1; j < RouteList.Count - 1; j++)
                {
                    int type_j = RouteList[j].Info.Type;
                    if (type_j == type_i)
                    {
                        cnt_repeat++;
                    }
                }
                if (cnt_repeat > threashold)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 到达下一个景点的达到时间
        /// </summary>
        /// <param name="newCustomer">下一个景点（新景点）</param>
        /// <param name="prevCustomer">当前景点</param>
        /// <param name="prevTime">当前景点可以开始游览的时间</param>
        /// <returns>下一个景点的可以开始游览时间</returns>
        public double NextServiceBeginTime(AbsNode newCustomer, AbsNode prevCustomer, double prevTime)
        {
            double travelTime = prevCustomer.TravelTime(newCustomer);
            double serviceTime = prevCustomer.Info.ServiceTime;
            double readyTime = newCustomer.Info.ReadyTime;
            return Math.Max(readyTime, prevTime + serviceTime + travelTime);
        }
        /// <summary>
        /// 获取路径中目标点的下一个节点，路径看作首尾相连
        /// </summary>
        /// <param name="vi">目标点</param>
        /// <returns>返回后件</returns>
        internal AbsNode getNext(AbsNode vi)
        {
            int vi_id = vi.Info.Id;
            int vi_index = 0;
            if (vi_id != 0)
            {
                vi_index = ((Customer)vi).Index();
            }
            return this.RouteList[vi_index + 1];
        }

        internal double WaitTime()
        {
            double waittime = 0;
            for (int i = 1; i < this.RouteList.Count; i++)
            {
                double arrivetime = ServiceBeginingTimes[i - 1] + RouteList[i - 1].Info.ServiceTime + RouteList[i - 1].TravelTime(RouteList[i]);
                double servicestarttime = ServiceBeginingTimes[i];
                if (arrivetime<servicestarttime)
                {
                    waittime += servicestarttime - arrivetime;
                }
            }
            return waittime;
        }

        internal AbsNode getPrevious(AbsNode vi)
        {
            int vi_id = vi.Info.Id;
            int vi_index = this.RouteList.Count - 1;
            if (vi_id!=0)
            {
                vi_index = ((Customer)vi).Index();
            }
            return this.RouteList[vi_index - 1];
        }

        public Route Copy()
        {
            var newRouteList = new List<AbsNode>(RouteList.Count);
            newRouteList.AddRange(RouteList.Select(node => node.ShallowCopy()));
            var r = new Route(Problem)
            {
                RouteList = newRouteList,
                ServiceBeginingTimes = new List<double>(ServiceBeginingTimes),
                ServiceTimes = new List<double>(ServiceTimes)
            };
            for (int i = 1; i < RouteList.Count - 1; ++i)
                ((Customer)r.RouteList[i]).Route = r;
            r.UpdateId();
            return r;
        }

        internal double Service()
        {
            double service = 0;
            for (int i = 0; i < ServiceTimes.Count; i++)
            {
                service += ServiceTimes[i];
            }
            return service;
        }

        public double Capacity()
        {
            double cap = 0.0;
            foreach (AbsNode customer in RouteList)
                cap += customer.Info.Score;
            return cap;
        }

        internal bool IsBetween(AbsNode node, AbsNode fromNode, AbsNode toNode)
        {
            int node_index = node is Customer ? ((Customer)node).Index() : 0;
            int fromNode_index = fromNode is Customer ? ((Customer)fromNode).Index() : 0;
            int toNode_index = toNode is Customer ? ((Customer)toNode).Index() : this.RouteList.Count-1;
            if (fromNode is Depot && toNode is Depot)
            {
                return (node is Depot) ? true : false;
            }
            if (fromNode_index<toNode_index)
            {
                return (node_index >= fromNode_index) && (node_index <= toNode_index);
            }
            if (fromNode_index==toNode_index)
            {
                return node_index == fromNode_index;
            }
            if (fromNode_index>toNode_index)
            {
                return node_index >= fromNode_index || node_index <= toNode_index;
            }
            return false;
        }

        public string PrintToString(bool printTime = true, bool printCapacity = false, bool printCapacityUnit = false )
        {
            //string routeText = "";
            //string serviceText = "";
            //string serviceBeginText = "";
            string TourInfo = string.Format("{0} 点 从 {1} 出发：\n", ServiceBeginingTimes[0], RouteList[0].Info.Name);
            for (int i = 1; i < RouteList.Count-1; ++i)
            {
                //routeText += RouteList[i].Info.Name;
                ////serviceText += RouteList[i].Info.ServiceTime.ToString("0.00") + " ";
                //serviceText += ServiceTimes[i].ToString("0.00") + " ";
                //serviceBeginText += ServiceBeginingTimes[i].ToString("0.00") + " ";
                double departuretime = ServiceBeginingTimes[i - 1] + ServiceTimes[i - 1];
                double arrivetime = departuretime + RouteList[i - 1].TravelTime(RouteList[i]);
                double waittime = Math.Max(0, ServiceBeginingTimes[i] - arrivetime);
                double servicetime = RouteList[i].Info.ServiceTime;
                TourInfo += string.Format("第{0}站：{1}, 出发时间 {2}, 到达时间 {3}, 等待时间 {4}, 游览时长 {5}, 时间窗：({6},{7}), 类型 {8} \n", i , RouteList[i].Info.Name,departuretime,arrivetime,waittime,servicetime,RouteList[i].Info.ReadyTime,RouteList[i].Info.DueDate, RouteList[i].Info.Type);
                //if (i != RouteList.Count - 1)
                //{
                //    routeText += "-";
                //    serviceText += " ";
                //    serviceBeginText += " ";
                //}
            }
            TourInfo += string.Format("{0} 点 到达 {1} ,旅途结束。\n", ServiceBeginingTimes[RouteList.Count-1],RouteList[RouteList.Count-1].Info.Name);
            //if (printTime)
            //{
            //    return (routeText + "\n" + serviceText+"\n");
            //}
            //else
            //{
            //    return routeText;
            //}
            //return (routeText + "," + serviceText);
            return TourInfo;
        }


        /// <summary>
        /// 路径的长度，或服务时间
        /// </summary>
        /// <returns></returns>
        public double Length()
        {
            double totalDist = 0;
            for (int i = 0; i < RouteList.Count - 1; ++i)
                totalDist += RouteList[i].Distance(RouteList[i + 1]);
            return totalDist;
        }


        /// <summary>
        /// 路径在解中的下标
        /// </summary>
        /// <returns></returns>
        public int Index()
        {
            for (int i = 0; i < Solution.Routes.Count; ++i)
                if (Solution.Routes[i] == this)
                    return i;
            return 0;
        }


        private void UpdateId()
        {
            if (RouteList.Count > 2)
                RouteId = RouteList[1].Info.Id.ToString(CultureInfo.InvariantCulture) + "-" +
                          RouteList[RouteList.Count - 2].Info.Id.ToString(CultureInfo.InvariantCulture);
            else
                RouteId = "<EMPTY>";
        }
        /// <summary>
        /// 计算路径的信息
        /// </summary>
        /// <returns>返回路径[0旅行时间（行驶距离）,1容量约束,2时间约束,3等待时间]</returns>
        public double[] getRouteInfo()
        {
            double traveltime = 0;
            double loadViol = 0;
            double twViol = 0;
            double watingtime = 0;
            for (int i = 0; i < this.RouteList.Count-1; i++)
            {
                traveltime += RouteList[i].Distance(RouteList[i + 1]);
                //loadViol += RouteList[i].Info.Demand;
                twViol += Math.Max(0, ServiceBeginingTimes[i] - RouteList[i].Info.DueDate);
                watingtime += i == 0 ? 0 : Math.Max(0, RouteList[i].Info.ReadyTime - (ServiceBeginingTimes[i - 1] + RouteList[i - 1].Distance(RouteList[i]) + RouteList[i - 1].Info.ServiceTime));
            }
            loadViol = Math.Max(loadViol-Problem.Tmax,0);
            return new double[] { traveltime, loadViol, twViol, watingtime };
        }

        public bool isEqual(Route route2)
        {
         
            if (this.RouteId != route2.RouteId)
            {
                return false;
            }
            if (this.RouteList.Count != route2.RouteList.Count)
            {
                return false;
            }
            for (int i = 1; i < route2.RouteList.Count-1; i++)
            {
                if (this.RouteList[i].Info.Id != route2.RouteList[i].Info.Id)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
