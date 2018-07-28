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

    public partial class listest : Form
    {
        private TraceSource m_ts;
        private StatusBox m_sb;

        public listest()
        {
            InitializeComponent();

            m_sb = new StatusBox(m_reHook);
            m_ts = new TraceSource("listest");
            m_ts.Listeners.Add(new listener(new ListenHook(m_sb)));
        }

        private void m_pbTestEvent_Click(object sender, EventArgs e)
        {
            m_ts.TraceEvent(TraceEventType.Critical, 1234, "{0}\t{1}\t{2}", Guid.NewGuid().ToString(), "Test", DateTime.Now.ToString());
        }
    }
}
