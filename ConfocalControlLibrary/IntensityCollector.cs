using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConfocalControlLibrary
{

    //Notes
    //Periodic spikes in the counts, need to insert a parameter to record last data point in datacount use that as the differential
    public class IntensityCollector : IDisposable
    {
        #region constructor
        public IntensityCollector(IntensityReader reader, int collectorSize, double integrationMiliseconds)
            : this(reader, collectorSize, TimeSpan.FromMilliseconds(integrationMiliseconds))
        {
        }

        public IntensityCollector(IntensityReader reader, int collectorSize, TimeSpan intergrationTime)
        {
            CollectorSize = collectorSize;
            IntergrationSpan = intergrationTime;
            Reader = reader;
            DataCollector = new Queue<double>();
            DoAveraging = false;

            Reader.OnRead += Reader_OnRead;
        }

        ~IntensityCollector()
        {
        }

        #endregion

        #region properties
        public double SamplingFrequency { get { return Reader.SamplingFrequency; } }

        public TimeSpan IntergrationSpan { get; private set; }

        public bool DoAveraging { get; set; }

        public int CollectorSize { get; private set; }

        private Queue<double> m_dataCollector;

        public Queue<double> DataCollector
        {
            get { return m_dataCollector; }
            set { m_dataCollector = value; }
        }

        public IntensityReader Reader { get; private set; }
        #endregion

        #region calculation
        protected double accumulator = 0;
        protected int lastTickCount = 0;
        protected double LastDataPoint = 0;

        double m_curData = 0;
        int m_curDataAvgCount = 0;

        private void Reader_OnRead(object sender, IntensityReader.DataChunk e)
        {
            // how many measurement values we have.
            int count = e.IsCounterData ? e.CounterData.GetLength(1) : e.AnalogData.GetLength(1);
            int avgCount = Convert.ToInt32(Math.Floor(IntergrationSpan.TotalSeconds * Reader.SamplingFrequency));
            
            lock (m_dataCollector)
            {
                for (int i = 0; i < count; i++)
                {
                    m_curData += e.CounterData[0, i];
                    m_curDataAvgCount += 1;
                    if (m_curDataAvgCount >= avgCount)
                    {
                        EnqueueNext(m_curData * Reader.SamplingFrequency / m_curDataAvgCount);
                        m_curData = 0;
                        m_curDataAvgCount = 0;
                    }
                }
            }

            LastDataPoint = e.CounterData[0, count - 1];
        }

        private void EnqueueNext(double data)
        {
            DataCollector.Enqueue(data);
            if (DataCollector.Count > CollectorSize)
            {
                DataCollector.Dequeue();
            }
        }
        #endregion

        #region return collection
        /// <summary>
        /// returns the current data.
        /// </summary>
        /// <returns></returns>
        public double[] GetData()
        {
            double[] data;
            lock (m_dataCollector)
            {
                data = m_dataCollector.ToArray();
            }
            return data;
        }

        /// <summary>
        /// CODE TEST: returns the current data in the form of a list.
        /// </summary>
        /// <returns></returns>
        public List<double> GetDataAsList()
        {
            return DataCollector.ToList();
        }

        public void Dispose()
        {
        }

        #endregion
    }
}

