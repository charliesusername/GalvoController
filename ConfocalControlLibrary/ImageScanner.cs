using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfocalControlLibrary
{
    public class ImageScanner
    {
        #region constructor

        public ImageScanner(IntensityReader reader, PositionWriter writer)
        {
            Reader = reader;
            Writer = writer;

            LastRecordingTime = DateTime.Now;

            Reader.OnRead += Reader_OnRead;
            Writer.OnAfterEndScan += Writer_OnAfterWriteEnd;
            Writer.OnBeforeBeginScan += Writer_OnBeforeBeginWrite;

            Image = new double[0, 0];
        }
        #endregion

        #region properties

        public IntensityReader Reader { get; private set; }
        public PositionWriter Writer { get; private set; }
        public bool IsScanning { get { return WaitingForScan || IsRecording; } }
        public bool WaitingForScan { get; private set; }
        public bool IsRecording { get; private set; }
        protected Scan CurrentScan { get; private set; }
        public DateTime ScanStartTime { get; private set; }
        public TimeSpan ScanStartReaderTimeOffset { get; private set; }

        /// <summary>
        /// The image to display.
        /// </summary>
        public double[,] Image { get; private set; }

        public event EventHandler OnComplete;

        public bool ScanTimerError { get; private set; }

        public DateTime LastRecordingTime { get; private set; }

        protected UInt32 LastDataPoint = 0;
        public int CurStreamIndex { get; private set; }

        public int MaxStreamIndex { get { return CurrentScan == null ? 0 : CurrentScan.TicksPerPixel * CurrentScan.XPixels * CurrentScan.YPixels; } }
        public double Completed { get { return CurrentScan == null ? 0 : 100.0 * CurStreamIndex / MaxStreamIndex; } }
        public TimeSpan Remaining { get { return CurrentScan == null ? new TimeSpan() : TimeSpan.FromSeconds((MaxStreamIndex - CurStreamIndex) * 1.0 / Writer.SamplingFrequency); } }

        #endregion

        #region Scan methods

        public void Abort()
        {
            Writer.Abort();
            IsRecording = false;
            WaitingForScan = false;
        }

        public virtual void Scan(Scan scan)
        {
            if (IsScanning)
                return;

            IsRecording = false;
            WaitingForScan = true;

            System.Threading.Tasks.Task.Run(() =>
            {
                // Waiting for the scan to be valid.
                if (!scan.IsValid)
                {
                    scan.Validate();
                    while (!scan.IsValid)
                    {
                        System.Threading.Thread.Sleep(1);
                    }
                }

                // Set parameters.
                CurrentScan = scan;
                Image = new double[scan.XPixels, scan.YPixels];

                scan.SamplingFrequency = Writer.SamplingFrequency;
                CurStreamIndex = -Convert.ToInt32(Math.Floor(CurrentScan.TriggerDelay * Writer.SamplingFrequency / 1000));

                // Stop the reader and make the reader wait for trigger.
                Reader.Stop();
                Reader.TareTimer(); // set current time to zero.

                // Write the scan.
                //Writer.WriterTask.Stream.Buffer.
                Writer.WriteScan(scan.Positions, true, Reader.TimeBaseTerminal);

                //System.Threading.Thread.Sleep(100);

                // called before the scan begins.
                InitScanParameters();

                // start the trigger. Wait 1ms and stop it.
                Reader.Start();
            });
        }

        private void Writer_OnBeforeBeginWrite(object sender, EventArgs e)
        {
            //InitScanParameters();
        }

        private void InitScanParameters()
        {
            // called before the scan begins.
            IsRecording = true;
            WaitingForScan = false;

            ScanStartReaderTimeOffset = Reader.TimeOffsetSinceStart;
            ScanStartTime = DateTime.Now;// + TimeSpan.FromMilliseconds(CurrentScan.DwellTime_in_MS * 9);
        }

        private void Writer_OnAfterWriteEnd(object sender, EventArgs e)
        {
            // this should be called when the measurement has ended. If still is scanning then we have an error.
            if(IsScanning)
            {
                Abort();
                System.Threading.Thread.Sleep(10);
                ScanTimerError = true;
            }
        }

        private void Reader_OnRead(object sender, IntensityReader.DataChunk data)
        {
            if (!IsScanning) return;

            if (WaitingForScan)
                return;

            // Calculate the time of the start using the current reader count.
            double seconds = (data.ReadStartOffset - ScanStartReaderTimeOffset).TotalSeconds;

            // Error check.
            if (seconds < 0)
            {
                ScanTimerError = true;
                return;
            }

            ScanTimerError = false;

            // end error check.

            for (int i = 0; i < data.DataCount; i++)
            {
                //double currentSecond = secs + i / CurrentScan.SamplingFrequency;
                //double timePerPixel = CurrentScan.TicksPerPixel / CurrentScan.SamplingFrequency;
                CurStreamIndex += 1;
                if (CurStreamIndex < 0)
                    continue;

                int xyIndex = CurStreamIndex/CurrentScan.TicksPerPixel;
                if (xyIndex >= CurrentScan.YPixels * CurrentScan.XPixels)
                {
                    if (!IsScanning)
                        return;

                    WaitingForScan = false;
                    IsRecording = false;
                    Abort();
                    Task.Run(() =>
                    {
                        if (OnComplete != null)
                            OnComplete(this, null);
                    });

                    if (CurrentScan.SetXYVolAfterScan)
                    {
                        Writer.SetPosition(CurrentScan.XVolAfterScan, CurrentScan.YVolAfterScan);
                    }

                    return;
                }
               
                //int xidx = xyIndex % CurrentScan.YPixels;
                //int yidx = (xyIndex - xidx) / CurrentScan.XPixels;
                int xidx = 0;
                int yidx = 0;
                CurrentScan.PositionFromPixelIndex(xyIndex, out xidx, out yidx);

                Image[xidx, yidx] += data.IsCounterData ? data.CounterData[0, i] : data.AnalogData[0, i];
                LastRecordingTime = DateTime.Now;
                //need to insert a parameter to record last data point in datacount use that as the differential for the next buffer reading
            }
        }

        #endregion
    }

}
