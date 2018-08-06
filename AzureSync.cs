using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Configuration;
using System.Net.Mail;
using System.Runtime.Remoting;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TCore.Pipeline;

namespace TCore.ListenAz
{
    // ============================================================================
    // L I S T E N  S Y N C
    //
    // these are the records that will allow us to sync the persistent storage to azure
    // ============================================================================
    public class ListenSync : IPipelineBase<ListenSync>
    {
        private string m_sFilename;
        private int m_cRecordsExpecting;
        private long m_ibFileOffsetLim;
        private bool m_fDoneWithFile;
        private ListenRecord.Partition m_part;

        public string Filename => m_sFilename;
        public long FileOffsetLim => m_ibFileOffsetLim;
        public int RecordsExpecting => m_cRecordsExpecting;
        public bool Done => m_fDoneWithFile;
        public ListenRecord.Partition Part => m_part;

        // empty ctor to make NUnit happy
        public ListenSync()  { }

        #region Constructors / Initialization

        /*----------------------------------------------------------------------------
        	%%Function: IPipelineBase<ListenSync>.InitFrom
        	%%Qualified: TCore.ListenAz.ListenSync.TCore.Pipeline.IPipelineBase<TCore.ListenAz.ListenSync>.InitFrom
        	%%Contact: rlittle
        	
            Initialize this instance from the given instance
        ----------------------------------------------------------------------------*/
        void IPipelineBase<ListenSync>.InitFrom(ListenSync lr)
        {
            m_sFilename = lr.m_sFilename;
            m_cRecordsExpecting = lr.m_cRecordsExpecting;
            m_ibFileOffsetLim = lr.m_ibFileOffsetLim;
            m_fDoneWithFile = lr.m_fDoneWithFile;
            m_part = lr.m_part;
        }

        /*----------------------------------------------------------------------------
        	%%Function: ListenSync
        	%%Qualified: TCore.ListenAz.ListenSync.ListenSync
        	%%Contact: rlittle
        	
            Create a new ListenSync
        ----------------------------------------------------------------------------*/
        public ListenSync(string sFilename, int cRecordsExpecting, long ibFileOffsetLim, ListenRecord.Partition part, bool fDone)
        {
            m_sFilename = sFilename;
            m_fDoneWithFile = fDone;
            m_cRecordsExpecting = cRecordsExpecting;
            m_ibFileOffsetLim = ibFileOffsetLim;
            m_part = part;
        }
        #endregion
    }

    // for now, just upload as we get notifications...
    // ============================================================================
    // S T A G E   2
    //
    // stage 2 is all about taking notifications from the hot pipe and collecting/syncing them to azure
    // Naively, we could just upload whenever we get notifications. But, we probably want to batch these up.
    // the problem with batching them up is that we might have a final batch waiting to upload on shutdown
    // (and it could be big). so maybe have a timer wake us up? This should be OK since we aren't cooperatively
    // multithreaded, we are truly thread safe, so if we wake up on our own we should be OK
    // ============================================================================
    public partial class Stage2
    {
        private ProducerConsumer<ListenSync> m_pipeSync;
        private listener.IHookListen m_ihl;

        /*----------------------------------------------------------------------------
        	%%Function: Stage2
        	%%Qualified: TCore.ListenAz.Stage2.Stage2
        	%%Contact: rlittle
        	
            Create the sync stage and start the pipe
        ----------------------------------------------------------------------------*/
        public Stage2(listener.IHookListen ihl)
        {
            m_pipeSync = new ProducerConsumer<ListenSync>((string sMessage) => { ihl.WriteLine(sMessage); }, ProcessQueuedRecord);
            m_ihl = ihl;
            m_pipeSync.Start();
        }

        /*----------------------------------------------------------------------------
        	%%Function: Stop
        	%%Qualified: TCore.ListenAz.Stage2.Stop
        	%%Contact: rlittle
        	
            Stope the sync pipe
        ----------------------------------------------------------------------------*/
        public void Stop()
        {
            m_pipeSync.Stop();
        }

        /*----------------------------------------------------------------------------
        	%%Function: TestSuspendConsumerThread
        	%%Qualified: TCore.ListenAz.Stage2.TestSuspendConsumerThread
        	%%Contact: rlittle
        	
            Suspend the consumer thread, only used for debugging
        ----------------------------------------------------------------------------*/
        public void TestSuspendConsumerThread()
        {
            m_pipeSync.TestSuspendConsumerThread();
        }

        /*----------------------------------------------------------------------------
        	%%Function: TestResumeConsumerThread
        	%%Qualified: TCore.ListenAz.Stage2.TestResumeConsumerThread
        	%%Contact: rlittle
        	
            resume the consumer thread
        ----------------------------------------------------------------------------*/
        public void TestResumeConsumerThread()
        {
            m_pipeSync.TestResumeConsumerThread();
        }

