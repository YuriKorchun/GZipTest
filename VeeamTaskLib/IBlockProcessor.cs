using System;
using System.Collections.Generic;
using System.Text;

namespace VeeamTaskLib {
    public interface IBlockProcessor {

        Result ProcessedResult { get; }
        Result Process();
        int BlockIndex { get;  }
        int ProcessedBlockSize { get; }

        byte[] ProcessedBlockData { get;  }

        bool IsWritten { get; set; }

    }
}
