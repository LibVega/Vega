This directory contains the source code and project files for the `content` dependency. 

This library is used to load audio and texture files at runtime. It acts to centralize and standardize C# access to the various libraries that implement the loading functionality.

This project only need to be rebuilt if its source is changed. Normally, pre-compiled binaries are made available for this library in `../Native`. The project generation task is provided by Premake.

## Build

If the library source is changed, or the component libraries updated, you must rebuild the source on all platforms and make the updated binaries available in `../Native`.

To build:

1. Run the `build.*` script correct for your platform (`.bat` for Windows, `.sh` for *nix).
2. Build the resulting project files (in an IDE or the terminal) in the `OUT` directory.
3. Copy the resulting shared library into `../Native`, and rename to match the existing project file.
