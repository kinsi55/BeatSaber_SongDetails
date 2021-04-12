﻿using ProtoBuf;
using SongDetailsCache.Structs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections.Immutable;
using System.Collections;

namespace SongDetailsCache {
	public class DiffArray : IEnumerable<SongDifficulty> {
		public SongDifficulty this[int i] => SongDetailsContainer.difficulties[i];

		public IEnumerator<SongDifficulty> GetEnumerator() => SongDetailsContainer.difficulties.AsEnumerable().GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => SongDetailsContainer.songs.GetEnumerator();

		internal DiffArray() { }
	}

	public class SongArray : IEnumerable<Song> {
		public Song this[int i] => SongDetailsContainer.songs[i];

		public IEnumerator<Song> GetEnumerator() => SongDetailsContainer.songs.AsEnumerable().GetEnumerator();
		IEnumerator IEnumerable.GetEnumerator() => SongDetailsContainer.songs.GetEnumerator();
		
		internal SongArray() { }


		/// <summary>
		/// Gets a song using its Map Hash
		/// </summary>
		/// <param name="hash">hexadecimal Map Hash, captialization does not matter</param>
		/// <param name="song">the song - Will be a random song if not found, make sure to check the return value of the method!</param>
		/// <returns>True if the song was found, false otherwise</returns>
		public unsafe bool FindByHash(string hash, out Song song) {
			if(hash.Length != 40) {
				song = SongDetailsContainer.songs[0];
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
				uint searchNeedle = (uint)Math.Floor(SongDetailsContainer.songs.Length * (c1 / (float)(uint.MaxValue)));

				// Yeaaahh it would be better to step left right left right in an alternating fashion but
				// this is already way faster than it needs to be and less complicated :) Maybe later.
				for(uint i = searchNeedle; i < SongDetailsContainer.songs.Length; i++) {
					uint songIndex = SongDetailsContainer.hashBytesLUT[i];

					byte* hBytes = SongDetailsContainer.hashBytes + (songIndex * SongDetailsContainer.HASH_SIZE_BYTES);
					uint a = *(uint*)hBytes;

					if(a > c1)
						break;
					else if(a != c1)
						continue;

					if(comp(hBytes)) {
						song = SongDetailsContainer.songs[songIndex];
						return true;
					}
				}

				for(uint i = searchNeedle; i-- > 0;) {
					uint songIndex = SongDetailsContainer.hashBytesLUT[i];

					byte* hBytes = SongDetailsContainer.hashBytes + (songIndex * SongDetailsContainer.HASH_SIZE_BYTES);
					uint a = *(uint*)hBytes;

					if(a < c1)
						break;
					else if(a != c1)
						continue;

					if(comp(hBytes)) {
						song = SongDetailsContainer.songs[songIndex];
						return true;
					}
				}

				song = SongDetailsContainer.songs[0];
				return false;
			}
		}

		/// <summary>
		/// Gets a song using its hexadecimal Map ID (Some times called Map Key)
		/// </summary>
		/// <param name="key">hexadecimal Map ID, captialization does not matter</param>
		/// <param name="song">the song - Will be a random song if not found, make sure to check the return value of the method!</param>
		/// <returns>True if the song was found, false otherwise</returns>
		public bool FindByMapId(string key, out Song song) => FindByMapId(Convert.ToUInt32(key, 16), out song);

		/// <summary>
		/// Gets a song using its Map ID
		/// </summary>
		/// <param name="key">Map ID</param>
		/// <param name="song">the song - Will be a random song if not found, make sure to check the return value of the method!</param>
		/// <returns>True if the song was found, false otherwise</returns>
		public bool FindByMapId(uint key, out Song song) {
			var idx = Array.BinarySearch(SongDetailsContainer.keys, key);

			if(idx == -1) {
				song = SongDetailsContainer.songs[0];
				return false;
			}

			song = SongDetailsContainer.songs[idx];
			return true;
		}
	}

	public class SongDetails {
		private SongDetails() {}

		public readonly SongArray songs = new SongArray();
		public readonly DiffArray difficulties = new DiffArray();

		static SongDetails auros = new SongDetails();

		static bool isLoading = false;
		public static Task<SongDetails> Init() {
			if(SongDetailsContainer.isDataAvailable)
				return Task.Run(() => auros);

			var resultCompletionSource = new TaskCompletionSource<SongDetails>();

			SongDetailsContainer.dataAvailableOrUpdated += () => {
				isLoading = false;
				resultCompletionSource.TrySetResult(auros);
			};

			SongDetailsContainer.dataLoadFailed += (ex) => {
				isLoading = false;
				resultCompletionSource.TrySetException(ex);
			};

			if(!isLoading && (isLoading = true))
				Task.Run(() => SongDetailsContainer.Load());

			return resultCompletionSource.Task;
		}



		/// <summary>
		/// Delegate used for filtering / searching songs by difficulties
		/// </summary>
		/// <param name="difficulty"></param>
		/// <returns></returns>
		public delegate bool DifficultyFilterDelegate(ref SongDifficulty difficulty);

