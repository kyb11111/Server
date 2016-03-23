using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using SuperControl.ServiceModel;

namespace SuperControl.TraceServerModel
{
  [Serializable, DataContract]
  [DbTable(HasAlternateKey = false,CacheMode = CacheMode.Both,TableName = "ECommerce",Realtime = false)]
  public class ECommerce : TraceModel
  {
       private Int32 m_Product;
       [DataMember, DbField(FieldName = "Product")]
       public Int32 Product
       {
           get { return m_Product; }
           set
           {
              if (m_Product != value)
              {
                  m_Product = value;
                  OnPropertyChanged("Product");
              }
           }
       }

       private String m_Price;
       [DataMember, DbField(FieldName = "Price")]
       public String Price
       {
           get { return m_Price; }
           set
           {
              if (m_Price != value)
              {
                  m_Price = value;
                  OnPropertyChanged("Price");
              }
           }
       }

       private Int32 m_Amount;
       [DataMember, DbField(FieldName = "Amount")]
       public Int32 Amount
       {
           get { return m_Amount; }
           set
           {
              if (m_Amount != value)
              {
                  m_Amount = value;
                  OnPropertyChanged("Amount");
              }
           }
       }

      public override void SetValueWithTableName(string fieldName, object value, out bool sendImmediately)
      {
          base.SetValueWithTableName(fieldName, value, out sendImmediately);
          switch (fieldName)
          {
              case "Product":
                  if (!m_Product.Equals(value))
                  {
                      m_Product = (System.Int32)ModelDatabaseEnumerator.ChangeType(value, typeof(System.Int32));
                      sendImmediately = true;
                      OnPropertyChanged("Product");
                  }
                  break;
              case "Price":
                  if (!m_Price.Equals(value))
                  {
                      m_Price = (System.String)ModelDatabaseEnumerator.ChangeType(value, typeof(System.String));
                      sendImmediately = true;
                      OnPropertyChanged("Price");
                  }
                  break;
              case "Amount":
                  if (!m_Amount.Equals(value))
                  {
                      m_Amount = (System.Int32)ModelDatabaseEnumerator.ChangeType(value, typeof(System.Int32));
                      sendImmediately = true;
                      OnPropertyChanged("Amount");
                  }
                  break;
              default:
                  sendImmediately = false;
                  break;
          }
      }
  }
}

