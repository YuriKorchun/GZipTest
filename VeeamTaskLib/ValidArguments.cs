namespace VeeamTaskLib {
    public class ValidArguments {
        public CommandEnum Command { get; set; }
        public string SourceFileName { get; set; }
        public string DestinationFileName { get; set; }
        public int ProcessorCount { get; set; }
        public int DefaultUncompressedBlockSize { get; set; }

    }

}
