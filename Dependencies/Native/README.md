Documentation specific to the native library dependencies.

## OpenAL-Soft (1.20.1 Release)

OpenAL-Soft is built from source on all platforms that support it. The CMAKE options are given below (only options that are not default):

### Windows
Type: VS2019 (x64)

* AMBDEC_PRESETS = OFF
* BACKEND_DSOUND = OFF
* BACKEND_WAVE = OFF
* BACKEND_WINMM = OFF
* CONFIG = OFF
* CPUEXT_SSE4_1 = OFF
* EXAMPLES = OFF
* HRTF_DEFS = OFF
* INSTALL = OFF
* REQUIRE_SSE = ON
* REQUIRE_SSE2 = ON
* REQUIRE_SSE3 = ON
* REQUIRE_WASAPI = ON
* TESTS = OFF
* UTILS = OFF
* CMAKE_BUILD_TYPE = Release