/*
 * Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
 * This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
 * the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.
 */

#define STB_VORBIS_HEADER_ONLY
#define DR_WAV_IMPLEMENTATION
#define DR_FLAC_IMPLEMENTATION

#include "./dr_flac.h"
#include "./dr_wav.h"
#include "./stb_vorbis.c"

#include <cstdint>
#include <string>
#include <fstream>


#ifdef _WIN32
#	define EXPORT_API extern "C" __declspec(dllexport)
#else
#	define EXPORT_API extern "C" __attribute__((visibility("default")))
#endif // _WIN32


enum class SoundError : int32_t
{
	NO_ERROR = 0,			// No Error
	FILE_NOT_FOUND = 1,		// The file does not exist
	UNKNOWN_TYPE = 2,		// The file type is not a known audio file type
	INVALID_FILE = 3,		// The file exists, but could not be opened or was invalid
}; // enum class SoundError


struct SoundFileInfo final
{
public:
	uint64_t totalFrames;	// Total frame count (frame is any set of concurrent samples across all channels)
	uint32_t sampleRate;	// The default playback rate (hz)
	uint32_t channels;		// The number of channels
}; // struct SoundFileInfo


/// Maintains a handle on a sound file
/// This type is designed to be used as an IntPtr in C# code
class SoundFileHandle final
{
public:
	enum FileType : int32_t
	{
		TYPE_UNKNOWN = 0,
		TYPE_WAV = 1,
		TYPE_FLAC = 2,
		TYPE_VORBIS = 3
	}; // enum FileType

public:
	explicit SoundFileHandle(const std::string& file)
		: type_{ TYPE_UNKNOWN }
		, fileName_{ file }
		, handle_{ nullptr }
		, info_{ }
		, lastError_{ SoundError::NO_ERROR }
	{
		// Try to open file
		std::ifstream fstm(file);
		if (!fstm.is_open()) {
			lastError_ = SoundError::FILE_NOT_FOUND;
			return;
		}
		
		// Detect the file type
		{
			const auto extPos = file.find_last_of('.');
			if (extPos == std::string::npos) {
				lastError_ = SoundError::UNKNOWN_TYPE;
				return;
			}
			const auto ext = file.substr(extPos);
			type_ =
				(ext == ".wav") ? TYPE_WAV :
				(ext == ".flac") ? TYPE_FLAC :
				(ext == ".ogg") ? TYPE_VORBIS : TYPE_UNKNOWN;
			if (type_ == TYPE_UNKNOWN) {
				lastError_ = SoundError::UNKNOWN_TYPE;
				return;
			}
		}

		// Initialize the file
		if (type_ == TYPE_WAV) {
			handle_.wav = new drwav;
			if (!drwav_init_file(handle_.wav, file.c_str(), nullptr)) {
				delete handle_.wav;
				handle_.wav = nullptr;
				lastError_ = SoundError::INVALID_FILE;
			}
		}
		else if (type_ == TYPE_FLAC) {
			handle_.flac = drflac_open_file(file.c_str(), nullptr);
			if (!handle_.flac) {
				lastError_ = SoundError::INVALID_FILE;
			}
		}
		else if (type_ == TYPE_VORBIS) {
			int err;
			handle_.vorbis = stb_vorbis_open_filename(file.c_str(), &err, nullptr);
			if (!handle_.vorbis) {
				lastError_ = SoundError::INVALID_FILE;
			}
		}
		if (lastError_ == SoundError::INVALID_FILE) { // return on invalid file
			return;
		}

		// Load file information
		if (type_ == TYPE_WAV) {
			info_.totalFrames = handle_.wav->totalPCMFrameCount;
			info_.sampleRate = handle_.wav->sampleRate;
			info_.channels = handle_.wav->channels;
		}
		else if (type_ == TYPE_FLAC) {
			info_.totalFrames = handle_.flac->totalPCMFrameCount;
			info_.sampleRate = handle_.flac->sampleRate;
			info_.channels = handle_.flac->channels;
		}
		else { // TYPE_VORBIS
			const auto info = stb_vorbis_get_info(handle_.vorbis);
			const auto count = stb_vorbis_stream_length_in_samples(handle_.vorbis);
			info_.totalFrames = count / info.channels;
			info_.sampleRate = info.sample_rate;
			info_.channels = info.channels;
		}

		lastError_ = SoundError::NO_ERROR;
	}
	~SoundFileHandle()
	{
		if (type_ == TYPE_WAV && handle_.wav) {
			drwav_uninit(handle_.wav);
			delete handle_.wav;
		}
		else if (type_ == TYPE_FLAC && handle_.flac) {
			drflac_close(handle_.flac);
		}
		else if (type_ == TYPE_VORBIS && handle_.vorbis) {
			stb_vorbis_close(handle_.vorbis);
		}
	}

	// Info
	inline FileType type() const { return type_; }
	inline const std::string& fileName() const { return fileName_; }
	inline const SoundFileInfo& info() const { return info_; }
	inline bool hasError() const { return lastError_ != SoundError::NO_ERROR; }
	inline SoundError error() const { return lastError_; }

private:
	FileType type_;
	const std::string fileName_;
	union
	{
		drwav* wav;
		drflac* flac;
		stb_vorbis* vorbis;
	} handle_;
	SoundFileInfo info_;
	SoundError lastError_;
}; // class SoundFileHandle



/// API: Get sound type
EXPORT_API int32_t soundGetFileType(SoundFileHandle* file)
{
	return int32_t(file->type());
}

/// API: Get sound filename
EXPORT_API const char* soundGetFileName(SoundFileHandle* file)
{
	return file->fileName().c_str();
}

/// API: Get sound error
EXPORT_API int32_t soundGetError(SoundFileHandle* file)
{
	return int32_t(file->error());
}

/// API: Get sound frame count
EXPORT_API uint64_t soundGetFrameCount(SoundFileHandle* file)
{
	return file->info().totalFrames;
}

/// API: Get sound sample rate
EXPORT_API uint32_t soundGetSampleRate(SoundFileHandle* file)
{
	return file->info().sampleRate;
}

/// API: Get sound channel count
EXPORT_API uint32_t soundGetChannelCount(SoundFileHandle* file)
{
	return file->info().channels;
}

/// API: Get sound info
EXPORT_API void soundGetInfo(SoundFileHandle* file, uint64_t* frames, uint32_t* rate, uint32_t* channels)
{
	*frames = file->info().totalFrames;
	*rate = file->info().sampleRate;
	*channels = file->info().channels;
}

/// API: Create Sound File
EXPORT_API SoundFileHandle* soundOpenFile(const char* const fileName, int32_t* error)
{
	auto handle = new SoundFileHandle(fileName);
	*error = int32_t(handle->error());
	if (handle->hasError()) {
		delete handle;
		return nullptr;
	}
	return handle;
}

/// API: Destroy sound file
EXPORT_API void soundCloseFile(SoundFileHandle* file)
{
	delete file;
}
