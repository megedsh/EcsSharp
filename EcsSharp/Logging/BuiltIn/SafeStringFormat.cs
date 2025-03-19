using System;
using System.Text;

namespace EcsSharp.Logging.BuiltIn
{
    public sealed class SafeStringFormat

    {
        private const    string          DefaultNullText = "(null)";
        private readonly IFormatProvider m_provider;
        private readonly string          m_format;
        private readonly object[]        m_args;

        public SafeStringFormat(IFormatProvider provider, string format, params object[] args)
        {
            m_provider = provider;
            m_format = format;
            m_args = args;
        }

        public override string ToString() => stringFormat(m_provider, m_format, m_args);

        private static string stringFormat(
            IFormatProvider provider,
            string format,
            params object[] args)
        {
            try
            {
                if (format == null)
                {
                    return null;
                }

                return args == null ? format : string.Format(provider, format, args);
            }
            catch (Exception ex)
            {
                Console.WriteLine("StringFormat: Exception while rendering format [{0}], Exception: {1} - ",format, ex);
                return stringFormatError(ex, format, args);
            }
        }

        private static string stringFormatError(
            Exception formatException,
            string format,
            object[] args)
        {
            try
            {
                StringBuilder buffer = new StringBuilder("<Error>");
                if (formatException != null)
                {
                    buffer.Append("Exception during StringFormat: ").Append(formatException.Message);
                }
                else
                {
                    buffer.Append("Exception during StringFormat");
                }

                buffer.Append(" <format>").Append(format).Append("</format>");
                buffer.Append("<args>");
                renderArray(args, buffer);
                buffer.Append("</args>");

                if (formatException != null)
                {
                    System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
                    buffer.Append("<stackTrace>");
                    buffer.Append(t);
                    buffer.Append("</stackTrace>");
                }

                buffer.Append("</Error>");
                return buffer.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"StringFormat: INTERNAL ERROR during StringFormat error handling :{ex.Message}");
                return "<Error>Exception during StringFormat. See Internal Log.</Error>";
            }
        }

        private static void renderArray(Array array, StringBuilder buffer)
        {
            if (array == null)
            {
                buffer.Append(DefaultNullText);
            }
            else if (array.Rank != 1)
            {
                buffer.Append(array);
            }
            else
            {
                buffer.Append("{");
                int length = array.Length;
                if (length > 0)
                {
                    renderObject(array.GetValue(0), buffer);
                    for (int index = 1; index < length; ++index)
                    {
                        buffer.Append(", ");
                        renderObject(array.GetValue(index), buffer);
                    }
                }

                buffer.Append("}");
            }
        }

        private static void renderObject(object obj, StringBuilder buffer)
        {
            if (obj == null)
            {
                buffer.Append(DefaultNullText);
            }
            else
            {
                try
                {
                    buffer.Append(obj);
                }
                catch (Exception ex)
                {
                    buffer.Append("<Exception: ").Append(ex.Message).Append(">");
                }
            }
        }
    }
}