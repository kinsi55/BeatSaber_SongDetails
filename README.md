# BeatSaber_SongDetails
A C# library which provides access to the ScoreSaber and BeatSaver information of all Songs using a cached local database which is provided by [Andruzzzhka](https://github.com/andruzzzhka/BeatSaberScrappedData), thanks a lot!

Can be used Standalone or as a BeatSaber Library. Aims to be highly optimized and allocate as little reference types as possible and passes value types by reference wherever possible.

Requires Protobuf.

# Usage

Everything is exposed in the `SongDetailsCache.SongDetails` class.

1. songs => array of all songs
2. difficulties => array of all difficulties of all songs. Due to how this library is built you should ideally not access a songs `.difficulties` for iteration purposes but instead iterate over difficulties and access the `.song`. Alternatively you can use the exposed convenience methods like `FindSongs` to find all Songs based off certain difficulty criteria.

## Available information

### Songs
- General Information
	- Bpm
	- Song Hash
	- Song Name
	- Song Author
	- Level Author
- BeatSaver Information
	- Downloads
	- Upvotes
	- Downvotes
	- Rating
	- Upload timestamp

### Song Difficulties
- General Information
	- Njs
	- Bombs / Notes / Obstacles count
	- Characteristic
- ScoreSaber Information
	- Score count
	- Ranked state (Ranked or not)
	- Star rating
	- Approximate achieveable #1 PP value
		- Uses a custom curve
		- Usually the error is way less than 5pp of the actual #1 score