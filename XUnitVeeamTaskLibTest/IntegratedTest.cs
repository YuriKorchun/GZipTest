using System;
using System.IO;
using System.Text;
using VeeamTaskLib;
using Xunit;

namespace XUnitVeeamTaskLibTest {
    public class IntegratedTest {
        [Theory]
        [InlineData(1048576,1, 1000000)]
        [InlineData(1048576, 4, 100000)]
        [InlineData(1048576, 0, 1000000)]
        [InlineData(1024, 1, 1000000)]
        [InlineData(1024, 2, 1000000)]
        public void TotalTest(int defaultSize, int processorCount, int blockCount) {
            var originalFile = @"c:\_TMP\test.file";
            var builder = new StringBuilder();
            for (int i = 0; i < blockCount; i++) {
                builder.Append(i.ToString() + " ");
            }
            string testString = builder.ToString();
            System.IO.File.WriteAllText(originalFile, testString);

            var commpressedFile = @"c:\_TMP\test.zip";
            File.Delete(commpressedFile);
            var veeamTask = new VeeamTask();
            string[] args = {
                "compress",
                originalFile,
                commpressedFile,
                defaultSize.ToString(),
                processorCount.ToString()
            };
            var result = veeamTask.Run(args);
            Assert.True(result.IsSuccess, result.Error);

            var uncomressedFile = @"c:\_TMP\test_copy.file";
            string[] args2 = {
                "decompress",
                commpressedFile,
                uncomressedFile,
                defaultSize.ToString(),
                processorCount.ToString()
            };
            var result2 = veeamTask.Run(args2);
            Assert.True(result.IsSuccess);

            var firstResult = FileEquals(originalFile, uncomressedFile);
            Assert.True(firstResult.IsSuccess, firstResult.Error);

            var secondResult = FileEquals(uncomressedFile, originalFile);
            Assert.True(secondResult.IsSuccess, secondResult.Error);

        }

        static Result FileEquals(string path1, string path2) {
            try {
                byte[] file1 = File.ReadAllBytes(path1);
                byte[] file2 = File.ReadAllBytes(path2);
                if (file1.Length == file2.Length) {
                    for (int i = 0; i < file1.Length; i++) {
                        if (file1[i] != file2[i]) {
                            return Result.Fail("Files are not equal!");
                        }
                    }
                    return Result.Ok();
                }
                return Result.Fail("Files are not equal!");
            } catch (Exception ex) {
                return Result.Fail($"Exception {ex.Message}");
            }
        }
    }
}
