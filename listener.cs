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

        private Stage1 m_stage1;
        private Stage2 m_stage2;

        class StageListener : IHookListen
        {
            private IHookListen m_ihlBase;
            private string m_sStageName;

            public StageListener(IHookListen ihlBase, string sStageName)
            {
                m_ihlBase = ihlBase;
                m_sStageName = sStageName;
            }

            public void WriteLine(string sText)
            {
                m_ihlBase.WriteLine($"{m_sStageName}: {sText}");
            }
        }

        public listener(IHookListen ihl = null)
        {
            m_sb = new StringBuilder();
            m_ihl = ihl;
            m_stage2 = new Stage2(new StageListener(ihl, "Stage2"));
            m_stage1 = new Stage1(new StageListener(ihl, "Stage1"), m_stage2);
        }

        public void Terminate()
        {
            m_stage1.Stop();
            m_stage2.Stop();
        }

        public void TestSuspend()
        {
            m_stage1.TestSuspendConsumerThread();
        }

        public void SetDebugDateOffset(int n)
        {
            m_stage1.SetTestOffsetMinutesDateTime(n);
        }
        public void TestResume()
        {
            m_stage1.TestResumeConsumerThread();
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
            m_stage1.RecordNewListenRecord(m_sb.ToString());
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
