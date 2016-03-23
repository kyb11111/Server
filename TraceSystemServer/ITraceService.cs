using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using SuperControl.ServiceModel;
//using SuperControl.RegistrationModel;
using System.Data;
namespace TraceServer
{
    /// <summary>
    /// Registration服务接口
    /// </summary>
   
   
    public partial interface ITraceService
    {
        [OperationContract(IsOneWay = false, IsInitiating = false, IsTerminating = false)]
        DateTime GetServerTime();
        [OperationContract(IsOneWay = false, IsInitiating = false, IsTerminating = false)]
        bool DownloadTextFile(string path,string fileName, out byte[] fileContent);
        [OperationContract(IsOneWay = false, IsInitiating = false, IsTerminating = false)]
        bool UploadTextFile(string path, string fileName, byte[] fileContent);
        [OperationContract(IsOneWay = false, IsInitiating = false, IsTerminating = false)]
        bool DeleteFile(string path, string fileName);
        [OperationContract(IsOneWay = false, IsInitiating = false, IsTerminating = false)]
        bool SaveQRCode(string path, string fileName);
    }

    /// <summary>
    /// Registration服务公布给客户端的回调接口
    /// </summary>
    public partial interface ITraceServiceCallback
    {
        
    }
}
