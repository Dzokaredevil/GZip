using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Collections;

namespace GZipStr
{
    public class GZipPool{
        /// <summary>
        /// Размер считытываемых за один раз данных из файла
        /// </summary>
        const int partSize = Int16.MaxValue * 100;

        /// <summary>
        /// Файл, из которого считываются данные для сжатия
        /// </summary>
        public BinaryReader InputStream { get; private set; }

        /// <summary>
        /// Файл в который будут записываться сжатые данные
        /// </summary>
        public BinaryWriter OutputStream { get; private set; }

        /// <summary>
        /// Тип обработки (сжатие или распаковка) входного и выходного файлов
        /// </summary>
        GZipCompressionMode compressMode;

        /// <summary>
        /// Количество потоков, которое изначально будет задействовано для обработки файла
        /// </summary>
        public int ThreadUnitCount { get; private set; }

        /// <summary>
        /// Количество потоков, занимающихся сжатием, которые завершили свою работу
        /// </summary>
        int completedWorkCount = 0;

        /// <summary>
        /// Определяет активирована ли сейчас обработка данных
        /// </summary>
        bool isCompressionWorking = true;

        /// <summary>
        /// Номер последнего, записанной на диск порции данных
        /// </summary>
        int lastRankToSave = -1;

        /// <summary>
        /// Потоки, которые будут сжимать дынные и записывать их в оперативную память
        /// </summary>
        List<GZipPoolThread> consumers = new List<GZipPoolThread>();

        /// <summary>
        /// Очередь в которую поступают файла, загруженные с диска
        /// </summary>
        Queue inputQueue = Queue.Synchronized(new Queue());

        /// <summary>
        /// Очередь из которой данные последовательно сохраняются на диск
        /// </summary>
        Queue outputQueue = Queue.Synchronized(new Queue());

        /// <summary>
        /// Поток, отвечающий за чтение порций данных с диска
        /// </summary>
        GZipPoolThread loader;

        /// <summary>
        /// Поток, отвечающий за запись порций данных на диск
        /// </summary>
        GZipPoolThread saver;

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        /// <param name="InputStream">Поток, из которого производится чтение порций данных</param>
        /// <param name="OutputStream">Поток, в который производится запись порций данных</param>
        /// <param name="compressMode">Тип обработки (сжатие или распаковка) входного и выходного файлов</param>
        public GZipPool(BinaryReader InputStream, BinaryWriter OutputStream, GZipCompressionMode compressMode){
            Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e){
                e.Cancel = true;
                if(isCompressionWorking){
                    isCompressionWorking = false;
                    Console.WriteLine("Программа приостановлена.");
                } else {
                    isCompressionWorking = true;
                    Console.WriteLine("Программа возобновила работу.");
                }
            };

            this.InputStream = InputStream;
            this.OutputStream = OutputStream;
            this.ThreadUnitCount = Environment.ProcessorCount;
            this.compressMode = compressMode;

            for (int i = 0; i < ThreadUnitCount; ++i){
                GZipPoolThread consumer = new GZipPoolThread(new Thread(ProcessData));
                consumers.Add(consumer);
            }

            loader = new GZipPoolThread(new Thread(LoadData));
            loader.thread.Start(loader);

            saver = new GZipPoolThread(new Thread(SaveData));
            saver.thread.Start(saver);

            foreach (var consumer in consumers){
                consumer.thread.Start(consumer);
            }
        }

