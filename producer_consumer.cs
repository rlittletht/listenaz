using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;

namespace TCore.ListenAz
{
    public class ListenRecord
    {
        private DateTime m_dttmForPartition;
        private Int64 m_dwTickCount;
        private string m_sAppName;
        private System.Diagnostics.TraceEventType m_tetEventType;
        private int m_nEventID;
        private int m_nInstanceID;
        private int m_nPid;
        private int m_nTid;
        private string m_sMessage;

        public ListenRecord()
        {

        }

        public ListenRecord(DateTime dttm, TraceEventType tet, string sMessage)
        {
            Process proc = Process.GetCurrentProcess();

            m_dttmForPartition = dttm;
            m_sMessage = sMessage;
            m_sAppName = Process.GetCurrentProcess().ProcessName;
            m_tetEventType = tet;
            m_nEventID = 0;
            m_nInstanceID = 0;
            m_nPid = proc.Id;
            m_nTid = System.Threading.Thread.CurrentThread.ManagedThreadId;
            m_dwTickCount = System.Environment.TickCount;
        }

        public ListenRecord(ListenRecord lr)
        {
            m_dttmForPartition = lr.m_dttmForPartition;
            m_dwTickCount = lr.m_dwTickCount;
            m_sAppName = lr.m_sAppName;
            m_tetEventType = lr.m_tetEventType;
            m_nEventID = lr.m_nEventID;
            m_nInstanceID = lr.m_nInstanceID;
            m_nPid = lr.m_nPid;
            m_nTid = lr.m_nTid;
            m_sMessage = lr.m_sMessage;
        }

        public struct Partition
        {
            public string Year;
            public string Month;
            public string Day;
            public string Hour;
        }

        static Partition PartitionParse(DateTime dttm)
        {
            Partition part;

            DateTime dttmUTC = dttm.ToUniversalTime();

            part.Year = dttmUTC.Year.ToString("D4");
            part.Month = dttmUTC.Month.ToString("D2");
            part.Day = dttmUTC.Day.ToString("D2");
            part.Hour = dttmUTC.Hour.ToString("D2");

            return part;
        }

        static string PartToString(Partition part)
        {
            return $"{part.Year}/{part.Month}/{part.Day}/{part.Hour}";
        }

        [Test]
        [TestCase("4/11/1972 12:00 +07:00", "1972/04/11/05")]
        [TestCase("4/11/1972 0:00 +07:00", "1972/04/10/17")]
        [TestCase("4/1/1972 12:00 +07:00", "1972/04/01/05")]
        [TestCase("4/11/2002 12:00 +07:00", "2002/04/11/05")]
        public static void TestPartitionParse(string sDttm, string sExpected)
        {
            DateTime dttm = DateTime.Parse(sDttm);

            Partition part = PartitionParse(dttm);

            Assert.AreEqual(sExpected, PartToString(part));
        }

        static string EventTypeToString(TraceEventType tet)
        {
            switch (tet)
            {
                case TraceEventType.Critical:
                    return "Critical";
                case TraceEventType.Error:
                    return "Error";
                case TraceEventType.Information:
                    return "Information";
                case TraceEventType.Verbose:
                    return "Verbose";
                case TraceEventType.Warning:
                    return "Warning";
                default:
                    return "UNKNOWN EVENT TYPE";
            }
        }

        public override string ToString()
        {
            Partition part = PartitionParse(m_dttmForPartition);

            return
                $"{PartToString(part)}: {m_dttmForPartition.ToString()}, tick({m_dwTickCount}), {EventTypeToString(m_tetEventType)}, {m_sAppName}, eid({m_nEventID}), inst({m_nInstanceID}), pid({m_nPid}), tid({m_nTid}), {m_sMessage}";
        }
    }

    public class SharedListenData
    {
        private List<ListenRecord> m_pllr;
        private bool m_fDone;
        private Object m_oLock;
        private AutoResetEvent m_evt;
        private listener.IHookListen m_ihl;

        public SharedListenData(listener.IHookListen ihl = null)
        {
            m_pllr = new List<ListenRecord>();
            m_fDone = false;
            m_oLock = new Object();
            m_evt = new AutoResetEvent(false);
            m_ihl = ihl;
        }

        public void HookLog(string sMessage)
        {
            m_ihl.WriteLine(sMessage);
        }
        public void HookListen(ListenRecord lr)
        {
            if (m_ihl == null)
                return;

            m_ihl.WriteLine(lr.ToString());
        }

        public void AddListenRecord(ListenRecord lr)
        {
            lock (m_oLock)
            {
                m_pllr.Add(lr);
                m_evt.Set(); //signal that there is data waiting
            }
        }

        public void TerminateListener()
        {
            lock (m_oLock)
            {
                m_fDone = true;
                m_evt.Set();
            }
        }

        public void WaitForEventSignal()
        {
            m_evt.WaitOne();
        }

        public bool IsDone()
        {
            bool fDone = false;
            lock (m_oLock)
                fDone = m_fDone;

            return fDone;
        }

        public List<ListenRecord> GrabListenRecords()
        {
            List<ListenRecord> pllr = new List<ListenRecord>(m_pllr.Count);

            lock (m_oLock)
            {
                foreach (ListenRecord lr in m_pllr)
                {
                    pllr.Add(new ListenRecord(lr));
                }

                m_pllr.Clear();
            }

            return pllr;
        }
    }

    public class ProducerConsumer
    {
        private SharedListenData m_sld;
        private Producer m_prod;
        private Consumer m_cons;
        private Thread m_threadConsumer;

        public ProducerConsumer(listener.IHookListen ihl = null)
        {
            m_sld = new SharedListenData(ihl);

            m_prod = new Producer(m_sld);
            m_cons = new Consumer(m_sld);
        }

        private int m_cSuspendedThread;

        public void TestSuspendConsumerThread()
        {
            if (++m_cSuspendedThread == 1)
                m_threadConsumer.Suspend();
        }

        public void TestResumeThread()
        {
            if (m_cSuspendedThread == 0)
                throw new Exception("poorly nested suspend");

            m_cSuspendedThread--;
            if (m_cSuspendedThread == 0)
                m_threadConsumer.Resume();
        }
        public Producer Start()
        {
            m_threadConsumer = new Thread(m_cons.Listen);
            m_threadConsumer.Start();

            return m_prod;
        }

        public void Stop()
        {
            m_sld.TerminateListener();
        }
    }
}