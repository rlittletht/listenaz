using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NUnit.Framework;
using TCore.Pipeline;

namespace TCore.ListenAz
{
    // these are the records that will allow us to sync the persistent storage to azure
    public class ListenSync : IPipelineBase<ListenSync>
    {
        private string m_sFilename;
        private int m_cRecordsExpecting;
        private int m_ibFileOffsetLim;
        private bool m_fDoneWithFile;
        private ListenRecord.Partition m_part;

        public ListenSync()
        {

        }

        void IPipelineBase<ListenSync>.InitFrom(ListenSync lr)
        {
            m_sFilename = lr.m_sFilename;
            m_cRecordsExpecting = lr.m_cRecordsExpecting;
            m_ibFileOffsetLim = lr.m_ibFileOffsetLim;
            m_fDoneWithFile = lr.m_fDoneWithFile;
        }

        public ListenSync(string sFilename, int cRecordsExpecting, int ibFileOffsetLim, bool fDone)
        {
            m_sFilename = sFilename;
            m_fDoneWithFile = fDone;
            m_cRecordsExpecting = cRecordsExpecting;
            m_ibFileOffsetLim = ibFileOffsetLim;
        }

    }
}
