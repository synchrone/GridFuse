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

namespace GridFS
{
    class GridFS : DokanOperations, IDisposable
    {
        private int count_ = 1;
        private readonly IDisposable _disposableStartResult;
        public readonly MongoGridFS gridFS;

        public GridFS(string mongoDbConnectionString, string dbName)
        {
            var mongoServer = MongoServer.Create(mongoDbConnectionString);
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
            return Path.Combine("C:\\",path);
        }

        public int CreateFile(string filename, FileAccess access, FileShare share, FileMode mode, FileOptions options, DokanFileInfo info)
        {
            info.Context = count_++;
            var file = gridFS.FindOne(filename);
            if (file != null)
            {
                return 0;
            }
            return -DokanNet.ERROR_FILE_NOT_FOUND;
        }

        public int OpenDirectory(string filename, DokanFileInfo info)
        {
            info.Context = count_++;
            return -DokanNet.ERROR_PATH_NOT_FOUND;
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

        public int ReadFile(string filename, byte[] buffer, ref uint readBytes, long offset, DokanFileInfo info)
        {
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

        public int WriteFile(string filename, byte[] buffer, ref uint writtenBytes, long offset, DokanFileInfo info)
        {
            return -1;
        }

        public int FlushFileBuffers(string filename, DokanFileInfo info)
        {
            return -1;
        }

        public int GetFileInformation(string filename, FileInformation fileinfo, DokanFileInfo info)
        {
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
            filename = GetPath(filename);

            var regex = String.Format(@"{0}[^\\]+\\?(?=.*)$", Regex.Escape(filename));
            var matchingFiles = gridFS.Find(
                Query.Matches("filename", new BsonRegularExpression(regex, "i"))
            );
            if(matchingFiles.Any())
            {
                foreach (var fsFileInfo in matchingFiles)
                {
                    files.Add(fsFileInfo.GetFileInformation());
                }
                return 0;
            }else
            {
                return -1;
            }
        }

        public int SetFileAttributes(string filename, FileAttributes attr, DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public int SetFileTime(string filename, DateTime ctime, DateTime atime, DateTime mtime, DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public int DeleteFile(string filename, DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public int DeleteDirectory(string filename, DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public int MoveFile(string filename, string newname, bool replace, DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public int SetEndOfFile(string filename, long length, DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public int SetAllocationSize(string filename, long length, DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public int LockFile(string filename, long offset, long length, DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public int UnlockFile(string filename, long offset, long length, DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public int GetDiskFreeSpace(ref ulong freeBytesAvailable, ref ulong totalBytes, ref ulong totalFreeBytes, DokanFileInfo info)
        {
            throw new NotImplementedException();
        }

        public int Unmount(DokanFileInfo info)
        {
            throw new NotImplementedException();
        }
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
    }
}
