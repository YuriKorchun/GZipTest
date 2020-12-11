using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace VeeamTaskLib {
    public class ChunkManager : IDisposable {
        private List<IChunk> _chunkList = new List<IChunk>();
        public Result Status = Result.Ok();
        private int _processorCount = 1;
        public ChunkManager(int processorCount) {
            _processorCount = processorCount;
        }
        public Result AddBlockAndTryToProcess(IBlockProcessor workBlockForProcessing) {
            if (Status.IsFailure) {
                return Status;
            }
            try {
                var chunkThread = new Thread(ProcessChunk);
                var chunk = new Chunk(chunkThread, workBlockForProcessing);
                _chunkList.Add(chunk);
                if (RunningChunkCount(_chunkList) <= _processorCount) {
                    chunk.ChunkThread.Start(workBlockForProcessing);
                }
                return Result.Ok();
            } catch (Exception ex) {
                return Result.Fail($"Thread processing exception: {ex.Message}");
            }
        }

        private void ProcessChunk(object objectBlockForProcessing) {
            if (Status.IsFailure) {
                return;
            }
            var block = (IBlockProcessor)objectBlockForProcessing;
            Status = block.Process();
        }

        public void WaitingUnfinishedChunks() {
            foreach (var thread in _chunkList) {
                if (thread.ChunkThread.IsAlive) {
                    thread.ChunkThread.Join();
                }
            }
        }

        public int StartUnstartedChunksAndCountRemained() {
            if (Status.IsFailure) {
                return -1;
            }
            foreach (var chunk in _chunkList.FindAll(p => p.ChunkThread.ThreadState == ThreadState.Unstarted)) {
                if (RunningChunkCount(_chunkList) <= _processorCount) {
                    chunk.ChunkThread.Start(chunk.Block);
                } else {
                    break;
                }
            }
            return _chunkList.FindAll(p => p.ChunkThread.ThreadState == ThreadState.Unstarted).Count;
        }

        private int RunningChunkCount(List<IChunk> threadQueue) {
            return threadQueue.FindAll(p => p.ChunkThread.ThreadState == ThreadState.Running).Count;
        }

        public void Dispose() {
            _chunkList.Clear();
        }
    }
    interface IChunk {
        Thread ChunkThread { get; }
        IBlockProcessor Block { get; }
    }

    public class Chunk : IChunk {
        public Thread ChunkThread { get; }
        public IBlockProcessor Block { get; }

        public Chunk(Thread thread, IBlockProcessor block) {
            ChunkThread = thread;
            Block = block;
        }
    }


}
