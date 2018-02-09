using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;

namespace ConfocalControlLibrary
{
    public class PositionWriter : IDisposable 
    {
        #region constructor
        public PositionWriter(string xChannelName, string yChannelName,
            double samplingFrequency, int maxBufferSize = 10000)
        {
            // Setting up the task.
            XChanName = xChannelName;
            YChanName = yChannelName;
            this.SamplingFrequency = samplingFrequency;
        }

        ~PositionWriter()
        {
            try { Dispose(); } catch { };
        }
        
        #endregion

        #region properties

        /// <summary>
        /// The name of the x channel/
        /// </summary>
        public String XChanName { get; private set; }

        /// <summary>
        /// The name of the y channel.
        /// </summary>
        public String YChanName { get; private set; }

        /// <summary>
        /// The task for writing to the galvo.
        /// </summary>
        public  Task WriterTask { get; private set; }

        /// <summary>
        /// The sampling frequency for the writer.
        /// </summary>
        public double SamplingFrequency { get; private set; }

        /// <summary>
        /// Called before scan.
        /// </summary>
        public event EventHandler OnBeforeBeginScan;

        /// <summary>
        /// Called after scan.
        /// </summary>
        public event EventHandler OnAfterEndScan;

        /// <summary>
        /// True if currently writing.
        /// </summary>
        public bool IsWriting { get; private set; }

        /// <summary>
        /// True if waiting for writing to start (multi sample).
        /// </summary>
        public bool IsWaitingForWriteStartEvent { get; private set; }

        private bool m_SynchronizeCallbacks = true;
        /// <summary>
        /// If true, all task read actions will be called in the same thread as the task.
        /// Implicates the task stop/start. Default is false.
        /// </summary>
        public bool SynchronizeCallbacks
        {
            get { return m_SynchronizeCallbacks; }
            set { m_SynchronizeCallbacks = value; }
        }

        /// <summary>
        /// The current scan position.
        /// </summary>
        public long ScanPosition { get; private set; }

        /// <summary>
        /// The total number of scan ticks to be written.
        /// </summary>
        public long TotalScanTicks { get; private set; }

        /// <summary>
        /// The last errpr thrown.
        /// </summary>
        public Exception LastError { get; private set; }

        /// <summary>
        /// Ture if there is a last error.
        /// </summary>
        public bool IsErrored { get { return LastError != null; } }

        protected IAsyncResult WriteAsyncResult { get; private set; }

        /// <summary>
        /// The writer object;
        /// </summary>
        public AnalogMultiChannelWriter MCWriter { get; private set; }

        #endregion

        #region Position mode sttings

        int m_lastSampleCount = -1;
        void ConfigureTask(int sampleCount, bool forceReset = false)
        {
            if (LastError != null || sampleCount == m_lastSampleCount && !forceReset)
                return;

            LastError = null;

            m_lastSampleCount = sampleCount;

            DestroyWriterTask();

            WriterTask = new Task();
            WriterTask.AOChannels.CreateVoltageChannel(XChanName, "XChannel", -10, 10, AOVoltageUnits.Volts);
            WriterTask.AOChannels.CreateVoltageChannel(YChanName, "YChannel", -10, 10, AOVoltageUnits.Volts);
            WriterTask.Stream.Timeout = Convert.ToInt32(sampleCount / SamplingFrequency) * 1000 + 1000;
            
            WriterTask.SynchronizeCallbacks = SynchronizeCallbacks;

            if (sampleCount == 0)
                return;

            WriterTask.Timing.ConfigureSampleClock(String.Empty, SamplingFrequency, SampleClockActiveEdge.Rising,
                sampleCount == 1 ? SampleQuantityMode.HardwareTimedSinglePoint : SampleQuantityMode.FiniteSamples,
                sampleCount);

            //long bufferSize = sampleCount > 500000 ? 500000 : sampleCount; // 10% of sample count;
            //bufferSize = bufferSize <= 1 ? 1 : bufferSize;
            //if (bufferSize > 1)
            //{
            //    //WriterTask.Stream.ConfigureOutputBuffer(bufferSize);
            //}
            
            WriterTask.Control(TaskAction.Verify);
            
            if (sampleCount > 1)
            {
                WriterTask.Done += WriterTask_Done;
                //WriterTask.SampleClock += WriterTask_SampleClock;
            }

            MCWriter = new AnalogMultiChannelWriter(WriterTask.Stream);
            MCWriter.SynchronizeCallbacks = SynchronizeCallbacks;
        }

