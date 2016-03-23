using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using SuperControl.ServiceModel;
using System.Timers;
using System.Threading;
using System.Reflection;
using SuperControl.TraceServerModel;

namespace TraceServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.PerSession, ConcurrencyMode = ConcurrencyMode.Multiple)]
    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    public partial class TraceService : ITraceService, IDisposable
    {
        private ITraceServiceCallback m_callback = null;
        internal static object s_ServiceLocker = new object();  
        
        private string loginName;
        public void Verify(string userName, string verificationCode)
        {
            lock (s_ServiceLocker)
            {
                try
                {
                    //获得回调接口实例
                    ITraceServiceCallback callback = OperationContext.Current.GetCallbackChannel<ITraceServiceCallback>();
                    if (!Verify(verificationCode))
                    {
                        callback.CloseSession();
                    }
                    else
                    {
                        m_callback = callback;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public bool Excute(SuperControl.ServiceModel.ExcuteAction[] actions)
        {
            bool issuccessed = false;
            List<ExcuteAction> retList = new List<ExcuteAction>();
            lock (s_ServiceLocker)
            {
                foreach (ExcuteAction action in actions)
                {
                    ModelBase model = action.ExcuteObject as ModelBase;
                    if (model == null)
                        continue;
                    Exception ex = null;

                    string logstring= string.Empty;
                    int maxrid = 0;
                    switch (action.ExcuteType)
                    {
                        case ExcuteType.Delete:
                            ModelMapping mp = new ModelMapping(model.GetType());
                            ModelFactoryCollection.DeleteModel(model, mp, out ex);
                            logstring = "delete";
                            if (ex == null)
                            {
                                ModelAccessManager.CacheManager.Remove(model);
                                retList.Add(action);
                            }
                            break;
                        case ExcuteType.Append:
                        case ExcuteType.Insert:
                            mp = new ModelMapping(model.GetType());
                            //model.Rid = ModelFactoryCollection.GetMaxRid(mp);
                            ModelFactoryCollection.InsertModel(model, mp, out ex);
                            logstring = "insert";
                            maxrid= ModelFactoryCollection.GetMaxRid(mp);
                            if (ex == null)
                            {
                                ModelAccessManager.CacheManager.Save(model);
                                retList.Add(action);
                            }
                            break;
                        case ExcuteType.Update:
                            mp = new ModelMapping(model.GetType());
                            logstring = "update";
                            ModelFactoryCollection.UpdateModel(model, mp, out ex);
                            if (ex == null)
                            {
                                ModelAccessManager.CacheManager.Save(model);
                                retList.Add(action);
                            }
                            break;
                        default:
                            break;
                    }                    
                    try
                    {
                        if (ex != null)
                        {
                            issuccessed = false;
                            m_callback.ErrorNotify("Registration", ex.Message, action.ExcuteType);
                        }
                        else
                        {
                            issuccessed = true;

                        }
                    }
                    catch (Exception e)
                    {
                        m_callback = null;
                        Console.WriteLine(e.Message);
                    }
                }
                if (retList.Count > 0)
                    SendAll(retList.ToArray());
            }
            return issuccessed;
        }

        public int InsertModel(ModelBase model)
        {
            if (model == null)
                return 0;
            lock (s_ServiceLocker)
            {
                ModelMapping mp = new ModelMapping(model.GetType());
                Exception ex = null;
                int rid = ModelFactoryCollection.InsertModelReturnRid(model, mp, out ex);
                if (ex == null)
                {
                    ModelAccessManager.CacheManager.Save(model);
                    ExcuteAction action = new ExcuteAction();
                    action.ExcuteObject = model;
                    model.Rid = rid;
                    action.ExcuteType = ExcuteType.Insert;
                    SendAll(action);
                }
                else
                {
                    m_callback.ErrorNotify(this.ToString(), ex.Message, ExcuteType.Insert);
                }
                return rid;
            }
        }

        private  void SendAll(params ExcuteAction[] actions)
        {
            try
            {
                m_callback.ExcuteNotify(actions);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} - {1}", m_callback, ex.Message);
            }
        }

        public ModelBase[] GetListAll(string modelType)
        {
            lock (s_ServiceLocker)
            {
                ModelCollection colleciton = ModelAccessManager.CacheManager[modelType];
                if (colleciton != null)
                    return colleciton.ToArray<TraceModel>();
                return new TraceModel[0];
            }
        }

        public ModelBase[] GetList(string modelType, int startRid, int count)
        {
            lock (s_ServiceLocker)
            {
                ModelCollection colleciton = ModelAccessManager.CacheManager[modelType];
                if (colleciton != null)
                    return colleciton.ToArray<TraceModel>(startRid, count);
                return new TraceModel[0];
            }
        }

        public string[] GetClientChcheModelTypeName()
        {
            lock (s_ServiceLocker)
            {
                return ModelAccessManager.GetClientChcheModelTypeName();
            }
        }

        public void Dispose()
        {
            lock (s_ServiceLocker)
            {
                lock (s_ServiceLocker)
                {
                    m_callback = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            m_callback = null;
        }     
    }
}