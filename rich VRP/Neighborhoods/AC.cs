using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OP.Data;

namespace rich_VRP.Neighborhoods
{
    /// <summary>
    /// 这个类是角度聚类
    /// 下面是使用方法说明
    /// 在初始化时，需要输入Problem，以及分类角度一个int型的list
    /// 分类角度要求是角度和需要小于360度，否则将会弹出报错（如果小于360，最后部分会自动分为一类）
    /// 初始化后，就能够进行类的输出，有两个输出方式
    /// 第一个是getCluster，输入节点ID，输出所在聚类(int)
    /// 第二个是getNodes，输入聚类ID，输出聚类中的节点信息(List<NodeInfo>)
    /// 第三个是getClusterNum，输出所在聚类数目
    /// 第四个是coverAll，输出是否全部节点都有簇id，（如果是否，就可以利用getNodes(0)查询所有没被覆盖节点）
    /// </summary>
    class AC
    {
        public int clusterNum;
        public List<int> clusterAngel;
        public List<clusterPoint> clusterPoints;
        public NodeInfo datum;
        public AC(Problem P, List<int> Angel)
        {
            int cheak = 0;
            foreach (var angel in Angel)
            {
                cheak = cheak + angel;
            }
            if (cheak > 360)
            {
                throw new Exception("角度聚类时分类未达到360度");
            }
            if (cheak < 360)
            {
                clusterAngel.Add(360 - cheak);
            }
            else
            {
                clusterAngel = Angel;
                clusterNum = 0;
                datum = P.AllNodes[0];
                clusterPoints = new List<clusterPoint>();
                setClusterPoint(P.AllNodes);
                clustering();
            }
        }
        /// <summary>
        /// 初始化聚类节点
        /// </summary>
        /// <param name="allNodes"></param>
        public void setClusterPoint(List<NodeInfo> allNodes)
        {
            for (int i = 0; i < allNodes.Count; i++)
            {
                if (allNodes[i].Id == datum.Id)
                {
                    continue;
                }
                else
                {
                    double angle = Math.Atan2((allNodes[i].Y - datum.Y), (allNodes[i].X - datum.X)) * 180 / Math.PI;
                    if (angle < 0)
                    {
                        angle = angle + 360;
                    }
                    clusterPoint cp = new clusterPoint(allNodes[i], angle);
                    clusterPoints.Add(cp);
                }
            }
        }
        /// <summary>
        /// 聚类程序（进行聚类）
        /// </summary>
        public void clustering()
        {
            int lowerBoundary = 0;
            int upperBoundary = 0;
            for (int i = 0; i < clusterAngel.Count; i++)
            {
                clusterNum++;
                lowerBoundary = upperBoundary;
                upperBoundary = upperBoundary + clusterAngel[i];
                for (int j = 0; j < clusterPoints.Count; j++)
                {
                    if (clusterPoints[j].ClusterID == 0 && clusterPoints[j].Angel >= lowerBoundary && clusterPoints[j].Angel < upperBoundary)
                    {
                        clusterPoints[j].ClusterID = clusterNum;
                    }
                }

            }
        }
        /// <summary>
        /// 查询该节点所在簇id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int getCluster(int id)
        {
            for (int i = 0; i < clusterPoints.Count; i++)
            {
                if (clusterPoints[i].Info.Id == id)
                {
                    return clusterPoints[i].ClusterID;
                }
            }
            return 0;
        }




        /// <summary>
        /// 输入路径，返回该路径的cluster
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public int getRouteCluster(Route route)
        {
            int clusterID = 0;

            return clusterID;

        }





        /// <summary>
        /// 查询该簇下的所有节点信息
        /// </summary>
        /// <param name="clusterId">查询的簇id</param>
        /// <returns></returns>
        public List<NodeInfo> getNodes(int clusterId)
        {
            if (clusterId > clusterNum || clusterId < 0)
            {
                throw new Exception("不存在该聚类");
            }
            List<NodeInfo> Nodes = new List<NodeInfo>();
            for (int i = 0; i < clusterPoints.Count; i++)
            {
                if (clusterPoints[i].ClusterID == clusterId)
                {
                    Nodes.Add(clusterPoints[i].Info);
                }
            }
            return Nodes;
        }
        /// <summary>
        /// 查询聚类数目
        /// </summary>
        /// <returns></returns>
        public int getClusterNum()
        {

            return clusterNum;
        }
        /// <summary>
        /// 检查是否所有节点被分类
        /// </summary>
        /// <returns></returns>
        public bool coverAll()
        {
            for (int i = 0; i < clusterPoints.Count; i++)
            {
                if (clusterPoints[i].ClusterID == 0)
                {
                    return false;
                }
            }
            return true;
        }
    }
    class clusterPoint
    {
        public NodeInfo Info;
        public int ClusterID;
        public double Angel;
        /// <summary>
        /// 在初始化的时候，所有的点的聚类id为0
        /// 在完成聚类后，应该只有仓库点的聚类id为0
        /// </summary>
        /// <param name="info"></param>
        public clusterPoint(NodeInfo info, double angel)
        {
            Info = info;
            ClusterID = 0;
            Angel = angel;
        }
    }
}
