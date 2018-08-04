using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Configuration;
using System.Net.Mail;
using System.Text.RegularExpressions;
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
        private long m_ibFileOffsetLim;
        private bool m_fDoneWithFile;
        private ListenRecord.Partition m_part;

        public string Filename => m_sFilename;
        public long FileOffsetLim => m_ibFileOffsetLim;
        public int RecordsExpecting => m_cRecordsExpecting;
        public bool Done => m_fDoneWithFile;
        public ListenRecord.Partition Part => m_part;

        public ListenSync()
        {

        }

        void IPipelineBase<ListenSync>.InitFrom(ListenSync lr)
        {
            m_sFilename = lr.m_sFilename;
            m_cRecordsExpecting = lr.m_cRecordsExpecting;
            m_ibFileOffsetLim = lr.m_ibFileOffsetLim;
            m_fDoneWithFile = lr.m_fDoneWithFile;
            m_part = lr.m_part;
        }

        public ListenSync(string sFilename, int cRecordsExpecting, long ibFileOffsetLim, ListenRecord.Partition part, bool fDone)
        {
            m_sFilename = sFilename;
            m_fDoneWithFile = fDone;
            m_cRecordsExpecting = cRecordsExpecting;
            m_ibFileOffsetLim = ibFileOffsetLim;
            m_part = part;
        }

    }

    // stage 2 is all about taking notifications from the hot pipe and collecting/syncing them to azure
    // Naively, we could just upload whenever we get notifications. But, we probably want to batch these up.
    // the problem with batching them up is that we might have a final batch waiting to upload on shutdown
    // (and it could be big). so maybe have a timer wake us up? This should be OK since we aren't cooperatively
    // multithreaded, we are truly thread safe, so if we wake up on our own we should be OK

    // for now, just upload as we get notifications...
    public class Stage2
    {
        private ProducerConsumer<ListenSync> m_pipeSync;
        private listener.IHookListen m_ihl;

        public Stage2(listener.IHookListen ihl)
        {
            m_pipeSync = new ProducerConsumer<ListenSync>((string sMessage) => { ihl.WriteLine(sMessage); },
                ProcessQueuedRecord);

            m_ihl = ihl;

            m_pipeSync.Start();
        }

        public void Stop()
        {
            m_pipeSync.Stop();
        }

        public void TestSuspendConsumerThread()
        {
            m_pipeSync.TestSuspendConsumerThread();
        }

        public void TestResumeConsumerThread()
        {
            m_pipeSync.TestResumeConsumerThread();
        }

        public void RecordNewListenSync(Stage1.ListenRecordFile lrf, bool fDone)
        {
            RecordNewListenSync(lrf.Filename, lrf.RecordsExpecting, lrf.FileOffsetLim, lrf.Part, fDone);
        }

        public void RecordNewListenSync(string sFilename, int cRecordsExpecting, long ibFFilOffsetLim, ListenRecord.Partition part, bool fDone)
        {
            ListenSync ls;
            ls = new ListenSync(sFilename, cRecordsExpecting, ibFFilOffsetLim, part, fDone);

            m_pipeSync.Producer.QueueRecord(ls);
        }

        // THESE VALUES CANNOT BE TOUCHED OUTSIDE OF THE CONSUMER THREAD (once the thread is started)

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

        void RecordIbOffsetStartForFile(string sFilename, long ib)
        {
            string sProcessedFile = $"{sFilename}.Processed";

            using (TextWriter tw = new StreamWriter(sProcessedFile, false))
            {
                tw.WriteLine(ib.ToString());
            }
        }

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

                using (StreamReader sr = new StreamReader(lr.Filename))
                {
                    sr.BaseStream.Seek(ibStart, SeekOrigin.Begin);
                    sr.DiscardBufferedData();

                    TextReader tr = sr;

                    while (ibStart < lr.FileOffsetLim)
                    {
                        string sLine = tr.ReadLine();
                        m_ihl.WriteLine($"Flush: {sLine}");
                        ibStart = sr.BaseStream.Position;
                    }
                }

                RecordIbOffsetStartForFile(lr.Filename, ibStart);
                if (enumerator.MoveNext())
                    lr = enumerator.Current;
                else
                    lr = null;
            }
        }
    }
}


