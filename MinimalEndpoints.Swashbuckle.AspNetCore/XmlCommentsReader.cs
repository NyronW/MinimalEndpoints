using System.Xml.Linq;

namespace MinimalEndpoints.Swashbuckle.AspNetCore;

public static class XmlCommentsReader
{
    public static Dictionary<string, XmlComments> LoadXmlComments(IEnumerable<string> xmlPaths)
    {
        var comments = new Dictionary<string, XmlComments>();

        foreach (var xmlPath in xmlPaths)
        {
            if (string.IsNullOrEmpty(xmlPath)) continue;

            var xdoc = XDocument.Load(xmlPath);

            foreach (var member in xdoc.Descendants("member"))
            {
                var name = member.Attribute("name")?.Value;

                if (string.IsNullOrEmpty(name)) continue;
                if (comments.ContainsKey(name)) continue;

                var summary = member.Element("summary")?.Value;
                var remarks = member.Element("remarks")?.Value;
                var parameters = member.Elements("param")
                    .Select(p => new XmlCommentParameter
                    {
                        Name = p.Attribute("name")?.Value!,
                        Description = p.Value
                    })
                    .ToList();

                var responses = member.Elements("response")
                    .Select(r => new XmlCommentResponse
                    {
                        StatusCode = r.Attribute("code")?.Value!,
                        Description = r.Value
                    })
                    .ToList();

                comments.Add(name, new XmlComments
                {
                    Name = name,
                    Summary = summary!,
                    Description = remarks!,
                    Parameters = parameters,
                    Responses = responses
                });
            }
        }

        return comments;
    }
}
