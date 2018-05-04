namespace GZipStr
{
    /// <summary>
    /// Поддерживаемые программой команды к выполнению
    /// </summary>
    enum SupportedCommands{
        /// <summary>
        /// Архивировать файл, который указан в качестве исходного
        /// </summary>
        compressInputFile,

        /// <summary>
        /// Разархивировать файл, который указан в качестве исходного
        /// </summary>
        decompressInputFile
    }
}