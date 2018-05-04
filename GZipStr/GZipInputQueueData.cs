using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GZipStr
{
    /// <summary>
    /// Данный класс отвечает за хранение порции информации, которая будет обработана в очереди
    /// </summary>
    class GZipInputQueueData{
        /// <summary>
        /// Буффер, в котором будут содержаться данные для обработки
        /// </summary>
        public byte[] Buffer { get; private set; }

        /// <summary>
        /// Ранг глобальной порции файла. Используется для ранжирования порций
        /// </summary>
        public static int globalRank = 0;

        /// <summary>
        /// Получает ранг текущей порции файла
        /// </summary>
        public int Rank { get; private set; }

        public GZipInputQueueData(byte[] buffer){
            this.Buffer = buffer;
            this.Rank = globalRank;
        }
    }
}
