using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace TCore.ListenAz
{
    public class Consumer
    {
        private SharedListenData m_sld;

        public Consumer(SharedListenData sld)
        {
            m_sld = sld;
        }

        void ProcessPendingRecords()
        {
            List<ListenRecord> pllr = m_sld.GrabListenRecords();

            m_sld.HookLog($"grabbed {pllr.Count} records...");
            foreach (ListenRecord lr in pllr)
                m_sld.HookListen(lr);
        }

        public void Listen()
        {
            while (!m_sld.IsDone())
            {
                m_sld.WaitForEventSignal();
                ProcessPendingRecords();
            }


        }
    }
}