using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GZipStr
{
    class GZipOutputQueueData{
        /// <summary>
        /// Получает ранг текущей порции файла.
        /// </summary>
        public int Rank { get; private set; }

        /// <summary>
        /// В данном потоке хранятся данные, подлежащие сжатию. Доступно только для чтения.
        /// </summary>
        public MemoryStream memoryStream { get; private set; }

        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        /// <param name="rank"></param>
        /// <param name="memoryStream"></param>
        public GZipOutputQueueData(int rank, MemoryStream memoryStream){
            this.Rank = rank;
            this.memoryStream = memoryStream;
        }
    }
}
