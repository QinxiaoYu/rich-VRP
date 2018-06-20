using System;
using System.Collections;
using System.Collections.Generic;
using OP.Data;

public static class Utility
{
    internal static List<Customer> FindCusByAngle(int cus_threshold, List<Customer> unVisitedCus)
    {
        List<Customer> cluster_cus = new List<Customer>();
        Hashtable ht = new Hashtable();
        for (int i = 0; i < unVisitedCus.Count; i++)
        {
            var cus_i = unVisitedCus[i];
            double angle = cus_i.GetAngel(Problem.StartDepot);
            ht.Add(cus_i.Info.Id, angle);
        }
        double[] valueArray = new double[ht.Count];
        int[] keyArray = new int[ht.Count];
        ht.Keys.CopyTo(keyArray, 0);
        ht.Values.CopyTo(valueArray, 0);
        Array.Sort(valueArray, keyArray);//升序排列
        int left_number = Math.Min(ht.Count, cus_threshold);
        for (int j = 0; j < left_number; j++)
        {
            cluster_cus.Add(Problem.SearchCusbyId(keyArray[j]));
        }
        return cluster_cus;
    }

    internal static List<Customer> FindCusByRadians(int cus_threshold, List<Customer> unVisitedCus)
    {
        List<Customer> cluster_cus = new List<Customer>();
        Hashtable ht = new Hashtable();
        for (int i = 0; i < unVisitedCus.Count; i++)
        {
            var cus_i = unVisitedCus[i];
            double radians = cus_i.TravelDistance(Problem.StartDepot);
            ht.Add(cus_i.Info.Id, radians);
        }
        double[] valueArray = new double[ht.Count];
        int[] keyArray = new int[ht.Count];
        ht.Keys.CopyTo(keyArray, 0);
        ht.Values.CopyTo(valueArray, 0);
        Array.Sort(valueArray, keyArray);//升序排列
        int left_number = Math.Min(ht.Count, cus_threshold);
        for (int j = 0; j < left_number; j++)
        {
            cluster_cus.Add(Problem.SearchCusbyId(keyArray[j]));
        }
        return cluster_cus;
    }

    internal static List<Customer> FindCusByAngleAndRadians(int cus_threshold, List<Customer> unVisitedCus)
    {
        List<Customer> cluster_cus = new List<Customer>();
        Hashtable ht = new Hashtable();
        double radians_threshold = 33000;
        do
        {
            for (int i = 0; i < unVisitedCus.Count; i++)
            {
                var cus_i = unVisitedCus[i];
                double radians = cus_i.TravelDistance(Problem.StartDepot);
                double angle = cus_i.GetAngel(Problem.StartDepot);
                if (radians < radians_threshold)
                {
                    ht.Add(cus_i.Info.Id, angle);
                }
                if (ht.Count == 0)
                {
                    radians_threshold += 33000;
                }


            }
        } while (ht.Count==0);

        double[] valueArray = new double[ht.Count];
        int[] keyArray = new int[ht.Count];
        ht.Keys.CopyTo(keyArray, 0);
        ht.Values.CopyTo(valueArray, 0);
        Array.Sort(valueArray, keyArray);//按照角度升序排列
        int left_number = Math.Min(ht.Count, cus_threshold);
        for (int j = 0; j < left_number; j++)
        {
            cluster_cus.Add(Problem.SearchCusbyId(keyArray[j]));
        }
        return cluster_cus;
    }
}