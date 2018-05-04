using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GZipStr
{
    /// <summary>
    /// Отвечает за тип обработки файла классом GZipPool
    /// </summary>
    public enum GZipCompressionMode{
        /// <summary>
        /// Файл подлежит сжатию
        /// </summary>
        Compress,

        /// <summary>
        /// Файл подлежит расжатию
        /// </summary>
        Decompress
    }
}
