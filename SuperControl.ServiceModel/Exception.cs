using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SuperControl.ServiceModel
{
    public class ModelServiceException : Exception
    {
        private string m_mesg;
        internal ModelServiceException(string format, params string[] arg)
        {
            m_mesg = string.Format(format, arg);
        }

        public override string Message
        {
            get
            {
                return m_mesg;
            }
        }
    }
}
