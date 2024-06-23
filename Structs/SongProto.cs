using ProtoBuf;
using System;
using System.Collections.Generic;

namespace SongDetailsCache.Structs {
	public enum RankedStatus : uint { Unranked, Ranked = 1, Qualified = 2, Queued = 3 }
	[Flags] 
	public enum RankedStates : uint { Unranked, ScoresaberRanked = 1 << 0, BeatleaderRanked = 1 << 1, ScoresaberQualified = 1 << 2, BeatleaderQualified = 1 << 3 }
	[Flags] 
	public enum UploadFlags : uint { None, Curated = 1 << 0, VerifiedUploader = 1 << 1 }

	[ProtoContract]
	class SongProto {
#pragma warning disable 649
		[ProtoMember(1)] public readonly float bpm;
		public readonly uint downloadCount;
		[ProtoMember(2)] public readonly uint upvotes;
		[ProtoMember(3)] public readonly uint downvotes;

		[ProtoMember(4)] public readonly uint uploadTimeUnix;
		[ProtoMember(12)] public readonly uint rankedChangeUnix;

		[ProtoMember(5)] public readonly uint mapId;

		[ProtoMember(6)] public readonly uint songDurationSeconds;


		[ProtoMember(7)] public readonly string songName;
		[ProtoMember(8)] public readonly string songAuthorName;
		[ProtoMember(9)] public readonly string levelAuthorName;
		[ProtoMember(10)] public readonly string uploaderName;

		[ProtoMember(13)] public readonly RankedStates rankedStates;

		[ProtoMember(11, OverwriteList = true)] internal readonly SongDifficultyProto[] difficulties;

		[ProtoMember(14)] public readonly ulong tags;
		[ProtoMember(15)] public readonly UploadFlags uploadFlags;

#pragma warning restore 649

		SongProto() {
			songDurationSeconds = 1;
		}
	}