        /// <summary>
        /// Данный метод производит обработку данных, которые находятся в очереди
        /// </summary>
        /// <param name="obj">Поток, которой отвечает за обработку данных</param>
        private void ProcessData(object obj){
            if(!(obj is GZipPoolThread)){
                throw new ArgumentException("Метод ProcessData ожидал получить объект класса GZipPoolThread, но получил объект другого класса");
            }

            GZipPoolThread gZipPoolThread = obj as GZipPoolThread;

            while (true){
                while(!isCompressionWorking) {
                    continue;
                }

                //Если очередь пустая, но загрузчик еще может считать что либо с диска
                if (inputQueue.Count == 0 && loader.thread.ThreadState == ThreadState.Running){
                    continue;
                }
                else 
                if (inputQueue.Count == 0 && loader.thread.ThreadState != ThreadState.Running){
                    completedWorkCount++;
                    gZipPoolThread.CompleteWork();
                    return;
                } else {
                    GZipInputQueueData gZipInputQueueData;

                    try{
                        gZipInputQueueData = inputQueue.Dequeue() as GZipInputQueueData;
                    }
                    catch(InvalidOperationException){
                        continue;
                    }
                   
                    switch (compressMode){
                        case GZipCompressionMode.Compress:{
                                MemoryStream ms = new MemoryStream();
                                using (var destinationGZ = new GZipStream(ms, CompressionMode.Compress, true)){
                                    destinationGZ.Write(gZipInputQueueData.Buffer, 0, gZipInputQueueData.Buffer.Count());
                                    outputQueue.Enqueue(new GZipOutputQueueData(gZipInputQueueData.Rank, ms));
                                }
                            }
                            break;
                        case GZipCompressionMode.Decompress:{
                                using (MemoryStream ms = new MemoryStream()){
                                    using (var destinationGZ = new GZipStream(ms, CompressionMode.Decompress, true)){
                                        using (var oms = new MemoryStream()){
                                            destinationGZ.CopyTo(oms);
                                            outputQueue.Enqueue(new GZipOutputQueueData(gZipInputQueueData.Rank, oms));
                                        }
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void SaveData(object obj){
            List<GZipOutputQueueData> linkedData = new List<GZipOutputQueueData>();
            GZipPoolThread gZipPoolThread = obj as GZipPoolThread;

            while(true){
                while (!isCompressionWorking){
                    continue;
                } try {
                    linkedData.Add(outputQueue.Dequeue() as GZipOutputQueueData);
                }
                catch(InvalidOperationException) {
                    continue;
                }

                if (linkedData.Count > 0){
                    ProcessOutputList(linkedData);
                }

                if(completedWorkCount == consumers.Count){
                    while(outputQueue.Count != 0){
                        linkedData.Add(outputQueue.Dequeue() as GZipOutputQueueData);
                    }

                    if (linkedData.Count != 0){
                        ProcessOutputList(linkedData);
                    }

                    gZipPoolThread.CompleteWork();
                    return;
                }
            }
        }

        private void ProcessOutputList(List<GZipOutputQueueData> linkedData){
            Thread.Sleep(1000);
            linkedData.Sort((x, y) => x.Rank.CompareTo(y.Rank));

            #region DEBUG
#if DEBUG
            using(StreamWriter sw = new StreamWriter("C:\\Users\\dimav\\Desktop\\Work\\Veeam\\logs.txt", true)){
                foreach (var x in linkedData){
                    sw.Write(x.Rank + " ");
                }
                sw.WriteLine();
            }
#endif
            #endregion

            List<GZipOutputQueueData> indexesToDelete = new List<GZipOutputQueueData>();
            lock (OutputStream.BaseStream){
                for (int i = 0; i < linkedData.Count; i++){
                    if (Math.Abs(linkedData[i].Rank - lastRankToSave) <= 1){
                        OutputStream.Write(linkedData[i].memoryStream.ToArray());
                        indexesToDelete.Add(linkedData[i]);
                        lastRankToSave++;
                    }
                }
                OutputStream.Flush();
            }

            foreach (var x in indexesToDelete){
                linkedData.Remove(x);
            }
        }

        private void LoadData(object obj){
            if(!(obj is GZipPoolThread)){
                throw new ArgumentException("Метод Load ожидал получить объект класса GZipPoolThread, но получил объект другого класса");
            }

            GZipPoolThread gZipPoolThread = obj as GZipPoolThread;

            byte[] buffer = new byte[partSize];

            long numBytesToRead = InputStream.BaseStream.Length;

            while (numBytesToRead > 0){
                while (!isCompressionWorking) {
                    continue;
                }

                int countOfBytes = InputStream.Read(buffer, 0, partSize);

                if(countOfBytes == 0) {
                    break;
                }

                byte[] b = new byte[countOfBytes];
                Array.Copy(buffer, b, countOfBytes);
                inputQueue.Enqueue(new GZipInputQueueData(b));
                GZipInputQueueData.globalRank++;
                numBytesToRead -= countOfBytes;
            }

            gZipPoolThread.CompleteWork();
        }

        public void Wait(){
            List<ManualResetEvent> manualEvents = new List<ManualResetEvent>();
            manualEvents.Add(loader.manualEvent);
            manualEvents.Add(saver.manualEvent);
            foreach(var consumer in consumers) {
                manualEvents.Add(consumer.manualEvent);
            }
            WaitHandle.WaitAll(manualEvents.ToArray());
        }
    }
}