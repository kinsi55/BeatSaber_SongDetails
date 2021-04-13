# BeatSaber_SongDetails
A C# library which provides access to the ScoreSaber and BeatSaver information of all Songs using a cached local database which is provided by [Andruzzzhka](https://github.com/andruzzzhka/BeatSaberScrappedData), thanks a lot!

Can be used Standalone or as a BeatSaber Library. Aims to be highly optimized by allocating as little reference types as possible and passing value types by reference wherever possible.

Requires Protobuf.

# Usage

Everything is exposed in the `SongDetailsCache.SongDetails` class.

To use this library you aquire an Instance of the SongDetails class by calling `SongDetails.Init()` and awaiting the result. This will handle possibly downloading / parsing the database if it is not cached in memory yet, otherwise it will reuse what is already there.

1. songs => Custom, readonly array of all songs. Has exposed methods for finding songs based off their Map ID or Hash
2. difficulties => Custom, readonly array of all difficulties of all songs. Due to how this library is built you should ideally not access `<Song>.difficulties` for iteration purposes but instead iterate over difficulties and access `<SongDifficulty>.song`. Alternatively you can use the exposed convenience methods of `SongDetails` like `FindSongs` to find all Songs based off certain difficulty criteria.

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
	- Song Duration (Seconds)
- ScoreSaber Information
	- Score count
	- Ranked state (Ranked or not)
	- Star rating
	- Approximate achieveable #1 PP value
		- Uses a custom curve
		- Usually the error is way less than 5pp of the actual #1 score but some outliers exist
