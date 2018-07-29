using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCore.Pipeline;

namespace TCore.ListenAz
{
    public class listener : TraceListener
    {
        public interface IHookListen
        {
            void WriteLine(string sText);
        }

        private StringBuilder m_sb;
        private IHookListen m_ihl;
        private ProducerConsumer<ListenRecord> m_pipeHot;

        public listener(IHookListen ihl = null)
        {
            m_sb = new StringBuilder();
            m_ihl = ihl;
            m_pipeHot = new ProducerConsumer<ListenRecord>((string sMessage) =>
            {
                ihl.WriteLine(sMessage);
            });
        }

        public void Terminate()
        {
            m_pipeHot.Stop();
        }

        public void TestSuspend()
        {
            m_pipeHot.TestSuspendConsumerThread();
        }

        public void TestResume()
        {
            m_pipeHot.TestResumeThread();
        }

        void InternalWrite(string sCategory, string sMessage)
        {
            m_sb.Append($"{sCategory}\t{sMessage}");
        }

        void InternalWriteLine(string sCategory, string sMessage)
        {
            InternalWrite(sCategory, sMessage);
            InternalFlushLine();
        }

        void InternalFlushLine()
        {
            m_pipeHot.Producer.QueueRecord(new ListenRecord(TraceEventType.Information, m_sb.ToString()));
            m_sb.Clear();
        }

        public override void Write(string sMessage)
        {
            InternalWrite("<unknown>", sMessage);
        }

        public override void Write(string sCategory, string sMessage)
        {
            InternalWrite(sCategory, sMessage);
        }

        public override void WriteLine(string sMessage)
        {
            InternalWriteLine("<unknown>", sMessage);
        }

        public override void WriteLine(string sCategory, string sMessage)
        {
            InternalWriteLine(sCategory, sMessage);
        }
    }
}
