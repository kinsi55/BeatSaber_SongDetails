using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Text;

namespace SongDetailsCache.Structs {
    [ProtoContract(SkipConstructor = true)]
    class SongProtoContainer {
#pragma warning disable 649
        [ProtoMember(1)] public readonly byte formatVersion;
        [ProtoMember(2)] public readonly uint scrapeEndedTimeUnix;
        [ProtoMember(4)] public readonly SongProto[] songs;
#pragma warning restore 649
    }
}