        private void DestroyWriterTask()
        {
            if (WriterTask != null)
            {
                Task t = WriterTask;
                WriterTask = null;
                t.FullDestroyTask();
                GC.Collect(0);
            }
        }


        #endregion

        #region Basic scan methods

        public void Abort()
        {
            if (WriterTask != null)
            {
                
                EndAsyncWriting();
                DestroyWriterTask();
                GC.Collect(0);
            }

            m_lastSampleCount = -1;
        }

        private void EndAsyncWriting()
        {
            if (WriteAsyncResult != null)
            {
                MCWriter.EndWrite(WriteAsyncResult);
                WriteAsyncResult = null;
            }
        }

        /// <summary>
        /// Sets the position of the galvo.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public virtual void SetPosition(double x, double y, bool async = false)
        {
            ConfigureTask(1);
            
            IsWriting = true;
            if(WriterTask.IsDone)
                WriterTask.Start();
            MCWriter.WriteSingleSample(false, new double[] { x, y });
            System.Threading.Thread.Sleep(10);
            IsWriting = false;
        }

        public virtual void WriteScan(double[,] positions, bool async = true,
            string triggerSource = null, DigitalEdgeStartTriggerEdge edge = DigitalEdgeStartTriggerEdge.Rising)
        {
            // Go to initial position. (before the scan starts);
            SetPosition(positions[0, 0], positions[0, 1]);

            // Confugyre the task and force recration of the task.
            ConfigureTask(positions.GetLength(1) * positions.GetLength(0), true);

            // If there is a trigger source then we are currently waiting to writer trigger.
            if (triggerSource != null)
            {
                WriterTask.Triggers.StartTrigger.ConfigureDigitalEdgeTrigger(triggerSource, edge);
                IsWaitingForWriteStartEvent = false;
                IsWriting = true;
            }
            else
            {
                IsWaitingForWriteStartEvent = true;
                IsWriting = false;
            }

            // define the start action.
            TotalScanTicks = positions.GetLength(1);

            MCWriter.WriteMultiSample(false, positions);
            //MCWriter.BeginWriteMultiSample(false, positions, (IAsyncResult rslt) =>
            //{
            //    //CallWriteScanEnd();
            //}, null);

            if (WriterTask.IsDone)
                WriterTask.Start();

            CallStartScanEvent();

            if (triggerSource != null)
                CallStartScanEvent();

            if (!async)
            {
                while (IsWriting || IsWaitingForWriteStartEvent)
                    System.Threading.Thread.Sleep(10);
            }
        }

        private void WriterTask_SampleClock(object sender, SampleClockEventArgs e)
        {
            try
            {
                if (IsWaitingForWriteStartEvent)
                {
                    IsWaitingForWriteStartEvent = false;
                    IsWriting = true;
                    CallStartScanEvent();
                    return;
                }

                if (!IsWriting)
                    return;

                if (!WriterTask.IsDone)
                    ScanPosition = WriterTask.Stream.TotalSamplesGeneratedPerChannel;

                if (ScanPosition >= TotalScanTicks)
                {
                    CallWriteScanEnd();
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
            }
        }

        private void WriterTask_Done(object sender, TaskDoneEventArgs e)
        {
            try
            {
                if (!IsWriting)
                    return;


                CallWriteScanEnd();
            }
            catch(Exception ex)
            {
                LastError = ex;
            }
            
        }

        private void CallWriteScanEnd()
        {
            IsWriting = false;

            DestroyWriterTask();

            // call end measurement.
            if (OnAfterEndScan != null)
                OnAfterEndScan(this, null);
        }

        private void CallStartScanEvent()
        {
            // call start measurment.
            if (OnBeforeBeginScan != null)
                OnBeforeBeginScan(this, null);
        }

        public void Dispose()
        {
            try
            {
                DestroyWriterTask();
            }
            catch
            {
            }
        }

        #endregion

    }

}
