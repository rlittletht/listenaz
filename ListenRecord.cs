using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using TCore.Pipeline;

namespace TCore.ListenAz
{
    public class ListenRecord: IPipelineBase<ListenRecord>
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

        void IPipelineBase<ListenRecord>.InitFrom(ListenRecord lr)
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

        public ListenRecord(TraceEventType tet, string sMessage)
        {
            Process proc = Process.GetCurrentProcess();

            m_dttmForPartition = DateTime.Now;
            m_sMessage = sMessage;
            m_sAppName = Process.GetCurrentProcess().ProcessName;
            m_tetEventType = tet;
            m_nEventID = 0;
            m_nInstanceID = 0;
            m_nPid = proc.Id;
            m_nTid = System.Threading.Thread.CurrentThread.ManagedThreadId;
            m_dwTickCount = System.Environment.TickCount;
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

    public class Stage1
    {

    }
}