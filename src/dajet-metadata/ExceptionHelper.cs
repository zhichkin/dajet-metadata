using System;

namespace DaJet.Metadata
{
    public static class ExceptionHelper
    {
        public static string GetErrorText(Exception ex)
        {
            string errorText = string.Empty;
            Exception error = ex;
            while (error != null)
            {
                errorText += (errorText == string.Empty) ? error.Message : Environment.NewLine + error.Message;
                error = error.InnerException;
            }
            return errorText;
        }
        public static string GetErrorTextAndStackTrace(Exception ex)
        {
            string errorText = GetErrorText(ex);

            string stackTrace = string.IsNullOrEmpty(ex.StackTrace)
                ? string.Empty
                : ex.StackTrace;

            return errorText + Environment.NewLine + stackTrace;
        }
    }
}