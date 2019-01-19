using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Swashbuckle.Swagger;

namespace Server
{
    public class CachingSwaggerProvider : ISwaggerProvider
    {
        private static readonly ConcurrentDictionary<string, SwaggerDocument> s_cache =
            new ConcurrentDictionary<string, SwaggerDocument>();

        private readonly ISwaggerProvider _swaggerProvider;

        public CachingSwaggerProvider(ISwaggerProvider swaggerProvider)
        {
            this._swaggerProvider = swaggerProvider;
        }

        public SwaggerDocument GetSwagger(string rootUrl, string apiVersion)
        {
            var cacheKey = string.Format("{0}_{1}", rootUrl, apiVersion);

            SwaggerDocument doc = null;

            if (!s_cache.TryGetValue(cacheKey, out doc))
            {
                doc = this._swaggerProvider.GetSwagger(rootUrl, apiVersion);
                var paths = new Dictionary<string, PathItem>();
                foreach (var item in doc.paths)
                {
                    var urls = item.Key.Split('/');
                    var i = urls[3].LastIndexOf('.') + 1;
                    if (i != -1)
                    {
                        urls[3] = urls[3].Substring(i);
                    }
                    urls[2] = apiVersion;
                    paths.Add(string.Join("/", urls), item.Value);
                }

                doc.paths = paths;

                s_cache.TryAdd(cacheKey, doc);
            }

            return doc;
        }

    }
}