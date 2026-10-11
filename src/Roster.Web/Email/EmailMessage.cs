using System.Net;
using System.Text;

namespace Roster.Web.Email;

/// <summary>What an email says: the same content becomes an HTML message and its plain-text alternative.</summary>
public sealed record EmailContentModel(
    string BrandName,
    string BrandColor,
    string Language,
    string Heading,
    IReadOnlyList<string> Paragraphs,
    string? ButtonLabel,
    string? ButtonUrl,
    string? ButtonFallback,
    string Footer);

/// <summary>
/// Builds the email in the brand look. Email clients ignore style sheets, so this is the one place where the styles are
/// inline, and it uses tables, which is what the clients render reliably. Everything that comes from people is encoded.
/// </summary>
public static class EmailMessage
{
    private const string Ink = "#2B2D42";
    private const string Muted = "#5C5F73";
    private const string Line = "#E3E4EC";
    private const string Page = "#F4F5F9";

    public static string ToHtml(EmailContentModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var color = SafeColor(model.BrandColor);
        var html = new StringBuilder();
        html.Append("<!DOCTYPE html><html lang=\"").Append(Enc(model.Language)).Append("\"><head><meta charset=\"utf-8\">")
            .Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\"><meta name=\"color-scheme\" content=\"light\">")
            .Append("<title>").Append(Enc(model.Heading)).Append("</title></head>");
        html.Append("<body style=\"margin:0;padding:0;background:").Append(Page).Append(";\">");
        html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"background:").Append(Page).Append(";\"><tr><td align=\"center\" style=\"padding:32px 16px;\">");
        html.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:520px;background:#FFFFFF;border:1px solid ").Append(Line).Append(";border-radius:16px;\">");

        // Brand bar
        html.Append("<tr><td style=\"padding:24px 32px 0 32px;font-family:Arial,Helvetica,sans-serif;font-size:20px;font-weight:800;color:").Append(color).Append(";\">")
            .Append(Enc(model.BrandName)).Append("</td></tr>");

        // Heading and text
        html.Append("<tr><td style=\"padding:16px 32px 0 32px;font-family:Arial,Helvetica,sans-serif;font-size:24px;line-height:30px;font-weight:800;color:").Append(Ink).Append(";\">")
            .Append(Enc(model.Heading)).Append("</td></tr>");
        foreach (var paragraph in model.Paragraphs)
        {
            html.Append("<tr><td style=\"padding:12px 32px 0 32px;font-family:Arial,Helvetica,sans-serif;font-size:16px;line-height:24px;color:").Append(Ink).Append(";\">")
                .Append(Enc(paragraph).Replace("\n", "<br>", StringComparison.Ordinal)).Append("</td></tr>");
        }

        // Button
        if (model.ButtonLabel is not null && model.ButtonUrl is not null)
        {
            html.Append("<tr><td style=\"padding:24px 32px 0 32px;\"><table role=\"presentation\" cellpadding=\"0\" cellspacing=\"0\"><tr>")
                .Append("<td bgcolor=\"").Append(color).Append("\" style=\"border-radius:12px;background:").Append(color).Append(";\">")
                .Append("<a href=\"").Append(Enc(model.ButtonUrl)).Append("\" style=\"display:inline-block;padding:14px 28px;font-family:Arial,Helvetica,sans-serif;font-size:16px;font-weight:700;color:#FFFFFF;text-decoration:none;border-radius:12px;\">")
                .Append(Enc(model.ButtonLabel)).Append("</a></td></tr></table></td></tr>");

            if (model.ButtonFallback is not null)
            {
                html.Append("<tr><td style=\"padding:20px 32px 0 32px;font-family:Arial,Helvetica,sans-serif;font-size:13px;line-height:20px;color:").Append(Muted).Append(";\">")
                    .Append(Enc(model.ButtonFallback)).Append("<br><a href=\"").Append(Enc(model.ButtonUrl)).Append("\" style=\"color:").Append(color).Append(";word-break:break-all;\">")
                    .Append(Enc(model.ButtonUrl)).Append("</a></td></tr>");
            }
        }

        // Footer
        html.Append("<tr><td style=\"padding:28px 32px 24px 32px;\"><div style=\"border-top:1px solid ").Append(Line)
            .Append(";padding-top:16px;font-family:Arial,Helvetica,sans-serif;font-size:12px;line-height:18px;color:").Append(Muted).Append(";\">")
            .Append(Enc(model.Footer)).Append("</div></td></tr>");

        html.Append("</table></td></tr></table></body></html>");
        return html.ToString();
    }

    public static string ToPlainText(EmailContentModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var text = new StringBuilder();
        text.AppendLine(model.Heading).AppendLine();
        foreach (var paragraph in model.Paragraphs)
        {
            text.AppendLine(paragraph).AppendLine();
        }

        if (model.ButtonLabel is not null && model.ButtonUrl is not null)
        {
            text.Append(model.ButtonLabel).Append(": ").AppendLine(model.ButtonUrl).AppendLine();
        }

        text.AppendLine("--").Append(model.Footer);
        return text.ToString();
    }

    private static string Enc(string value) => WebUtility.HtmlEncode(value);

    /// <summary>Only a plain #RRGGBB color goes into a style; anything else falls back to the default brand color.</summary>
    private static string SafeColor(string color) =>
        color.Length == 7 && color[0] == '#' && color.Skip(1).All(Uri.IsHexDigit) ? color : "#5000A5";
}
