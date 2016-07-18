﻿namespace Fr8.Infrastructure.Data.DataTransferObjects
{
    public class FileDTO
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; }
        public string CloudStorageUrl { get; set; }
        public string Tags { get; set; }
    }
}