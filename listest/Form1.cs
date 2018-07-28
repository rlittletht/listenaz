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

namespace listest
{
    public partial class Form1 : Form
    {
        private TraceSource m_ts;

        public Form1()
        {
            InitializeComponent();
            m_ts = new TraceSource("listest");
            for (int i = 0; i < Trace.Listeners.Count; i++)
            {
                m_ts.Listeners.Add(System.Diagnostics.Trace.Listeners[i]);
            }
        }

        private void m_pbTestEvent_Click(object sender, EventArgs e)
        {
            m_ts.TraceEvent(TraceEventType.Critical, 1234, "{0}\t{1}\t{2}", Guid.NewGuid().ToString(), "Test", DateTime.Now.ToString());
        }
    }
}
