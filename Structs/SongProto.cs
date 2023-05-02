using ProtoBuf;
using System;
using System.Collections.Generic;

namespace SongDetailsCache.Structs {
	public enum RankedStatus : uint { Unranked, Ranked = 1, Qualified = 2, Queued = 3 }
	[Flags] public enum RankedStates : uint { Unranked, ScoresaberRanked = 1 << 0, BeatleaderRanked = 1 << 1, ScoresaberQualified = 1 << 2, BeatleaderQualified = 1 << 3 }

	[ProtoContract]
	class SongProto {
#pragma warning disable 649
		[ProtoMember(1)] public readonly float bpm;
		[ProtoMember(2)] public readonly uint downloadCount;
		[ProtoMember(3)] public readonly uint upvotes;
		[ProtoMember(4)] public readonly uint downvotes;

		[ProtoMember(5)] public readonly uint uploadTimeUnix;
		[ProtoMember(14)] public readonly uint rankedChangeUnix;

		[ProtoMember(6)] public readonly uint mapId;

		[ProtoMember(8)] public readonly uint songDurationSeconds;

		[ProtoMember(9, OverwriteList = true)] public readonly byte[] hashBytes;


		[ProtoMember(10)] public readonly string songName;
		[ProtoMember(11)] public readonly string songAuthorName;
		[ProtoMember(12)] public readonly string levelAuthorName;

		[ProtoMember(15)] public readonly RankedStatus rankedState;

		[ProtoMember(17)] public readonly RankedStates rankedStates;

		[ProtoMember(13, OverwriteList = true)] internal readonly SongDifficultyProto[] difficulties;

		[ProtoMember(16)] public readonly string uploaderName;
#pragma warning restore 649

		SongProto() {
			songDurationSeconds = 1;
			rankedState = RankedStatus.Unranked;
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
			rankedStatus = proto?.rankedState ?? 0;
			rankedStates = proto?.rankedStates ?? 0;
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
		/// Ranked status of the map on ScoreSaber
		/// </summary>
		[Obsolete("rankedStatus has been replaced in favor of rankedStates and will be removed in the future")]
		public readonly RankedStatus rankedStatus;

		/// <summary>
		/// Ranked status of the map on ScoreSaber and BeatLeader
		/// </summary>
		public readonly RankedStates rankedStates;

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