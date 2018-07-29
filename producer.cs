using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace TCore.ListenAz
{
    public class Producer
    {
        private SharedListenData m_sld;

        public Producer(SharedListenData sld)
        {
            m_sld = sld;
        }

        public void RecordEvent(TraceEventType tet, string sMessage)
        {
            ListenRecord lr = new ListenRecord(DateTime.Now, tet, sMessage);

            m_sld.AddListenRecord(lr);
        }
    }
}