using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Configuration;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
    public class Stage2
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

        #region ListenSyncFile

        // This is the sync side of the ListenRecordFile. It allows buffered line reading
        // of the file (including swapping buffers and maintinging non-spanning parsing)

        // this is mostly just a fancy wrapper around getting lines out of the file.
        public class ListenSyncFile
        {
            private FileStream m_fs;
            private long m_ibStart;
            private long m_ibLim;
            private Buffer m_bufferMain;
            private Buffer m_bufferSwap;

            private bool m_fUseSwapBuffer = false;

            private Buffer BufferCurrent => m_fUseSwapBuffer ? m_bufferSwap : m_bufferMain;
            private Buffer BufferOther => !m_fUseSwapBuffer ? m_bufferSwap : m_bufferMain;

            /*----------------------------------------------------------------------------
            	%%Function: ListenSyncFile
            	%%Qualified: TCore.ListenAz.Stage2.ListenSyncFile.ListenSyncFile
            	%%Contact: rlittle
            	
                Create a new ListenSyncFile, noting the starting offset and the offset
                lim we can read to.

                The file is opened Read only, allowing ReadWrite share opening (to allow
                the other thread to open the file read/write)
            ----------------------------------------------------------------------------*/
            public ListenSyncFile(string sFilename, long ibStart, long ibLim)
            {
                m_fs = new FileStream(sFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                m_ibStart = ibStart;
                m_ibLim = ibLim;
                m_fs.Seek(m_ibStart, SeekOrigin.Begin);
                m_bufferMain = new Buffer(m_fs, ibLim);
                m_bufferSwap = new Buffer(m_fs, ibLim);
            }

            // ============================================================================
            // B U F F E R
            //
            // Allows buffering on top of a FileStream
            // ============================================================================
            class Buffer
            {
                private byte[] m_rgb = new byte[1024];
                private int m_ibBufferStart = -1;
                private int m_ibBufferLim = -1;
                private readonly long m_ibFileLim;
                private readonly Stream m_stm;

                public int Start => m_ibBufferStart;
                public int Lim => m_ibBufferLim;
                public byte[] Bytes => m_rgb;

                /*----------------------------------------------------------------------------
                	%%Function: Buffer
                	%%Qualified: TCore.ListenAz.Stage2.ListenSyncFile.Buffer.Buffer
                	%%Contact: rlittle
                	
                    create a new buffer on top of the given filestream
                ----------------------------------------------------------------------------*/
                public Buffer(FileStream stm, long ibFileLim)
                {
                    m_stm = stm;
                    m_ibFileLim = ibFileLim;
                }

                /*----------------------------------------------------------------------------
                	%%Function: FillBuffer
                	%%Qualified: TCore.ListenAz.Stage2.ListenSyncFile.Buffer.FillBuffer
                	%%Contact: rlittle
                	
                    ibStart is where we should start filling the buffer at (presumable
                    everything before ibStart should be untouched by us because its been
                    prefilled)
                ----------------------------------------------------------------------------*/
                public bool FillBuffer(int ibStart)
                {
                    if (m_stm.Position >= m_ibFileLim)
                        return false;

                    long cbToRead = Math.Min(1024 - ibStart, m_ibFileLim - m_stm.Position);

                    if (cbToRead != (int)cbToRead)
                        throw new Exception("read overflow");

                    int cbRead = m_stm.Read(m_rgb, ibStart, (int)cbToRead);

                    if (cbRead != cbToRead)
                        throw new Exception("read failure");

                    m_ibBufferStart = ibStart;
                    m_ibBufferLim = ibStart + cbRead;

                    return true;
                }

                /*----------------------------------------------------------------------------
                	%%Function: SetPos
                	%%Qualified: TCore.ListenAz.Stage2.ListenSyncFile.Buffer.SetPos
                	%%Contact: rlittle
                	
                    set the seek position
                ----------------------------------------------------------------------------*/
                public void SetPos(int ib)
                {
                    m_ibBufferStart = ib;
                }
            }

            /*----------------------------------------------------------------------------
            	%%Function: SwapBuffer
            	%%Qualified: TCore.ListenAz.Stage2.ListenSyncFile.SwapBuffer
            	%%Contact: rlittle
            	
                swap the current buffer with the other buffer (and fill the new buffer)
                make sure we copy from ibPreserve to the end of the buffer to ensure we 
                have a contiguous token
            ----------------------------------------------------------------------------*/
            bool SwapBuffer(int ibPreserve)
            {
                int ibDest = 0;

                if (ibPreserve != -1)
                {
                    // they are requesting that some portion of the current buffer
                    // be moved into the swap buffer when we read it -- this way
                    // we can have a token span a buffer boundary (though not be
                    // larger than a single buffer)
                    int ibCopy = ibPreserve;
                    while (ibCopy < BufferCurrent.Lim)
                        BufferOther.Bytes[ibDest++] = BufferCurrent.Bytes[ibCopy++];
                }

                if (!BufferOther.FillBuffer(ibDest))
                    return false;

                m_fUseSwapBuffer = !m_fUseSwapBuffer;

                return true;
            }

            /*----------------------------------------------------------------------------
            	%%Function: ReadLine
            	%%Qualified: TCore.ListenAz.Stage2.ListenSyncFile.ReadLine
            	%%Contact: rlittle
            	
                read a line from the ListenSyncFile. return the string, or null
                if we are out of lines to read.  we will always return a line at
                the end of the buffer, even if its not terminated with a line ending
            ----------------------------------------------------------------------------*/
            public string ReadLine()
            {
                if (BufferCurrent.Start >= BufferCurrent.Lim)
                {
                    if (!SwapBuffer(-1)) // nothing to preserve, just fill the buffer
                        return null;
                }

                // start reading the line from the current position in the current buffer
                int ib = BufferCurrent.Start;
                int ibLineStart = ib;
                bool fLookingForLF = false;
                
                while (true)
                {
                    byte b = BufferCurrent.Bytes[ib];

                    if (b == 0x0a)
                    {
                        // we're done. If we were looking for it, great. if not, no matter, we're still done...
                        BufferCurrent.SetPos(ib + 1);
                        int cbLineEndingAdjust = fLookingForLF ? 1 : 0;

                        // remember we don't want the line ending as part of the string we construct. Since ib hasn't been adjusted
                        // for this character, the only thing we have to worry about is if there was a leading CR
                        return Encoding.UTF8.GetString(BufferCurrent.Bytes, ibLineStart, ib - ibLineStart - cbLineEndingAdjust);
                    }

                    if (fLookingForLF)
                    {
                        // was looking for a matching LF, but didn't find. must be a naked LF
                        // push back this character (or rather,just don't eat it)
                        BufferCurrent.SetPos(ib);

                        // remember to chop off the LF
                        return Encoding.UTF8.GetString(BufferCurrent.Bytes, ibLineStart, ib - ibLineStart - 1);
                    }

                    if (b == 0x0d)
                    {
                        fLookingForLF = true;
                    }
                    // otherwise, keep going forward

                    ib++;
                    if (ib >= BufferCurrent.Lim)
                    {
                        if (ibLineStart == 0)
                        {
                            // hmm, we have the entire buffer to ourselves, but no line ending was
                            // found. just invent a break here
                            return Encoding.UTF8.GetString(BufferCurrent.Bytes, ibLineStart, ib - ibLineStart);
                            // the next time they call ReadLine, it will fill the next buffer
                        }

                        if (!SwapBuffer(ibLineStart))
                        {
                            // couldn't fill the next buffer, so we are out of space...just return what we have
                            return Encoding.UTF8.GetString(BufferCurrent.Bytes, ibLineStart, ib - ibLineStart);
                        }

                        // otherwise, we are good to go.
                        // the new buffer has all the stuff we already parsed between [ibLineStart and ib)
                        // so rebase them all such that ibLineStart is now 0
                        ib -= ibLineStart;
                        ibLineStart = 0;

                        if (ib >= BufferCurrent.Lim)
                            throw new Exception("internal state failure");
                    }
                }
            }

            /*----------------------------------------------------------------------------
            	%%Function: Position
            	%%Qualified: TCore.ListenAz.Stage2.ListenSyncFile.Position
            	%%Contact: rlittle
            	
            ----------------------------------------------------------------------------*/
            public long Position()
            {
                return m_fs.Position;
            }

            /*----------------------------------------------------------------------------
            	%%Function: Close
            	%%Qualified: TCore.ListenAz.Stage2.ListenSyncFile.Close
            	%%Contact: rlittle
            	
            ----------------------------------------------------------------------------*/
            public void Close()
            {
                m_fs.Close();
                m_fs.Dispose();
                m_fs = null;
            }
        }
        #endregion


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


