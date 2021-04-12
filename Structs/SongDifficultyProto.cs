﻿using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SongDetailsCache.Structs {
    public enum MapDifficulty : byte { Easy = 0, Normal, Hard, Expert, ExpertPlus }
    public enum MapCharacteristic : byte { Custom = 0, Standard, OneSaber, NoArrows, NinetyDegree, ThreeSixtyDegree, Lightshow, Lawless }

    [ProtoContract(SkipConstructor = true)]
    class SongDifficultyProto {
#pragma warning disable 649
        [ProtoMember(1)] public readonly MapCharacteristic characteristic;
        [ProtoMember(2)] public readonly MapDifficulty difficulty;

        [ProtoMember(3)] public readonly uint scoreCount;
        [ProtoMember(4)] public readonly uint starsT100;
        [ProtoMember(5)] public readonly bool ranked;

        [ProtoMember(6)] public readonly uint njsT100;

        [ProtoMember(7)] public readonly uint bombs;
        [ProtoMember(8)] public readonly uint notes;
        [ProtoMember(9)] public readonly uint obstacles;
#pragma warning restore
    }

    public readonly struct SongDifficulty {
        internal SongDifficulty(uint songIndex, SongDifficultyProto proto) {
            this.songIndex = songIndex;

            characteristic = proto.characteristic;
            difficulty = proto.difficulty;
            scoreCount = proto.scoreCount;
            stars = proto.starsT100 / 100f;
            ranked = proto.ranked;
            njs = proto.njsT100 / 100f;
            bombs = proto.bombs;
            notes = proto.notes;
            obstacles = proto.obstacles;
        }

        internal readonly uint songIndex;

        /// <summary>
        /// Amount of Scoresaber scores for this difficulty
        /// </summary>
        public readonly uint scoreCount;
        /// <summary>
        /// Scoresaber difficulty rating of this difficulty
        /// </summary>
        public readonly float stars;

        /// <summary>
        /// NJS (Note Jump Speed) of this difficulty
        /// </summary>
        public readonly float njs;
        /// <summary>
        /// Amount of bombs in this Difficulty
        /// </summary>
        public readonly uint bombs;
        /// <summary>
        /// Amount of notes in this Difficulty
        /// </summary>
        public readonly uint notes;
        /// <summary>
        /// Amount of obstacles in this Difficulty
        /// </summary>
        public readonly uint obstacles;

        public readonly MapCharacteristic characteristic;
        public readonly MapDifficulty difficulty;

        /// <summary>
        /// Returns if the Difficulty is ranked on ScoreSaber
        /// </summary>
        public readonly bool ranked;

        /// <summary>
        /// The Song this Difficulty belongs to
        /// </summary>
        public ref Song song => ref SongDetailsContainer.songs[songIndex];

        /// <summary>
        /// Returns an approximated PP value of the possible #1 ScoreSaber score.
        /// Its usually within 5pp of the real value.
        /// </summary>
        public float approximatePpValue {
            get {
                if(stars <= 0.1 || !ranked)
                    return 0;

                return stars * (45f + ((10f - stars) / 7f));
            }
        }
    }
}