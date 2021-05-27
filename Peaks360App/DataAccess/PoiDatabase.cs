using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Peaks360App.Extensions;
using SQLite;
using Peaks360Lib.Domain.Models;
using Peaks360Lib.Utilities;
using Peaks360Lib.Domain.ViewModel;
using Peaks360Lib.Domain.Enums;

namespace Peaks360App.DataAccess
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

                    if (!Database.TableMappings.Any(m => m.MappedType.Name == typeof(PhotoData).Name))
                    {
                        Database.CreateTablesAsync(CreateFlags.None, typeof(PhotoData)).ConfigureAwait(false);
                    }

                    if (!Database.TableMappings.Any(m => m.MappedType.Name == typeof(DownloadedElevationData).Name))
                    {
                        Database.CreateTablesAsync(CreateFlags.None, typeof(DownloadedElevationData)).ConfigureAwait(false);
                    }
                    

                    initialized = true;
                }
            }
            catch(Exception)
            {
                throw;
            }
            
        }

        #region PoisToDownload

        public async Task<IEnumerable<PoisToDownload>> GetDownloadedPoisAsync()
        {
            var task = Database.Table<PoisToDownload>().ToListAsync();
            task.Wait();
            return task.Result;
        }

        public bool IsAnyDownloadedPois()
        {
            var task = Database.Table<PoisToDownload>().CountAsync();
            task.Wait();
            return task.Result > 0;
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

        #endregion PoisToDownload

        #region Poi

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

        public async Task<int> GetItemCount()
        {
            return await Database.Table<Poi>().CountAsync();
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
            Peaks360Lib.Utilities.GpsUtils.BoundingRect(loc, distance, out min, out max);

            var query = $@"SELECT * FROM [Poi] WHERE 1=1
            and [Longitude] > {min.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)} 
            and [Longitude] < {max.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)} 
            and [Latitude] > {min.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)} 
            and [Latitude] < {max.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

            var task = Database.QueryAsync<Poi>(query);
            task.Wait();
            return task.Result;
        }

        internal PoiViewItem GetNearestPoi(GpsLocation loc, IGpsUtilities iGpsUtilities)
        {
            var candidates = GetItems(loc, 0.2);
            List<PoiViewItem> items = new PoiViewItemList(candidates, loc, iGpsUtilities);
            var item = items.OrderBy(x => x.GpsLocation.Distance).FirstOrDefault();
            return item;
        }

        public IEnumerable<Poi> GetMyItems()
        {
            return Database.QueryAsync<Poi>($"SELECT * FROM [Poi] WHERE [Source] = \"{Guid.Empty.ToString()}\"").Result;
        }

        public async Task<IEnumerable<Poi>> GetFavoriteItemsAsync()
        {
            return await Database.QueryAsync<Poi>("SELECT * FROM [Poi] WHERE [Favorite] = true");
        }

        public IEnumerable<Poi> FindItems(string name, PoiCategory? category, PoiCountry? country, bool favourites)
        {
            var query = $"SELECT * FROM [Poi] WHERE 1=1";

            if (name != null)
            {
                var nameNoAccent = name?.RemoveDiacritics().ToLower();
                query += $" AND LOWER([NameNoAccent]) LIKE '%{nameNoAccent}%'";
            }

            if (country != null)
            {
                query += $" AND Country = {(int)country}";
            }
            
            if (category != null)
            {
                query += $" AND Category = {(int)category}";
            }

            if (favourites)
            {
                query += $" AND [Favorite] = true";
            }

            return Database.QueryAsync<Poi>(query).Result.Take(1000);
        }

        public async Task<IEnumerable<Poi>> FindItemsAsync(string name)
        {
            var nameNoAccent = name.RemoveDiacritics().ToLower();

            return await Database.QueryAsync<Poi>($"SELECT * FROM [Poi] WHERE LOWER([NameNoAccent]) LIKE '%{nameNoAccent}%'");
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
            item.NameNoAccent = item.Name.RemoveDiacritics().ToLower();

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
            foreach (var item in items)
            {
                item.NameNoAccent = item.Name.RemoveDiacritics().ToLower();
            }

            var task = Database.InsertAllAsync(items);
            task.Wait();
            return task.Result;
        }

        public async Task<int> UpdateItemAsync(Poi item)
        {
            item.NameNoAccent = item.Name.RemoveDiacritics().ToLower();

            return await Database.UpdateAsync(item);
        }
        public int UpdateItem(Poi item)
        {
            item.NameNoAccent = item.Name.RemoveDiacritics().ToLower();

            var task = Database.UpdateAsync(item);
            task.Wait();
            return task.Result;
        }

        public async Task<int> DeleteItemAsync(Poi item)
        {
            return await Database.DeleteAsync(item);
        }

        #endregion Poi

        #region PhotoData

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

        public async Task<IEnumerable<PhotoData>> GetPhotoDataItemsAsync()
        {
            return await Database.Table<PhotoData>().ToListAsync();
        }

        public IEnumerable<PhotoData> GetPhotoDataItems()
        {
            var task = Database.Table<PhotoData>().ToListAsync();
            task.Wait();
            return task.Result;
        }

        public PhotoData GetPhotoDataItem(long id)
        {
            var task = Database.Table<PhotoData>().Where(i => i.Id == id).FirstOrDefaultAsync();
            task.Wait();
            if (!task.Result.LeftTiltCorrector.HasValue || task.Result.LeftTiltCorrector < -10000 || task.Result.LeftTiltCorrector > 10000)
                task.Result.LeftTiltCorrector = 0;
            if (!task.Result.RightTiltCorrector.HasValue || task.Result.RightTiltCorrector < -10000 || task.Result.RightTiltCorrector > 10000)
                task.Result.RightTiltCorrector = 0;

            return task.Result;
        }

        #endregion PhotoData

        #region DownloadedElevationData

        public int InsertItem(DownloadedElevationData item)
        {
            var task = Database.InsertAsync(item);
            task.Wait();
            return task.Result;
        }

        public int UpdateItem(DownloadedElevationData item)
        {
            var task = Database.UpdateAsync(item);
            task.Wait();
            return task.Result;
        }

        public int DeleteItem(DownloadedElevationData item)
        {
            var task = Database.DeleteAsync(item);
            task.Wait();
            return task.Result;
        }

        public int DeleteAllDownloadedElevationData()
        {
            var task = Database.Table<DownloadedElevationData>().DeleteAsync(x => true);
            task.Wait();
            return task.Result;
        }

        public async Task<IEnumerable<DownloadedElevationData>> GetDownloadedElevationDataAsync()
        {
            return await Database.Table<DownloadedElevationData>().ToListAsync();
        }

        public IEnumerable<DownloadedElevationData> GetDownloadedElevationData()
        {
            var task = GetDownloadedElevationDataAsync();
            task.Wait();
            return task.Result;
        }

        public DownloadedElevationData GetDownloadedElevationDataItem(long id)
        {
            var task = Database.Table<DownloadedElevationData>().Where(i => i.Id == id).FirstOrDefaultAsync();
            task.Wait();
            return task.Result;
        }

        #endregion DownloadedElevationData
    }
}
