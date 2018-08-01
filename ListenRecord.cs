using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using NUnit.Framework;
using TCore.Pipeline;

namespace TCore.ListenAz
{
    // ============================================================================
    // L I S T E N  R E C O R D
    // ============================================================================
    public class ListenRecord : IPipelineBase<ListenRecord>
    {
        private Partition m_part;
        private DateTime m_dttm;
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

        #region Constructors / Initialization

        /*----------------------------------------------------------------------------
        	%%Function: IPipelineBase<ListenRecord>.InitFrom
        	%%Qualified: TCore.ListenAz.ListenRecord.TCore.Pipeline.IPipelineBase<TCore.ListenAz.ListenRecord>.InitFrom
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        void IPipelineBase<ListenRecord>.InitFrom(ListenRecord lr)
        {
            m_dttm = lr.m_dttm;
            m_part = lr.m_part;
            m_dwTickCount = lr.m_dwTickCount;
            m_sAppName = lr.m_sAppName;
            m_tetEventType = lr.m_tetEventType;
            m_nEventID = lr.m_nEventID;
            m_nInstanceID = lr.m_nInstanceID;
            m_nPid = lr.m_nPid;
            m_nTid = lr.m_nTid;
            m_sMessage = lr.m_sMessage;
        }

        /*----------------------------------------------------------------------------
        	%%Function: ListenRecord
        	%%Qualified: TCore.ListenAz.ListenRecord.ListenRecord
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        public ListenRecord(TraceEventType tet, string sMessage, int nDebugMinutesOffset = 0)
        {
            Process proc = Process.GetCurrentProcess();

            m_dttm = DateTime.Now;
            if (nDebugMinutesOffset != 0)
                m_dttm = m_dttm.AddMinutes(nDebugMinutesOffset);

            m_part = PartitionParse(m_dttm);
            m_sMessage = sMessage;
            m_sAppName = Process.GetCurrentProcess().ProcessName;
            m_tetEventType = tet;
            m_nEventID = 0;
            m_nInstanceID = 0;
            m_nPid = proc.Id;
            m_nTid = System.Threading.Thread.CurrentThread.ManagedThreadId;
            m_dwTickCount = System.Environment.TickCount;
        }

        /*----------------------------------------------------------------------------
        	%%Function: ListenRecord
        	%%Qualified: TCore.ListenAz.ListenRecord.ListenRecord
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        public ListenRecord(DateTime dttm, TraceEventType tet, string sMessage)
        {
            Process proc = Process.GetCurrentProcess();

            m_dttm = dttm;
            m_part = PartitionParse(m_dttm);
            m_sMessage = sMessage;
            m_sAppName = Process.GetCurrentProcess().ProcessName;
            m_tetEventType = tet;
            m_nEventID = 0;
            m_nInstanceID = 0;
            m_nPid = proc.Id;
            m_nTid = System.Threading.Thread.CurrentThread.ManagedThreadId;
            m_dwTickCount = System.Environment.TickCount;
        }

        /*----------------------------------------------------------------------------
        	%%Function: ListenRecord
        	%%Qualified: TCore.ListenAz.ListenRecord.ListenRecord
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        public ListenRecord(ListenRecord lr)
        {
            m_dttm = lr.m_dttm;
            m_part = lr.m_part;
            m_dwTickCount = lr.m_dwTickCount;
            m_sAppName = lr.m_sAppName;
            m_tetEventType = lr.m_tetEventType;
            m_nEventID = lr.m_nEventID;
            m_nInstanceID = lr.m_nInstanceID;
            m_nPid = lr.m_nPid;
            m_nTid = lr.m_nTid;
            m_sMessage = lr.m_sMessage;
        }

        #endregion

        public Partition Part => m_part;

        // ============================================================================
        // P A R T I T I O N
        // ============================================================================
        public struct Partition
        {
            public string Year;
            public string Month;
            public string Day;
            public string Hour;

            public Partition(string Year, string Month, string Day, string Hour)
            {
                this.Year = Year;
                this.Month = Month;
                this.Day = Day;
                this.Hour = Hour;
            }

            public static Partition Zero => new Partition(null, null, null, null);

            public Partition(Partition value)
            {
                this.Year = value.Year;
                this.Month = value.Month;
                this.Day = value.Day;
                this.Hour = value.Hour;
            }

            public static bool operator !=(Partition part1, Partition part2)
            {
                return !(part1 == part2);
            }

            public static bool operator ==(Partition part1, Partition part2)
            {
                if (part1.Hour != part2.Hour)
                    return false;

                if (part1.Day != part2.Day
                    || part1.Month != part2.Month
                    || part1.Year != part2.Year)
                    return false;

                return true;
            }
        }

        /*----------------------------------------------------------------------------
        	%%Function: PartitionParse
        	%%Qualified: TCore.ListenAz.ListenRecord.PartitionParse
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
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

        [Test]
        public static void TestPartitionCompare()
        {
            Partition partLeft = new Partition("2002", "12", "02", "10");
            Partition partRight = PartitionParse(DateTime.Parse("12/2/2002 10:00 +0"));

            Assert.AreEqual(partLeft, partRight);
        }

        [Test]
        public static void TestZeroCompare()
        {
            Partition partLeft = new Partition(null, null, null, null);
            Partition partRight = Partition.Zero;
            Assert.AreEqual(partLeft, partRight);
        }

        /*----------------------------------------------------------------------------
        	%%Function: EventTypeToString
        	%%Qualified: TCore.ListenAz.ListenRecord.EventTypeToString
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
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

        public static string s_sCsvHeader =
            "date,eventTickCount,level,applicationName,eventID,instanceID,processID,threadID,message";

        public string ToCsv()
        {
            Partition part = PartitionParse(m_dttm);

            string sDate = m_dttm.ToUniversalTime().ToString("yyyy-mm-dd'T'HH:MM:ss");
            return
                $"{sDate},{m_dwTickCount},{EventTypeToString(m_tetEventType)},\"{m_sAppName}\",{m_nEventID},{m_nInstanceID},{m_nPid},{m_nTid},\"{m_sMessage}\"";
        }

        public override string ToString()
        {
            Partition part = PartitionParse(m_dttm);

            return
                $"{PartToString(part)}: {m_dttm.ToString()}, tick({m_dwTickCount}), {EventTypeToString(m_tetEventType)}, {m_sAppName}, eid({m_nEventID}), inst({m_nInstanceID}), pid({m_nPid}), tid({m_nTid}), {m_sMessage}";
        }
    }

    // this will setup stage 1 pipeline, ready for the Listener to give us records and we will get them
    // into persistent local storage.  we will use our session ID as a directory to store the logs into
    // we don't want thousands of tiny files, so we will append to a file until it gets to a set size then
    // we will start a new file.

    // The Stage 1 Consumer (CONS1) will:
    //      IF Current file >= LIMIT records *OR* Current partition != Incoming partition, call PROD2.Queue("FileName, DONE FLAG"), DONE FLAG means everyone is HANDS OFF t.  set to No Current File
    //      IF no current file, then Create new file in APPDATA\\SESSIONID\\GUID.CSV, // FALLTHROUGH
    //      append to current file UNTIL PARTITION CHANGES, Call PROD2.Queue("FileName + SeekLimit + Number of lines added?")  (THIS MIGHT grow bigger than LIMIT since we don't want to create a new file in the middle of this)
    //          IF PARTITION CHANGES, the go back to top of loop

    // The stage 2 Producer (PROD2) will:
    //      Take FileName + current tell limit, add to QUEUE

    // The stage 2 Consumer (CONST2) will:
    //      Open the file, seek to the location given in FILENAME.PROCESSED, and read each line and process them.
    //          As we process each line, write the new "Written Limit" to FILENAME.PROCESSED
    //          (i.e. this file will always have a single value in it, which represents the seek location that
    //          is right after the last record processed)
    //      If DONE FLAG is set on record, then DELETE FILENAME and DELETE FILENAME.PROCESSED


    public class Stage1
    {
        private ProducerConsumer<ListenRecord> m_pipeHot;
        private Guid m_guidSession;
        private string m_sFileRoot;
        private listener.IHookListen m_ihl;

        private string
            m_sLogFolder; // this is something like "widgetFinderLogs" - initialized when the listener is created (and consistent across sessions)

        private int
            m_nTestOffsetMinutesDateTime; // this allows a test harness to mess with the datetime stamp. this value will be added to the datetime.now() when we create the listenrecord (the value is treated as minutes)

        public Stage1(listener.IHookListen ihl, string sLogFolder = "TestLogs")
        {
            m_pipeHot = new ProducerConsumer<ListenRecord>((string sMessage) => { ihl.WriteLine(sMessage); },
                ProcessQueuedRecord);

            m_guidSession = Guid.NewGuid();
            m_sLogFolder = sLogFolder;
            m_sFileRoot = Path.Combine(Path.GetTempPath(), m_sLogFolder, $"{m_guidSession.ToString()}");
            Directory.CreateDirectory(m_sFileRoot);

            m_sCurrentFile = null;
            m_partCurrent = ListenRecord.Partition.Zero;
            m_ihl = ihl;

            m_pipeHot.Start();
        }

        public void Stop()
        {
            m_pipeHot.Stop();
        }

        public void SetTestOffsetMinutesDateTime(int nMinutes)
        {
            m_nTestOffsetMinutesDateTime = nMinutes;
        }

        public void TestSuspendConsumerThread()
        {
            m_pipeHot.TestSuspendConsumerThread();
        }

        public void TestResumeConsumerThread()
        {
            m_pipeHot.TestResumeConsumerThread();
        }

        public void RecordNewListenRecord(string sMessage)
        {
            ListenRecord lr;

            if (m_nTestOffsetMinutesDateTime != 0)
            {
                lr = new ListenRecord(TraceEventType.Information, sMessage, m_nTestOffsetMinutesDateTime);
            }
            else
            {
                lr = new ListenRecord(TraceEventType.Information, sMessage);
            }

            m_pipeHot.Producer.QueueRecord(lr);
        }

        // THESE VALUES CANNOT BE TOUCHED OUTSIDE OF THE CONSUMER THREAD (once the thread is started)
        private string m_sCurrentFile;
        private int m_cCurRecords;
        private ListenRecord.Partition m_partCurrent;

        private static readonly int s_cMaxRecords = 1000;

        public void ProcessQueuedRecord(IEnumerable<ListenRecord> pllr)
        {
            IEnumerator<ListenRecord> enumerator = pllr.GetEnumerator();

            // enumerator.Reset();

            if (!enumerator.MoveNext())
                return;

            ListenRecord lr = enumerator.Current;
            if (lr == null)
                return;

            while (lr != null)
            {
                if (m_cCurRecords > s_cMaxRecords || m_partCurrent != lr.Part)
                {
                    // SEND NOTIFICATION HERE:
                    //    m_stage2.Producer2.Post(m_sCurrentFile, s_cMaxRecords, m_partCurrent, DoneRecord==TRUE)
                    m_sCurrentFile = null;
                }

                if (m_sCurrentFile == null)
                {
                    Guid guidFile = Guid.NewGuid();
                    m_sCurrentFile = Path.Combine(m_sFileRoot, $"{guidFile.ToString()}.csv");
                    m_cCurRecords = 0;
                    m_partCurrent = lr.Part;
                }

                using (TextWriter tw = new StreamWriter(m_sCurrentFile, true))
                {
                    while (lr != null && m_partCurrent == lr.Part)
                    {
                        if (m_cCurRecords == 0)
                            tw.WriteLine(ListenRecord.s_sCsvHeader);

                        tw.WriteLine(lr.ToCsv());
                        m_cCurRecords++;
                        if (!enumerator.MoveNext())
                            lr = null;

                        lr = enumerator.Current;
                    }
                }
            }
            // LASTLY, send a notification for the current file and the current record

            // if m_sCurrentFile != null, m_stage2.Producer2.Post(m_sCurrentFile, s_cMaxRecords, m_partCurrent, DONE==FALSE
        }
    }
}