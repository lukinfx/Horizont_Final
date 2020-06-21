using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HorizontApp.Domain.Models;
using SQLite;

namespace HorizontApp.DataAccess
{
    public static class Constants
    {
        public const string DatabaseFilename = "PoiList.db3";

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
                var basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(basePath, DatabaseFilename);
            }
        }
    }

    public class PoiDatabase
    {
        static readonly Lazy<SQLiteAsyncConnection> lazyInitializer = new Lazy<SQLiteAsyncConnection>(() =>
        {
            return new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
        });

        static SQLiteAsyncConnection Database => lazyInitializer.Value;
        static bool initialized = false;

        public PoiDatabase()
        {
            Initialize()/*.SafeFireAndForget(false)*/;
        }

        private void Initialize()
        {
            try
            {
                if (!initialized)
                {
                    if (!Database.TableMappings.Any(m => m.MappedType.Name == typeof(Poi).Name))
                    {
                        Database.CreateTablesAsync(CreateFlags.None, typeof(Poi)).ConfigureAwait(false);
                        initialized = true;
                    }
                }
            }
            catch(Exception ex)
            {
                throw;
            }
            
        }

        public Task<List<Poi>> GetItemsAsync()
        {
            return Database.Table<Poi>().ToListAsync();
        }

        public Task<List<Poi>> GetFavoriteItemsAsync()
        {
            return Database.QueryAsync<Poi>("SELECT * FROM [Poi] WHERE [Favorite] = true");
        }

        public Task<Poi> GetItemAsync(long id)
        {
            return Database.Table<Poi>().Where(i => i.Id == id).FirstOrDefaultAsync();
        }

        public Task<int> InsertItemAsync(Poi item)
        {
            return Database.InsertAsync(item);
        }

        public Task<int> UpdateItemAsync(Poi item)
        {
            return Database.UpdateAsync(item);
        }

        public Task<int> DeleteItemAsync(Poi item)
        {
            return Database.DeleteAsync(item);
        }
    }
}