        /*----------------------------------------------------------------------------
        	%%Function: RecordNewListenSync
        	%%Qualified: TCore.ListenAz.Stage2.RecordNewListenSync
        	%%Contact: rlittle
        	
            record a new ListenSync record for syncing
        ----------------------------------------------------------------------------*/
        public void RecordNewListenSync(Stage1.ListenRecordFile lrf, bool fDone)
        {
            RecordNewListenSync(lrf.Filename, lrf.RecordsExpecting, lrf.FileOffsetLim, lrf.Part, fDone);
        }

        /*----------------------------------------------------------------------------
        	%%Function: RecordNewListenSync
        	%%Qualified: TCore.ListenAz.Stage2.RecordNewListenSync
        	%%Contact: rlittle
        	
        ----------------------------------------------------------------------------*/
        public void RecordNewListenSync(string sFilename, int cRecordsExpecting, long ibFFilOffsetLim, ListenRecord.Partition part, bool fDone)
        {
            ListenSync ls;
            ls = new ListenSync(sFilename, cRecordsExpecting, ibFFilOffsetLim, part, fDone);

            m_pipeSync.Producer.QueueRecord(ls);
        }

        // FUTURE: If in the future we try to batch things together (which means that
        // we will collect sync records across thread iterations), we will have to be
        // careful to NOT update the processed offset in persistent storage until we
        // have gotten an ack'ed write to the server.  if we crashed before the
        // pending batch entries are flushed to the server, then we will lose them
        // The persistent storage has to reflect exactly what has made it to the server
        
        // in order to do this, we have to add a layer between the durable file and
        // in-memory (intercepting and adjusting for what we have in memory, or on disk
        // (sort of like a cache)

        /*----------------------------------------------------------------------------
        	%%Function: IbOffsetToStartFile
        	%%Qualified: TCore.ListenAz.Stage2.IbOffsetToStartFile
        	%%Contact: rlittle
        	
            Figure out the offset that we should start processing the file at
            (this was written by a previous sync consumer to denote how far we
            had processed
        ----------------------------------------------------------------------------*/
        long IbOffsetToStartFile(string sFilename)
        {
            string sProcessedFile = $"{sFilename}.Processed";

            if (File.Exists(sProcessedFile))
            {
                using (TextReader tr = new StreamReader(sProcessedFile))
                {
                    string sLine = tr.ReadLine();

                    return Int64.Parse(sLine);
                }
            }

            return 0;
        }

        /*----------------------------------------------------------------------------
        	%%Function: RecordIbOffsetStartForFile
        	%%Qualified: TCore.ListenAz.Stage2.RecordIbOffsetStartForFile
        	%%Contact: rlittle
        	
            Record the offset that we have processed up to (future iterations will
            use this to know where to start processing from)
        ----------------------------------------------------------------------------*/
        void RecordIbOffsetStartForFile(string sFilename, long ib)
        {
            string sProcessedFile = $"{sFilename}.Processed";

            using (TextWriter tw = new StreamWriter(sProcessedFile, false))
            {
                tw.WriteLine(ib.ToString());
            }
        }

        // This is the sync side of the ListenRecordFile. It allows buffered line reading
        // of the file (including swapping buffers and maintinging non-spanning parsing)

        // this is mostly just a fancy wrapper around getting lines out of the file.


        /*----------------------------------------------------------------------------
        	%%Function: ProcessQueuedRecord
        	%%Qualified: TCore.ListenAz.Stage2.ProcessQueuedRecord
        	%%Contact: rlittle
        	
            Process a record from the hot pipe -- right now, just print out 
            that we would have processed the record.  future, copy it to azure.
            future future, batch up the uploads
        ----------------------------------------------------------------------------*/
        public void ProcessQueuedRecord(IEnumerable<ListenSync> pllr)
        {
            IEnumerator<ListenSync> enumerator = pllr.GetEnumerator();

            if (!enumerator.MoveNext())
                return;

            ListenSync lr = enumerator.Current;
            if (lr == null)
                return;

            while (lr != null)
            {
                // figure out what records we want to push to the server
                // to determine where to start, look for file lr.sFileName&".Processed"
                //   if not found, then start from beginning
                //   if found, read the value from the file and that is the offset to start from.
                long ibStart = IbOffsetToStartFile(lr.Filename);

                m_ihl.WriteLine(
                    $"Dumping records for {Regex.Escape(lr.Filename)}, starting at {ibStart}. Partition: {ListenRecord.PartToString(lr.Part)}");

                ListenSyncFile lsf = new ListenSyncFile(lr.Filename, ibStart, lr.FileOffsetLim);

                string sLine;

                while ((sLine = lsf.ReadLine()) != null)
                {
                    m_ihl.WriteLine($"Flush: {sLine}");
                }

                RecordIbOffsetStartForFile(lr.Filename, lsf.Position());
                lsf.Close();

                if (enumerator.MoveNext())
                    lr = enumerator.Current;
                else
                    lr = null;
            }
        }
    }
}


