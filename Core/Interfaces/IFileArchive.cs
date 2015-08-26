﻿
using System.IO;
using Data.Entities;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for File Archive service
    /// </summary>
    public interface IFileArchive
    {
        /// <summary>
        /// Writes the file into file repository
        /// </summary>
        void WriteFile(FileDO fileDo, FileStream file, string fileName);

        /// <summary>
        /// Reads file from repository
        /// </summary>
        byte[] ReadFile(FileDO curFile);

        /// <summary>
        /// Deletes file from repository
        /// </summary>
        bool DeleteFile(FileDO curFile);
    }
}
