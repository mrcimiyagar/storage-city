using System.IO;
using Microsoft.EntityFrameworkCore;

namespace MessengerService
{
    public class SqliteDatabaseContext : DbContext
    {
        private readonly string _filesDirPath;
        private readonly long _fileId;
        
        public SqliteDatabaseContext(string filesDirPath, long fileId)
        {
            this._filesDirPath = filesDirPath;
            this._fileId = fileId;
        }
        
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseSqlite("Data Source=" + Path.Combine(_filesDirPath, _fileId.ToString()) + ";Version=3;");
    }
}