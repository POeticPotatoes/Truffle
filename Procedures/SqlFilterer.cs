using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Truffle.Procedures
{
    public static class SqlFilter
    {
        public static IEnumerable<(T item, int index)> WithIndex<T>(this IEnumerable<T> source)
        {
            return source.Select((item, index) => (item, index));
        }

        public static string FilterString(string cmd, HttpRequest req)
        {
            string requestBody = String.Empty;
            using (StreamReader streamReader = new StreamReader(req.Body))
            {

                requestBody = streamReader.ReadToEnd();
                var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(requestBody);
                // foreach (var (entry, index) in values.WithIndex())
                // {
                //     if (index == 0)
                //     {
                //         cmd = String.Concat(cmd, " where ", entry.Key, entry.Value);
                //     }
                //     else
                //     {
                //         cmd = String.Concat(cmd, " and ", entry.Key, entry.Value);
                //     }
                // }

                var first = true;
                foreach (var entry in values)
                {
                    if (first)
                    {
                        cmd = String.Concat(cmd, " where ", entry.Key, entry.Value);
                        first = false;
                    }
                    else
                    {
                        cmd = String.Concat(cmd, " and ", entry.Key, entry.Value);
                    }
                }
            }
            return cmd;
        }
    }
}