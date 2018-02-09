using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NationalInstruments.DAQmx;


namespace ConfocalControlLibrary
{
    public class IntensityReader : IDisposable
    {
        //Notes:
        // If needed add a property for the counter terminal located in Methods
        // Clean up counter clock for full functionality
        //Set Counter Clock to a faster time and correct samplesperreadtick
        
        #region Constructor

        public const double DefaultSamplingFrequency = 5000;
        public const string DefaultCounterInputTerminal = "/Card/PFI0";

        /// <summary>
        /// When working with channel name
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="counterInputTerminal"></param>
        /// <param name="isCounter"></param>
        /// <param name="samplingFrequency"></param>
        public IntensityReader(
            string channelName,
            bool isCounter,
            double samplingFrequency = IntensityReader.DefaultSamplingFrequency, 
            string counterInputTerminal = IntensityReader.DefaultCounterInputTerminal,
            string clockChannelName = "/card/ctr1",
            string timeBaseTerminal = "/Card/PFI14",
            int readDurationUs = 100000)
        {
            if (readDurationUs < 10)
                readDurationUs = 10;

            IsCounter = isCounter;
            ChannelName = channelName;
            ClockChannelName = clockChannelName;
            TimeBaseTerminal = timeBaseTerminal;
            InputTerminal = counterInputTerminal;
            SamplingFrequency = samplingFrequency;

            ReadDuration = TimeSpan.FromMilliseconds(readDurationUs * 1.0 / 1000);
        }

        ~IntensityReader()
        {

            try { Dispose(); } catch { };
        }

        #endregion

        #region properties

        /// <summary>
        /// True if the reader is counter based.
        /// </summary>
        public bool IsCounter { get; private set; }

        /// <summary>
        /// the channel name for the reader.
        /// </summary>
        public string ChannelName { get; private set; }

        /// <summary>
        /// The read clock channel name.
        /// </summary>
        public string ClockChannelName { get; private set; }

        private Task m_readerTask = null;

        /// <summary>
        /// DAQmx task that holds the input channels
        /// </summary>
        public Task ReaderTask
        {
            get { return m_readerTask; }
            private set { m_readerTask = value; }
        }

        private Task m_TimeBaseTask = null;

        /// <summary>
        /// Daqmx task that holds  digial clock for Time base
        /// </summary>
        public Task TimeBaseTask
        {
            get { return m_TimeBaseTask; }
            private set { m_TimeBaseTask = value; }
        }

        /// <summary>
        /// Wait handler for the reader.
        /// </summary>
        protected System.Threading.ManualResetEvent ReaderWaitHandle { get; private set; }

        /// <summary>
        /// sampling frequency for input channel
        /// </summary>
        public double SamplingFrequency { get; private set; }

        /// <summary>
        /// The reader for the analog channels.
        /// </summary>
        public AnalogMultiChannelReader AnalogReader { get; private set; }

        /// <summary>
        /// The reader for the counter channels.
        /// </summary>
        public CounterMultiChannelReader CounterReader { get; private set; }

        /// <summary>
        /// The duration of the read.
        /// </summary>
        public TimeSpan ReadDuration { get; set; }

        /// <summary>
        /// The timebase output terminal.
        /// </summary>
        public string TimeBaseTerminal { get; set; }

        /// <summary>
        /// Sets the input terminal for the counter.
        /// </summary>
        public string InputTerminal { get; set; }

        /// <summary>
        /// The event called when the data is read.
        /// </summary>
        public event EventHandler<DataChunk> OnRead;
        
        /// <summary>
        /// The last data chunk that was read by the reader.
        /// </summary>
        public uint[] LastReadDataChunk { get; private set; }

        /// <summary>
        /// Ticks read since last reader start.
        /// </summary>
        public long ReadTicks { get; private set; }

        /// <summary>
        /// The time offset since the start of the read.
        /// </summary>
        public TimeSpan TimeOffsetSinceStart { get { return TimeSpan.FromSeconds(ReadTicks * 1.0 / SamplingFrequency); } }


        /// <summary>
        /// Reader is reading?
        /// </summary>
        public bool IsReading { get; private set; }

        /// <summary>
        /// The number of samples per read tick.
        /// </summary>
        public int SamplesPerReadTick { get { return Convert.ToInt32(Math.Floor(ReadDuration.TotalSeconds * SamplingFrequency)); } }

        /// <summary>
        /// The current reead result
        /// </summary>
        protected IAsyncResult CurReadWaitResult { get; private set; }

        private int m_Timeout = 10000;

        /// <summary>
        /// The timeout for the Task activity. (Default 10000);
        /// </summary>
        public int Timeout
        {
            get { return m_Timeout; }
            set { m_Timeout = value; }
        }

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

        #endregion

        #region Reading methods

        public void TareTimer()
        {
            ReadTicks = 0;
        }

