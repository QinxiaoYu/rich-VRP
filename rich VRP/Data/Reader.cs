using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using LinqToExcel;
using System.Linq;
using System.Xml.Linq;
using System.Reflection;
using System.Text;
using System.Data.OleDb;
using ExcelDataReader;
namespace OP.Data
{


    internal class OpProblemReader
    {
      
        public void Read(string txtFilePath)
        {
			string nodefile = txtFilePath + "/input_node.xlsx";
			string disfile = txtFilePath + "/input_distance-time.txt";
			string vehfile = txtFilePath + "/input_vehicle_type.xlsx";
            var nodes = new List<NodeInfo>();
            var types = new List<VehicleType>();         
            string abbr = "DOGGY"; //数据集名称，如C100.txt, 则abbr = C100;此处无用
            double Tmax = 14400; //可能没用
            int numV = 1000; //车辆数
            int numC = 0;
            int numS = 0;
            int numD = 0;
			DataTable nodeTable = ExcelTocsv(nodefile);
			foreach (DataRow dr in nodeTable.Rows)
            {
                //abbr = Path.GetFileNameWithoutExtension(txtFilePath);
                string line = string.Empty;

                //read node info
                //序号 类型 经度 纬度 包裹总重量 包裹总体积 商家最早收货时间 商家最晚收货时间
                    //int _c_id, int _x, int _y, int _s, int _r_t, int _d_t, int _s_t
				int _c_id = int.Parse(dr[0].ToString());
				int _type = int.Parse(dr[1].ToString()); //类型
				double _x = double.Parse(dr[2].ToString());//经度 
				double _y = double.Parse(dr[3].ToString());//纬度
				double _w = _type == 2 ? double.Parse(dr[4].ToString()) : 0;//重量
				double _v = _type == 2 ? double.Parse(dr[5].ToString()) : 0;//体积
				double _s_t = _type == 1 ? 0 : 0.5 * 60;//卸货时间，恒定为0.5h，换算成30min
                    int _r_t = 480; //商家最早收货时间 换算成从0点开始的分钟，如8:00，则为480；24:00，则为1440
                    int _d_t = 1440;//商家最晚收货时间
                    if (_type == 2)
                    {
					DateTime _r_t1 = DateTime.Parse(dr[6].ToString());
                        int min = _r_t1.Minute;
                        int hour = _r_t1.Hour;
                        _r_t = hour * 60 + min; //商家最早收货时间 换算成从0点开始的分钟，如8:00，则为480；24:00，则为1440
					DateTime _d_t1 = DateTime.Parse(dr[7].ToString());
                        int min_d_t = _d_t1.Minute;
                        int hour_d_t = _d_t1.Hour;
                        _d_t = hour_d_t * 60 + min_d_t;//商家最晚收货时间
                    }
            
                                 
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
                    
                   
     
            }
			Problem.SetNodes(nodes, abbr, Tmax, numV, numD, numC, numS);
			int max_dis = int.MinValue;
			int min_dis = int.MaxValue;
			int max_tt = int.MinValue;
			int min_tt = int.MaxValue;
            using (StreamReader Dreader = new StreamReader(disfile))
            {
                string str_Dis = string.Empty;
                str_Dis = Dreader.ReadLine();
                str_Dis = Dreader.ReadLine();
                while (!string.IsNullOrWhiteSpace(str_Dis))
                {
                    string[] paras = str_Dis.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    int _i = int.Parse(paras[1]);
                    int _j = int.Parse(paras[2]);
                    int _dis_ij = int.Parse(paras[3]);
                    int _tt_ij = int.Parse(paras[4]);
                    Problem.SetDistanceANDTravelIJ(_i, _j, _tt_ij, _dis_ij); 
                    str_Dis = Dreader.ReadLine();
                    if (_dis_ij>max_dis)
                    {
                        max_dis = _dis_ij;
                    }
                    if(_dis_ij<min_dis)
                    {
                        min_dis = _dis_ij;
                }
                    if (_tt_ij > max_tt)
                    {
                        max_tt = _tt_ij;
                    }
                    if (_tt_ij < min_tt)
                    {
                        min_tt = _tt_ij;
                    }
                }
                Dreader.Close();
                Console.WriteLine(max_tt.ToString() + ","+min_tt.ToString() + "," + max_dis.ToString() + "," + min_dis.ToString());
            }

			DataTable vehicleTable = ExcelTocsv(vehfile);
			foreach (DataRow dr in vehicleTable.Rows)
            {
				int _vtid = int.Parse(dr[0].ToString());
				string _name = dr[1].ToString();
				double _v = double.Parse(dr[2].ToString());
				double _w = double.Parse(dr[3].ToString());
				int _maxnum = dr[4].ToString() == "unlimited" ? numC : int.Parse(dr[4].ToString());
				double _maxrange = double.Parse(dr[5].ToString()); //按照米算
				double _chargetime = double.Parse(dr[6].ToString()); //按照小时算
				double _vcost = double.Parse(dr[7].ToString());
				double _fcost = double.Parse(dr[8].ToString());
				double _chargerate = 100.0000 / 60;
                    types.Add(new VehicleType
                    {
                        VehTypeID = _vtid,
                        Name = _name,
                        Volume = _v,
                        Weight = _w,
                        MaxRange = _maxrange,
                        MaxNum = _maxnum,
                        ChargeTime = _chargetime, //0.5h
					ChargeCostRate = _chargerate, // 100RMB/h
					VariableCost = _vcost / 1000,
                        FixedCost = _fcost,
                    });


            }


            Problem.SetVehicleTypes(types);   
            
        }

