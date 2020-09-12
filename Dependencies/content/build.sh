#!/usr/bin/env bash
### Microsoft Public License (Ms-PL) - Copyright (c) 2020 Sean Moss
### This file is subject to the terms and conditions of the Microsoft Public License, the text of which can be found in
### the 'LICENSE' file at the root of this repository, or online at <https://opensource.org/licenses/MS-PL>.

### *nix script for generating the project build files with Premake

# Detect machine
unameVal="$(uname -s)"
machine=
case "${unameVal}" in
    Linux*)     machine=Linux;;
    Darwin*)    machine=MacOS;;
    CYGWIN*)    echo "Cannot build on Cygwin"; exit 1;;
    MINGW*)     echo "Cannot build on MinGW"; exit 1;;
    *)          echo "Unknown build system ${unameVal}"; exit 1;;
esac
echo "Building on platform ${machine}"

# Run machine commands
if [ "${machine}" == "Linux" ]; then
    ./premake5_l --file=./content.project gmake2
elif [ "${machine}" == "MacOS" ]; then
    ./premake5_m --file=./content.project gmake2
fi
