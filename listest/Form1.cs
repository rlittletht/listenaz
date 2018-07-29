using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using TCore.ListenAz;
using TCore.StatusBox;

namespace listest
{
    public partial class listest : Form
    {
        private TraceSource m_ts;
        private StatusBox m_sb;
        private listener m_listener;

        public listest()
        {
            InitializeComponent();

            m_sb = new StatusBox(m_reHook);
            m_ts = new TraceSource("listest");
            m_ts.Listeners.Add(m_listener = new listener(new ListenHook(m_sb)));
        }

        private void m_pbTestEvent_Click(object sender, EventArgs e)
        {
            TimeThis("SingleEvent",
                () =>
                {
                    m_ts.TraceEvent(TraceEventType.Critical, 1234, "{0}\t{1}\t{2}", Guid.NewGuid().ToString(), "Test",
                        DateTime.Now.ToString());
                });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (m_listener != null)
                m_listener.Terminate();
            base.OnClosing(e);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            TimeThis("ThreeEvents", () =>
            {
                m_ts.TraceEvent(TraceEventType.Critical, 1234, "{0}\t{1}\t{2}", Guid.NewGuid().ToString(),
                    "Test Event 1", DateTime.Now.ToString());
                m_ts.TraceEvent(TraceEventType.Critical, 1234, "{0}\t{1}\t{2}", Guid.NewGuid().ToString(),
                    "Test Event 2", DateTime.Now.ToString());
                m_ts.TraceEvent(TraceEventType.Critical, 1234, "{0}\t{1}\t{2}", Guid.NewGuid().ToString(),
                    "Test Event 3", DateTime.Now.ToString());
            });
        }

        private void button2_Click(object sender, EventArgs e)
        {
            m_listener.TestSuspend();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            m_listener.TestResume();
        }

        delegate void GenericFun();

        void TimeThis(string sAction, GenericFun fun)
        {
            MicroTimer timer = new MicroTimer();
            timer.Start();
            fun();
            timer.Stop();

            m_sb.AddMessage($"{sAction}: Elapsed {timer.ElapsedMsec()}");
        }
    }

    // ============================================================================
    // M I C R O  T I M E R
    // ============================================================================
    public class MicroTimer
    {
        private Stopwatch m_sw;

        private int m_msec;
        private int m_msecStop;

        public MicroTimer()
        {
            m_sw = new Stopwatch();
            m_sw.Start();

            m_msec = Environment.TickCount;
            m_msecStop = -1;
        }

        public void Reset()
        {
            m_sw.Reset();
        }

        public void Start()
        {
            m_sw.Start();
        }

        public void Stop()
        {
            m_sw.Stop();
            m_msecStop = Environment.TickCount;
        }

        public double Seconds()
        {
            return m_sw.ElapsedMilliseconds / 1000.0;
            // return (Environment.TickCount - m_msec) / 1000.0;
        }

        public long Microsec
        {
            get
            {
                return m_sw.ElapsedTicks / (Stopwatch.Frequency / (1000L * 1000L));
            }
        }

        public long Msec()
        {
            return m_sw.ElapsedMilliseconds;
            // return m_msecStop - m_msec;
        }

        public double MsecFloat
        {
            get
            {
                return ((double)Microsec) / 1000.0;
            }
        }
        public string Elapsed()
        {
            if (m_sw.IsRunning)
                m_sw.Stop();
            //if (m_msecStop == -1)
            //Stop();

            return String.Format("{0}", Seconds().ToString());
        }

        public string ElapsedMsec()
        {
            if (m_sw.IsRunning)
                m_sw.Stop();
            //if (m_msecStop == -1)
            //Stop();

            return Msec().ToString();
        }
    }

    public class ListenHook : listener.IHookListen
    {
        private StatusBox m_sb;

        public ListenHook(StatusBox sb)
        {
            m_sb = sb;
        }

        public void WriteLine(string sMessage)
        {
            m_sb.AddMessage(sMessage, StatusBox.MSGT.Body);
        }
    }
}
