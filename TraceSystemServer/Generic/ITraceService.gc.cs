using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using SuperControl.ServiceModel;
using SuperControl.TraceServerModel;


namespace TraceServer
{
    /// <summary>
    /// Registration服务所需要的接口
    /// </summary>
    [ServiceContract(SessionMode = SessionMode.Required, CallbackContract = typeof(ITraceServiceCallback))]
    [KnownTypeAssign("GetListAll", true, typeof(TraceModel))]
    [KnownTypeAssign("GetList", true, typeof(TraceModel))]
    [KnownTypeAssign("Excute", true, typeof(TraceModel))]
    [KnownTypeAssign("InsertModel", true, typeof(TraceModel), typeof(RegisteItem))]
    [KnownTypeAssign("ExcuteNotify", true, typeof(TraceModel))]
    [KnownTypeAssignBehavior]
    public partial interface ITraceService
    {
        /// <summary>
        /// 用户登录验证，如果用户通过授权服务登录了系统，那么可以从授权系统模块中获得登录的用户名以及动态验证码
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="verificationCode">动态验证码</param>
        /// <returns>是否通过验证</returns>
        [OperationContract(IsOneWay = true, IsInitiating = true, IsTerminating = false)]
        void Verify(string userName, string verificationCode);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelType"></param>
        /// <param name="startRid"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [OperationContract(IsOneWay = false, IsInitiating = false, IsTerminating = false)]
        string[] GetClientChcheModelTypeName();//获取模型列表

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelType"></param>
        /// <returns></returns>
        [OperationContract(IsOneWay = false, IsInitiating = false, IsTerminating = false)]
        ModelBase[] GetListAll(string modelType);//获取模型列表

        /// <summary>
        /// 
        /// </summary>
        /// <param name="modelType"></param>
        /// <param name="startRid"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [OperationContract(IsOneWay = false, IsInitiating = false, IsTerminating = false)]
        ModelBase[] GetList(string modelType, int startRid, int count);//获取模型列表

        /// <summary>
        /// 对Registration的数据库对象进行添加、修改及删除操作，将启用事务，执行操作前系统会查看用户拥有的权限
        /// 添加操作需要用户拥有系统预定义的ModelAdd权限
        /// 修改操作需要用户拥有系统预定义的ModelEdit权限
        /// 删除操作需要用户拥有系统预定义的ModelRemove权限
        /// 执行操作失败时将由ErrorNotify回调接口返回错误信息
        /// </summary>
        /// <param name="action">执行的一系列操作</param>
        [OperationContract(IsOneWay = false, IsInitiating = false, IsTerminating = false)]
        bool Excute(ExcuteAction[] actions);

        /// <summary>
        /// 增加对象到数据库
        /// </summary>
        /// <param name="model">要增加的对象</param>
        /// <returns>为新增对象分配的rid,如果小于等于0表示不成功/returns>
        [OperationContract(IsOneWay = false, IsInitiating = false, IsTerminating = false)]
        int InsertModel(ModelBase model);
    }

    /// <summary>
    /// Registration服务公布给客户端的回调接口
    /// </summary>
    public partial interface ITraceServiceCallback
    {
        /// <summary>
        /// Registration模型对象的添加、修改及删除的通知
        /// </summary>
        /// <param name="action">一系列操作结果的通知</param>
        [OperationContract(IsOneWay = true)]
        void ExcuteNotify(ExcuteAction[] actions);

        /// <summary>
        /// 实时数据的通知，配置了Realtime标签的类都可以利用此接口向客户端快速且大量的发布数据更新
        /// </summary>
        /// <param name="value">实时数据</param>
        [OperationContract(IsOneWay = true)]
        void RealTimeNotify(string modelType, RealtimeData[] data);

        /// <summary>
        /// 错误信息通知
        /// </summary>
        /// <param name="serviceName">出错的服务接口名，为空表示是系统错误</param>
        /// <param name="message">错误信息</param>
        [OperationContract(IsOneWay = true)]
        void ErrorNotify(string serviceName, string message, ExcuteType type);

        [OperationContract(IsOneWay = true)]
        void ping();
        /// <summary>
        /// 从服务器端关闭回话
        /// </summary>
        [OperationContract(IsOneWay = true, IsTerminating = true)]
        void CloseSession();
    }
}
