using System;
using System.Text;

namespace KindomDataAPIServer.Common
{
    public static class ExceptionLogHelper
    {
        public static string Format(Exception ex)
        {
            if (ex == null)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            AppendException(builder, "Exception", ex);

            var innerException = ex.InnerException;
            var depth = 1;
            while (innerException != null)
            {
                builder.AppendLine();
                AppendException(builder, "InnerException " + depth, innerException);
                innerException = innerException.InnerException;
                depth++;
            }

            return builder.ToString();
        }

        private static void AppendException(StringBuilder builder, string label, Exception ex)
        {
            builder.Append(label)
                .Append(": ")
                .Append(ex.GetType().FullName)
                .Append(": ")
                .Append(ex.Message);

            if (!string.IsNullOrEmpty(ex.StackTrace))
            {
                builder.AppendLine()
                    .Append(ex.StackTrace);
            }
        }
    }
}
