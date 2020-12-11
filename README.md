# GZipTest
C# test task
Implement a command line tool using C# for block-by-block compressing and decompressing of files using
class System.IO.Compression.GzipStream.
During compression source file should be split by blocks of the same size, for example, block of 1MB. Each
block then should be compressed and written to the output file independently of others blocks. Application
should effectively parallel and synchronize blocks processing in multicore environment and should be able
to process files, that are larger than available RAM size.
Program code must be safe and robust in terms of exceptions. In case of exceptional situations user should
be informed by user friendly message, that allows user to fix occurred issue, for example, in case of OS
limitations.
Only basic classes and synchronization objects should be used for multithreading (Thread,
Manual/AutoResetEvent, Monitor, Semaphore, Mutex), it is not allowed to use async/await, ThreadPool,
BackgroundWorker, TPL.
Source code should satisfy OOP and OOD principles (readability, classes separation and so on).
Use the following command line arguments:
• compressing: GZipTest.exe compress [original file name] [archive file name]
• decompressing: GZipTest.exe decompress [archive file name] [decompressing file name]
On success program should return 0, otherwise 1.
Note: format of the archive is up to solution author, and does not affects final score, for example there is
no requirement for archive file to be compatible with GZIP file format.
Please send us solution source files and Visual Studio project. Briefly describe architecture and algorithms
used.

Compress files
Разработать консольное приложение на C# для поблочного сжатия и распаковки файлов с помощью
System.IO.Compression.GzipStream.
Для сжатия исходный файл делится на блоки одинакового размера, например, в 1 мегабайт.
Каждый блок сжимается и записывается в выходной файл независимо от остальных блоков.
Программа должна эффективно распараллеливать и синхронизировать обработку блоков в
многопроцессорной среде и уметь обрабатывать файлы, размер которых превышает объем
доступной оперативной памяти.
В случае исключительных ситуаций необходимо проинформировать пользователя понятным
сообщением, позволяющим пользователю исправить возникшую проблему, в частности, если
проблемы связаны с ограничениями операционной системы.
При работе с потоками допускается использовать только базовые классы и объекты синхронизации
(Thread, Manual/AutoResetEvent, Monitor, Semaphor, Mutex) и не допускается использовать
async/await, ThreadPool, BackgroundWorker, TPL.
Код программы должен соответствовать принципам ООП и ООД (читаемость, разбиение на классы
и т.д.).
Параметры программы, имена исходного и результирующего файлов должны задаваться в
командной строке следующим образом:
GZipTest.exe compress/decompress [имя исходного файла] [имя результирующего файла]
В случае успеха программа должна возвращать 0, при ошибке возвращать 1.
Примечание: формат архива остаётся на усмотрение автора, и не имеет значения для оценки
качества тестового, в частности соответствие формату GZIP опционально.

