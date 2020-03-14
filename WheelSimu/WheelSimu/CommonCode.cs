using System.Text;
using System;

namespace Core
{
    public static class CommonCode
    {


        //public static int GetPort(string IP, int i, int TryTimes)
        //{
        //    string[] TmpStr;
        //    int getPort;

        //    getPort = 0;
        //    TmpStr = IP.Split(".");

        //    for (int j = 0; j < TmpStr.Length; j++)
        //    {
        //        int.TryParse(TmpStr[j], out int tmpData);
        //        getPort += tmpData * (j + i) * (TryTimes + 7) + TryTimes;
        //    }


        //    while (getPort > 10000)
        //    {
        //        getPort -= TryTimes * 777;
        //    }
        //    return getPort;
        //}

        public static int GetPort(int TryTimes)
        {
            int getPort;

            getPort = 5050; //+ TryTimes;
          

            while (getPort > 10000)
            {
                getPort -= TryTimes * 777;
            }
            return getPort;
        }
    }
}