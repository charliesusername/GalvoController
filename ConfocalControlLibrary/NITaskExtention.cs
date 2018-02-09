using NationalInstruments.DAQmx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConfocalControlLibrary
{
    public static class NITaskExtention
    {
        public static void StopAndDispose(this Task t)
        {
            t.Stop();
            t.Control(TaskAction.Abort);
            t.Control(TaskAction.Verify);
            t.Control(TaskAction.Unreserve);
            t.Control(TaskAction.Verify);
            t.Dispose();

            GC.Collect(0);
        }

        // attemps to stop and dispose.
        public static bool TryStopAndDispose(this Task t)
        {
            try
            {
                t.StopAndDispose();
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public static void FullDestroyTask(this Task t, int timeout=5000)
        {
            DateTime started = DateTime.Now;
            while(true)
            {
                if (t.TryStopAndDispose())
                    break;
                else if ((DateTime.Now - started).TotalMilliseconds > timeout)
                    throw new Exception("Could not destroy working task.");
                System.Threading.Thread.Sleep(10);
            }
        }
    }
}
