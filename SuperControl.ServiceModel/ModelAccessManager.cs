using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace SuperControl.ServiceModel
{
    public interface IExcuteNotify
    {
        void ErrorNotify(string message,ExcuteType type);
        void ExcuteNotify(ExcuteAction [] actions);
    }

    public delegate void ErrorNotify(string message);

    public class ModelAccessManager
    {
        #region 静态变量和方法
        private static List<Type> s_typeList = new List<Type>();
        public static void Load()
        {

            foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach(Type t in asm.GetTypes())
                {
                    BuildLoadSequence(t);
                }
            }
            foreach (Type t in s_typeList)
            {
                InternalLoad(t);
            }
        }

        public static void Load(params string[] namespaces)
        {
            foreach (string ns in namespaces)
            {
                foreach (Type t in Assembly.Load(ns).GetTypes())
                {
                    BuildLoadSequence(t);
                }
            }
            foreach (Type t in s_typeList)
            {
                InternalLoad(t);
            }
        }

        private static void BuildLoadSequence(Type type)
        {
            if (!VerifyType(type))
                return;
            if (s_typeList.Contains(type))
            {
                s_typeList.Remove(type);
            }
            s_typeList.Add(type);
            BuildLoadSequence(type.BaseType);
        }

        public static void Load(Type type)
        {
            if (!VerifyType(type))
                return;
            InternalLoad(type);
        }

        private static bool VerifyType(Type type)
        {
            if (!type.IsSubclassOf(typeof(ModelBase)))
                return false;
            DbTableAttribute attribute = Attribute.GetCustomAttribute(type, typeof(DbTableAttribute)) as DbTableAttribute;
            if (attribute == null)
                return false;
            if (attribute.CacheMode == CacheMode.CacheInClient || attribute.CacheMode == CacheMode.NoCache)
                return false;
            return true;
        }

        private static void InternalLoad(Type type)
        {
            DateTime time = DateTime.Now;
            Console.Write("加载{0}到存根:", type.Name);
            int count = 0;
            ModelFactoryCollection.BuildFactories(type);
            ModelEnumerator reader = new ModelEnumerator(type);
            ModelCacheManager.Instance.CreateModelCollection(type);
            foreach (ModelBase model in reader)
            {
                ModelCacheManager.Instance.Save(model);
                count++;
            }
            Console.Write("{0}个记录 用时", count);
            Console.WriteLine(DateTime.Now.Subtract(time));
        }

        public static string[] GetClientChcheModelTypeName()
        {
            List<string> list = new List<string>();
            foreach (Type type in s_typeList)
            {
                DbTableAttribute attribute = Attribute.GetCustomAttribute(type, typeof(DbTableAttribute)) as DbTableAttribute;;
                if (attribute.CacheMode == CacheMode.CacheInClient || attribute.CacheMode == CacheMode.Both)
                    list.Add(type.Name);
            }
            return list.ToArray();
        }

        private static readonly object m_lock = new object();
        public static void InsertModel(ModelBase item, out Exception e,out int id)
        {
            lock (m_lock)
            {
                id = -1;
                e = null;
                Boolean isCache = true;
                DbTableAttribute attribute = Attribute.GetCustomAttribute(item.GetType(), typeof(DbTableAttribute)) as DbTableAttribute;
                if (attribute.CacheMode == CacheMode.CacheInClient || attribute.CacheMode == CacheMode.NoCache)
                    isCache = false;
                try
                {
                    ModelMapping mp = new ModelMapping(item.GetType());
                    // 插入后获得的MaxRid应该减1为最新插入元素的Rid
                    int maxid = ModelFactoryCollection.GetMaxRid(mp);
                    item.Rid = maxid;
                    ModelFactoryCollection.InsertModel(item, mp, out e);
                    if (e == null && isCache)
                    {
                        id = maxid;
                        ModelCacheManager.Instance.Save(item);
                    }
                    else
                        return;
                }
                catch (Exception ex)
                {
                    e = ex;
                }
            }
              
        }
        public static void DeleteModel(ModelBase item, out Exception e)
        {
            lock (m_lock)
            {
                e = null;
                try
                {
                    ModelMapping mp = new ModelMapping(item.GetType());
                    ModelFactoryCollection.DeleteModel(item, mp, out e);
                    if (e == null)
                    {
                        ModelCacheManager.Instance.Remove(item);
                    }
                    else
                        return;
                }
                catch (Exception ex)
                {
                    e = ex;
                }
            }
        }
        public static void UpdateModel(ModelBase item, out Exception e)
        {
            lock (m_lock)
            {
                e = null;
                try
                {
                    ModelMapping mp = new ModelMapping(item.GetType());
                    ModelFactoryCollection.UpdateModel(item, mp, out e);
                    if (e == null)
                    {
                        ModelCacheManager.Instance.Save(item);
                    }
                    else
                        return;
                }
                catch (Exception ex)
                {
                    e = ex;
                }
            }
        }
        public static void AppendModel(ModelBase item, out Exception e)
        {
            e = null;
            try
            {
                ModelMapping mp = new ModelMapping(item.GetType());
                ModelFactoryCollection.AppendModel(item, mp, out e);
                if (e == null)
                {
                    ModelCacheManager.Instance.Save(item);
                }
                else
                    return;
            }
            catch (Exception ex)
            {
                e = ex;
            }
        }



        public static ModelCacheManager CacheManager
        {
            get { return ModelCacheManager.Instance; }
        }

        public static void CloseAllConnection()
        {
            ModelFactoryCollection.CloseAllConnection();
        }
        #endregion


        public static void Excute(ExcuteAction[] actions, IExcuteNotify notify,out bool result)
        {
            result = true;
            foreach (ExcuteAction ea in actions)
            {
                switch (ea.ExcuteType)
                {
                    case ExcuteType.Insert:
                        {
                            ModelBase model = ea.ExcuteObject as ModelBase;
                            if (model != null)
                            {
                                Exception ex;
                                int id;
                                InsertModel(model, out ex,out id);
                                if (ex != null || id < 0)
                                {
                                    notify.ErrorNotify(ex.Message, ExcuteType.Insert);
                                    result = false;
                                    return;
                                }
                                else
                                {
                                    model.Rid = id;
                                }
                            }
                            break;
                        }
                    case ExcuteType.Delete:
                        {
                            ModelBase model = ea.ExcuteObject as ModelBase;
                            if (model != null)
                            {
                                Exception ex;
                                DeleteModel(model, out ex);
                                if (ex != null)
                                {
                                    notify.ErrorNotify(ex.Message, ExcuteType.Delete);
                                    result = false;
                                    return;
                                }
                            }
                            break;
                        }
                    case ExcuteType.Update:
                        {
                            ModelBase model = ea.ExcuteObject as ModelBase;
                            if (model != null)
                            {
                                Exception ex;
                                UpdateModel(model, out ex);
                                if (ex != null)
                                {
                                    notify.ErrorNotify(ex.Message,ExcuteType.Update);
                                    result = false;
                                    return;
                                }
                            }
                            break;
                        }
                    case ExcuteType.CacheSave:
                        {
                            ModelBase model = ea.ExcuteObject as ModelBase;                            
                            if (model != null)
                            {
                                try
                                {
                                    ModelCacheManager.Instance.Save(model);
                                }
                                catch (Exception ex)
                                {
                                    notify.ErrorNotify(ex.Message,ExcuteType.Update);
                                    result = false;
                                    return;
                                }
                            }
                            break;                            
                           
                        }
                    case ExcuteType.CacheRemove:
                        {
                            break;
                        }
                }
            }           
           // notify.ExcuteNotify(actions);
        }

        public static ModelBase[] GetLogList(string modelTypeName, DateTime beginTime, DateTime endTime,out Exception ex)
        {
            ex = null;
            Type modelType = null;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type t in asm.GetTypes())
                {
                    if (t.Name == modelTypeName)
                        modelType = t;
                }
            }
            if (modelType==null)
                return null;
            ModelEnumerator reader = null; ;
            try
            {
                string where;
                if (modelType.Name == "UserLoginLog")
                    where = "logDateTime>'" + beginTime.ToString() + "' and logDateTime<'" + endTime.ToString() + "'";
                else
                    where = "ChangeDatetime>'" + beginTime.ToString() + "' and ChangeDatetime<'" + endTime.ToString() + "'";
                reader = new ModelEnumerator(modelType, where);
               
            }
            catch (Exception e)
            {
                ex = e;
            }
            if (reader != null)
                return reader.ToArray();
            else
                return null;
            
        }
       
    }
}
