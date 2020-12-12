using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

using System.Threading;

namespace VeeamTaskLib {
    public partial class FileProcessor {

        private const int _marker = 0x11111111;
        private int _defaultUncompressedBlockSize;
        private int _processorCount;

        public Result Process(ValidArguments arguments) {

            _defaultUncompressedBlockSize = arguments.DefaultUncompressedBlockSize == 0 ? 1024 * 1024 : arguments.DefaultUncompressedBlockSize;
            _processorCount = arguments.ProcessorCount <= 0 || arguments.ProcessorCount > Environment.ProcessorCount ? Environment.ProcessorCount : arguments.DefaultUncompressedBlockSize;

            try {
                Result result = Result.Fail($"Wrong command {arguments.Command}");
                switch (arguments.Command) {
                    case CommandEnum.Compress:
                        result = Compress(arguments.SourceFileName, arguments.DestinationFileName, _defaultUncompressedBlockSize);
                        break;
                    case CommandEnum.Decompress:
                        result = Decompress(arguments.SourceFileName, arguments.DestinationFileName);
                        break;
                    default:
                        break;
                }
                return result;
            } catch (FileNotFoundException ex) {
                var message = "File not found exception" + ": " + ex.Message;
                message += $"\n {ex}";
                return Result.Fail(message);
            } catch (FileLoadException ex) {
                var message = "File load exception" + ": " + ex.Message;
                message += $"\n {ex}";
                return Result.Fail(message);
            } catch (IOException ex) {
                var message = "Input/output exception" + ": " + ex.Message;
                message += $"\n File exception{ex}";
                return Result.Fail(message);
            }
        }
        private class UncompressedReader : IDisposable {
            private FileStream _inputStream;
            private int _inputBlockSize;
            private int _currentBlockCount;
            public readonly int TotalBlockCount;

            private static int CalculateUncompressedBlockCount(FileStream uncompressedStream, int uncompressedBlockSize) {
                var workBlocksCount = (int)(uncompressedStream.Length / uncompressedBlockSize);
                workBlocksCount += (uncompressedStream.Length % uncompressedBlockSize > 0) ? 1 : 0;
                return workBlocksCount;
            }

            public UncompressedReader(string uncompressedFileName, int uncompressedBlockSize) {
                FileStream uncompressedStream = new FileStream(uncompressedFileName, FileMode.Open, FileAccess.Read);
                _inputStream = uncompressedStream;
                _inputBlockSize = uncompressedBlockSize;
                TotalBlockCount = CalculateUncompressedBlockCount(_inputStream, _inputBlockSize);
            }
            public Result<ReadBlock> ReadNext() {
                var readBlock = new ReadBlock() {
                    ActualReadSize = 0,
                    ReadBlockData = new byte[_inputBlockSize],
                    BlockIndex = _currentBlockCount
                };
                try {
                    readBlock.ActualReadSize = _inputStream.Read(readBlock.ReadBlockData, 0, _inputBlockSize);
                    _currentBlockCount++;
                    return Result.Ok<ReadBlock>(readBlock);
                } catch (Exception ex) {
                    return Result.Fail<ReadBlock>($"Read original block exception: {ex.Message}");
                }
            }

            public void Dispose() {
                _inputStream.Dispose();
            }
        }

