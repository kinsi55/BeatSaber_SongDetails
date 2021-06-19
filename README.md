# BeatSaber_SongDetails
A C# library which provides access to the ScoreSaber and BeatSaver information of all Songs using a cached local database which is provided by [Andruzzzhka](https://github.com/andruzzzhka/BeatSaberScrappedData), thanks a lot!

Can be used Standalone or as a BeatSaber Library. Aims to be highly optimized by allocating as little reference types as possible and passing value types by reference wherever possible.

Requires Protobuf.

# Usage

Everything is exposed in the `SongDetailsCache.SongDetails` class.

To use this library you aquire an Instance of the SongDetails class by calling `SongDetails.Init()` and awaiting the result. This will handle possibly downloading / parsing the database if it is not cached in memory yet, otherwise it will reuse what is already there.

1. songs => Custom, readonly array of all songs. Has exposed methods for finding songs based off their Map ID or Hash
2. difficulties => Custom, readonly array of all difficulties of all songs. Due to how this library is built you should ideally not access `<Song>.difficulties` for iteration purposes but instead iterate over difficulties and access `<SongDifficulty>.song`. Alternatively you can use the exposed convenience methods of `SongDetails` like `FindSongs` to find all Songs based off certain difficulty criteria.

There are also two statically available events on SongDetailsContainer, dataAvailableOrUpdated and dataLoadFailed

**WHENEVER `dataAvailableOrUpdated` IS CALLED YOU SHOULD INVALIDATE *ANY* REFERENCE YOU HAVE TO ANY SongDetails CLASS**. Almost all properties are resolved dynamically using indexes and the order of items is very likely to change after the dataset is updated.

## Available information

### Songs
- General Information
	- Bpm
	- Song Duration (Seconds)
	- Song Hash
	- Song Name
	- Song Author
	- Level Author
- BeatSaver Information
	- Map ID (Numeric and Hexadecimal)
	- Song cover URL
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
		- Usually the error is way less than 5pp of the actual #1 score but some outliers exist
	- Planned: If the song is unranked / qualified / ranked and when it switched to qualified / ranked