        public void ReadSmall(string txtFilePath)
        {
            string nodefile = txtFilePath + "/input_node_small.xlsx";
            string disfile = txtFilePath + "/input_distance-time.txt";
            string vehfile = txtFilePath + "/input_vehicle_type.xlsx";
            var nodes = new List<NodeInfo>();
            var types = new List<VehicleType>();
            string abbr = "DOGGY"; //数据集名称，如C100.txt, 则abbr = C100;此处无用
            double Tmax = 14400; //可能没用
            int numV = 1000; //车辆数
            int numC = 0;
            int numS = 0;
            int numD = 0;
            DataTable nodeTable = ExcelTocsv(nodefile);
            foreach (DataRow dr in nodeTable.Rows)
            {
                //abbr = Path.GetFileNameWithoutExtension(txtFilePath);
                string line = string.Empty;

                //read node info
                //序号 类型 经度 纬度 包裹总重量 包裹总体积 商家最早收货时间 商家最晚收货时间
                //int _c_id, int _x, int _y, int _s, int _r_t, int _d_t, int _s_t
                int _c_id = int.Parse(dr[0].ToString());
                int _type = int.Parse(dr[1].ToString()); //类型
                double _x = double.Parse(dr[2].ToString());//经度 
                double _y = double.Parse(dr[3].ToString());//纬度
                double _w = _type == 2 ? double.Parse(dr[4].ToString()) : 0;//重量
                double _v = _type == 2 ? double.Parse(dr[5].ToString()) : 0;//体积
                double _s_t = _type == 1 ? 0 : 0.5 * 60;//卸货时间，恒定为0.5h，换算成30min
                int _r_t = 480; //商家最早收货时间 换算成从0点开始的分钟，如8:00，则为480；24:00，则为1440
                int _d_t = 1440;//商家最晚收货时间
                if (_type == 2)
                {
                    DateTime _r_t1 = DateTime.Parse(dr[6].ToString());
                    int min = _r_t1.Minute;
                    int hour = _r_t1.Hour;
                    _r_t = hour * 60 + min; //商家最早收货时间 换算成从0点开始的分钟，如8:00，则为480；24:00，则为1440
                    DateTime _d_t1 = DateTime.Parse(dr[7].ToString());
                    int min_d_t = _d_t1.Minute;
                    int hour_d_t = _d_t1.Hour;
                    _d_t = hour_d_t * 60 + min_d_t;//商家最晚收货时间
                }


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



            }
            Problem.SetNodes(nodes, abbr, Tmax, numV, numD, numC, numS);
            int max_dis = int.MinValue;
            int min_dis = int.MaxValue;
            int max_tt = int.MinValue;
            int min_tt = int.MaxValue;
            using (StreamReader Dreader = new StreamReader(disfile))
            {
                string str_Dis = string.Empty;
                str_Dis = Dreader.ReadLine();
                str_Dis = Dreader.ReadLine();
                while (!string.IsNullOrWhiteSpace(str_Dis))
                {
                    string[] paras = str_Dis.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    int _i = int.Parse(paras[1]);
                    int _j = int.Parse(paras[2]);
                    int _dis_ij = int.Parse(paras[3]);
                    int _tt_ij = int.Parse(paras[4]);
                    if (_i>30 && _i<1001 || _j>30 && _j<1001)
                    {
                        str_Dis = Dreader.ReadLine();
                        continue;
                    }
                    if (_i>1000 )
                    {
                        _i -= 970;
                    }
                    if (_j>1000)
                    {
                        _j -= 970;
                    }
                    Problem.SetDistanceANDTravelIJ(_i, _j, _tt_ij, _dis_ij);
                    str_Dis = Dreader.ReadLine();
                    if (_dis_ij > max_dis)
                    {
                        max_dis = _dis_ij;
                    }
                    if (_dis_ij < min_dis)
                    {
                        min_dis = _dis_ij;
                    }
                    if (_tt_ij > max_tt)
                    {
                        max_tt = _tt_ij;
                    }
                    if (_tt_ij < min_tt)
                    {
                        min_tt = _tt_ij;
                    }
                }
                Dreader.Close();
                Console.WriteLine(max_tt.ToString() + "," + min_tt.ToString() + "," + max_dis.ToString() + "," + min_dis.ToString());
            }

            DataTable vehicleTable = ExcelTocsv(vehfile);
            foreach (DataRow dr in vehicleTable.Rows)
            {
                int _vtid = int.Parse(dr[0].ToString());
                string _name = dr[1].ToString();
                double _v = double.Parse(dr[2].ToString());
                double _w = double.Parse(dr[3].ToString());
                int _maxnum = dr[4].ToString() == "unlimited" ? numC : int.Parse(dr[4].ToString());
                double _maxrange = double.Parse(dr[5].ToString()); //按照米算
                double _chargetime = double.Parse(dr[6].ToString()); //按照小时算
                double _vcost = double.Parse(dr[7].ToString());
                double _fcost = double.Parse(dr[8].ToString());
                double _chargerate = 100.0000 / 60;
                types.Add(new VehicleType
                {
                    VehTypeID = _vtid,
                    Name = _name,
                    Volume = _v,
                    Weight = _w,
                    MaxRange = _maxrange,
                    MaxNum = _maxnum,
                    ChargeTime = _chargetime, //0.5h
                    ChargeCostRate = _chargerate, // 100RMB/h
                    VariableCost = _vcost / 1000,
                    FixedCost = _fcost,
                });


            }


            Problem.SetVehicleTypes(types);

        }

        public DataTable ExcelTocsv(string excelPath)
		{
			FileStream stream = File.Open(excelPath, FileMode.Open, FileAccess.Read);

			////1. Reading from a binary Excel file ('97-2003 format; *.xls)
			//IExcelDataReader excelReader = ExcelReaderFactory.CreateBinaryReader(stream);

			//2. Reading from a OpenXml Excel file (2007 format; *.xlsx)
			IExcelDataReader excelReader = ExcelReaderFactory.CreateOpenXmlReader(stream);

			//3. DataSet - The result of each spreadsheet will be created in the result.Tables
			//DataSet result = excelReader.AsDataSet();
			int count = excelReader.ResultsCount;

			//excelReader.IsFirstRowAsColumnNames = true;

			DataSet result = excelReader.AsDataSet(new ExcelDataSetConfiguration()
			{
				ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
				{
					UseHeaderRow = true
    }
			});
			DataTable dt = result.Tables[0];
			return dt;


}
	}
}

