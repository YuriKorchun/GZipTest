using System;
using System.Collections.Generic;
using System.Text;

namespace VeeamTaskLib {
    public class VeeamTask {

        public Result Run(string[] args) {
            var checkArgumentsResult = ArgumentsValidator.Check(args);
            if (checkArgumentsResult.IsFailure) {
                return checkArgumentsResult;
            }
            var fileProcessor = new FileProcessor();
            var processResult = fileProcessor.Process(checkArgumentsResult.Value);
            return processResult;
        }
    }
}
