using System;
using System.IO;
using System.Text;

namespace TCore.ListenAz
{
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

                if (cbToRead != (int) cbToRead)
                    throw new Exception("read overflow");

                int cbRead = m_stm.Read(m_rgb, ibStart, (int) cbToRead);

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
                    return Encoding.UTF8.GetString(BufferCurrent.Bytes, ibLineStart,
                        ib - ibLineStart - cbLineEndingAdjust);
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
}