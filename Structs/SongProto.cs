using ProtoBuf;
using System;
using System.Collections.Generic;

namespace SongDetailsCache.Structs {

    [ProtoContract(SkipConstructor = true)]
    class SongProto {
#pragma warning disable 649
        [ProtoMember(1)] public readonly float bpm;
        [ProtoMember(2)] public readonly uint downloadCount;
        [ProtoMember(3)] public readonly uint upvotes;
        [ProtoMember(4)] public readonly uint downvotes; 

        [ProtoMember(5)] public readonly uint uploadTimeUnix;


        [ProtoMember(6)] public readonly uint mapId;

        [ProtoMember(8)] public readonly uint songDurationSeconds;

        [ProtoMember(9, OverwriteList = true)] public readonly byte[] hashBytes;


        [ProtoMember(10)] public readonly string songName;
        [ProtoMember(11)] public readonly string songAuthorName;
        [ProtoMember(12)] public readonly string levelAuthorName;

        [ProtoMember(13, OverwriteList = true)] internal readonly SongDifficultyProto[] difficulties;
#pragma warning restore 649
    }

    public readonly struct Song {
        internal Song(uint index, uint diffOffset, byte diffCount, SongProto proto) {
            this.index = index;
            this.diffOffset = diffOffset;
            this.diffCount = diffCount;

            bpm = proto.bpm;
            downloadCount = proto.downloadCount;
            upvotes = proto.upvotes;
            downvotes = proto.downvotes;
            uploadTimeUnix = proto.uploadTimeUnix;
        }

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
                var tmp = upvotes / tot;

                return (float)(tmp - (tmp - 0.5) * Math.Pow(2, -Math.Log10(tot + 1)));
            }
        }

        /// <summary>
        /// Unix timestamp of when the map was uploaded
        /// </summary>
        public readonly uint uploadTimeUnix;

        /// <summary>
        /// Returns the uploadTimeUnix converted to a DateTime object
        /// </summary>
        public DateTime uploadTime => DateTimeOffset.FromUnixTimeSeconds(uploadTimeUnix).DateTime;

        internal readonly uint index;
        internal readonly uint diffOffset;
        internal readonly byte diffCount;

        /// <summary>
        /// Hexadecimal representation of the Map ID
        /// </summary>
        public readonly string key => SongDetailsContainer.keys[index].ToString("x");

        /// <summary>
        /// Hexadecimal representation of the Map Hash
        /// </summary>
        public readonly string hash => HexUtil.SongBytesToHash(index); // This should probably not be here.

        public ref readonly string songName => ref SongDetailsContainer.songNames[index];
        public ref readonly string songAuthorName => ref SongDetailsContainer.songAuthorNames[index];
        public ref readonly string levelAuthorName => ref SongDetailsContainer.levelAuthorNames[index];

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

        /// <summary>
        /// All difficulties that belong to this Song
        /// </summary>
        public IReadOnlyCollection<SongDifficulty> difficulties => new ArraySegment<SongDifficulty>(SongDetailsContainer.difficulties, (int)diffOffset, diffCount);
    }
}