		/// <summary>
		/// Finds indexes of songs which have difficulties that pass the check condition
		/// </summary>
		/// <param name="check">condition to check difficulties for</param>
		/// <returns>Collection of songs which have difficulties that passed the condition check</returns>
		public IReadOnlyCollection<uint> FindSongIndexes(DifficultyFilterDelegate check) {
			var l = new List<uint>();

			for(uint i = 0, last = uint.MaxValue; i < SongDetailsContainer.difficulties.Length; i++) {
				ref var x = ref SongDetailsContainer.difficulties[i];

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
		public IReadOnlyCollection<Song> FindSongs(DifficultyFilterDelegate check) {
			var l = new List<Song>();

			for(uint i = 0, last = uint.MaxValue; i < SongDetailsContainer.difficulties.Length; i++) {
				ref var x = ref SongDetailsContainer.difficulties[i];

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
		public int CountSongs(DifficultyFilterDelegate check) {
			var count = 0;

			for(uint i = 0, last = uint.MaxValue; i < SongDetailsContainer.difficulties.Length; i++) {
				ref var x = ref SongDetailsContainer.difficulties[i];

				if(last == x.songIndex || !check(ref x))
					continue;

				count++;

				last = x.songIndex;
			}

			return count;
		}
	}

	static class SongDetailsContainer {
		internal const int HASH_SIZE_BYTES = 20;

		internal static uint[] keys = null;
		internal unsafe static byte* hashBytes = null;
		internal unsafe static uint* hashBytesLUT = null;

		internal static string[] songNames = null;
		internal static string[] songAuthorNames = null;
		internal static string[] levelAuthorNames = null;


		internal static Song[] songs { get; private set; } = null;
		internal static SongDifficulty[] difficulties { get; private set; } = null;


		internal static Action dataAvailableOrUpdated;
		internal static Action<Exception> dataLoadFailed;

		internal static bool isDataAvailable => songs != null && songs.Length > 0;

		internal static async Task Load(bool reload = false) {
			if(!reload && isDataAvailable)
				return;

			FileInfo fInfo = new FileInfo(DataGetter.cachePath);

			bool shouldLoadFresh = !fInfo.Exists || fInfo.LastWriteTime < DateTime.Now - TimeSpan.FromHours(12);

			// Might as well always load the cached one so thats there while we (possibly) download fresh data.
			if(fInfo.Exists) {
				try {
					//At the odd chance that somehow loading fresh data was faster than loading the cached one, make sure to not replace it
					using(var cachedStream = DataGetter.ReadCachedDatabase())
						Process(cachedStream, false);
				} catch {
					shouldLoadFresh = true;
				}
			}

			try {
				//At the odd chance that somehow loading fresh data was faster than loading the cached one, make sure to not replace it
				using(var stream = await DataGetter.UpdateAndReadDatabase())
					Process(stream);

				if(isDataAvailable) {
					dataAvailableOrUpdated?.Invoke();
				} else {
					dataLoadFailed?.Invoke(new Exception("Data load failed for unknown reason"));
				}
			} catch(Exception ex) {
				if(!isDataAvailable)
					dataLoadFailed?.Invoke(ex);
			}
		}

		static unsafe void Process(Stream stream, bool force = true) {
			if(!force && songs != null)
				return;
#if DEBUG
			var sw = new Stopwatch(); sw.Start();
#endif

			var parsed = Serializer.Deserialize<SongProto[]>(stream);
#if DEBUG
			Console.WriteLine("[SongDetailsCache] Parsed {0} songs in {1}ms", parsed.Length, sw.ElapsedMilliseconds);

			sw.Restart();
#endif

			if(parsed.Length == 0)
				throw new Exception("Parsing data yielded no songs");


			// Stuff gotta be sorted for Binary search (Key lookup) to work. The Data is presorted but this is a failafe
			parsed = parsed.OrderBy(x => x.mapId).ToArray();
#if DEBUG
			Console.WriteLine("[SongDetailsCache] Sorted in {1}ms", parsed.Length, sw.ElapsedMilliseconds);

			sw.Restart();
#endif
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
#if DEBUG
			Console.WriteLine("[SongDetailsCache] Allocated memory...");
#endif
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
#if DEBUG
			Console.WriteLine("[SongDetailsCache] Did basic parsing...");
#endif

			// Build LUT for fast hash lookup
			var sortedByHashes = newSongs.OrderBy(x => *(uint*)(newHashes + (x.index * HASH_SIZE_BYTES))).ToArray();

			for(int i = 0; i < newSongs.Length; i++)
				newHashesLUT[i] = sortedByHashes[i].index;
#if DEBUG
			Console.WriteLine("[SongDetailsCache] Built LUT...");
#endif
			if(!force && songs != null) {
				Marshal.FreeHGlobal((IntPtr)newHashes);
				Marshal.FreeHGlobal((IntPtr)newHashesLUT);
				return;
			}

#if DEBUG
			sw.Stop();
#endif
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

#if DEBUG
			Console.WriteLine("[SongDetailsCache] Transforming data took {0}ms, got {1} diffs", sw.ElapsedMilliseconds, difficulties.Length);
#endif

			if(isDataAvailable)
				dataAvailableOrUpdated?.Invoke();
		}
	}
}