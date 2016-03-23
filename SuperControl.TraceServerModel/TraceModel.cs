using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.ComponentModel;
using SuperControl.ServiceModel;

namespace SuperControl.TraceServerModel
{
  [Serializable, DataContract]
  [KnownType(typeof(ProductType))]
  [KnownType(typeof(UserInfo))]
  [KnownType(typeof(Product))]
  [KnownType(typeof(TraceInfo))]
  public class TraceModel : ModelBase , INotifyPropertyChanged
  {
  }
}
