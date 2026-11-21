using System.Text;

namespace ryvo_interview_be.Api;

/// <summary>
/// Minimal RFC 4180 CSV writer: CRLF row endings, quoting only when a value needs
/// it, and doubled quotes inside quoted fields. It never formats values itself,
/// so callers stay responsible for using an invariant culture.
/// </summary>
public sealed class CsvBuilder
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly StringBuilder _builder = new();

    public CsvBuilder Row(params string?[] values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0)
                _builder.Append(',');
            _builder.Append(Escape(values[i]));
        }

        _builder.Append("\r\n");
        return this;
    }

    public byte[] ToUtf8Bytes() => Utf8WithoutBom.GetBytes(_builder.ToString());

    private static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var needsQuoting =
            value.Contains(',') ||
            value.Contains('"') ||
            value.Contains('\r') ||
            value.Contains('\n') ||
            value[0] == ' ' ||
            value[^1] == ' ';

        return needsQuoting
            ? $"\"{value.Replace("\"", "\"\"")}\""
            : value;
    }
}
