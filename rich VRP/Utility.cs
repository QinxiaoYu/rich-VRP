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
        Hashtable ht_angle = new Hashtable();
        Hashtable ht_radians = new Hashtable();
        //double radians_threshold = 33000;
     
        for (int i = 0; i < unVisitedCus.Count; i++)
        {
            var cus_i = unVisitedCus[i];          
            double angle = cus_i.GetAngel(Problem.StartDepot);
            ht_angle.Add(cus_i.Info.Id, angle);
        }
  

        double[] valueArray_angle = new double[ht_angle.Count];
        int[] keyArray_angle = new int[ht_angle.Count];     
        ht_angle.Keys.CopyTo(keyArray_angle, 0);
        ht_angle.Values.CopyTo(valueArray_angle, 0);    
        Array.Sort(valueArray_angle, keyArray_angle);//按照角度升序排列
        double start_angle = valueArray_angle[0];
        double end_angle = Math.Min(Math.Floor(start_angle / 10) * 10 + 20, 360);
        

        if (start_angle<150)
        {
            end_angle = 150;
        }
        if (start_angle>=150 && start_angle <170)
        {
            end_angle = 170;
        }
        if (start_angle >= 170 && start_angle < 190)
        {
            end_angle = 190;
        }
        if (start_angle >= 190 && start_angle < 210)
        {
            end_angle = 210;
        }
        if (start_angle >= 210 && start_angle < 230)
        {
            end_angle = 230;
        }
        if (start_angle >= 230 && start_angle < 250)
        {
            end_angle = 250;
        }
        if (start_angle >= 250 && start_angle < 270)
        {
            end_angle =270;
        }
        if (start_angle >= 270 && start_angle < 290)
        {
            end_angle =290;
        }
        if (start_angle >= 290 && start_angle < 310)
        {
            end_angle = 310;
        }
        if (start_angle>=310)
        {
            end_angle = 360;
        }

        for (int j = 0; j < keyArray_angle.Length; j++)
        {
            int id_closet_angle = keyArray_angle[j];
            double cur_angel = (double)ht_angle[id_closet_angle];
            if (cur_angel>=end_angle)
            {
                break;
            }
            double radians = Problem.GetDistanceIJ(id_closet_angle, Problem.StartDepot.Info.Id);
            ht_radians.Add(id_closet_angle, radians);
           
        }
        double[] valueArray_radians = new double[ht_radians.Count];
        int[] keyArray_radians = new int[ht_radians.Count];
        ht_radians.Keys.CopyTo(keyArray_radians, 0);
        ht_radians.Values.CopyTo(valueArray_radians, 0);
        Array.Sort(valueArray_radians, keyArray_radians);//按照半径升序排列
        double start_radian = valueArray_radians[0];   
        double end_radian = Math.Min( Math.Floor(start_radian/ 10000)*10000 + 10000,80000);
        for (int j = 0; j < keyArray_radians.Length; j++)
        {
            int id_closet_radian = keyArray_radians[j];
            double cur_radian = (double)ht_radians[id_closet_radian];
            if (cur_radian >= end_radian)
            {
                break;
            }
            cluster_cus.Add(Problem.SearchCusbyId(id_closet_radian));

        }

        return cluster_cus;
    }

    internal static List<Customer> FindCusByTime(double time_threshold, List<Customer> unVisitedCus)
    {
        List<Customer> cluster_cus = new List<Customer>();
        foreach (Customer cus in unVisitedCus)
        {
            if (cus.Info.DueDate<=time_threshold)
            {
                cluster_cus.Add(cus);
            }
        }
        if (cluster_cus.Count == 0)
        {
            cluster_cus.AddRange(unVisitedCus);
        }
        return cluster_cus;
    }



}