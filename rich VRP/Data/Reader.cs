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
            var types = new List<VehicleType>();
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
                    double _s_t = _type==1? 0:0.5 * 60;//卸货时间，恒定为0.5h，换算成30min
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
                    double _dis_ij = double.Parse(paras[3]);
                    double _tt_ij = double.Parse(paras[4]);
                    p.SetDistanceIJ(_i, _j, _tt_ij, _dis_ij); //陈鹏写
                    str_Dis = Dreader.ReadLine();
                }
                Dreader.Close();
            }

            using (StreamReader Vreader = new StreamReader(vehfile))
            {
                string str_VehType = string.Empty;
                str_VehType = Vreader.ReadLine();
                while (!string.IsNullOrWhiteSpace(str_VehType))
                {
                    string[] paras = str_VehType.Split(new char[1] { '	' }, StringSplitOptions.RemoveEmptyEntries);
                    int _vtid = int.Parse(paras[0]);
                    string _name = paras[1];
                    double _v = double.Parse(paras[2]);
                    double _w = double.Parse(paras[3]);
                    int _maxnum = paras[4] == "不限" ? numC : int.Parse(paras[4]);
                    double _maxrange = double.Parse(paras[5]);
                    double _chargetime = double.Parse(paras[6])*60;
                    double _vcost = double.Parse(paras[7]);
                    double _fcost = double.Parse(paras[8]);

                    types.Add(new VehicleType
                    {
                        VehTypeID = _vtid,
                        Name = _name,
                        Volume = _v,
                        Weight = _w,
                        MaxRange = _maxrange,
                        MaxNum = _maxnum,
                        ChargeTime = _chargetime,
                        ChargeCostRate = 50, //直接计算一次充电费用为50 = 100*0.5
                        VariableCost = _vcost,
                        FixedCost = _fcost,
                    });
                }
                Vreader.Close();
            }
            p.SetVehicleTypes(types);

            return p;
            
        }    
    }

}