using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace SuperControl.ServiceModel
{
    [Serializable, DataContract]
    public class RegisteItem
    {
        [DataMember(IsRequired = true)]
        public string TypeName
        {
            get;
            set;
        }

        [DataMember]
        public int Rid
        {
            get;
            set;
        }

        [DataMember]
        public string AltKey
        {
            get;
            set;
        }

        [DataMember]
        public bool RegOnly
        {
            get;
            set;
        }

        public ModelBase Model
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TypeName))
                    return null;
                if (Rid == 0)
                {
                    if (string.IsNullOrWhiteSpace(AltKey))
                        return null;
                    return ModelCacheManager.Instance[TypeName, AltKey];
                }
                else
                {
                    return ModelCacheManager.Instance[TypeName, Rid];
                }
            }
        }

        public ModelBase[] GetModelAndAllChildren()
        {
            List<ModelBase> list = new List<ModelBase>();
            ModelBase model = Model;
            if (model != null)
            {
                list.Add(model);
                ModelBase.AddChildrenToList(model, list);
            }
            return list.ToArray();
        }
    }
}
