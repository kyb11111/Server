using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.ServiceModel.Description;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;

namespace SuperControl.ServiceModel
{
    /// <summary>
    /// 缓存模式，指示该类型是否在WCF服务器端或客户端的存根管理器中保存
    /// </summary>
    public enum CacheMode
    {
        /// <summary>
        /// 不需要在存根管理器中缓存
        /// </summary>
        NoCache,
        /// <summary>
        /// 需要在WCF服务器端的存根管理器中缓存
        /// </summary>
        CacheInServer,
        /// <summary>
        /// 需要在WCF客户端的存根管理器中缓存（暂时不起作用）
        /// </summary>
        CacheInClient,
        /// <summary>
        /// 在服务器与客户端两方都进行缓存
        /// </summary>
        Both
    }

    /// <summary>
    /// 定义模型对象类型所对应的数据库表
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class DbTableAttribute : Attribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public DbTableAttribute()
        {
            TableName = string.Empty;
            CacheMode = CacheMode.Both;
        }

        /// <summary>
        /// 该模型对象对应的数据库表名，如果为空则表示类名就是表名
        /// </summary>
        public string TableName
        {
            get;
            set;
        }

        /// <summary>
        /// 该表是否需要传输实时数据，
        /// 如果该属性设置为true，
        /// 则该模型对象
        /// 服务器端自动生成的代码需要重载GetRealtimeData函数
        /// 客户端自动生成的代买需要重载SetRealtimeData函数
        /// </summary>
        public bool Realtime
        {
            get;
            set;
        }

        /// <summary>
        /// 该表是否有可选索引键，
        /// 如果该属性设置为true，
        /// 则该模型对象
        /// 服务器端及客户端自动生成的代码需要重载AlternateKey属性，返回索引键如，Point返回Pid
        /// </summary>
        public bool HasAlternateKey
        {
            get;
            set;
        }

        /// <summary>
        /// 缓存模式，指示该类型是否在WCF服务器端或客户端的存根管理器中保存
        /// </summary>
        public CacheMode CacheMode
        {
            get;
            set;
        }
    }

    /// <summary>
    /// 定义模型对象属性所对应的数据库字段
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class DbFieldAttribute : Attribute
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public DbFieldAttribute()
        {
            FieldName = string.Empty;
        }

        /// <summary>
        /// 该属性对应的数据库字段名，如果为空则表示属性名就是字段名
        /// </summary>
        public string FieldName
        {
            get;
            set;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ChildrenModelAttribute : Attribute
    {
        internal Type m_childType;
        internal string m_protertyName;

        public ChildrenModelAttribute(Type childType)
            : this(childType, string.Empty)
        {
        }

        public ChildrenModelAttribute(Type childType, string protertyName)
        {
            m_childType = childType;
            m_protertyName = protertyName;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
    public class KnownTypeAssignAttribute : Attribute
    {
        private string m_operationName;
        private HashSet<Type> m_knownTypes = new HashSet<Type>();

        public KnownTypeAssignAttribute(string method, bool subTypeInclude, params Type[] knownTypes)
        {
            m_operationName = method;
            foreach (Type type in knownTypes)
            {
                m_knownTypes.Add(type);
                if (subTypeInclude)
                {
                    GetSubTypes(type);
                }
            }
        }

        public KnownTypeAssignAttribute(string method, Type targetAttribute)
        {
            m_operationName = method;
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                GetKnownTypes(assembly, targetAttribute);
            }
        }

        public KnownTypeAssignAttribute(string method, string targetAssmbly, Type targetAttribute)
        {
            m_operationName = method;
            Assembly assembly = Assembly.Load(targetAssmbly);
            GetKnownTypes(assembly, targetAttribute);
        }

        private void GetKnownTypes(Assembly assembly, Type targetAttribute)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsDefined(targetAttribute, false))
                {
                    m_knownTypes.Add(type);
                }
            }
        }

        private void GetSubTypes(Type type)
        {
            foreach (Type sub in type.Assembly.GetTypes())
            {
                if (sub.IsSubclassOf(type))
                {
                    m_knownTypes.Add(sub);
                }
            }
        }

        public string OperationName
        {
            get { return m_operationName; }
        }

        public Type[] KnownTypes
        {
            get { return m_knownTypes.ToArray(); }
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class KnownTypeAssignBehaviorAttribute : Attribute, IContractBehavior
    {
        public void AddBindingParameters(ContractDescription contractDescription, ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            //throw new NotImplementedException();
        }

        public void ApplyClientBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            //throw new NotImplementedException();
        }

        public void ApplyDispatchBehavior(ContractDescription contractDescription, ServiceEndpoint endpoint, DispatchRuntime dispatchRuntime)
        {
            foreach (KnownTypeAssignAttribute assign in Attribute.GetCustomAttributes(contractDescription.ContractType, typeof(KnownTypeAssignAttribute)))
            {
                OperationDescription operation = contractDescription.Operations.Find(assign.OperationName);
                if (operation != null)
                {
                    foreach (Type type in assign.KnownTypes)
                    {
                        operation.KnownTypes.Add(type);
                    }
                }
            }
        }

        public void Validate(ContractDescription contractDescription, ServiceEndpoint endpoint)
        {
            //throw new NotImplementedException();
        }
    }
}
