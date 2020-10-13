using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HorizontLib.Domain.Models;
using SQLite;

namespace HorizontApp.DataAccess
{
    public static class ConstantsPhotoDatabase
    {
        public const string DatabaseFilename = "PhotoList.db3";

        public const SQLite.SQLiteOpenFlags Flags =
            // open the database in read/write mode
            SQLite.SQLiteOpenFlags.ReadWrite |
            // create the database if it doesn't exist
            SQLite.SQLiteOpenFlags.Create |
            // enable multi-threaded database access
            SQLite.SQLiteOpenFlags.SharedCache;

        public static string DatabasePath
        {
            get
            {
                var basePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(basePath, DatabaseFilename);
            }
        }
    }

    public class PhotoDatabase
    {
        static readonly Lazy<SQLiteAsyncConnection> lazyInitializer = new Lazy<SQLiteAsyncConnection>(() =>
        {
            return new SQLiteAsyncConnection(ConstantsPhotoDatabase.DatabasePath, ConstantsPhotoDatabase.Flags);
        });

        static SQLiteAsyncConnection Database => lazyInitializer.Value;
        static bool initialized = false;

        public PhotoDatabase()
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                if (!initialized)
                {
                    if (!Database.TableMappings.Any(m => m.MappedType.Name == typeof(PhotoData).Name))
                    {
                        Database.CreateTablesAsync(CreateFlags.None, typeof(PhotoData)).ConfigureAwait(false);
                    }
                    initialized = true;
                }
            }
            catch (Exception ex)
            {
                throw;
            }

        }


        public int InsertItem(PhotoData item)
        {
            var task = Database.InsertAsync(item);
            task.Wait();
            return task.Result;
        }

        public int UpdateItem(PhotoData item)
        {
            var task = Database.UpdateAsync(item);
            task.Wait();
            return task.Result;
        }

        public int DeleteItem(PhotoData item)
        {
            var task = Database.DeleteAsync(item);
            task.Wait();
            return task.Result;
        }

        public async Task<IEnumerable<PhotoData>> GetItemsAsync()
        {
            return await Database.Table<PhotoData>().ToListAsync();
        }

        public IEnumerable<PhotoData> GetItems()
        {
            var task = Database.Table<PhotoData>().ToListAsync();
            task.Wait();
            return task.Result;
        }

        /*public IEnumerable<PhotoData> GetItem(string fileName)
        {
            return Database.QueryAsync<PhotoData>($"SELECT * FROM [PhotoData] WHERE [PhotoFileName] = \"{fileName}\"").Result;
        }*/


        public int DeleteAllFromSource(Guid source)
        {
            var task = Database.ExecuteAsync($"DELETE FROM [PhotoData]");
            task.Wait();
            return task.Result;
        }
    }
}