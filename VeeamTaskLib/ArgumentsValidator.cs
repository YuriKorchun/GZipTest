using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace VeeamTaskLib {
    public static class ArgumentsValidator {

        private static Dictionary<string, CommandEnum> Commands = new Dictionary<string, CommandEnum>() {
            { "compress", CommandEnum.Compress },
            { "decompress", CommandEnum.Decompress }
        };

        private static string Help =
    "\n help: GZipTest.exe help" +
    "\n compressing: GZipTest.exe compress [original file name] [archive file name]" +
    "\n decompressing: GZipTest.exe decompress [archive file name] [decompressing file name]";
        private static string UseHelp = "Use HELP as argument for help";

        public static Result<ValidArguments> Check(string[] args) {

            var validParameters = new ValidArguments();

            if (args.Length == 0) {
                return Result.Fail<ValidArguments>($"There are no agruments! {UseHelp}");
            }

            string command = args[0];

            if (args.Length == 1 && command.Equals("help", StringComparison.OrdinalIgnoreCase)) {
                return Result.Fail<ValidArguments>($"{Help}");
            }

            if (args.Length < 3) {
                return Result.Fail<ValidArguments>($"Wrong number of agruments! {UseHelp}");
            }

            args = args.Select(p => p.ToLower()).ToArray();

            if (!Commands.TryGetValue(command, out var commandEnum)) {
                return Result.Fail<ValidArguments>($"Wrong command {command}! {UseHelp}");
            }

            var sourceFileName = args[1];

            if (!File.Exists(sourceFileName)) {
                return Result.Fail<ValidArguments>($"{sourceFileName} - source file doesn't exist!");
            }

            var destinationFileName = args[2];

            if (String.Equals(sourceFileName, destinationFileName, StringComparison.OrdinalIgnoreCase)) {
                return Result.Fail<ValidArguments>($"{sourceFileName} - source and destination files can not be the same!");
            }

            var sourceFileInfo = new FileInfo(sourceFileName);
            var destinationFileInfo = new FileInfo(destinationFileName);
            var destinationDriveName = destinationFileInfo.Directory.Root.FullName;

            var destinationDriveInfo = DriveInfo.GetDrives().FirstOrDefault(p => destinationDriveName.Contains(p.Name, StringComparison.InvariantCultureIgnoreCase));
            if (destinationDriveInfo == null) {
                return Result.Fail<ValidArguments>($"File name {destinationFileName} is not valid!");
            }

            if (destinationDriveInfo.TotalFreeSpace < sourceFileInfo.Length) {
                return Result.Fail<ValidArguments>($"There are no enough free space! Needs {String.Format(sourceFileInfo.Length,"000,000,000,000")} but have got {destinationDriveInfo.TotalFreeSpace}");
            }

            if (File.Exists(destinationFileName)) {
                //return Result.Fail<ValidArguments>($"{destinationFileName} - destination file already exist!");
            }

            validParameters.Command = commandEnum;
            validParameters.SourceFileName = sourceFileName;
            validParameters.DestinationFileName = destinationFileName;

            if (args.Length == 4 && Int32.TryParse(args[3], out int defaultSize)) {
                validParameters.DefaultUncompressedBlockSize = defaultSize;
            }

            if (args.Length == 5 && Int32.TryParse(args[4], out int processorCount)) {
                validParameters.ProcessorCount = processorCount;
            }
            
            return Result.Ok<ValidArguments>(validParameters);
        }

    }
}
