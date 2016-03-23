using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data.Common;
using System.Data;
using System.Collections.ObjectModel;

namespace SuperControl.ServiceModel
{
    public class HistoricalDataEventArgs : EventArgs
    {
        public readonly HistoricalDataCommand Command;
        public readonly string SessionMsg;
        public readonly bool MoreData;
        public readonly HistoricalData[] Data;

        internal HistoricalDataEventArgs(HistoricalDataCommand command, HistoricalData[] data, string sessionMsg, bool moreData)
        {
            Command = command;
            SessionMsg = sessionMsg;
            MoreData = moreData;
            Data = data;
        }
    }

    public class HistoricalDataManager
    {
        static private HistoricalDataManager s_Instance;
        static public HistoricalDataManager Instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new HistoricalDataManager();
                return s_Instance;
            }
        }

        private ModelFactory m_factory = null;
        private Dictionary<string, uint> m_pidDic = new Dictionary<string, uint>();
        private HistoricalDataManager()
        {
            string configName = ConfigurationManager.AppSettings["HistDbServer"];
            m_factory = ModelFactory.CreateFactory(configName);
            if (m_factory != null)
                m_factory.Initialize(configName);
            //建立Pid到Rid的对照表
            if (m_factory.DbConnection.State != System.Data.ConnectionState.Open)
            {
                m_factory.DbConnection.Open();
            }
            string sql = string.Format("select Rid,Pid from Point");
            m_factory.DbCommand.CommandText = sql;
            DbDataReader reader = m_factory.DbCommand.ExecuteReader();
            while (reader.Read())
            {
                int rid = Convert.ToInt32(reader["Rid"]);
                string pid = reader["Pid"].ToString();
                m_pidDic.Add(pid, (uint)rid);
            }
            reader.Close();

            m_factory.DbConnection.Close();//先关闭连接,等待查询时再打开连接;
        }

        public void LoadHistoricalData(HistoricalDataCommand command, string sessionMsg)
        {
            if (m_factory == null)
                return;
            lock (this)
            {
                try
                {
                    //查询开始打开连接
                    m_factory.DbConnection.Open();
                    Console.WriteLine("Open hist database--------------------");
                    string field = "Eng";
                    if (command.DataType == SampleDataType.Ana)
                        field = "Eng";
                    else if (command.DataType == SampleDataType.Dig)
                        field = "Cs";

                    string sql = string.Format("select history(member ={0},function ={1} ,stime = {2},etime ={3},interval = {4} msec,output = (date,value,all)) from Point where Rid={5}",
                        field,
                        command.Func.ToString().ToLower(),
                        command.StartTime.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                        command.EndTime.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                        command.Interval,
                        command.Rid);
                    Console.WriteLine(sql);
                    m_factory.DbCommand.CommandText = sql;
                    DbDataReader reader = m_factory.DbCommand.ExecuteReader();

                    int count = SystemConfig.MaxArrayLength;
                    List<HistoricalData> list = new List<HistoricalData>();
                    while (reader.Read())
                    {
                        if (command.DataType == SampleDataType.Ana)
                        {
                            AnaHistoricalData data = new AnaHistoricalData();
                            data.Time = DateTime.Parse(reader["date"].ToString());
                            ushort s = (ushort)short.Parse(reader["status"].ToString());
                            data.State = (SCHistStatus)s;
                            data.Value = (double)Convert.ChangeType(reader["value"], typeof(double));
                            list.Add(data);
                        }
                        else if (command.DataType == SampleDataType.Dig)
                        {
                            DigHistoricalData data = new DigHistoricalData();
                            data.Time = DateTime.Parse(reader["date"].ToString());
                            ushort s = (ushort)short.Parse(reader["status"].ToString());
                            data.State = (SCHistStatus)s;
                            data.Value = (ushort)Convert.ChangeType(reader["value"], typeof(ushort));
                            list.Add(data);
                        }
                        else
                            break;

                        if (count > 0)
                            count--;
                        else
                        {
                            //达到最大数组数量时发送事件
                            count = SystemConfig.MaxArrayLength;
                            if (HistoricalDataLoaded != null)
                            {
                                HistoricalDataLoaded(this, new HistoricalDataEventArgs(
                                    command, list.ToArray(), sessionMsg, true));
                                Console.WriteLine(string.Format("send data, count = {0}", list.Count));
                            }
                            list.Clear();
                        }
                    }
                    reader.Close();
                    //剩余数据发送事件
                    if (HistoricalDataLoaded != null)
                    {
                        HistoricalDataLoaded(this, new HistoricalDataEventArgs(
                            command, list.ToArray(), sessionMsg, false));
                        Console.WriteLine(string.Format("send data, count = {0}", list.Count));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    //查询完了关闭连接
                    m_factory.DbConnection.Close();
                    Console.WriteLine("Close hist database--------------------");
                }
                finally
                {
                    //查询完了关闭连接
                    m_factory.DbConnection.Close();
                    Console.WriteLine("Close hist database--------------------");
                }
            }
        }

        public HistoricalData[] GetHistoricalData(HistoricalDataCommand command, string sessionMsg)
        {
            return GetHistoricalData(command, sessionMsg, string.Empty);
        }

        public HistoricalData[] GetHistoricalData(HistoricalDataCommand command, string sessionMsg, string pid)
        {
            if (m_factory == null)
                return new HistoricalData[0];
            List<HistoricalData> list = new List<HistoricalData>();
            lock (this)
            {
                try
                {
                    //查询开始打开连接
                    m_factory.DbConnection.Open();

                    uint rid = 0;
                    string sql;
                    DbDataReader reader;
                    if (pid.Trim() != string.Empty)
                    {
                        if (!m_pidDic.TryGetValue(pid.Trim(), out rid))
                            rid = 0;
                    }
                    else
                    {
                        rid = command.Rid;
                    }
                    if (rid > 0)
                    {
                        sql = string.Format("select history(member =Eng,function ={0} ,stime = {1},etime ={2},interval = {3} msec,output = (date,value,all)) from Ana where Rid={4}",
                            command.Func.ToString().ToLower(),
                            command.StartTime.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                            command.EndTime.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                            command.Interval,
                            rid);
                        Console.WriteLine(sql);
                        m_factory.DbCommand.CommandText = sql;
                        reader = m_factory.DbCommand.ExecuteReader();

                        while (reader.Read())
                        {
                            if (command.DataType == SampleDataType.Ana)
                            {
                                AnaHistoricalData data = new AnaHistoricalData();
                                data.Time = DateTime.Parse(reader["date"].ToString());
                                ushort s = (ushort)short.Parse(reader["status"].ToString());
                                data.State = (SCHistStatus)s;
                                data.Value = (double)Convert.ChangeType(reader["value"], typeof(double));
                                list.Add(data);
                            }
                            else if (command.DataType == SampleDataType.Dig)
                            {
                                DigHistoricalData data = new DigHistoricalData();
                                data.Time = DateTime.Parse(reader["date"].ToString());
                                ushort s = (ushort)short.Parse(reader["status"].ToString());
                                data.State = (SCHistStatus)s;
                                //data.Value = (ushort)Convert.ChangeType(reader["value"], typeof(ushort));
                                data.Value = (ushort)double.Parse(reader["value"].ToString());
                                list.Add(data);
                            }
                            else
                                break;

                        }
                        reader.Close();
                    }
                    else
                    {
                        Console.WriteLine("无效的Pid或Rid:Pid={0},Rid={1}", pid, rid);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    //查询完了关闭连接
                    m_factory.DbConnection.Close();
                }
                finally
                {
                    //查询完了关闭连接
                    m_factory.DbConnection.Close();
                }
            }
            return list.ToArray();
        }

        public Dictionary<string, ObservableCollection<HistoricalData>> GetHistoricalData(HistoricalDataCommand command, string sessionMsg, params string[] pids)
        {
            if (m_factory == null || pids.Length <= 0)
                return new Dictionary<string, ObservableCollection<HistoricalData>>();
            Dictionary<string, ObservableCollection<HistoricalData>> dic = new Dictionary<string, ObservableCollection<HistoricalData>>();
            lock (this)
            {
                try
                {
                    HashSet<string> set = new HashSet<string>();
                    foreach (string pid in pids)
                    {
                        set.Add(pid.Trim());
                    }
                    //查询开始打开连接
                    m_factory.DbConnection.Open();

                    uint rid = 0;
                    string sql;
                    DbDataReader reader;//snapshot
                    sql = string.Format("select Pid,history(member =Eng,function ={0} ,stime = {1},etime ={2},interval = {3} msec,output = (date,value,all)) from Point where ",
                        command.Func.ToString().ToLower(),
                        command.StartTime.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                        command.EndTime.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                        command.Interval);
                    bool isFirst = true;
                    foreach (string pid in set)
                    {
                        if (string.IsNullOrWhiteSpace(pid))
                            continue;
                        if (m_pidDic.TryGetValue(pid.Trim(), out rid))
                        {
                            if (isFirst)
                            {
                                sql += string.Format("Rid = {0} ", rid);
                                isFirst = false;
                            }
                            else
                            {
                                sql += string.Format("or Rid = {0} ", rid);
                            }
                        }
                    }
                    sql.TrimEnd(' ');
                    Console.WriteLine(sql);

                    m_factory.DbCommand.CommandText = sql;
                    reader = m_factory.DbCommand.ExecuteReader();

                    while (reader.Read())
                    {
                        string pid = reader["Pid"].ToString();
                        //创建存储该Pid点的集合
                        ObservableCollection<HistoricalData> list;
                        if (!dic.TryGetValue(pid, out list))
                        {
                            list = new ObservableCollection<HistoricalData>();
                            dic.Add(pid, list);
                        }
                        if (command.DataType == SampleDataType.Ana)
                        {
                            AnaHistoricalData data = new AnaHistoricalData();
                            data.Time = DateTime.Parse(reader["date"].ToString());
                            ushort s = (ushort)short.Parse(reader["status"].ToString());
                            data.State = (SCHistStatus)s;
                            data.Value = (double)Convert.ChangeType(reader["value"], typeof(double));
                            list.Add(data);
                        }
                        else if (command.DataType == SampleDataType.Dig)
                        {
                            DigHistoricalData data = new DigHistoricalData();
                            data.Time = DateTime.Parse(reader["date"].ToString());
                            ushort s = (ushort)short.Parse(reader["status"].ToString());
                            data.State = (SCHistStatus)s;
                            data.Value = (ushort)Convert.ChangeType(reader["value"], typeof(ushort));
                            list.Add(data);
                        }
                        else
                            break;

                    }
                    reader.Close();

                    //DataTable table = new DataTable("HistData");
                    //table.Load(reader);

                    //foreach (DataRow row in table.Rows)
                    //{
                    //    string pid = row["Pid"].ToString();
                    //    //创建存储该Pid点的集合
                    //    ObservableCollection<HistoricalData> list;
                    //    if (!dic.TryGetValue(pid, out list))
                    //    {
                    //        list = new ObservableCollection<HistoricalData>();
                    //        dic.Add(pid, list);
                    //    }

                    //    if (command.DataType == SampleDataType.Ana)
                    //    {
                    //        AnaHistoricalData data = new AnaHistoricalData();
                    //        data.Time = DateTime.Parse(reader["date"].ToString());
                    //        ushort s = (ushort)short.Parse(reader["status"].ToString());
                    //        data.State = (SCHistStatus)s;
                    //        data.Value = (double)Convert.ChangeType(reader["value"], typeof(double));
                    //        list.Add(data);
                    //    }
                    //    else if (command.DataType == SampleDataType.Dig)
                    //    {
                    //        DigHistoricalData data = new DigHistoricalData();
                    //        data.Time = DateTime.Parse(reader["date"].ToString());
                    //        ushort s = (ushort)short.Parse(reader["status"].ToString());
                    //        data.State = (SCHistStatus)s;
                    //        data.Value = (ushort)Convert.ChangeType(reader["value"], typeof(ushort));
                    //        list.Add(data);
                    //    }
                    //    else
                    //        break;
                    //}
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    //查询完了关闭连接
                    m_factory.DbConnection.Close();
                }
                finally
                {
                    //查询完了关闭连接
                    m_factory.DbConnection.Close();
                }
            }
            return dic;
        }

        public Dictionary<int, ObservableCollection<HistoricalData>> GetHistoricalData(HistoricalDataCommand command, string sessionMsg, params int[] rids)
        {
            if (m_factory == null || rids.Length <= 0)
                return new Dictionary<int, ObservableCollection<HistoricalData>>();
            Dictionary<int, ObservableCollection<HistoricalData>> dic = new Dictionary<int, ObservableCollection<HistoricalData>>();
            lock (this)
            {
                try
                {
                    HashSet<int> set = new HashSet<int>();
                    foreach (int rid in rids)
                    {
                        set.Add(rid);
                    }
                    //查询开始打开连接
                    m_factory.DbConnection.Open();

                    string sql;
                    DbDataReader reader;//snapshot
                    sql = string.Format("select Rid,history(member =Eng,function ={0} ,stime = {1},etime ={2},interval = {3} msec,output = (date,value,all)) from Point where ",
                        command.Func.ToString().ToLower(),
                        command.StartTime.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                        command.EndTime.ToString("yyyy/MM/dd HH:mm:ss.fff"),
                        command.Interval);
                    bool isFirst = true;
                    foreach (int rid in set)
                    {
                        if (isFirst)
                        {
                            sql += string.Format("Rid = {0} ", rid);
                            isFirst = false;
                        }
                        else
                        {
                            sql += string.Format("or Rid = {0} ", rid);
                        }
                    }
                    sql.TrimEnd(' ');
                    Console.WriteLine(sql);

                    m_factory.DbCommand.CommandText = sql;
                    reader = m_factory.DbCommand.ExecuteReader();

                    while (reader.Read())
                    {
                        int rid;
                        if (!int.TryParse(reader["Rid"].ToString(), out rid))
                        {
                            continue;
                        }
                        //创建存储该Pid点的集合
                        ObservableCollection<HistoricalData> list;
                        if (!dic.TryGetValue(rid, out list))
                        {
                            list = new ObservableCollection<HistoricalData>();
                            dic.Add(rid, list);
                        }
                        if (command.DataType == SampleDataType.Ana)
                        {
                            AnaHistoricalData data = new AnaHistoricalData();
                            data.Time = DateTime.Parse(reader["date"].ToString());
                            ushort s = (ushort)short.Parse(reader["status"].ToString());
                            data.State = (SCHistStatus)s;
                            data.Value = (double)Convert.ChangeType(reader["value"], typeof(double));
                            list.Add(data);
                        }
                        else if (command.DataType == SampleDataType.Dig)
                        {
                            DigHistoricalData data = new DigHistoricalData();
                            data.Time = DateTime.Parse(reader["date"].ToString());
                            ushort s = (ushort)short.Parse(reader["status"].ToString());
                            data.State = (SCHistStatus)s;
                            data.Value = (ushort)Convert.ChangeType(reader["value"], typeof(ushort));
                            list.Add(data);
                        }
                        else
                            break;

                    }
                    reader.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    //查询完了关闭连接
                    m_factory.DbConnection.Close();
                }
                finally
                {
                    //查询完了关闭连接
                    m_factory.DbConnection.Close();
                }
            }
            return dic;
        }

        //hushan add
        private uint GetPointRid(string Pid)
        {
            if (m_factory == null)
                return 0;
            lock (this)
            {
                string sql = string.Format("select * from Point where Pid='{0}'",Pid);                   
                m_factory.DbCommand.CommandText = sql;
                DbDataReader reader = m_factory.DbCommand.ExecuteReader();             
                uint rid=0;
                while (reader.Read())
                {
                    rid = Convert.ToUInt32(reader["Rid"]);
                }
                reader.Close();
                return rid;
            }
        }
        public event EventHandler<HistoricalDataEventArgs> HistoricalDataLoaded;
    }
}
