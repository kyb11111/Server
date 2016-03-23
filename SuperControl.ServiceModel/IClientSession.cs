using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SuperControl.ServiceModel
{
    public interface IClientSession
    {
        void OnPing();

        void OnDisconnected();

        void OnDisconnected(IClientSession client);
    }
}
