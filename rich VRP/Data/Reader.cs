using System.Collections.Generic;
using System.IO;
using System;

namespace OP.Data
{
    interface IProblemReader
    {
        Problem Read(string source);
    }


    internal class OpProblemReader : IProblemReader
    {
      
        public Problem Read(string txtFilePath)
        {
            string nodefile = txtFilePath + "\\node.txt";
            string disfile = txtFilePath + "\\data.txt";
            string vehfile = txtFilePath + "\\veh.txt";         
            var nodes = new List<NodeInfo>();
            var p = new Problem();
            string abbr = "DOGGY"; //数据集名称，如C100.txt, 则abbr = C100;此处无用
            double Tmax = 14400; //可能没用
            int numV = 1000; //车辆数
            int numC = 0;
            int numS = 0;
            int numD = 0;
            using (StreamReader reader = new StreamReader(nodefile))
            {
                //abbr = Path.GetFileNameWithoutExtension(txtFilePath);
                string line = string.Empty;

                //read node info
                //序号 类型 经度 纬度 包裹总重量 包裹总体积 商家最早收货时间 商家最晚收货时间
                line = reader.ReadLine();
       
                while (!string.IsNullOrWhiteSpace(line))
                {
                    string[] paras = line.Split(new char[1] { '	' }, StringSplitOptions.RemoveEmptyEntries);
                    //int _c_id, int _x, int _y, int _s, int _r_t, int _d_t, int _s_t
                    int _c_id = int.Parse(paras[0]);                                   
                    int _type = int.Parse(paras[1]); //类型
                    double _x = double.Parse(paras[2]);//经度 
                    double _y = double.Parse(paras[3]);//纬度
                    double _w = _type==2? double.Parse(paras[4]):0;//重量
                    double _v = _type==2? double.Parse(paras[5]):0;//体积
                    double _s_t = _type==1? 1.0*60:0.5 * 60;//卸货时间，恒定为0.5h
                    double _r_t = _type==2?double.Parse(paras[6]):480; //商家最早收货时间 换算成从0点开始的分钟，如8:00，则为480；24:00，则为1440
                    double _d_t = _type==2?double.Parse(paras[7]):1440; //商家最晚收货时间
                    switch (_type)
                    {
                        case 1:
                            numD++;
                            break;
                        case 2:
                            numC++;
                            break;
                        default:
                            numS++;
                            break;
                    }

                    nodes.Add(new NodeInfo
                    {
                        Id = _c_id,
                        Type = _type,
                        X = _x,
                        Y = _y,
                        Weight = _w,
                        Volume = _v,
                        ReadyTime = _r_t,
                        DueDate = _d_t,
                        ServiceTime = _s_t,
                        
                    });
                    
                    line = reader.ReadLine();
                   
                }
                reader.Close();
     
            }

            p.SetNodes(nodes, abbr, Tmax, numV,numD,numC,numS);
           
            using (StreamReader Dreader = new StreamReader(disfile))
            {
                string str_Dis = string.Empty;
                str_Dis = Dreader.ReadLine();
                while (!string.IsNullOrWhiteSpace(str_Dis))
                {
                    string[] paras = str_Dis.Split(new char[1] { '	' }, StringSplitOptions.RemoveEmptyEntries);
                    int _i = int.Parse(paras[1]);
                    int _j = int.Parse(paras[2]);
                    int _dis_ij = int.Parse(paras[4]);
                    p.SetDistanceIJ(_i, _j, _dis_ij); //默认给的距离时间是个完全矩阵
                    str_Dis = Dreader.ReadLine();
                }
            
            Console.WriteLine(Tmax.ToString("0.00"));
          
            //sw.Flush();
            //sw.Close();
            return p;
            
        }

      
    }

}