        /// <summary>
        /// Starts the reader and redefined the task
        /// </summary>
        public void Start()
        {
            if (IsReading)
                return;

            // create the reader and the channels.
            CreateReadTask();
            CreateTimebaseTask();

            // calling the read.
            ReadData();

            // reading.
            if (ReaderTask.IsDone)
                ReaderTask.Start();
            if (TimeBaseTask.IsDone)
                TimeBaseTask.Start();

            IsReading = true;
        }

        /// <summary>
        /// Stops the read.
        /// </summary>
        public void Stop()
        {

            if ( IsReading && CurReadWaitResult != null && !CurReadWaitResult.IsCompleted &&
                (CounterReader != null || AnalogReader != null))
            {

                try
                {
                    if (IsCounter)
                        CounterReader.EndReadMultiSampleUInt32(CurReadWaitResult);
                    else AnalogReader.EndReadMultiSample(CurReadWaitResult);
                }
                catch(Exception ex)
                {
                    /// attempt to end he read failed.
                }

                // clear the async result.
                CurReadWaitResult = null;
            }
            IsReading = false;

            DestroyReaderTask();
            DestroyTimebaseTask();
        }

        private void DestroyReaderTask()
        {
            // need to configure the reader.
            if (ReaderTask != null)
            {
                // Disposing of the current task.
                Task t = ReaderTask;
                ReaderTask = null;
                t.FullDestroyTask();
            }

            // Destroy the readers if any.
            CounterReader = null;
            AnalogReader = null;
            GC.Collect(0);
        }

        /// <summary>
        /// Destroyes the clock task.
        /// </summary>
        private void DestroyTimebaseTask()
        {
            if (TimeBaseTask != null)
            {
                Task t = TimeBaseTask;
                TimeBaseTask = null;
                t.FullDestroyTask();
            }

            GC.Collect(0);
        }

        /// <summary>
        /// creates a new read channel suckaz
        /// </summary>
        /// <param name="channelName"></param>
        /// <param name="isCounter"></param>
        public void CreateReadTask()
        {
            DestroyReaderTask();

            ReaderWaitHandle = new System.Threading.ManualResetEvent(true);
            ReaderTask = new Task("IntensityReader");
            ReaderTask.SynchronizeCallbacks = SynchronizeCallbacks;
            ReaderTask.Stream.Timeout = Timeout;


            // Creates Input Channel task after checking channel type
            string cname = new String(ChannelName.Where(Char.IsLetter).ToArray());
            if (!IsCounter)
            {
                this.ReaderTask.AIChannels.CreateVoltageChannel(ChannelName, "IntensityReader " + cname,
                    AITerminalConfiguration.Differential, -10, 10, AIVoltageUnits.Volts);

                AnalogReader = new AnalogMultiChannelReader(ReaderTask.Stream);
                AnalogReader.SynchronizeCallbacks = SynchronizeCallbacks;
            }
            else
            {
                this.ReaderTask.CIChannels.CreateCountEdgesChannel(ChannelName, "CounterReader " + cname,
                    CICountEdgesActiveEdge.Rising, 10, CICountEdgesCountDirection.Up);

                this.ReaderTask.CIChannels.All.CountEdgesTerminal = InputTerminal;
                this.ReaderTask.CIChannels.All.CountEdgesCountResetTerminal = "/CARD/PFI0";

                CounterReader = new CounterMultiChannelReader(ReaderTask.Stream);
                CounterReader.SynchronizeCallbacks = SynchronizeCallbacks;
            }

            LastReadDataChunk = null;
            // configureing the clock.
            ReaderTask.Timing.ConfigureSampleClock(TimeBaseTerminal,
                SamplingFrequency, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples);
            ReaderTask.Control(TaskAction.Verify);
        }

        /// <summary>
        /// Creates the clock tasks that tells when to read the data.
        /// </summary>
        public void CreateTimebaseTask()
        {
            DestroyTimebaseTask();

            TimeBaseTask = new Task("IntensityReaderTimebase");
            double Delay = 0;
            double HighTime = .0000001;

            TimeBaseTask.COChannels.CreatePulseChannelTime(ClockChannelName,
                "TimeBaseChannel", COPulseTimeUnits.Seconds, COPulseIdleState.Low, Delay, 1 / (SamplingFrequency) - HighTime, HighTime);

            this.TimeBaseTask.COChannels.All.PulseTerminal = TimeBaseTerminal;
            TimeBaseTask.Timing.ConfigureImplicit(SampleQuantityMode.ContinuousSamples);

            TimeBaseTask.SynchronizeCallbacks = SynchronizeCallbacks;
            TimeBaseTask.Control(TaskAction.Verify);
        }

        #endregion

        #region data queue

        /// <summary>
        /// The data q.
        /// </summary>
        protected Queue<DataChunk> m_dataQ = new Queue<DataChunk>();

        public class DataChunk : EventArgs
        { 
            public DataChunk(TimeSpan offset, double[,] analogData)
            {
                ReadStartOffset = offset;
                AnalogData = analogData;
            }

