//using System;
//using OP.Data;
//using System.Collections.Generic;
//using System.IO;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Linq;
//using System.Drawing;
//using System.Drawing.Design;
//namespace rich_VRP
//{
//	public class Perspective
//	{

//		Fleet fleet;
//		public List<result> results { get; set; }

//		public void setResults(string path)
//		{

//			results = new List<result>();
//			using (CsvFileReader reader = new CsvFileReader(path))
//			{
//				CsvRow row = new CsvRow();
//				//标示是否是读取的第一行
//				bool IsFirst = true;
//				while (reader.ReadRow(row))
//				{
//					if (IsFirst == true)
//					{
//						IsFirst = false;
//						continue;
//					}
//					result r = new result(row);
//					r.restorRoutList(row[2]);
//					results.Add(r);

//				}
//			}


//		}

//		public void resoterFleet()
//		{
//			OpProblemReader reader = new OpProblemReader();
//			string dir = Directory.GetCurrentDirectory();
//			Problem problem = reader.Read(dir);
//			Problem.MinWaitTimeAtDepot = 60;
//			Problem.setAllNodesDict();
//			fleet = new Fleet();
//			fleet.VehFleet = new List<Vehicle>();
//			foreach (var result in results)
//			{
//				Vehicle v = new Vehicle(result.TypeId, result.VehId);
//				v.VehRouteList = new List<Route>();
//				foreach (var r in result.dist_sep)
//				{
//					Route route = new Route();
//					route.RouteList = new List<AbsNode>();
//					foreach (var nodeID in r)
//					{
//						AbsNode node = Problem.AllNodesDict[nodeID];

//						route.RouteList.Add(node);
//					}
//					v.VehRouteList.Add(route);

//				}
//				v.charge_cnt = result.charge_cnt;
//				v.charge_cost = result.charge_cost;
//				v.distance = result.distance;
//				v.distribute_arr_tm = result.distribute_arr_tm;
//				v.distribute_lea_tm = result.distribute_lea_tm;
//				v.fixed_use_cost = result.fixed_use_cost;
//				v.total_cost = result.total_cost;
//				v.tran_cost = result.tran_cost;
//				v.wait_cost = result.wait_cost;
//				fleet.VehFleet.Add(v);

//			}

//		}

//		public void Draw()
//		{
			
//			string path = "/Users/chenpeng/Documents/Project/GitHub/QinxiaoYu/rich-VRP.git/rich VRP/bin/Debug/reslut6310423451.bmp";
//			double scale = 15;
//			double x_max = 0;
//			double y_max = 0;
//			double x_min = int.MaxValue;
//			double y_min = int.MaxValue;

//			foreach (var item in Problem.AllNodes)
//			{
//				if (item.X > x_max)
//				{
//					x_max = (int)item.X;
//				}
//				if (item.Y > y_max)
//				{
//					y_max = (int)item.Y;
//				}
//				if (item.Y < y_min)
//				{
//					y_min = (int)item.Y;
//				}
//				if (item.X < x_min)
//				{
//					x_min = (int)item.X;
//				}
//			}

//			double width = (x_max - x_min) * scale + 100 + 100;
//			Double highth = (y_max - y_min) * scale + 100 + 100;

//			//Bitmap pic = new Bitmap(width, highth);
//			Bitmap pic = new Bitmap();

//			var g = Graphics.FromImage(pic);

//			g.Clear(Color.White);

//			var rColors = new List<Brush> { Brushes.Red, Brushes.Blue, Brushes.Green };
//			var vColors = new List<Brush> { Brushes.DarkRed, Brushes.DarkBlue, Brushes.DarkGreen, Brushes.DarkOrange, Brushes.DarkViolet };



//			foreach (var node in Problem.AllNodes)
//			{
//				if (node.Id == 0)
//				{
//					Point pt = new Point((int)(node.X - x_min) * scale + 100, (int)(node.Y - y_min) * scale + 100);
//					g.FillRectangle(Brushes.Red, pt.X, pt.Y, 10, 10);
//				}
//				else
//				{
//					Point pt = new Point((int)(node.X - x_min) * scale + 100, (int)(node.Y - y_min) * scale + 100);
//					//g.FillEllipse(new SolidBrush(Color.FromArgb(Math.Min(255, (int)Math.Round(1000 * 5 * node.PenaltyRate)), 0, 0, 255)), pt.X, pt.Y, (float)node.Score, (float)node.Score);
//					g.FillEllipse(Brushes.Black, pt.X, pt.Y, 5,5);
//				}


//			}

//			//	int numV = solution.Routes.Count - 1;

//			//	for (int i = 0; i < numV; i++)
//			//	{
//			//		Route route = solution.Routes[i];
//			//		var rPen = new Pen(vColors[i], 2);
//			//		System.Drawing.Drawing2D.AdjustableArrowCap lineCap = new System.Drawing.Drawing2D.AdjustableArrowCap(4, 4, false);
//			//		rPen.CustomEndCap = lineCap;
//			//		for (var j = 0; j < route.RouteList.Count - 1; ++j)
//			//		{
//			//			var start = route.RouteList[j].Info;
//			//			var end = route.RouteList[j + 1].Info;

//			//			var p0 = new Point((int)Math.Round((start.X - x_min) * scale + 100 + start.Score / 2),
//			//							(int)Math.Round((start.Y - y_min) * scale + 100 + start.Score / 2));
//			//			var p1 = new Point((int)Math.Round((end.X - x_min) * scale + 100 + start.Score / 2),
//			//							(int)Math.Round((end.Y - y_min) * scale + 100 + end.Score / 2));
//			//			g.DrawLine(rPen, p0, p1);

//			//		}
//			//	}
//				pic.Save(path);
//			//}


//		}
//		public class result
//		{
//			public int VehId { get; set; }
//			public int TypeId { get; set; }
//			public List<List<int>> dist_sep { get; set; }
//			public string distribute_lea_tm { get; set; }
//			public string distribute_arr_tm { get; set; }
//			public double distance { get; set; }
//			public double tran_cost { get; set; }
//			public double charge_cost { get; set; }
//			public double wait_cost { get; set; }
//			public double fixed_use_cost { get; set; }
//			public double total_cost { get; set; }
//			public int charge_cnt { get; set; }

//			public result(CsvRow row)
//			{
//				VehId = int.Parse(row[0]);
//				TypeId = int.Parse(row[1]);
//				distribute_lea_tm = row[3];
//				distribute_arr_tm = row[4];
//				distance = Double.Parse(row[5]);
//				tran_cost = Double.Parse(row[6]);
//				charge_cost = Double.Parse(row[7]);
//				wait_cost = Double.Parse(row[8]);
//				fixed_use_cost = Double.Parse(row[9]);
//				total_cost = Double.Parse(row[10]);
//				charge_cnt = int.Parse(row[11]);

//			}
//			public void restorRoutList(string seq)
//			{
//				dist_sep = new List<List<int>>();
//				string seqT = seq.Trim(';', '0');
//				foreach (var item in Regex.Split(seqT, ";0;", RegexOptions.IgnoreCase))
//				{
//					string seqR = string.Concat("0;", item, ";0");
//					string[] seqA = seqR.Split(';');
//					List<String> listS = new List<String>(seqA);
//					var newlist = listS.Select<string, int>(q => Convert.ToInt32(q));
//					dist_sep.Add(new List<int>(newlist));
//				}

//			}

//		}
//	}
//}




