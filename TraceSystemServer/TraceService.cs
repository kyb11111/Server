using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SuperControl.ServiceModel;
using System.Runtime.Serialization;
using System.IO;
using System.Xml;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Reflection;
using ThoughtWorks.QRCode.Codec;

namespace TraceServer
{
     public partial class TraceService
    {
        private readonly object m_lock = new object();
        private string m_ip;
        /// <summary>
        /// 登录验证
        /// </summary>
        /// <param name="verificationCode"></param>
        /// <returns></returns>
        internal bool Verify(string verificationCode)
        {
            //进行登陆验证
            return true;
        }

        /// <summary>
        /// 获取服务器时间
        /// </summary>
        /// <returns></returns>
        public DateTime GetServerTime()
        {
            return DateTime.Now;
        }

        public bool DownloadTextFile(string path, string fileName, out byte[] fileContent)
        {
            fileContent = new byte[0] { };
            try
            {
                string filePath = System.IO.Path.Combine(Environment.CurrentDirectory + "\\" + path + "\\", fileName);
                FileStream fs = File.OpenRead(filePath); //OpenRead
                int filelength = 0;
                filelength = (int)fs.Length; //获得文件长度 
                fileContent = new Byte[filelength]; //建立一个字节数组 
                fs.Read(fileContent, 0, filelength); //按字节流读取 
                fs.Close();

            }
            catch (Exception ex)
            {
                return false;
            }
            return true;
        }

        public bool UploadTextFile(string path, string fileName, byte[] fileContent)
        {
            lock (s_ServiceLocker)
            {
                try
                {
                    string filePath = System.IO.Path.Combine(Environment.CurrentDirectory + "\\" + path);
                    if (!Directory.Exists(filePath))
                    {
                        Directory.CreateDirectory(filePath);
                    }
                    FileStream stream = new FileStream(filePath + "\\" + fileName, FileMode.Create);
                    stream.Write(fileContent, 0, fileContent.Length);
                    stream.Flush();
                    stream.Close();
                    return true;
                }
                catch (Exception ex)
                {
                    ErrorNotify(ex.Message, SuperControl.ServiceModel.ExcuteType.GetLog);
                    return false;
                }
            }
        }

        public bool DeleteFile(string path, string fileName)
        {
            try
            {
                string filePath = System.IO.Path.Combine(Environment.CurrentDirectory + "\\" + path + "\\", fileName);
                File.Delete(filePath);
                return true;
            }
            catch (Exception ex)
            {
                ErrorNotify(ex.Message, SuperControl.ServiceModel.ExcuteType.GetLog);
                return false;
            }
        }

        public bool SaveQRCode(string path, string fileName)
        {
            //创建二维码对象
            QRCodeEncoder qrCodeEncoder = new QRCodeEncoder();
            //设置编码模式                 
            qrCodeEncoder.QRCodeEncodeMode = QRCodeEncoder.ENCODE_MODE.BYTE;
            //设置编码测量度
            qrCodeEncoder.QRCodeScale = 4;
            //设置编码版本
            qrCodeEncoder.QRCodeVersion = 7;
            //设置编码错误纠正
            qrCodeEncoder.QRCodeErrorCorrect = QRCodeEncoder.ERROR_CORRECTION.M;
            try
            {
                string filePath = System.IO.Path.Combine(Environment.CurrentDirectory + "\\" + path);
                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }
                //生成二维码图片
                qrCodeEncoder.Encode(fileName).Save(filePath + "//" + fileName);
                //图像按钮的图像URL
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        /// <summary>
        /// 调用回调函数通知客户端执行出错
        /// </summary>
        /// <param name="message">错误信息</param>
        /// <param name="type">操作的类型</param>
        public void ErrorNotify(string message, ExcuteType type)
        {
            if (m_callback != null)
                m_callback.ErrorNotify("registration", message, type);
        }
    }

    public static class XmlSerializer
    {

        public static string ToXml(object sourceObj)
        {
            if (sourceObj != null)
            {
                DataContractSerializer x = new DataContractSerializer(sourceObj.GetType());
                StringBuilder sb = new StringBuilder();
                using (XmlWriter writer = XmlWriter.Create(sb))
                {
                    x.WriteObject(writer, sourceObj);
                    writer.Flush();
                    return sb.ToString();
                }
            }
            return "";
        }

        public static object ToObject(string xmlFile, Type objType)
        {
            object result = null;
            StringReader stringReader = new StringReader(xmlFile);

            using (XmlReader reader = XmlReader.Create(stringReader))
            {
                DataContractSerializer x = new DataContractSerializer(objType);
                result = x.ReadObject(reader);
            }
            return result;
        }
    }
}