            public DataChunk(TimeSpan offset, uint[,] counterData)
            {
                ReadStartOffset = offset;
                CounterData = counterData;
            }

            /// <summary>
            /// TimeStamp for Data Read
            /// </summary>
            public TimeSpan ReadStartOffset { get; private set; }

            
            protected double[,] m_AnalogData;

            /// <summary>
            /// Data Chunk if Analog
            /// </summary>
            public double[,] AnalogData
            {
                get { return m_AnalogData; }
                set { m_AnalogData = value; }
            }

            protected uint[,] m_CounterData;

            /// <summary>
            /// Data Chunk if Counter
            /// </summary>
            public uint[,] CounterData
            {
                get { return m_CounterData; }
                set { m_CounterData = value; }
            }

            /// <summary>
            /// If true, then the data sent was counter data.
            /// </summary>
            public bool IsCounterData { get { return CounterData != null; } }

            public int DataCount { get { return IsCounterData ? CounterData.GetLength(1) : AnalogData.GetLength(1); } }
        }


        System.Threading.Tasks.Task m_queueProcessing = null;
        protected virtual void DoQueueProcessing()
        {
            if (m_queueProcessing != null && m_queueProcessing.Status == System.Threading.Tasks.TaskStatus.Running
                || m_dataQ.Count == 0)
                return;

            m_queueProcessing = System.Threading.Tasks.Task.Run(() =>
            {
                while (m_dataQ.Count > 0)
                {
                    //Console.WriteLine("Chunky " + m_dataQ.Peek());
                    DataChunk chunk = m_dataQ.Dequeue();
                    if (chunk.IsCounterData)
                    {
                        // need to correct counter data structure.
                        ConvertCounterContinuesToCountsPerTick(chunk.CounterData);
                    }

                    if (OnRead != null)
                    {
                        OnRead(this, chunk);
                    }
                }
            });
        }

        private void ConvertCounterContinuesToCountsPerTick(uint[,] data)
        {
            // Saving the lvalues.
            uint[] lvalues = new uint[data.GetLength(0)];

            // j=1..4. So fast enouph.
            for (int j = 0; j < data.GetLength(0); j++)
                lvalues[j] = data[j, data.GetLength(1) - 1];

            //bool hasLastValues = m_lastRawValuesCounterData == null;
            uint lastV = 0;
            int dataLen = data.GetLength(1);

            // now we need to load the values.
            for (int j = 0; j < data.GetLength(0); j++)
            {
                // number of read channels.
                lastV = LastReadDataChunk == null ? data[j, 0] : LastReadDataChunk[j];

                // corret the first value to counts.
                for (int i = dataLen - 1; i > 0; i--)
                {
                    // read data channel at j.
                    data[j, i] = data[j, i] - data[j, i - 1];
                }

                data[j, 0] = data[j, 0] - lastV;
            }

            LastReadDataChunk = lvalues;
        }

        #endregion

        #region Data reading

        /// <summary>
        /// Async call to read the data.
        /// </summary>
        protected void ReadData()
        {
            if (IsCounter)
            {
                CurReadWaitResult = CounterReader.BeginReadMultiSampleUInt32(SamplesPerReadTick, OnDataRead, null);
            }
            else
            {
                CurReadWaitResult = AnalogReader.BeginReadMultiSample(SamplesPerReadTick, OnDataRead, null);
            }
        }

        /// <summary>
        /// On data was read.
        /// </summary>
        /// <param name="rslt"></param>
        protected void OnDataRead(IAsyncResult rslt)
        {
            if (!rslt.IsCompleted || !IsReading)
                return;

            DataChunk c = null;
            TimeSpan offset = TimeSpan.FromSeconds(ReadTicks * 1.0 / SamplingFrequency);

            if (IsCounter)
            {
                // data is counter.
                uint[,] data = CounterReader.EndReadMultiSampleUInt32(rslt);
                ReadTicks += data.GetLength(1);
                c = new DataChunk(offset, data);
            }
            else
            {
                // data is analog.
                double[,] data = AnalogReader.EndReadMultiSample(rslt);
                
                // adding to the read ticks.
                ReadTicks += data.GetLength(1);
                c = new DataChunk(offset, data);
            }

            // call async again.
            if (IsReading)
                ReadData();

            // No data.
            if (c.DataCount == 0)
                return;

            m_dataQ.Enqueue(c);

            // call to process the queue if needed.
            DoQueueProcessing();
        }

        #endregion

        #region Dispose

        /// <summary>
        /// Destroy and dispose all tasks.
        /// </summary>
        public void Dispose()
        {
            Stop();
            m_dataQ.Clear();
            while (m_queueProcessing.Status == System.Threading.Tasks.TaskStatus.Running)
            {
                System.Threading.Thread.Sleep(1);
            }

            OnRead = null;
        }

        #endregion
    }
}
