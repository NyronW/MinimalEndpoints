using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using MinimalEndpoints.Extensions.Http.ContentNegotiation;
using MinimalEndpoints.Extensions.Http.ModelBinding;
using System.Text;
using System.Xml.Serialization;

namespace MinimalEndpoints.Extensions.Http;

public static class HttpExtensions
{
    public static async ValueTask<TModel?> GetModelAsync<TModel>(this HttpRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var binders = request.HttpContext.RequestServices.GetServices<IEndpointModelBinder>().ToList();

            IEndpointModelBinder? binder = binders.FirstOrDefault(x => x.CanHandle(request.ContentType));

            if (binder == null)
            {
                throw new EndpointModelBindingException(
                    $"Unable to read the request because the request content type '{request.ContentType}' is not a supported content type.",
                    instance: request.Path.Value);
            }

            TModel? model = await binder.BindAsync<TModel>(request, cancellationToken);

            return model;
        }
        catch (Exception e)
        {
            var logger = request.HttpContext.RequestServices.GetService<ILogger>();

            logger?.LogError("Unhandled error occured while trying to bind incoming data", e);

            var env = request.HttpContext.RequestServices.GetService<IWebHostEnvironment>();
           
            //throw exception if where are in dev environment or its a bindign error
            if (env.IsDevelopment() || e is EndpointModelBindingException) throw;


            throw new EndpointModelBindingException(
              $"An error occurred while deserializing input data.",
              instance: request.Path.Value, exception: e);
        }
    }

    public static async ValueTask<TValue?> ReadFromXmlAsync<TValue>(this HttpRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        if (!request.HasXmlContentType(out var charset)) ThrowContentTypeError(request);

        var encoding = GetEncodingFromCharset(charset);
        var (inputStream, usesTranscodingStream) = GetInputStream(request.HttpContext, encoding);

        try
        {
            var body = await new StreamReader(inputStream).ReadToEndAsync();

            if (inputStream.CanSeek)
            {
                inputStream.Position = 0;
            }

            using (var sw = new StringReader(body))
            {
                var serializer = new XmlSerializer(typeof(TValue));
                return (TValue?)serializer.Deserialize(sw);
            }
        }
        finally
        {
            if (usesTranscodingStream) await inputStream.DisposeAsync();
        }
    }

    public static bool HasXmlContentType(this HttpRequest request)
    {
        return request.HasXmlContentType(out _);
    }

    public static Task SendAsync(this HttpResponse response, object obj, int? statusCode, string? contentType = null, CancellationToken cancellationToken = default)
    {
        var negotiators = response.HttpContext.RequestServices.GetServices<IResponseNegotiator>().ToList();
        IResponseNegotiator? negotiator = null;

        MediaTypeHeaderValue.TryParseList(response.HttpContext.Request.Headers["Accept"], out var accept);
        if (accept != null)
        {
            var ordered = accept.OrderByDescending(x => x.Quality ?? 1);

            foreach (var acceptHeader in ordered)
            {
                negotiator = negotiators.FirstOrDefault(x => x.CanHandle(acceptHeader));
                if (negotiator != null) break;
            }
        }

        if (negotiator == null)
            negotiator = negotiators.First(x => x.CanHandle(new MediaTypeHeaderValue("application/json")));

        return negotiator.Handle(response.HttpContext, obj, statusCode, contentType, cancellationToken);
    }

    #region Helper
    private static bool HasXmlContentType(this HttpRequest request, out StringSegment charset)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (!MediaTypeHeaderValue.TryParse(request.ContentType, out MediaTypeHeaderValue? mt))
        {
            charset = StringSegment.Empty;
            return false;
        }

        if (mt == null)
        {
            charset = StringSegment.Empty;
            return false;
        }

        // Matches application/xml
        if (mt.MediaType.Equals("application/xml", StringComparison.OrdinalIgnoreCase))
        {
            charset = mt.Charset;
            return true;
        }

        // Matches +xml, e.g. application/ld+xml
        if (mt.Suffix.Equals("xml", StringComparison.OrdinalIgnoreCase))
        {
            charset = mt.Charset;
            return true;
        }

        charset = StringSegment.Empty;
        return false;
    }

    private static void ThrowContentTypeError(HttpRequest request)
    {
        throw new EndpointModelBindingException(
            $"Unable to read the request as XML because the request content type '{request.ContentType}' is not a known XML content type.",
            instance: request.Path.Value);
    }

    private static (Stream inputStream, bool usesTranscodingStream) GetInputStream(HttpContext httpContext, Encoding? encoding)
    {
        if (encoding == null || encoding.CodePage == Encoding.UTF8.CodePage)
        {
            return (httpContext.Request.Body, false);
        }

        var inputStream = Encoding.CreateTranscodingStream(httpContext.Request.Body, encoding, Encoding.UTF8, leaveOpen: true);
        return (inputStream, true);
    }

    private static Encoding? GetEncodingFromCharset(StringSegment charset)
    {
        if (charset.Equals("utf-8", StringComparison.OrdinalIgnoreCase))
        {
            // This is an optimization for utf-8 that prevents the Substring caused by
            // charset.Value
            return Encoding.UTF8;
        }

        try
        {
            // charset.Value might be an invalid encoding name as in charset=invalid.
            return charset.HasValue ? Encoding.GetEncoding(charset.Value) : null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Unable to read the request as XML because the request content type charset '{charset}' is not a known encoding.", ex);
        }
    }
    #endregion
}