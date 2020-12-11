using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace VeeamTaskLib {
    public class BlockDecompressor : IBlockProcessor {
        public Result ProcessedResult { get; private set; } = Result.Fail("Not processed yet");
        public int BlockIndex { get; private set; }
        public int SourceBlockSize { get; private set; }
        public byte[] SourceBlockData { get; private set; }
        public int ProcessedBlockSize { get; private set; }
        public byte[] ProcessedBlockData { get; private set; }

        private int _defaultBlockSize;

        private BlockWriter _writer;

        public BlockDecompressor(BlockWriter writer, int blockIndex, int compressedBlockSize, byte[] compressedBlockData, int defaultUncompressedBlockSize) {
            _writer = writer;
            BlockIndex = blockIndex;
            SourceBlockSize = compressedBlockSize;
            SourceBlockData = compressedBlockData.ToArray();
            _defaultBlockSize = defaultUncompressedBlockSize;
        }

        public Result Process() {
            try {
                byte[] decompressedBytes = new byte[_defaultBlockSize];
                using (MemoryStream compressedOutput = new MemoryStream(SourceBlockData)) {
                    using (GZipStream decompressionStream = new GZipStream(compressedOutput, CompressionMode.Decompress)) {
                        ProcessedBlockSize = decompressionStream.Read(decompressedBytes, 0, _defaultBlockSize);
                        ProcessedBlockData = decompressedBytes.ToArray();
                    }
                }
                ProcessedResult = Result.Ok();
                ProcessedResult = _writer.AddBlockAndTryToWrite(this);
                return ProcessedResult;
            } catch (Exception ex) {
                ProcessedResult = Result.Fail($"Block compressing exception: {ex.Message}");
                return ProcessedResult;
            }
        }
    }
}
