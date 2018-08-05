using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
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

        // empty constructure for NUnit
        public ListenRecord() { }

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

        #region Partitions

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

        /*----------------------------------------------------------------------------
        	%%Function: PartToString
        	%%Qualified: TCore.ListenAz.ListenRecord.PartToString
        	%%Contact: rlittle
        	
            get a canonical string representation for the part
        ----------------------------------------------------------------------------*/
        public static string PartToString(Partition part)
        {
            return $"{part.Year}/{part.Month}/{part.Day}/{part.Hour}";
        }
        #endregion

        #region Unit Tests
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
        #endregion

        #region Log/String Generation
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

        /*----------------------------------------------------------------------------
        	%%Function: ToCsv
        	%%Qualified: TCore.ListenAz.ListenRecord.ToCsv
        	%%Contact: rlittle
        	
            Convert this ListenRecord into comma separated string 
        ----------------------------------------------------------------------------*/
        public string ToCsv()
        {
            Partition part = PartitionParse(m_dttm);

            string sDate = m_dttm.ToUniversalTime().ToString("yyyy-mm-dd'T'HH:MM:ss");
            return
                $"{sDate},{m_dwTickCount},{EventTypeToString(m_tetEventType)},\"{m_sAppName}\",{m_nEventID},{m_nInstanceID},{m_nPid},{m_nTid},\"{m_sMessage}\"";
        }

        /*----------------------------------------------------------------------------
        	%%Function: ToString
        	%%Qualified: TCore.ListenAz.ListenRecord.ToString
        	%%Contact: rlittle
        	
            Get a string representation for this ListenRecord
        ----------------------------------------------------------------------------*/
        public override string ToString()
        {
            Partition part = PartitionParse(m_dttm);

            return
                $"{PartToString(part)}: {m_dttm.ToString()}, tick({m_dwTickCount}), {EventTypeToString(m_tetEventType)}, {m_sAppName}, eid({m_nEventID}), inst({m_nInstanceID}), pid({m_nPid}), tid({m_nTid}), {m_sMessage}";
        }
        #endregion
    }

    // GENERAL APPROACH TO THE 2 STAGE PIPELINE

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

    // ============================================================================
    // S T A G E   1
    //
    // This is the hot part of the pipeline. ListenRecords take care of recording
    // the log information from the listener on the main thread, then on another 
    // thread, these reorded log messages are written to durable storage (filesystem)
    // and a ListenSync is created to the sync pipeline (which will take care of
    // syncing durable storage to azure)
    // ============================================================================
    public class Stage1
    {
        private ProducerConsumer<ListenRecord> m_pipeHot;
        private Guid m_guidSession;
        private string m_sFileRoot;
        private listener.IHookListen m_ihl;
        private Stage2 m_stage2;

        // this is something like "widgetFinderLogs" - initialized when the listener is created (and consistent across sessions)
        private string m_sLogFolder;

        // this allows a test harness to mess with the datetime stamp. this value will be added to the datetime.now() when we create the listenrecord (the value is treated as minutes)
        private int m_nTestOffsetMinutesDateTime;

        /*----------------------------------------------------------------------------
        	%%Function: Stage1
        	%%Qualified: TCore.ListenAz.Stage1.Stage1
        	%%Contact: rlittle
        	
            Create the ListenRecorder (the hot part of the pipe).
            This stage requires the other stage to already be created.
        ----------------------------------------------------------------------------*/
        public Stage1(listener.IHookListen ihl, Stage2 stage2, string sLogFolder = "TestLogs")
        {
            m_stage2 = stage2;
            m_pipeHot = new ProducerConsumer<ListenRecord>((string sMessage) => { ihl.WriteLine(sMessage); }, ProcessQueuedRecord);

            m_guidSession = Guid.NewGuid();
            m_sLogFolder = sLogFolder;
            m_sFileRoot = Path.Combine(Path.GetTempPath(), m_sLogFolder, $"{m_guidSession.ToString()}");
            Directory.CreateDirectory(m_sFileRoot);

            m_ihl = ihl;

            m_pipeHot.Start();
        }

        /*----------------------------------------------------------------------------
        	%%Function: Stop
        	%%Qualified: TCore.ListenAz.Stage1.Stop
        	%%Contact: rlittle
        	
            Stop the hot pipe. This does not stop the sync pipe
        ----------------------------------------------------------------------------*/
        public void Stop()
        {
            m_pipeHot.Stop();
        }

        /*----------------------------------------------------------------------------
        	%%Function: SetTestOffsetMinutesDateTime
        	%%Qualified: TCore.ListenAz.Stage1.SetTestOffsetMinutesDateTime
        	%%Contact: rlittle
        	
            set an internal offset for the recorder. this allows us to simulate
            crossing partition boundaries
        ----------------------------------------------------------------------------*/
        public void SetTestOffsetMinutesDateTime(int nMinutes)
        {
            m_nTestOffsetMinutesDateTime = nMinutes;
        }

        /*----------------------------------------------------------------------------
        	%%Function: TestSuspendConsumerThread
        	%%Qualified: TCore.ListenAz.Stage1.TestSuspendConsumerThread
        	%%Contact: rlittle
        	
            Suspend the consumer thread (used for debugging only)
        ----------------------------------------------------------------------------*/
        public void TestSuspendConsumerThread()
        {
            m_pipeHot.TestSuspendConsumerThread();
        }

        /*----------------------------------------------------------------------------
        	%%Function: TestResumeConsumerThread
        	%%Qualified: TCore.ListenAz.Stage1.TestResumeConsumerThread
        	%%Contact: rlittle
        	
            resume the consumer thread
        ----------------------------------------------------------------------------*/
        public void TestResumeConsumerThread()
        {
            m_pipeHot.TestResumeConsumerThread();
        }

        /*----------------------------------------------------------------------------
        	%%Function: RecordNewListenRecord
        	%%Qualified: TCore.ListenAz.Stage1.RecordNewListenRecord
        	%%Contact: rlittle
        	
            create a new ListenRecord for this message and post it to the hotPipe
        ----------------------------------------------------------------------------*/
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

        private static readonly int s_cMaxRecords = 1000;

        #region ListenRecordFile

        // ============================================================================
        // L I S T E N  R E C O R D  F I L E
        //
        // The Source/Sink mechanism between the hot pipe and the sync pipe is a 
        // persistent file on the disk.  The hot pipe gets WRITE access to the file
        // and the sync pipe gets only READ access. We stricly append to the file,
        // so the sync pipe can always get at the data that the hot pipe has posted
        // to it.
        //
        // In order to do this, we need to be explicit about the seek positions that
        // the sync pipe is supposed to look at, which means we have to take care
        // of our own filestream writing (buffered streams will cause problems)
        //
        // This file also takes care of knowing how many records have been written, 
        // as well as what partition this file represents (so we can determing if
        // future ListenRecords belong to the same partition)
        // ============================================================================
        public class ListenRecordFile
        {
            // private StreamWriter m_sw;
            private FileStream m_fs;
            private string m_sCurrentFile;
            private int m_cCurRecords;
            private int m_cCurRecordsStart;
            private ListenRecord.Partition m_partCurrent;
            private long m_ibOffsetLim;

            public string Filename => m_sCurrentFile;
            public int RecordsExpecting => m_cCurRecords - m_cCurRecordsStart;
            public ListenRecord.Partition Part => m_partCurrent;
            public long FileOffsetLim => m_ibOffsetLim;

            /*----------------------------------------------------------------------------
            	%%Function: ListenRecordFile
            	%%Qualified: TCore.ListenAz.Stage1.ListenRecordFile.ListenRecordFile
            	%%Contact: rlittle
            	
                Create a new ListenRecord file -- this is just a <GUID>.csv appended
                to the FileRoot we are given
            ----------------------------------------------------------------------------*/
            public ListenRecordFile(string sFileRoot, ListenRecord.Partition part)
            {
                Guid guidFile = Guid.NewGuid();
                m_sCurrentFile = Path.Combine(sFileRoot, $"{guidFile.ToString()}.csv");
                m_cCurRecords = 0;
                m_cCurRecordsStart = 0;
                m_ibOffsetLim = 0;
                m_partCurrent = part;
            }

            /*----------------------------------------------------------------------------
            	%%Function: Flush
            	%%Qualified: TCore.ListenAz.Stage1.ListenRecordFile.Flush
            	%%Contact: rlittle
            	
                Flush the file and make note of the lim seek position (so we can 
                inform the sync pipe)
            ----------------------------------------------------------------------------*/
            public long Flush()
            {
                if (m_fs == null)
                    return -1;

                m_fs.Flush();
                m_ibOffsetLim = m_fs.Position;
                return m_ibOffsetLim;
            }

            /*----------------------------------------------------------------------------
            	%%Function: FlushAndClose
            	%%Qualified: TCore.ListenAz.Stage1.ListenRecordFile.FlushAndClose
            	%%Contact: rlittle
            	
                flush the underlying file and CLOSE it. this will let us tell the 
                sync pipe that we are done with the file, and the sync pipe now
                gets to take ownership of the file (and delete it if necessary)
            ----------------------------------------------------------------------------*/
            public long FlushAndClose()
            {
                if (m_fs == null)
                    return -1;

                m_fs.Flush();
                m_ibOffsetLim = m_fs.Position;
                m_fs.Close();
                m_fs.Dispose();
                m_fs = null;

                return m_ibOffsetLim;
            }

            /*----------------------------------------------------------------------------
            	%%Function: FCanAppendPartitionRecord
            	%%Qualified: TCore.ListenAz.Stage1.ListenRecordFile.FCanAppendPartitionRecord
            	%%Contact: rlittle
            	
                Determine if the file is too big, or if the partitions don't match. if either
                is true, return false (we cannot append to this file)
            ----------------------------------------------------------------------------*/
            public bool FCanAppendPartitionRecord(ListenRecord.Partition part)
            {
                if (m_cCurRecords > s_cMaxRecords || m_partCurrent != part)
                    return false;

                return true;
            }

            static byte[] s_rgbNewline = { 0x0d, 0x0a };

            /*----------------------------------------------------------------------------
            	%%Function: WriteLine
            	%%Qualified: TCore.ListenAz.Stage1.ListenRecordFile.WriteLine
            	%%Contact: rlittle
            	
                Our version of WriteLine, uses UTF8
            ----------------------------------------------------------------------------*/
            void WriteLine(string sFile)
            {
                if (m_fs == null)
                    m_fs = new FileStream(m_sCurrentFile, FileMode.Append, FileAccess.Write, FileShare.Read);

                byte[] rgb;

                rgb = Encoding.UTF8.GetBytes(sFile);

                m_fs.Write(rgb, 0, rgb.Length);
                m_fs.Write(s_rgbNewline, 0, 2);
            }

            /*----------------------------------------------------------------------------
            	%%Function: WriteListenRecord
            	%%Qualified: TCore.ListenAz.Stage1.ListenRecordFile.WriteListenRecord
            	%%Contact: rlittle
            	
                Write the given ListenRecord to this file
            ----------------------------------------------------------------------------*/
            public void WriteListenRecord(ListenRecord lr)
            {
                if (m_cCurRecords == 0)
                    WriteLine(ListenRecord.s_sCsvHeader);

                WriteLine(lr.ToCsv());
                
                m_cCurRecords++;
            }
        }
        #endregion

        // THIS CANNOT BE TOUCHED OUTSIDE OF THE CONSUMER THREAD (once the thread is started)
        private ListenRecordFile m_lrfCurrent;

        /*----------------------------------------------------------------------------
        	%%Function: ProcessQueuedRecord
        	%%Qualified: TCore.ListenAz.Stage1.ProcessQueuedRecord
        	%%Contact: rlittle
        	
            Process a record that was queued for us. Save the records to persistent
            storage, and let the sync pipe know about them.
        ----------------------------------------------------------------------------*/
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
                if (m_lrfCurrent != null && !m_lrfCurrent.FCanAppendPartitionRecord(lr.Part))
                {
                    // SEND NOTIFICATION HERE:
                    m_lrfCurrent.FlushAndClose();
                    m_stage2.RecordNewListenSync(m_lrfCurrent, true);
                    m_lrfCurrent = null;
                }

                if (m_lrfCurrent == null)
                {
                    m_lrfCurrent = new ListenRecordFile(m_sFileRoot, lr.Part);
                }


                while (lr != null && m_lrfCurrent.FCanAppendPartitionRecord(lr.Part))
                {
                    m_lrfCurrent.WriteListenRecord(lr);

                    if (!enumerator.MoveNext())
                        lr = null;
                    else
                        lr = enumerator.Current;
                }

                m_lrfCurrent.Flush(); // FUTURE: Get rid of flush :(
            }

            // LASTLY, send a notification for the current file and the current record
            //m_lrfCurrent.FlushAndClose();  // we don't stricly have to close the file since we told the other thread we are NOT hands off of the file
            m_stage2.RecordNewListenSync(m_lrfCurrent, false);
        }
    }
}