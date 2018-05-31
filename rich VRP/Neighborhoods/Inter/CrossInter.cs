using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rich_VRP.Neighborhoods.Inter
{
	//routing 内部交换
	internal class OrExchange: IntraExchange
    {
		public override Route RouteExchange(Route route)
		{
			Route bestroute = route.Copy();
			Route temproute = route.Copy();
			int CustomerNumber = route.RouteList.Count - 2;
			bool IsOver = false;
			while (!IsOver)
			{
				temproute = OrOptN(bestroute, 3);
				if (temproute != null)
				{
					bestroute = temproute.Copy();
					continue;
				}
				else
				{
					temproute = OrOptN(bestroute, 2);
					if (temproute != null)
					{
						bestroute = temproute.Copy();
						continue;
					}
					else
					{
						temproute = OrOptN(bestroute, 1);
						if (temproute != null)
						{
							bestroute = temproute.Copy();
							continue;
						}
						else
						{
							IsOver = true;
						}
					}

				}

			}
			return bestrout
			}

		private Route OrOptN(Route route, int n)
		{
			Route temproute = route.Copy();
			int NumberCustomer = temproute.RouteList.Count - 2;
			List<Customer> InsertCustomers = new List<Customer>();
			if (NumberCustomer >= 2 * n + 1)
			{
				for (int i = 1; i < NumberCustomer + 1; i++)
				{
					for (int k = 0; k < n; k++)
					{
						int removeposition = i;
						int mod_r = (int)(removeposition % (temproute.RouteList.Count - 2));
						if (mod_r != i && mod_r != 0)
							removeposition = 1;
						InsertCustomers.Add((Customer)temproute.RouteList[removeposition]);
						temproute.RemoveAt(removeposition);
					}
					InsertCustomers.Reverse();
					for (int j = i; j <= i + NumberCustomer - 2 * n - 1; j++)
					{
						List<Customer> tempInsertCustomers = new List<Customer>(InsertCustomers.ToArray());
						Route temproutej = temproute.Copy();
						for (int kk = 0; kk < n; kk++)
						{
							int insertposition = j + n + 1;
							int mod_i = (int)(insertposition % (temproutej.RouteList.Count - 1));
							if (mod_i == 0)
							{
								temproutej.InsertCustomer(tempInsertCustomers[0], temproutej.RouteList.Count - 1);
								tempInsertCustomers.RemoveAt(0);
							}
							else
							{
								temproutej.InsertCustomer(tempInsertCustomers[0], mod_i);
								tempInsertCustomers.RemoveAt(0);
							}


						}
						if (temproutej.IsFeasible() && temproutej.Length() < route.Length())
						{
							return temproutej;
						}

					}
					temproute = route.Copy();
				}
			}
			return nul   
			}
				
	}
}
