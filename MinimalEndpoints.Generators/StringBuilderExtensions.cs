using System.Text;

namespace MinimalEndpoints.Generators;

public static class StringBuilderExtensions
{
    /// <summary>
    /// Appends a new line to the StringBuilder.
    /// </summary>
    public static StringBuilder NewLine(this StringBuilder sb)
    {
        return sb.AppendLine();
    }

    /// <summary>
    /// Appends a string with the specified number of tab indentations.
    /// </summary>
    /// <param name="tabs">The number of tabs to prepend.</param>
    /// <param name="value">The string value to append.</param>
    public static StringBuilder AppendWithTab(this StringBuilder sb, int tabs, string value)
    {
        if (tabs > 0)
        {
            sb.Append(new string('\t', tabs));
        }
        return sb.Append(value);
    }

    /// <summary>
    /// Appends a line with the specified number of tab indentations.
    /// </summary>
    /// <param name="tabs">The number of tabs to prepend.</param>
    /// <param name="value">The string value to append.</param>
    public static StringBuilder AppendLineWithTab(this StringBuilder sb, int tabs, string value)
    {
        if (tabs > 0)
        {
            sb.Append(new string('\t', tabs));
        }
        return sb.AppendLine(value);
    }

    /// <summary>
    /// Appends a formatted string with the specified number of tab indentations.
    /// </summary>
    /// <param name="tabs">The number of tabs to prepend.</param>
    /// <param name="format">The composite format string.</param>
    /// <param name="args">The object array that contains zero or more objects to format.</param>
    public static StringBuilder AppendFormatWithTab(this StringBuilder sb, int tabs, string format, params object[] args)
    {
        if (tabs > 0)
        {
            sb.Append(new string('\t', tabs));
        }
        return sb.AppendFormat(format, args);
    }


    public static StringBuilder AppendFormattedLineWithTab(this StringBuilder sb, int tabs, string format, params object[] args)
    {
        if (tabs > 0)
        {
            sb.Append(new string('\t', tabs));
        }
        return sb.AppendFormat(format, args).NewLine();
    }
}
