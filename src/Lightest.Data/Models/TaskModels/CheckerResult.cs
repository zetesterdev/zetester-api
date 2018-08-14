﻿namespace Lightest.Data.Models.TaskModels
{
    public class CheckerResult
    {
        public int UploadId { get; set; }

        public string Type { get; set; }

        public string Status { get; set; }

        public string Message { get; set; }

        public int SuccessfulTests { get; set; }

        public int FailedTest { get; set; }
    }
}