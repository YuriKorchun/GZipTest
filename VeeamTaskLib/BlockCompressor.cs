using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace VeeamTaskLib {
    public class BlockCompressor : IBlockProcessor {
        public Result ProcessedResult { get; private set; } = Result.Fail("Not processed yet");
        public int BlockIndex { get; }
        public int SourceBlockSize { get; private set; }
        public byte[] SourceBlockData { get; private set; }
        public int ProcessedBlockSize { get; private set; }
        public byte[] ProcessedBlockData { get; private set; }

        private BlockWriter _writer;

        public BlockCompressor(BlockWriter writer, int blockIndex, int uncompressedBlockSize, byte[] uncompressedBlockData) {
            _writer = writer;
            BlockIndex = blockIndex;
            SourceBlockSize = uncompressedBlockSize;
            SourceBlockData = new byte[uncompressedBlockSize];
            SourceBlockData = uncompressedBlockData.ToArray();
        }

        public Result Process() {
            try {
                using (MemoryStream compressedOutput = new MemoryStream()) {
                    using (GZipStream compressionStream = new GZipStream(compressedOutput, CompressionLevel.Optimal, leaveOpen: true)) {
                        compressionStream.Write(SourceBlockData, 0, SourceBlockSize);
                    }
                    ProcessedBlockSize = (int)compressedOutput.Length;
                    ProcessedBlockData = compressedOutput.ToArray();
                }
                ProcessedResult = Result.Ok();
                ProcessedResult = _writer.AddBlockAndTryToWrite(this);
                return ProcessedResult;
            }
            catch (Exception ex) {
                ProcessedResult = Result.Fail($"Block compressing error: {ex.Message}");
                return ProcessedResult;
            }
        }
    }
}