	public readonly struct Song {
		internal Song(uint index, uint diffOffset, byte diffCount, SongProto proto) {
			this.index = index;
			this.diffOffset = diffOffset;
			this.diffCount = diffCount;

			bpm = proto?.bpm ?? 0;
			downloadCount = proto?.downloadCount ?? 0;
			upvotes = proto?.upvotes ?? 0;
			downvotes = proto?.downvotes ?? 0;
			uploadTimeUnix = proto?.uploadTimeUnix ?? 0;
			rankedChangeUnix = proto?.rankedChangeUnix ?? 0;
			songDurationSeconds = proto?.songDurationSeconds ?? 0;
			rankedStates = proto?.rankedStates ?? 0;
			uploadFlags = proto?.uploadFlags ?? 0;
			tags = proto?.tags ?? 0;
		}

		public static Song none => new Song(uint.MinValue, 0, 0, null);

		public readonly float bpm;
		public readonly uint downloadCount;
		public readonly uint upvotes;
		public readonly uint downvotes;

		/// <summary>
		/// The BeatSaver rating of this song
		/// </summary>
		public float rating {
			get {
				float tot = upvotes + downvotes;
				if(tot == 0)
					return 0;

				var tmp = upvotes / tot;

				return (float)(tmp - (tmp - 0.5) * Math.Pow(2, -Math.Log10(tot + 1)));
			}
		}

		/// <summary>
		/// Unix timestamp of when the map was uploaded
		/// </summary>
		public readonly uint uploadTimeUnix;

		/// <summary>
		/// Unix timestamp of when any of the difficulties of this map changed its ranked status
		/// </summary>
		public readonly uint rankedChangeUnix;

		/// <summary>
		/// Returns the uploadTimeUnix converted to a DateTime object
		/// </summary>
		public DateTime uploadTime => DateTimeOffset.FromUnixTimeSeconds(uploadTimeUnix).DateTime;

		/// <summary>
		/// The lenght of the song in seconds
		/// </summary>
		public readonly uint songDurationSeconds;

		/// <summary>
		/// The lenght of the song in seconds but as a Timespan
		/// </summary>
		public TimeSpan songDuration => TimeSpan.FromSeconds(songDurationSeconds);

		/// <summary>
		/// Index of the Song in the Songs array
		/// </summary>
		public readonly uint index;

		/// <summary>
		/// Index of the first difficulty of this song in the difficulties array
		/// </summary>
		public readonly uint diffOffset;

		/// <summary>
		/// Amount of difficulties this song has
		/// </summary>
		public readonly byte diffCount;

		/// <summary>
		/// Hexadecimal representation of the Map ID
		/// </summary>
		public readonly string key => SongDetailsContainer.keys[index].ToString("x");

		/// <summary>
		/// Numeric representation of the Map ID
		/// </summary>
		public readonly uint mapId => SongDetailsContainer.keys[index];

		/// <summary>
		/// Ranked status of the map on ScoreSaber and BeatLeader
		/// </summary>
		public readonly RankedStates rankedStates;

		/// <summary>
		/// The BeatSaver Tags set for this map like Genre and Map Type
		/// This is a bitflag field, the respective bits value of a tag can be retrieved from
		/// the SongDetails.tags Dictionary
		/// </summary>
		public readonly ulong tags;

		/// <summary>
		/// Some additional Flags / Details for an upload
		/// </summary>
		public readonly UploadFlags uploadFlags;

		/// <summary>
		/// Hexadecimal representation of the Map Hash
		/// </summary>
		public readonly string hash => HexUtil.SongBytesToHash(index); // This should probably not be here.

		public readonly string songName => SongDetailsContainer.songNames[index];
		public readonly string songAuthorName => SongDetailsContainer.songAuthorNames[index];
		public readonly string levelAuthorName => SongDetailsContainer.levelAuthorNames[index];

		public readonly string coverURL => $"https://cdn.beatsaver.com/{hash.ToLower()}.jpg";

		public readonly string uploaderName => SongDetailsContainer.uploaderNames[index];

		/// <summary>
		/// Helper method to check if the Song has a tag set
		/// </summary>
		/// <param name="tag">String representation of the BeatSaver Tag</param>
		/// <returns>True if the Tag is set for the Song</returns>
		public bool HasTag(string tag) {
			if(tags == 0)
				return false;

			if(SongDetailsContainer.tags != null && SongDetailsContainer.tags.TryGetValue(tag, out var v))
				return (tags & v) != 0;

			return false;
		}

		/// <summary>
		/// Helper method to get a difficulty of this Song
		/// </summary>
		/// <param name="diff">Requested difficulty</param>
		/// <param name="difficulty">the difficulty - Will be a random difficulty if not found, make sure to check the return value of the method!</param>
		/// <returns>True if the requested difficulty exists, false otherwise</returns>
		public bool GetDifficulty(out SongDifficulty difficulty, MapDifficulty diff, MapCharacteristic characteristic = MapCharacteristic.Standard) {
			for(int i = 0; i < diffCount; i++) {
				ref var x = ref SongDetailsContainer.difficulties[i + diffOffset];

				if(x.difficulty == diff && x.characteristic == characteristic) {
					difficulty = x;
					return true;
				}
			}

			difficulty = SongDetailsContainer.difficulties[0];
			return false;
		}

		public bool GetDifficulty(out SongDifficulty difficulty, MapDifficulty diff, string characteristic) {
			if(characteristic == "Standard")
				return GetDifficulty(out difficulty, diff);

			if(characteristic == "360Degree" || characteristic == "Degree360" || characteristic == "ThreeSixtyDegree")
				return GetDifficulty(out difficulty, diff, MapCharacteristic.ThreeSixtyDegree);

			if(characteristic == "90Degree" || characteristic == "Degree90" || characteristic == "NinetyDegree")
				return GetDifficulty(out difficulty, diff, MapCharacteristic.NinetyDegree);

			if(Enum.TryParse<MapCharacteristic>(characteristic, out var pDiff))
				return GetDifficulty(out difficulty, diff, pDiff);

			difficulty = SongDetailsContainer.difficulties[0];
			return false;
		}

		/// <summary>
		/// All difficulties that belong to this Song
		/// </summary>
		public IReadOnlyCollection<SongDifficulty> difficulties => new ArraySegment<SongDifficulty>(SongDetailsContainer.difficulties, (int)diffOffset, diffCount);
	}
}