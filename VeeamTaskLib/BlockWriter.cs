using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VeeamTaskLib {
    public class BlockWriter: IDisposable {
        public Result Status { get; private set; } = Result.Ok();
        private SortedList<int, IBlockProcessor> _sortedList = new SortedList<int, IBlockProcessor>();
        private readonly object _threadLock = new object();
        private readonly BinaryWriter _binaryWriter;
        private int _latestWrittenBlockIndex = 0;
        private readonly int _marker;
        private readonly FileStream _destinationStream;

        public BlockWriter(int marker, string destinationFileName) {
            _marker = marker;
            _destinationStream =  new FileStream(destinationFileName, FileMode.Create, FileAccess.Write);
            _binaryWriter = new BinaryWriter(_destinationStream);
        }

        public Result WriteHeader(int index, int size) {
            try {
                _binaryWriter.Write(_marker);
                _binaryWriter.Write(index);
                _binaryWriter.Write(size);
                return Result.Ok();
            } catch (Exception ex) {
                Status = Result.Fail($"File {nameof(_binaryWriter)} writing header exception: {ex.Message}");
                return Status;
            }
        }

        public Result AddBlockAndTryToWrite(IBlockProcessor block) {
            try {
                if (Status.IsFailure) {
                    return Status;
                }
                if (block.ProcessedResult.IsFailure) {
                    Status = block.ProcessedResult;
                    return Status;
                }
                lock (_threadLock) {
                    if (_latestWrittenBlockIndex == block.BlockIndex && _sortedList.Count == 0) {
                        var result = WriteBlock(block);
                        _latestWrittenBlockIndex++;
                        Status = result;
                        return result;
                    }
                    _sortedList.Add(block.BlockIndex, block);
                    while (true) {
                        if (_sortedList.TryGetValue(_latestWrittenBlockIndex, out var nextBlock)) {
                            var result = WriteBlock(nextBlock);
                            if (result.IsFailure) {
                                Status = result;
                                return result;
                            }
                            _sortedList.Remove(_latestWrittenBlockIndex);
                            _latestWrittenBlockIndex++;
                        } else {
                            break;
                        }
                    }
                }
                return Result.Ok();
            } catch (Exception ex) {
                Status = Result.Fail($"Writing current block exceptoin: {ex.Message}");
                return Status;
            }
        }

        private Result WriteBlock(IBlockProcessor nextBlock) {
            if (nextBlock is BlockCompressor) {
                var resultWriteHeader = WriteHeader(nextBlock.BlockIndex, nextBlock.ProcessedBlockSize);
                if (resultWriteHeader.IsFailure) {
                    Status = resultWriteHeader;
                    return Status;
                }
            }
            _binaryWriter.Write(nextBlock.ProcessedBlockData, 0, nextBlock.ProcessedBlockSize);
            nextBlock.IsWritten = true;
            return Result.Ok();
        }

        public void Dispose() {
            _binaryWriter.Dispose();
            _destinationStream.Dispose();
            _sortedList.Clear();
        }
    }
}
