using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Dokan;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.GridFS;
using MongoDB.Driver.Wrappers;

namespace GridFS
{
    class GridFS : DokanOperations, IDisposable
    {
        private int count_ = 1;
        private readonly IDisposable _disposableStartResult;
        protected readonly MongoGridFS gridFS;

        public GridFS(string mongoDbConnectionString, string dbName)
        {
            var mongoClient = new MongoClient(mongoDbConnectionString);
            var mongoServer = mongoClient.GetServer();
            var mongoDatabase = mongoServer.GetDatabase(dbName);
            _disposableStartResult = mongoServer.RequestStart(mongoDatabase);
            gridFS = mongoDatabase.GridFS;
        }

        public void Dispose()
        {
            _disposableStartResult.Dispose();
        }

        protected string GetPath(string path)
        {
            return Path.Combine(@"C:\",path.TrimStart('\\'));
        }

        public int CreateFile(string filename, FileAccess access, FileShare share, FileMode mode, FileOptions options, DokanFileInfo info)
        {
            Trace.TraceInformation("CreateFile: {0}", filename);
            return gridFS.FindFileOrDir(GetPath(filename)) != null ? 0 : -DokanNet.ERROR_FILE_NOT_FOUND;
        }

        public int OpenDirectory(string filename, DokanFileInfo info)
        {
            Trace.TraceInformation("OpenDirectory: {0}", filename);
            info.Context = count_++;
            if (filename == "\\")
            {
                return 0;
            }
            return -DokanNet.ERROR_PATH_NOT_FOUND;
        }

        public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info)
        {
            Trace.TraceInformation("ReadFile: {0}", filename);
            var file = gridFS.FindOne(GetPath(filename));
            try
            {
                using(var fs = file.OpenRead())
                {
                    fs.Seek(offset, SeekOrigin.Begin);
                    readBytes = (uint)fs.Read(buffer, 0, buffer.Length);
                    return 0;
                }
            }catch(Exception e)
            {
                Trace.TraceError(e.Message + Environment.NewLine + e.Message);
                return -1;
            }
        }

        public int GetFileInformation(string filename, FileInformation fileinfo, DokanFileInfo info)
        {
            Trace.TraceInformation("GetFileInformation: {0}", filename);
            if (filename == "\\")
            {
                fileinfo.Length = 0;
                fileinfo.Attributes = FileAttributes.Directory;
                fileinfo.CreationTime = fileinfo.LastAccessTime =  fileinfo.LastWriteTime = DateTime.Now;
                return 0;
            }
            var file = gridFS.FindOne(GetPath(filename));
            if(file != null)
            {
                file.UpdateFileInformation(ref fileinfo);
                return 0;
            }
            return -1;
        }

        public int FindFiles(string filename, ArrayList files, DokanFileInfo info)
        {
            Trace.TraceInformation("FindFiles: {0}",filename);
            filename = GetPath(filename);

            var regex = String.Format(@"{0}[^\\]+\\?(?=.*)$", Regex.Escape(filename));
            var matchingFiles = gridFS.Find(
                Query.Matches("filename", new BsonRegularExpression(regex, "i"))
            );

            foreach (var fsFileInfo in matchingFiles)
            {
                files.Add(fsFileInfo.GetFileInformation());
            }
            return 0;
        }

        public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info)
        {
            freeBytesAvailable = 500000000;
            totalBytes = (ulong)gridFS.Database.GetStats().DataSize + (freeBytesAvailable*2);
            totalFreeBytes = freeBytesAvailable;
            return 0;
        }

        public int Unmount(DokanFileInfo info)
        {
            return 0;
        }

        public int CreateDirectory(string filename, DokanFileInfo info)
        {
            return -1;
        }

        public int Cleanup(string filename, DokanFileInfo info)
        {
            return 0;
        }

        public int CloseFile(string filename, DokanFileInfo info)
        {
            return 0;
        }

        #region Write-related stuff

        public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info)
        {
            return -1;
        }

        public int FlushFileBuffers(string filename, DokanFileInfo info)
        {
            return -1;
        }

        public int SetFileAttributes(string filename, FileAttributes attr, DokanFileInfo info)
        {
            return -1;
        }

        public int SetFileTime(string filename, DateTime ctime, DateTime atime, DateTime mtime, DokanFileInfo info)
        {
            return -1;
        }

        public int DeleteFile(string filename, DokanFileInfo info)
        {
            return -1;
        }

        public int DeleteDirectory(string filename, DokanFileInfo info)
        {
            return -1;
        }

        public int MoveFile(string filename, string newname, bool replace, DokanFileInfo info)
        {
            return -1;
        }

        public int SetEndOfFile(string filename, long length, DokanFileInfo info)
        {
            return -1;
        }

        public int SetAllocationSize(string filename, long length, DokanFileInfo info)
        {
            return -1;
        }

        public int LockFile(string filename, long offset, long length, DokanFileInfo info)
        {
            return -1;
        }

        public int UnlockFile(string filename, long offset, long length, DokanFileInfo info)
        {
            return -1;
        }

        #endregion

    }
    public static class Extensions
    {
        public static void UpdateFileInformation(this MongoGridFSFileInfo file, ref FileInformation dokanFileInformation)
        {
            dokanFileInformation.Length = file.Length;
            dokanFileInformation.CreationTime = file.UploadDate;
            dokanFileInformation.Attributes = FileAttributes.ReadOnly;
            dokanFileInformation.LastAccessTime = DateTime.Now;
            dokanFileInformation.LastWriteTime = DateTime.Now;
        }
        public static FileInformation GetFileInformation(this MongoGridFSFileInfo that)
        {
            var finfo = new FileInformation();
            that.UpdateFileInformation(ref finfo);
            return finfo;
        }

        public static FileInformation FindFileOrDir(this MongoGridFS that, string filename)
        {
            var file = that.FindOne(filename);
            if (file != null)
            {
                return file.GetFileInformation();
            }
            
            // okay, we should search for a directory
            var containingDir = Path.GetDirectoryName(filename);
            var childCount = that.Files.Count(Query.Matches("filename", String.Format("/^{0}",containingDir)));
            if(childCount > 0)
            {
                return new FileInformation{
                    Attributes = FileAttributes.Directory,
                    CreationTime = DateTime.Now,
                    LastAccessTime = DateTime.Now,
                    LastWriteTime = DateTime.Now,
                    FileName = Path.GetFileName(filename),
                    Length = 0
                };
            }
            return null;
        }
    }
}
