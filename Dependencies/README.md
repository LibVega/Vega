This directory contains the library dependencies for Vega.

**Note: All libraries are simply rehosted, and carry their original licenses and links to their original project/author pages.**

# Native Libraries

## Native Library Naming
Native library dependencies are embedded into the Vega library as `EmbeddedResource` items. There is utility code in the library that is able to detect the correct library to extract and load at runtime. All native libraries have the same base resource name, with platform-specific extensions:

* `.win` - Windows binaries
* `.mac` - MacOS desktop binaries
* `.lin` - Linux desktop binaries (not used currently, Linux dependencies must be installed separately)

Extracted libraries are put in a specific folder within the system "Local Application Data" folder, so they only need to be extracted once.

These libraries can be overridden by placing other binaries into the same directory as the application. These will be detected, and loaded instead of the embedded libraries. This allows third-party updates to the libraries, as well as provides a "re-linking" mechanism that is required by some library licenses.

## [GLFW3](https://www.glfw.org/)

* License: [zlib](https://github.com/glfw/glfw/blob/master/LICENSE.md)
* Description: Used to handle window operations and peripheral input events.
* Version: `3.3+` (`3.3.2` included for Windows/MacOS)
* Type: Pre-compiled binaries from GLFW authors

## [OpenAL-Soft](https://openal-soft.org/)

* License: [LGPLv2](https://github.com/kcat/openal-soft/blob/master/COPYING)
  * Note that this license requires re-linkability. There is built-in functionality available within Vega to allow easy re-linking by simply putting a different version of the library in the same directory as the application.
* Description: Used for audio playback and effects
* Version: `1.20.1` included for Windows/MacOS
* Type: Manually built from source release

## [ContentLoader](https://github.com/VegaLib/ContentLoader)

This is a separately maintained library, part of the Vega project. It relies on third-party code, information about which can be found in that repo.

* License: [MIT](https://github.com/VegaLib/ContentLoader/blob/master/LICENSE)
* Description: Used to load certain kinds of content
* Version: `N/A`
* Type: Manually built from source release
