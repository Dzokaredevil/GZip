using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace GZipStr
{
    class Program{
        const float MegaBytesToBytesCoefficient = 1048576.0F;
        const int RequiredArgumentsCount = 3;
        static SupportedCommands programCommand;

        static void Main(string[] args){
            CheckProgramArguments(args);

            var command = args[0];

            try{
                programCommand = CheckCommand(command);
            }
            catch (ArgumentException ex){
                Console.WriteLine(ex.Message);
                WriteMessageAndExit(1);
            }

            var sourceFilePath = args[1];
            var destinationFilePath = args[2];

            CheckSourceFileExistence(sourceFilePath);
            CheckDestinationFileExistence(destinationFilePath);

            var sourceFileSize = new FileInfo(sourceFilePath).Length;
            var processorUnitCount = Environment.ProcessorCount;
            var availiableMemory = Convert.ToInt64(new PerformanceCounter("Memory", "Available MBytes").NextValue() * MegaBytesToBytesCoefficient);

            #region _DEBUG
#if DEBUG
            Stopwatch stopWatch = new Stopwatch();
            Console.WriteLine("Количество ядер процессора: " + processorUnitCount);
            Console.WriteLine("Количество свободной оперативной памяти (Б): " + availiableMemory);
            Console.WriteLine("Путь к файлу, подлежащего обработке: " + sourceFilePath);
            Console.WriteLine("Путь к файлу с результатом обработки: " + destinationFilePath);
            Console.WriteLine("Размер файла, подлежащего обработке (Б): " + sourceFileSize);
            stopWatch.Start();
#endif
            #endregion

            switch (programCommand){
                case SupportedCommands.compressInputFile:{
                        Compress(sourceFilePath, destinationFilePath);
                    } break;
                case SupportedCommands.decompressInputFile:{
                        Decompress(sourceFilePath, destinationFilePath);
                    } break;
                default: throw new NotImplementedException("В перечисление команд задана новая, необработанная команда");
            }

            #region _DEBUG
#if DEBUG
            stopWatch.Stop();
            TimeSpan ts = stopWatch.Elapsed;
            string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds / 10);
            Console.WriteLine("Алгоритм отработал за " + elapsedTime + " секунд");
#endif
            #endregion

            WriteMessageAndExit();
        }

        private static SupportedCommands CheckCommand(string command){
            switch (command){
                case "compress":
                    return SupportedCommands.compressInputFile;
                case "decompress":
                    return SupportedCommands.decompressInputFile;
                default:
                    throw new ArgumentException("Команда " + command + " не поддерживается");
            }
        }

        private static void CheckProgramArguments(string[] args){
            if (args.Length != RequiredArgumentsCount){
                Console.WriteLine("Программа получила неверное количество аргументов");
                WriteMessageAndExit(1);
            }
        }

        private static void CheckDestinationFileExistence(string destinationFilePath){
            if(File.Exists(destinationFilePath)){
                Console.WriteLine("Файл в который ведется запись сжатых данных уже существует, перезаписать ?(y/n)");

                var userAnswer = string.Empty;
                do {
                    userAnswer = Console.ReadLine().ToLower();
                    if (userAnswer == "n"){
                        WriteMessageAndExit();
                    }

                } while (userAnswer != "y");

                File.Delete(destinationFilePath);
            }
            File.Create(destinationFilePath).Close();
        }

        private static void CheckSourceFileExistence(string sourceFilePath){
            if (!File.Exists(sourceFilePath)){
                Console.WriteLine("Файл " + sourceFilePath + " не найден");
                WriteMessageAndExit(1);
            }
        }

        static void WriteMessageAndExit(int exitCode = 0){
            Console.WriteLine("Нажмите <Enter> для выхода из программы");
            Console.ReadLine();
            Environment.Exit(exitCode);
        }
        
        private static void Compress(string uncompressedFilePath, string compressedFilePath){
            using(BinaryReader inputStream = new BinaryReader(new FileStream(uncompressedFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))){
                using (BinaryWriter outputStream = new BinaryWriter(new FileStream(compressedFilePath, FileMode.Open, FileAccess.Write, FileShare.ReadWrite)))
                {
                    GZipPool gZipPool = new GZipPool(inputStream, outputStream, GZipCompressionMode.Compress);
                    gZipPool.Wait();
                }
            }
        }

        static void Decompress(string uncompressedFilePath, string compressedFilePath){
            using (BinaryReader inputStream = new BinaryReader(new FileStream(uncompressedFilePath, FileMode.Open, FileAccess.Read))){
                using (BinaryWriter outputStream = new BinaryWriter(new FileStream(compressedFilePath, FileMode.Open, FileAccess.Write)))
                {
                    GZipPool gZipPool = new GZipPool(inputStream, outputStream, GZipCompressionMode.Decompress);
                    gZipPool.Wait();
                }
            }
        }
    }
}