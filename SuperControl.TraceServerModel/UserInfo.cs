using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SuperControl.ServiceModel;

namespace SuperControl.TraceServerModel
{
  [Serializable, DataContract]
  [DbTable(HasAlternateKey = false,CacheMode = CacheMode.Both,TableName = "UserInfo",Realtime = false)]
  public class UserInfo : TraceModel
  {
       private String m_UserName;
       [DataMember, DbField(FieldName = "UserName")]
       public String UserName
       {
           get { return m_UserName; }
           set
           {
              if (m_UserName != value)
              {
                  m_UserName = value;
                  OnPropertyChanged("UserName");
              }
           }
       }

       private String m_PassWord;
       [DataMember, DbField(FieldName = "PassWord")]
       public String PassWord
       {
           get { return m_PassWord; }
           set
           {
              if (m_PassWord != value)
              {
                  m_PassWord = value;
                  OnPropertyChanged("PassWord");
              }
           }
       }

       private Int32 m_UserType;
       [DataMember, DbField(FieldName = "UserType")]
       public Int32 UserType
       {
           get { return m_UserType; }
           set
           {
              if (m_UserType != value)
              {
                  m_UserType = value;
                  OnPropertyChanged("UserType");
              }
           }
       }

       private String m_Remark;
       [DataMember, DbField(FieldName = "Remark")]
       public String Remark
       {
           get { return m_Remark; }
           set
           {
              if (m_Remark != value)
              {
                  m_Remark = value;
                  OnPropertyChanged("Remark");
              }
           }
       }

      public override void SetValueWithTableName(string fieldName, object value, out bool sendImmediately)
      {
          base.SetValueWithTableName(fieldName, value, out sendImmediately);
          switch (fieldName)
          {
              case "UserName":
                  if (!m_UserName.Equals(value))
                  {
                      m_UserName = (System.String)ModelDatabaseEnumerator.ChangeType(value, typeof(System.String));
                      sendImmediately = true;
                      OnPropertyChanged("UserName");
                  }
                  break;
              case "PassWord":
                  if (!m_PassWord.Equals(value))
                  {
                      m_PassWord = (System.String)ModelDatabaseEnumerator.ChangeType(value, typeof(System.String));
                      sendImmediately = true;
                      OnPropertyChanged("PassWord");
                  }
                  break;
              case "UserType":
                  if (!m_UserType.Equals(value))
                  {
                      m_UserType = (System.Int32)ModelDatabaseEnumerator.ChangeType(value, typeof(System.Int32));
                      sendImmediately = true;
                      OnPropertyChanged("UserType");
                  }
                  break;
              case "Remark":
                  if (!m_Remark.Equals(value))
                  {
                      m_Remark = (System.String)ModelDatabaseEnumerator.ChangeType(value, typeof(System.String));
                      sendImmediately = true;
                      OnPropertyChanged("Remark");
                  }
                  break;
              default:
                  sendImmediately = false;
                  break;
          }
      }
  }
}

