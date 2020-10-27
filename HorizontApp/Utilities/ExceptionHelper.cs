using System;

namespace HorizontApp.Utilities
{
    public class ExceptionHelper
    {
        public static string Exception2ErrorMessage(Exception ex)
        {
            //TODO: Use also ex.InnerException
            //ex.Message + ex.InnerException.Message + ex.InnerException.InnerException.Message ...
            return ex.Message;
        }
    }
}