using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tester
{
    class Program
    {
        #region Main

        static void Main(string[] args)
        {
            
            AddMenuItem("Zavs Class Tester", ClassTester);
            AddMenuItem("CounterTester", CounterTester);
            AddMenuItem("AnanlogTester", AnanlogTester);
            AddMenuItem("Test Scan", DoScanTest);
            AddMenuItem("Test user defined scan", DoUserDefinedScan);
            AddMenuItem("PointSelectTester", PointSelectTester);
            AddMenuItem("Eventful", Events);
            AddMenuItem("Write direct scan", DoWriterDirectScan);
            AddMenuItem("Partial Multichan Voltage Dump", DoPartialMultichanVoltageDump);
            AddMenuItem("TestUnsafeBufferCopy", TestUnsafeBufferCopy);
            AddMenuItem("Exit (or write q)", ExitProgram);
            printMenu();
        }

        #endregion

        #region Console menu
        static void AddMenuItem(string functionName, Action function)
        {
            m_menuItems.Add(new Tuple<string, Action>(functionName, function));
        }
        static void printMenu()
        {
            while (m_isRunning)
            {
                GC.Collect(0);
                Console.Clear();
                Console.WriteLine("Select Menu Option:");
                int index = 0;
                foreach (var item in m_menuItems)
                {
                    Console.WriteLine((index + 1) + ") " + item.Item1);
                    index++;
                }
                Console.WriteLine();
                Console.Write("Option: ");
                string line = Console.ReadLine();

                if(line.Trim().ToLower()=="q"|| line.Trim().ToLower()=="quit")
                {
                    m_isRunning = false;
                    return;
                }

                int selected;
                if (!int.TryParse(line, out selected))
                    continue;

                selected -= 1;

                if (selected < 0 || selected >= m_menuItems.Count)
                    continue;
                m_menuItems[selected].Item2();

            }
        }

        static bool m_isRunning = true;

        static List<Tuple<string, Action>> m_menuItems = new List<Tuple<string, Action>>();

        #endregion

        #region ClassTester

        static void ClassTester()
        {
            ConfocalControlLibrary.IntensityReader reader = new ConfocalControlLibrary.IntensityReader("card/ai0", false);
            ConfocalControlLibrary.PositionWriter writer = new ConfocalControlLibrary.PositionWriter("card/ao0", "card/ao1", 5000);

            ConfocalControlLibrary.ImageScanner scanner = new ConfocalControlLibrary.ImageScanner(reader, writer);

            bool isComplete = false;
            scanner.OnComplete += (o, e) =>
            {
                Console.WriteLine("Scan complete.");
                isComplete = true;
            };

            scanner.Scan(new ConfocalControlLibrary.Scan(-1, -1, 2, 2, 10, 0.1));

            while (!isComplete)
                System.Threading.Thread.Sleep(10);



            Console.ReadKey();

        }
        #endregion

        #region CounterTester

        static void CounterTester()
        {
            ConfocalControlLibrary.IntensityReader reader = new ConfocalControlLibrary.IntensityReader("Card/ctr0", true, 5000, "/card/PFI0");
            reader.Timeout = 1000;

            //ConfocalControlLibrary.IntensityCollector Collector = new ConfocalControlLibrary.IntensityCollector(reader, 2000, 3);
            reader.OnRead += (sender, ev) =>
            {
                Console.Write(" R " + ev.CounterData.Cast<uint>().Sum((d) => d) + " ");
            };
            reader.Start();
            Console.WriteLine("Started");
            reader.Stop();
            Console.WriteLine("Stopped");
            reader.Start();
            Console.WriteLine("Started");
            reader.Stop();
            Console.WriteLine("Stopped");
            reader.Start();
            Console.WriteLine("Started");

            System.Threading.Thread.Sleep(500);

            reader.Stop();
            Console.WriteLine("Stopped");
            reader.Start();
            Console.WriteLine("Started");
            System.Threading.Thread.Sleep(500);
            reader.Stop();

            Console.WriteLine("Read complete.");
            Console.ReadLine();
            Console.Clear();
            GC.Collect(0);
        }
        #endregion

        #region AnalogTester
        static void AnanlogTester()
        {
            ConfocalControlLibrary.IntensityReader reader = new ConfocalControlLibrary.IntensityReader("Card/ai0", false, 500);
            ConfocalControlLibrary.IntensityCollector Collector = new ConfocalControlLibrary.IntensityCollector(reader, 1000, 3);

            reader.Start();

            System.Threading.Thread.Sleep(1000);

            reader.Stop();

            Collector.GetData();
            int listSize = Collector.DataCollector.Count();

            Console.WriteLine("Array contains " + listSize + " number of entries");
            Console.WriteLine("Let's take a look inside");

            //int max = Math.Min(10, Collector.DataCollector.Count);
            int max = 10;

            if (max == 0)
            {
                Console.WriteLine("Uh oh, There's nothing in here.");
            }
            else
            {
                for (int i = 0; i < max; i++)
                {

                    double[] currentValue = Collector.GetData();
                    double displayValue = currentValue[0];

                    Console.WriteLine(i + ": " + displayValue);


                }

            }
            Console.WriteLine("Read complete.");

            Console.ReadLine();

            // Display index matrix

            Console.Clear();

        }

        #endregion

        #region Scantest

        static void DoScanTest()
        {
            int sfreq = 5000;
            ConfocalControlLibrary.IntensityReader reader = new ConfocalControlLibrary.IntensityReader("Card/ctr0", true, sfreq, "/card/PFI0");
            ConfocalControlLibrary.IntensityCollector Collector = new ConfocalControlLibrary.IntensityCollector(reader, 2000, 3);
            ConfocalControlLibrary.PositionWriter writer = new ConfocalControlLibrary.PositionWriter(
                "card/ao0", "card/ao1", sfreq);//, "/card/ctr2");

            ConfocalControlLibrary.ImageScanner scanner = new ConfocalControlLibrary.ImageScanner(reader, writer);

            // start the reader.
            reader.Start();

            System.Threading.Thread.Sleep(200);

            writer.SetPosition(0, 0);

            ConfocalControlLibrary.Scan s =
                new ConfocalControlLibrary.Scan(-0.5, -0.5, 1, 1, 100, 1, ConfocalControlLibrary.ScanType.Step, sfreq);

            System.DateTime start = DateTime.Now;
            s.Validate();
            //while (!s.PositionAtEnd)
            //    s.NextScanVoltages(sfreq, 100000);
            Console.WriteLine("Calculated scan in : " + (DateTime.Now - start));

            reader.OnRead += (r, data) =>
            {
                if (!scanner.IsRecording)
                    return;
                double avg = data.CounterData.Cast<uint>().Sum(v => v) / data.CounterData.Length;
                Console.Write(avg+",");
            };

            //start scan
            scanner.Scan(s);
            System.Threading.Thread.Sleep(100);

            start = DateTime.Now;
            DateTime lastElapsed = start;
            while (scanner.IsScanning)
            {
                System.Threading.Thread.Sleep(1);
                TimeSpan elapsed = DateTime.Now - lastElapsed;
                if (elapsed.TotalSeconds > 1)
                {
                    lastElapsed = DateTime.Now;
                    Console.WriteLine(DateTime.Now - start);
                }
            }


            Console.WriteLine("Done.");

            System.Threading.Thread.Sleep(1000);
            writer.SetPosition(0, 0);
            System.Threading.Thread.Sleep(10);
            Console.WriteLine("At 0,0");
            Console.ReadKey();
            GC.Collect(0);
        }

        static void DoUserDefinedScan()
        {
            int N = 100;
            Console.Write("Enter number of pxiels (nxn) n (Enter = " + N + "): ");
            string line = Console.ReadLine();
            
            if (line.Trim().Length == 0 || !int.TryParse(line, out N))
            {
                if (line.Trim().Length != 0)
                { 
                    Console.WriteLine("Eror. Enter a number,");
                    Console.ReadKey();
                    return;
                }
            }
            double delay = 0.06;
            Console.Write("Enter delay time [ms] (Enter=" + delay + "): ");

            line = Console.ReadLine();
            

            if (line.Trim().Length==0 || !double.TryParse(line, out delay))
            {
                if (line.Trim().Length != 0)
                {
                    Console.WriteLine("Eror. Enter a number,");
                    Console.ReadKey();
                    return;
                }
            }

            int sfreq = 50000;

            // creating the scan.
            ConfocalControlLibrary.Scan s =
                new ConfocalControlLibrary.Scan(-0.5, -0.5, 1, 1, N, delay, ConfocalControlLibrary.ScanType.Ramp, sfreq);

            s.TriggerDelay = 0.2;
            s.MoveToXYPositionAfterScan(0, 0);

            System.DateTime start = DateTime.Now;
            s.Validate();
            //while (!s.PositionAtEnd)
            //    s.NextScanVoltages(sfreq, 100000);
            Console.WriteLine("Calculated scan in : " + (DateTime.Now - start));
            Console.WriteLine("Calculated scan of " + s.getTotalNumberOfVoltagePoints(sfreq) + " voltage points in : " + (DateTime.Now - start));

            ConfocalControlLibrary.IntensityReader reader = new ConfocalControlLibrary.IntensityReader("Card/ctr0", true, sfreq, "/card/PFI0");
            ConfocalControlLibrary.IntensityCollector Collector = new ConfocalControlLibrary.IntensityCollector(reader, 2000, 3);
            ConfocalControlLibrary.PositionWriter writer = new ConfocalControlLibrary.PositionWriter(
                "card/ao0", "card/ao1", sfreq);//, "/card/ctr2");

            ConfocalControlLibrary.ImageScanner scanner = new ConfocalControlLibrary.ImageScanner(reader, writer);

            // start the reader.
            reader.Start();

            writer.SetPosition(0, 0);

            int pntsRead = 0;
            reader.OnRead += (r, data) =>
            {
                if (!scanner.IsRecording)
                    return;
                double avg = data.CounterData.Cast<uint>().Sum(v => v) / data.CounterData.Length;
                Console.Write(avg + ",");
                pntsRead += 1;
            };

            Console.WriteLine("Scanning for: " + s.ScanTime.ToString());

            scanner.Scan(s);
            System.Threading.Thread.Sleep(1);

            start = DateTime.Now;
            DateTime lastElapsed = start;
            while (scanner.IsScanning)
            {
                System.Threading.Thread.Sleep(1);
                TimeSpan elapsed = DateTime.Now - lastElapsed;
                if (elapsed.TotalSeconds > 1)
                {
                    lastElapsed = DateTime.Now;
                    Console.WriteLine(DateTime.Now - start + " (avg. read. " + pntsRead + ")");
                }
            }

            Console.WriteLine("Done.");
            Console.WriteLine("Should be at 0,0");
            Console.WriteLine("Waiting 3 secs.");
            writer.Dispose();
            reader.Dispose();

            Console.ReadKey();
        }


        static void DoWriterDirectScan()
        {
            int samplingFrequency = 40000;
            int N = 1000;
            ConfocalControlLibrary.PositionWriter writer = new ConfocalControlLibrary.PositionWriter(
                "card/ao0", "card/ao1", samplingFrequency);//, "/card/ctr2");
            
            ConfocalControlLibrary.Scan scan = new ConfocalControlLibrary.Scan(-0.5, -0.5, 1, 1, N, 0.01, ConfocalControlLibrary.ScanType.Ramp, samplingFrequency);

            double totalTime = scan.ScanTime.TotalSeconds;

            bool waitingForScan = true;
            writer.OnAfterEndScan += (sender, ev) =>
            {
                waitingForScan = false;
                Console.WriteLine("Complete");
            };

            Console.WriteLine("Stating scan of " + totalTime.ToString("#.00") + " seconds");
            //writer.WriteScan(scan.Positions);

            DateTime start = DateTime.Now;
            DateTime lastElapsed = start;
            while (waitingForScan)
            {
                System.Threading.Thread.Sleep(1);
                TimeSpan elapsed = DateTime.Now - lastElapsed;
                if(elapsed.TotalSeconds>1)
                {
                    lastElapsed = DateTime.Now;
                    Console.WriteLine((DateTime.Now - start) + " wpos: " + writer.ScanPosition);
                }
            }

            Console.WriteLine("Done.");
        }

        #endregion

        #region PointSelectTester
        static void PointSelectTester()
        {
            ConfocalControlLibrary.PositionWriter gw = new ConfocalControlLibrary.PositionWriter(
                "Card/ao0", "Card/ao1", 5000);//, "/card/ctr2");

            double x = 0, y = 0;

            while (true)
            {
                Console.Write("Enter X: ");    
                if(double.TryParse(Console.ReadLine(),out x))
                {
                    Console.Write("Enter Y: ");
                    if (double.TryParse(Console.ReadLine(), out y))
                    {
                        gw.SetPosition(x, y);
                    }
                    else
                    {
                        gw.WriterTask.Dispose();
                        return;
                    }
                }
                else
                {
                    gw.WriterTask.Dispose();
                    return;
                }
            }

            gw.WriterTask.Dispose();

        }

        static void DoPartialMultichanVoltageDump()
        {
            int sfreq = 200000;
            DateTime started = DateTime.Now;
            ConfocalControlLibrary.Scan s = new ConfocalControlLibrary.Scan(-0.5, -0.5, 1, 1, 30, 0.000001, ConfocalControlLibrary.ScanType.Ramp, sfreq);
            s.Validate();
            //while (!s.PositionAtEnd)
            //{
            //    double[,] pos = s.NextScanVoltages(sfreq, 100000);

            //}
            Console.WriteLine("Compleated (" + s.getTotalNumberOfVoltagePoints(sfreq) + ") in: " + (DateTime.Now - started));
            Console.ReadKey();
        }

        static unsafe void TestUnsafeBufferCopy()
        {
            int N = 100000000;
            double[,] multidim = new double[2, N];
            DateTime mark = DateTime.Now;
            // normal assign.
            for (var i = 0; i < N; i++)
            {
                multidim[0, i] = i;
                multidim[0, i] = i * 2;
            }
            Console.WriteLine("Safe compleated (" + N + ") in: " + (DateTime.Now - mark).TotalSeconds);
            mark = DateTime.Now;
            fixed (double* _multidim = multidim)
            {
                double* _arr = _multidim;
                for (var i = 0; i < N; i++)
                {
                    _arr[0] = i;
                    _arr[1] = i * 2;
                    _arr += 2;
                }
            }
            Console.WriteLine("UnSafe compleated (" + N + ") in: " + (DateTime.Now - mark).TotalSeconds);
            Console.Read();
        }

        #endregion

        #region Nick Event
        public delegate int dgPointer(int a, int b);

        static void Events()
        {
            ConfocalControlLibrary.Adder a = new ConfocalControlLibrary.Adder();
            a.OnMultipleOfFiveReached += a_MultipleOfFiveReached;
            dgPointer pAdder = new dgPointer(a.Add);
            int iAnswer = pAdder(4, 3);
            Console.WriteLine("iAnswer={0}", iAnswer);
            iAnswer = pAdder(4, 11);
            Console.WriteLine("iAnswer={0}", iAnswer);
            Console.ReadKey();
        }
         
        static void a_MultipleOfFiveReached(object sender, ConfocalControlLibrary.MultipleOfFiveEventArgs e)
        {
            Console.WriteLine("Multiple of Fives!!!!!", e.Total);
        }

            #endregion

        #region ExitProgram
        static void ExitProgram()
        {
            Environment.Exit(0);
        }
        #endregion
    }
}