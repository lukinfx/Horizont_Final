using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HorizontApp.Domain.Models;
using HorizontApp.Utilities;
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
                    }
                    
                    if (!Database.TableMappings.Any(m => m.MappedType.Name == typeof(PoisToDownload).Name))
                    {
                        Database.CreateTablesAsync(CreateFlags.None, typeof(PoisToDownload)).ConfigureAwait(false);
                    }

                    initialized = true;
                }
            }
            catch(Exception ex)
            {
                throw;
            }
            
        }

        public async Task<IEnumerable<PoisToDownload>> GetDownloadedPoisAsync()
        {
            var task = Database.Table<PoisToDownload>().ToListAsync();
            task.Wait();
            return task.Result;
        }

        public int InsertItem(PoisToDownload item)
        {
            var task = Database.InsertAsync(item);
            task.Wait();
            return task.Result; 
        }

        public int UpdateItem(PoisToDownload item)
        {
            var task = Database.UpdateAsync(item);
            task.Wait();
            return task.Result;
        }

        public int DeleteItem(PoisToDownload item)
        {
            var task = Database.DeleteAsync(item);
            task.Wait();
            return task.Result;
        }

        public async Task<IEnumerable<Poi>> GetItemsAsync()
        {
            return await Database.Table<Poi>().ToListAsync();
        }

        public IEnumerable<Poi> GetItems()
        {
            var task = Database.Table<Poi>().ToListAsync();
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// Returns all point within a given distance (in kilometers)
        /// </summary>
        /// <param name="loc"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public IEnumerable<Poi> GetItems(GpsLocation loc, double distance)
        {
            GpsLocation min;
            GpsLocation max;
            GpsUtils.BoundingRect(loc, distance, out min, out max);

            //TODO: resolve problem with +-180 dg
            var query = @$"SELECT * FROM [Poi] WHERE 1=1
            and [Longitude] > {min.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)} 
            and [Longitude] < {max.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)} 
            and [Latitude] > {min.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)} 
            and [Latitude] < {max.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

            var task = Database.QueryAsync<Poi>(query);
            task.Wait();
            return task.Result;
        }

        public async Task<IEnumerable<Poi>> GetFavoriteItemsAsync()
        {
            return await Database.QueryAsync<Poi>("SELECT * FROM [Poi] WHERE [Favorite] = true");
        }

        public async Task<Poi> GetItemAsync(long id)
        {
            return await Database.Table<Poi>().Where(i => i.Id == id).FirstOrDefaultAsync();
        }

        public Poi GetItem(long id)
        {
            var task = Database.Table<Poi>().Where(i => i.Id == id).FirstOrDefaultAsync();
            task.Wait();
            return task.Result;
        }

        public int InsertItemAsync(Poi item)
        {
            var task = Database.InsertAsync(item);
            task.Wait();
            return task.Result;
        }

        public int DeleteAllFromSource(Guid source)
        {
            var task = Database.ExecuteAsync($"DELETE FROM [Poi] WHERE [Source] = \"{source.ToString()}\"");
            task.Wait();
            return task.Result;
        }

        public int InsertAll(IEnumerable<Poi> items)
        {
            var task = Database.InsertAllAsync(items);
            task.Wait();
            return task.Result;
        }

        public async Task<int> UpdateItemAsync(Poi item)
        {
            return await Database.UpdateAsync(item);
        }
        public int UpdateItem(Poi item)
        {
            var task = Database.UpdateAsync(item);
            task.Wait();
            return task.Result;
        }

        public async Task<int> DeleteItemAsync(Poi item)
        {
            return await Database.DeleteAsync(item);
        }
    }
}