        private Result Compress(string uncompressedFileName, string compressedFileName, int uncompressedBlockSize) {
            try {
                using (ChunkManager chunkManager = new ChunkManager(_processorCount)) {
                    using (UncompressedReader uncompressedReader = new UncompressedReader(uncompressedFileName, uncompressedBlockSize)) {
                        using (var processedWriter = new BlockWriter(_marker, compressedFileName)) {
                            var writeHeaderResult = processedWriter.WriteHeader(uncompressedReader.TotalBlockCount, uncompressedBlockSize);
                            if (writeHeaderResult.IsFailure) {
                                return writeHeaderResult;
                            }
                            while (true) {
                                var readBlockResult = uncompressedReader.ReadNext();
                                if (readBlockResult.IsFailure) {
                                    return Result.Fail(readBlockResult.Error);
                                }
                                var readBlock = readBlockResult.Value;
                                if (readBlock.ActualReadSize <= 0) {
                                    break;
                                }
                                var workBlockForProcessing = new BlockCompressor(processedWriter, readBlock.BlockIndex, readBlock.ActualReadSize, readBlockResult.Value.ReadBlockData);
                                var startThreadResult = chunkManager.AddBlockAndTryToProcess(workBlockForProcessing);
                                if (startThreadResult.IsFailure) {
                                    return Result.Fail(startThreadResult.Error);
                                }
                                if (processedWriter.Status.IsFailure) {
                                    return processedWriter.Status;
                                }
                                chunkManager.StartUnstartedChunksAndCountRemained();
                                if (chunkManager.Status.IsFailure) {
                                    return chunkManager.Status;
                                }
                                chunkManager.RemoveWrittenChunks();
                            }
                            while (chunkManager.StartUnstartedChunksAndCountRemained() > 0) {
                                Thread.Sleep(10);
                            }
                            chunkManager.WaitingUnfinishedChunks();
                            if (processedWriter.Status.IsFailure) {
                                return processedWriter.Status;
                            }
                            chunkManager.StartUnstartedChunksAndCountRemained();
                            if (chunkManager.Status.IsFailure) {
                                return chunkManager.Status;
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                return Result.Fail($"Compressing error: {ex.Message}");
            }
            return Result.Ok();
        }

        private class CompressedReader : IDisposable {
            private string _nonValidArchiveErrorMessage;

            private readonly FileStream _inputStream;
            private readonly BinaryReader _inputBinaryReader;
            private string _imputFileName;
            private readonly int _marker;
            public int TotalBlockCount { get; private set; }
            public int MaxBlockSize { get; private set; }
            private int _currentBlockCount;
            private int _offset = 0;
            private const int blockHeaderSize = sizeof(int) * 3;

            public int CurrentBlockCounter { get; private set; }
            public CompressedReader(string compressedFileName, int marker) {
                _imputFileName = compressedFileName;
                _nonValidArchiveErrorMessage = $"Input file is not a valid archive: {_imputFileName}";
                _inputStream = new FileStream(compressedFileName, FileMode.Open, FileAccess.Read);
                _inputBinaryReader = new BinaryReader(_inputStream);
                _marker = marker;
            }

            public Result ReadFirst() {
                var marker = _inputBinaryReader.ReadInt32();
                if (marker != _marker) {
                    return Result.Fail(_nonValidArchiveErrorMessage);
                }
                TotalBlockCount = _inputBinaryReader.ReadInt32();
                MaxBlockSize = _inputBinaryReader.ReadInt32();
                _offset += (blockHeaderSize);
                return Result.Ok();
            }

            public Result<ReadBlock> ReadNext() {

                var readBlock = new ReadBlock() {
                    ActualReadSize = 0,
                    ReadBlockData = new byte[MaxBlockSize],
                    BlockIndex = _currentBlockCount
                };

                if (_currentBlockCount >= TotalBlockCount) {
                    return Result.Fail<ReadBlock>($"Extra read block {_currentBlockCount}, but should be no more than {TotalBlockCount}!");
                }

                var seek = _inputBinaryReader.BaseStream.Seek(_offset, SeekOrigin.Begin);
                if (seek <= 0) {
                    return Result.Ok(readBlock);
                }

                var marker = _inputBinaryReader.ReadInt32();
                if (marker != _marker) {
                    return Result.Fail<ReadBlock>(_nonValidArchiveErrorMessage);
                }
                readBlock.BlockIndex = _inputBinaryReader.ReadInt32();
                if (readBlock.BlockIndex != _currentBlockCount) {
                    return Result.Fail<ReadBlock>(_nonValidArchiveErrorMessage);
                }
                readBlock.ActualReadSize = _inputBinaryReader.ReadInt32();
                readBlock.ReadBlockData = _inputBinaryReader.ReadBytes(readBlock.ActualReadSize);
                _currentBlockCount++;
                _offset += (blockHeaderSize + readBlock.ActualReadSize);
                return Result.Ok(readBlock);
            }
            public void Dispose() {
                _inputStream.Dispose();
            }
        }

        private Result Decompress(string sourceFileName, string destinationFileName) {
            try {
                using (ChunkManager chunkManager = new ChunkManager(_processorCount)) {
                    using (var compressedReader = new CompressedReader(sourceFileName, _marker)) {
                        using (var processedWriter = new BlockWriter(_marker, destinationFileName)) {
                            var readFirstResult = compressedReader.ReadFirst();
                            if (readFirstResult.IsFailure) {
                                return readFirstResult;
                            }
                            for (int i = 0; i < compressedReader.TotalBlockCount; i++) {
                                var nextBlockResult = compressedReader.ReadNext();
                                if (nextBlockResult.IsFailure) {
                                    return Result.Fail(nextBlockResult.Error);
                                }
                                var nextBlock = nextBlockResult.Value;
                                var workBlockForDecompression = new BlockDecompressor(processedWriter, nextBlock.BlockIndex, nextBlock.ActualReadSize, nextBlock.ReadBlockData, compressedReader.MaxBlockSize);
                                var startThreadResult = chunkManager.AddBlockAndTryToProcess(workBlockForDecompression);
                                if (startThreadResult.IsFailure) {
                                    return Result.Fail(startThreadResult.Error);
                                }
                                chunkManager.StartUnstartedChunksAndCountRemained();
                                if (chunkManager.Status.IsFailure) {
                                    return chunkManager.Status;
                                }
                                chunkManager.RemoveWrittenChunks();
                            }
                            while (chunkManager.StartUnstartedChunksAndCountRemained() > 0) {
                                Thread.Sleep(10);
                            }
                            chunkManager.WaitingUnfinishedChunks();
                            if (processedWriter.Status.IsFailure) {
                                return processedWriter.Status;
                            }
                            chunkManager.StartUnstartedChunksAndCountRemained();
                            if (chunkManager.Status.IsFailure) {
                                return chunkManager.Status;
                            }
                        }
                    }
                }
            } catch (Exception ex) {
                return Result.Fail($"Decompressing error: {ex.Message}");
            }
            return Result.Ok();
        }

        private class ReadBlock {
            public int ActualReadSize { get; set; }
            public byte[] ReadBlockData { get; set; }
            public int BlockIndex { get; set; }
        }

    }
}
