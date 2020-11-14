# Vega

[![License](https://img.shields.io/badge/License-Ms--PL-blue)](https://github.com/VegaLib/Vega/blob/master/LICENSE)
[![Build Status](https://travis-ci.com/VegaLib/Vega.svg?branch=master)](https://travis-ci.com/VegaLib/Vega)

Vulkan-powered .NET 5 framework for games, visualizations, and other multimedia apps. 

This library is inflenced primarily by the [Monogame](https://www.monogame.net/) project, but designed to map more closely to the underlying graphics API (Vulkan). The result is a library that can be picked up by relative beginners, but also rewards developers who are willing to get a bit dirty with optimizations and lower-level operations.

Currently, Vega supports 64-bit Windows, MacOS, and Linux desktop applications. Mobile and console support is expected, but desktop is the main focus until the library is nearing feature-complete. 32-bit will not be supported.

## Dependencies

Dependencies in the form of libraries or other files are included in the Vega library binary as embedded resources. Native libraries are extracted and loaded at runtime. This allows the entirety of the Vega library to be used as a single file. While this may bloat the size of the library a bit, it allows for simple distribution with drop-and-play functionality.

#### Vulkan

The Vulkan runtime is required to run applications built with Vega. This is often supplied with the GPU drivers, and thus needs no futher actions from the users of the application. The Vulkan SDK is not required at this time for developers, but needs to be installed if the Vulkan debug layers are wanted in Debug builds.

## Acknowledgements

Thanks to the following projects/groups for their libraries or tools used by or adapted for Vega:

* [GLFW3](https://www.glfw.org/) - Library used for windowing and input
* [OpenAL-Soft](https://openal-soft.org/) - Library used for audio playback and effects
* Additional dependencies used in the [ContentLoader](https://github.com/VegaLib/ContentLoader) project

## Licensing

The Vega project, with all related code and assets, is licensed under the [Microsoft Public License (Ms-PL)](https://opensource.org/licenses/MS-PL). The license allows public, commericial, and private use, but requires that changes to the original source be under the Ms-PL (or compatible) license if made open source. This license was chosen to allow permissive use of the Vega project, but to still encourage improvements to the code to be shared with others. See the LICENSE file in this repo, or online at the link above, for the full details.

Dependency libraries used by Vega are used within and are subject to their original licenses. All libraries belong to their original authors - the Vega authors make no ownership or authorship claims on them. Please see each of the libraries for more details.
