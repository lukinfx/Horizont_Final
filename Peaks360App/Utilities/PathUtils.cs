using System;
using System.IO;
using Android.Content;
using Newtonsoft.Json;
using Java.Lang;
using Java.Nio;
using Android.Media;
using Android.Graphics;
using Android.Provider;
using ExifLib;
using Peaks360App.DataAccess;
using Peaks360Lib.Domain.Models;
using Peaks360App.AppContext;

namespace Peaks360App.Utilities
{
    public class PathUtil
    {
        public static string GetPath(Context context, Android.Net.Uri uri)
        {
            string selection = null;
            string[] selectionArgs = null;
            if (DocumentsContract.IsDocumentUri(context, uri))
            {
                if (isExternalStorageDocument(uri))
                {
                    string docId = DocumentsContract.GetDocumentId(uri);
                    var split = docId.Split(":");
                    return Android.OS.Environment.ExternalStorageDirectory + "/" + split[1];
                }
                else if (isDownloadsDocument(uri))
                {
                    string id = DocumentsContract.GetDocumentId(uri);
                    uri = ContentUris.WithAppendedId(
                        Android.Net.Uri.Parse("content://downloads/public_downloads"), Long.ParseLong(id));
                }
                else if (isMediaDocument(uri))
                {
                    string docId = DocumentsContract.GetDocumentId(uri);
                    var split = docId.Split(":");
                    string type = split[0];
                    if ("image".Equals(type))
                    {
                        uri = MediaStore.Images.Media.ExternalContentUri;
                    }
                    else if ("video".Equals(type))
                    {
                        uri = MediaStore.Video.Media.ExternalContentUri;
                    }
                    else if ("audio".Equals(type))
                    {
                        uri = MediaStore.Audio.Media.ExternalContentUri;
                    }

                    selection = "_id=?";
                    selectionArgs = new string[] {split[1]};
                }
            }

            if ("content".Equals(uri.Scheme.ToLower()))
            {
                string[] projection = {MediaStore.Images.Media.InterfaceConsts.Data};
                try
                {
                    var cursor = context.ContentResolver.Query(uri, projection, selection, selectionArgs, null);
                    int column_index = cursor.GetColumnIndexOrThrow(MediaStore.Images.Media.InterfaceConsts.Data);
                    if (cursor.MoveToFirst())
                    {
                        return cursor.GetString(column_index);
                    }
                }
                catch (System.Exception)
                {
                    //TODO: error handling
                }
            }
            else if ("file".Equals(uri.Scheme.ToLower()))
            {
                return uri.Path;
            }

            return null;
        }


        public static bool isExternalStorageDocument(Android.Net.Uri uri)
        {
            return "com.android.externalstorage.documents".Equals(uri.Authority);
        }

        public static bool isDownloadsDocument(Android.Net.Uri uri)
        {
            return "com.android.providers.downloads.documents".Equals(uri.Authority);
        }

        public static bool isMediaDocument(Android.Net.Uri uri)
        {
            return "com.android.providers.media.documents".Equals(uri.Authority);
        }
    }
}