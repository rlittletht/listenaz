using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        private ProducerConsumer m_prodcons;
        private Producer m_prod; // why have both? I could just have prodcons be able to provide the prod pointer

        public listener(IHookListen ihl = null)
        {
            m_sb = new StringBuilder();
            m_ihl = ihl;
            m_prodcons = new ProducerConsumer(ihl);
            m_prod = m_prodcons.Start();
        }

        public void Terminate()
        {
            m_prodcons.Stop();
        }

        public void TestSuspend()
        {
            m_prodcons.TestSuspendConsumerThread();
        }

        public void TestResume()
        {
            m_prodcons.TestResumeThread();
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
            m_prod.RecordEvent(TraceEventType.Information, m_sb.ToString());
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
