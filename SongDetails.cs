using ProtoBuf;
using SongDetailsCache.Structs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SongDetailsCache {
	public static class SongDetails {
		internal const int HASH_SIZE_BYTES = 20;

		internal static uint[] keys = null;
		internal unsafe static byte* hashBytes = null;
		internal unsafe static uint* hashBytesLUT = null;

		internal static string[] songNames = null;
		internal static string[] songAuthorNames = null;
		internal static string[] levelAuthorNames = null;


		public static Song[] songs { get; private set; } = null;
		public static SongDifficulty[] difficulties { get; private set; } = null;



		/// <summary>
		/// Raised whenever SongDetails initially is initialized or updates its dataset
		/// </summary>
		public static Action dataAvailableOrUpdated;

		/// <summary>
		/// Returns if SongDetails is initialized has has loaded data
		/// </summary>
		public static bool isDataAvailable => songs != null && songs.Length > 0;

		/// <summary>
		/// Delegate used for filtering / searching songs by difficulties
		/// </summary>
		/// <param name="difficulty"></param>
		/// <returns></returns>
		public delegate bool FilterDelegate(ref SongDifficulty difficulty);

		/// <summary>
		/// Finds indexes of songs which have difficulties that pass the check condition
		/// </summary>
		/// <param name="check">condition to check difficulties for</param>
		/// <returns>Collection of songs which have difficulties that passed the condition check</returns>
		public static IReadOnlyCollection<uint> FindSongIndexes(FilterDelegate check) {
			if(!isDataAvailable)
				throw new Exception("SongDetails data not available!");

			var l = new List<uint>();

			for(uint i = 0, last = uint.MaxValue; i < difficulties.Length; i++) {
				ref var x = ref difficulties[i];

				if(last == x.songIndex || !check(ref x))
					continue;
					
				l.Add(x.songIndex);

				last = x.songIndex;
			}

			return l;
		}

		/// <summary>
		/// Finds the songs which have difficulties that pass the check condition
		/// </summary>
		/// <param name="check">condition to check difficulties for</param>
		/// <returns>Collection of songs which have difficulties that passed the condition check</returns>
		public static IReadOnlyCollection<Song> FindSongs(FilterDelegate check) {
			if(!isDataAvailable)
				throw new Exception("SongDetails data not available!");

			var l = new List<Song>();

			for(uint i = 0, last = uint.MaxValue; i < difficulties.Length; i++) {
				ref var x = ref difficulties[i];

				if(last == x.songIndex || !check(ref x))
					continue;

				l.Add(x.song);

				last = x.songIndex;
			}

			return l;
		}

		/// <summary>
		/// Counts the songs which have difficulties that pass the check condition
		/// </summary>
		/// <param name="check">condition to check difficulties for</param>
		/// <returns>Count of songs that have matching difficulties</returns>
		public static int CountSongs(FilterDelegate check) {
			if(!isDataAvailable)
				throw new Exception("SongDetails data not available!");

			var count = 0;

			for(uint i = 0, last = uint.MaxValue; i < difficulties.Length; i++) {
				ref var x = ref difficulties[i];

				if(last == x.songIndex || !check(ref x))
					continue;

				count++;

				last = x.songIndex;
			}

			return count;
		}

		/// <summary>
		/// Gets a song using its Map Hash
		/// </summary>
		/// <param name="hash">hexadecimal Map Hash, captialization does not matter</param>
		/// <param name="song">the song - Will be a random song if not found, make sure to check the return value of the method!</param>
		/// <returns>True if the song was found, false otherwise</returns>
		public static unsafe bool FindSongByHash(string hash, out Song song) {
			if(!isDataAvailable)
				throw new Exception("SongDetails data not available!");

			if(hash.Length != 40) {
				song = songs[0];
				return false;
			}

			fixed(byte* _a = HexUtil.ToBytes(hash)) {
				uint c1 = *(uint*)_a;
				long c2 = *(long*)(_a + 4);
				long c3 = *(long*)(_a + 12);

				bool comp(byte* a) {
					return *(long*)(a + 4) == c2 &&
							*(long*)(a + 12) == c3;
				}

				// This episode of optimization is sponsored by "Average"
				// Will this become slower over time? Yes. Will this ever be slow? No.
				uint searchNeedle = (uint)Math.Floor(songs.Length * (c1 / (float)(uint.MaxValue)));

				//int offs = 0;
				//bool searchNeg = true;
				//bool searchPos = true;

				//for(; ;) {
				//	uint songIndex = hashBytesLUT[searchNeedle + offs];

				//	byte* hBytes = hashBytes + (songIndex * HASH_SIZE_BYTES);

				//	if(comp(hBytes)) {
				//		song = songs[songIndex];
				//		return true;
				//	}

				//	if(offs >= 0 && searchNeg) {
				//		offs = ~offs;

				//		if()
				//	}

				//	offs = offs >= 0 ? ~offs : (offs * -1);
				//}
				// Yeaaahh it would be better to step left right left right in an alternating fashion but
				// this is already way faster than it needs to be and less complicated :) Maybe later.
				for(uint i = searchNeedle; i < songs.Length; i++) {
					uint songIndex = hashBytesLUT[i];

					byte* hBytes = hashBytes + (songIndex * HASH_SIZE_BYTES);
					uint a = *(uint*)hBytes;

					if(a > c1)
						break;
					else if(a != c1)
						continue;

					if(comp(hBytes)) {
						song = songs[songIndex];
						return true;
					}
				}

				for(uint i = searchNeedle; i-- > 0;) {
					uint songIndex = hashBytesLUT[i];

					byte* hBytes = hashBytes + (songIndex * HASH_SIZE_BYTES);
					uint a = *(uint*)hBytes;

					if(a < c1)
						break;
					else if(a != c1)
						continue;

					if(comp(hBytes)) {
						song = songs[songIndex];
						return true;
					}
				}

				song = songs[0];
				return false;
			}
		}

		/// <summary>
		/// Gets a song using its hexadecimal Map ID (Some times called Map Key)
		/// </summary>
		/// <param name="key">hexadecimal Map ID, captialization does not matter</param>
		/// <param name="song">the song - Will be a random song if not found, make sure to check the return value of the method!</param>
		/// <returns>True if the song was found, false otherwise</returns>
		public static bool FindSongByMapId(string key, out Song song) => FindSongByMapId(Convert.ToUInt32(key, 16), out song);

		/// <summary>
		/// Gets a song using its Map ID
		/// </summary>
		/// <param name="key">Map ID</param>
		/// <param name="song">the song - Will be a random song if not found, make sure to check the return value of the method!</param>
		/// <returns>True if the song was found, false otherwise</returns>
		public static bool FindSongByMapId(uint key, out Song song) {
			if(!isDataAvailable)
				throw new Exception("SongDetails data not available!");

			var idx = Array.BinarySearch(keys, key);

			if(idx == -1) {
				song = songs[0];
				return false;
			}

			song = songs[idx];
			return true;
		}

		static bool didInit = false;
		public static void Init() { if(!didInit && (didInit = true)) Load(false); }

		internal static void Load(bool reload = false) {
			if(!reload && isDataAvailable)
				return;

			FileInfo fInfo = new FileInfo(DataGetter.cachePath);

			bool shouldLoadFresh = !fInfo.Exists || fInfo.LastWriteTime < DateTime.Now - TimeSpan.FromHours(12);

			// Might as well always load the cached one so thats there while we (possibly) download fresh data.
			if(fInfo.Exists) {
				Stopwatch sw = new Stopwatch();
				Console.WriteLine("[SongDetailsCache] Loading cached SongDetail database...");
				sw.Start();

				try {
					//At the odd chance that somehow loading fresh data was faster than loading the cached one, make sure to not replace it
					using(var cachedStream = DataGetter.ReadCachedDatabase())
						Process(cachedStream, false);

					Console.WriteLine("[SongDetailsCache] Loaded cached database containing {0} songs in {1}ms", songs.Length, sw.ElapsedMilliseconds);
				} catch(Exception ex) {
					Console.WriteLine("[SongDetailsCache] Failed to load cached database:");
					Console.WriteLine(ex);

					if(!shouldLoadFresh)
						loadNew();
				}
				sw.Stop();
			}

			void loadNew() => new Task(async () => {
				Stopwatch sw = new Stopwatch();
				Console.WriteLine("Loading fresh SongDetail database...");
				sw.Start();

				try {
					//At the odd chance that somehow loading fresh data was faster than loading the cached one, make sure to not replace it
					using(var stream = await DataGetter.UpdateAndReadDatabase())
						Process(stream);

					Console.WriteLine("[SongDetailsCache] Loaded fresh database containing {0} songs in {1}ms", songs.Length, sw.ElapsedMilliseconds);
				} catch(Exception ex) {
					Console.WriteLine("[SongDetailsCache] Failed to load fresh database:");
					Console.WriteLine(ex);
				}
				sw.Stop();
			}).Start();

			if(shouldLoadFresh) 
				loadNew();
		}

		static unsafe void Process(Stream stream, bool force = true) {
			if(!force && songs != null)
				return;

			var sw = new Stopwatch(); sw.Start();

			var parsed = Serializer.Deserialize<SongProto[]>(stream);
			Console.WriteLine("[SongDetailsCache] Parsed {0} songs in {1}ms", parsed.Length, sw.ElapsedMilliseconds);

			if(!force && songs != null)
				return;

			sw.Restart();

			// Stuff gotta be sorted for Binary search (Key lookup) to work. The Data is presorted but this is a failafe
			parsed = parsed.OrderBy(x => x.mapId).ToArray();

			Console.WriteLine("[SongDetailsCache] Sorted in {1}ms", parsed.Length, sw.ElapsedMilliseconds);

			sw.Restart();

			GC.Collect();
			var newSongs = new Song[parsed.Length];

			var newKeys = new uint[parsed.Length];
			var newHashes = (byte*)Marshal.AllocHGlobal(parsed.Length * HASH_SIZE_BYTES);
			var newHashesLUT = (uint*)Marshal.AllocHGlobal(4 * parsed.Length);

			var newSongNames = new string[parsed.Length];
			var newSongAuthorNames = new string[parsed.Length];
			var newLevelAuthorNames = new string[parsed.Length];

			var newDiffs = new SongDifficulty[parsed.Sum(x => x.difficulties?.Length ?? 0)];

			uint diffIndex = 0;

			Console.WriteLine("[SongDetailsCache] Allocated memory...");

			for(uint i = 0; i < parsed.Length; i++) {
				var parsedSong = parsed[i];

				newSongs[i] = new Song(i, diffIndex, (byte)Math.Min(255, parsedSong.difficulties?.Length ?? 0), parsedSong);

				ref var builtSong = ref newSongs[i];

				newKeys[i] = parsedSong.mapId;
				Marshal.Copy(parsedSong.hashBytes, 0, (IntPtr)(newHashes + (i * HASH_SIZE_BYTES)), HASH_SIZE_BYTES);

				newSongNames[i] = parsedSong.songName;
				newSongAuthorNames[i] = parsedSong.songAuthorName;
				newLevelAuthorNames[i] = parsedSong.levelAuthorName;

				if(parsedSong.difficulties == null)
					continue;
					
				foreach(var diff in parsedSong.difficulties)
					newDiffs[diffIndex++] = new SongDifficulty(i, diff);
			}

			Console.WriteLine("[SongDetailsCache] Did basic parsing...");

			// Build LUT for fast hash lookup
			var sortedByHashes = newSongs.OrderBy(x => *(uint*)(newHashes + (x.index * HASH_SIZE_BYTES))).ToArray();

			for(int i = 0; i < newSongs.Length; i++)
				newHashesLUT[i] = sortedByHashes[i].index;

			Console.WriteLine("[SongDetailsCache] Built LUT...");

			if(!force && songs != null) {
				Marshal.FreeHGlobal((IntPtr)newHashes);
				Marshal.FreeHGlobal((IntPtr)newHashesLUT);
				return;
			}

			sw.Stop();
			songs = newSongs;

			keys = newKeys;

			if(hashBytes != null)
				Marshal.FreeHGlobal((IntPtr)hashBytes);
			hashBytes = newHashes;

			if(hashBytesLUT != null)
				Marshal.FreeHGlobal((IntPtr)hashBytesLUT);
			hashBytesLUT = newHashesLUT;

			songNames = newSongNames;
			songAuthorNames = newSongAuthorNames;
			levelAuthorNames = newLevelAuthorNames;

			difficulties = newDiffs;

			Console.WriteLine("[SongDetailsCache] Transforming data took {0}ms, got {1} diffs", sw.ElapsedMilliseconds, difficulties.Length);

			if(isDataAvailable)
				dataAvailableOrUpdated?.Invoke();
		}
	}
}
