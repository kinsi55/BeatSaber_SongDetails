using System;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO.Compression;
using System.Collections.Generic;

namespace SongDetailsCache {
	static class DataGetter {
		public static readonly IReadOnlyDictionary<string, (string, TimeSpan)> dataSources = new Dictionary<string, (string, TimeSpan)>() {
			{ "Direct", ("https://raw.githubusercontent.com/andruzzzhka/BeatSaberScrappedData/master/songDetails2.gz", TimeSpan.FromSeconds(25)) },
			// Caches stuff for 12 hours as backup
			{ "JSDelivr", ("https://cdn.jsdelivr.net/gh/andruzzzhka/BeatSaberScrappedData/songDetails2.gz", TimeSpan.FromSeconds(25)) },
			// Caches stuff for 5 hours, bandwidth 512KB/s, but at least its a way to get the data at all for people behind China Firewall
			{ "WGzeyu", ("https://beatmods.gtxcn.com/github/BeatSaberScrappedData/songDetails2.gz", TimeSpan.FromSeconds(50)) }
		};

		//const string dataUrl = "http://127.0.0.1/SongDetailsCache.proto.gz";

		private static HttpClient client = null;
		public static string cachePath = Path.Combine(Environment.CurrentDirectory, "UserData", "SongDetailsCache.proto");
		public static string cachePathEtag(string source) => Path.Combine(Environment.CurrentDirectory, "UserData", $"SongDetailsCache.proto.{source}.etag");

		public class DownloadedDatabase {
			public string source;
			public string etag;
			public MemoryStream stream;
		}

		public static async Task<DownloadedDatabase> UpdateAndReadDatabase(string dataSourceName = "Direct") {
			if(client == null) {
				client = new HttpClient(new HttpClientHandler() {
					AutomaticDecompression = DecompressionMethods.None,
					AllowAutoRedirect = false
				});

				client.DefaultRequestHeaders.ConnectionClose = true;
			}

			dataSourceName = dataSources.Keys.FirstOrDefault(x => x == dataSourceName) ?? dataSources.Keys.First();
			var dataSource = dataSources[dataSourceName];

			client.Timeout = dataSource.Item2;
			using(var req = new HttpRequestMessage(HttpMethod.Get, dataSource.Item1)) {
				try {
					if(File.Exists(cachePathEtag(dataSourceName)))
						req.Headers.Add("If-None-Match", File.ReadAllText(cachePathEtag(dataSourceName)));
				} catch { }

				using(var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead)) {
					if(resp.StatusCode == HttpStatusCode.NotModified)
						return null;

					if(resp.StatusCode != HttpStatusCode.OK)
						throw new Exception($"Got unexpected HTTP response: {resp.StatusCode} {resp.ReasonPhrase}");

					using(var stream = await resp.Content.ReadAsStreamAsync()) {
						var fs = new MemoryStream();
						using(var decompressed = new GZipStream(stream, CompressionMode.Decompress))
							await decompressed.CopyToAsync(fs);
						//Returning the file handle so we can end the HTTP request
						fs.Position = 0;
						return new DownloadedDatabase() {
							source = dataSourceName,
							etag = resp.Headers.ETag.Tag,
							stream = fs
						};
					}
				}
			}
		}

		public static async Task WriteCachedDatabase(DownloadedDatabase db) {
			//Using create here so that a possibly existing file (And handles to it) are kept intact
			using(var fs = new FileStream(cachePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read | FileShare.Delete, 8192, true)) {
				fs.Position = 0;

				db.stream.Position = 0;
				await db.stream.CopyToAsync(fs);
				fs.SetLength(db.stream.Length);
			}

			File.WriteAllText(cachePathEtag(db.source), db.etag);
		}

		public static Stream ReadCachedDatabase() {
			if(!File.Exists(cachePath))
				return null;

			return new FileStream(cachePath, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.Read);
		}

		public static bool HasCachedData(int maximumAgeHours = 12) {
			if(!File.Exists(cachePath))
				return false;

			FileInfo fInfo = new FileInfo(cachePath);

			return fInfo.LastWriteTime > DateTime.Now - TimeSpan.FromHours(maximumAgeHours);
		}
	}
}
