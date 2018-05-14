﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Zetester.Data.Models
{
    public class ArchiveUpload
    {
        [BsonId]
        public string UploadId { get; set; }

        public BsonBinaryData File { get; set; }

        public string Language { get; set; }
    }
}