using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace GZipStr
{
    /// <summary>
    /// Этот класс отвечает за предоставления потока в котором будет происходить обработка файла
    /// </summary>
    class GZipPoolThread{
        /// <summary>
        /// Поток, в котором происходит обработка куска файла
        /// </summary>
        public Thread thread { get; private set; }

        public ManualResetEvent manualEvent { get; private set; }

        public GZipPoolThread(Thread thread){
            this.thread = thread;
            manualEvent = new ManualResetEvent(false);
        }

        /// <summary>
        /// Данный метод позволяет указать объекту, что работа по обработке файла была завершена
        /// </summary>
        public void CompleteWork(){
            manualEvent.Set();
        }
    }
}
