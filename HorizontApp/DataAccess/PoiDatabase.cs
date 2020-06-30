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

        public async Task<IEnumerable<Poi>> GetItemsAsync()
        {
            return await Database.Table<Poi>().ToListAsync();
        }

        public IEnumerable<Poi> GetItems()
        {
            var result = Database.Table<Poi>().ToListAsync();
            result.Wait();
            return result.Result;
        }

        public async Task<IEnumerable<Poi>> GetFavoriteItemsAsync()
        {
            return await Database.QueryAsync<Poi>("SELECT * FROM [Poi] WHERE [Favorite] = true");
        }

        public async Task<Poi> GetItemAsync(long id)
        {
            return await Database.Table<Poi>().Where(i => i.Id == id).FirstOrDefaultAsync();
        }

        public async Task<int> InsertItemAsync(Poi item)
        {
            return await Database.InsertAsync(item);
        }

        public async Task<int> InsertAllAsync(IEnumerable<Poi> items)
        {
            return await Database.InsertAllAsync(items);
        }

        public async Task<int> UpdateItemAsync(Poi item)
        {
            return await Database.UpdateAsync(item);
        }

        public async Task<int> DeleteItemAsync(Poi item)
        {
            return await Database.DeleteAsync(item);
        }
    }
}
