﻿# GZipStream for Veeam

Консольная программа на C#, предназначенная для поблочного сжатия и расжатия файлов с помощью System.IO.Compression.GzipStream. 

Для компрессии исходный файл делится на блоки одинакового размера, например, в 1 мегабайт. Каждый блок компрессится и записывается в выходной файл независимо от остальных блоков.

Программа эффективно распараллеливает и синхронизирует обработку блоков  в многопроцессорной среде и умеет обрабатывать файлы, размер которых превышает объем доступной оперативной памяти. 
В случае исключительных ситуаций информирует пользователя понятным сообщением, позволяющим пользователю исправить возникшую проблему, в частности если проблемы связаны с ограничениями операционной системы.
При работе с потоками использует только стандартные классы и библиотеки из .Net 3.5 (исключая ThreadPool, BackgroundWorker, TPL). Реализация с использованием Thread-ов.
Код программы соответствует принципам ООП и ООД (читаемость, разбиение на классы и т.д.). 
Параметры программы, имена исходного и результирующего файлов задаются в командной строке следующим образом:
GZipTest.exe compress/decompress [имя исходного файла] [имя результирующего файла]