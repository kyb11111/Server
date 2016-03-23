using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.ServiceModel;
using SuperControl.ServiceModel;
using System.Data.Odbc;
using System.Data.Sql;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data;
using System.Xml;
using System.Threading;
using System.IO;
namespace TraceServer
{
    class Program
    {
        static bool Running = true;
        static void Main(string[] args)
        {
            DateTime time = DateTime.Now;
            Console.WriteLine("加载数据");
            ModelAccessManager.Load("SuperControl.TraceServerModel");
            Console.Write("加载数据完成，总用时:");
            Console.WriteLine(DateTime.Now.Subtract(time));
            Console.WriteLine("启动服务");
            ServiceHost host = new ServiceHost(typeof(TraceServer.TraceService));
            Thread thread = new Thread(DataProc);
            thread.Start();
            host.Open();
            Console.WriteLine("TraceSystemService服务已经运行（回车将关闭服务）...");
            Console.ReadLine();
            Running = false;
            thread.Join();
            Console.WriteLine("关闭服务");
        }

        static void DataProc()
        {
            while(Running)
            {
                DateTime time = DateTime.Now;
                Console.WriteLine("加载数据");
                ModelAccessManager.Load("SuperControl.TraceServerModel");
                Console.Write("加载数据完成，总用时:");
                Console.WriteLine(DateTime.Now.Subtract(time));
                Thread.Sleep(1000);

            }
        }
    }
}
