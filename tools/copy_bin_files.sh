#!/bin/bash

. ~/bin/debug_util.sh

RUN_CMD -m "cd bin/Release"

RUN_CMD -m "cp -p KanchokuWS.exe* kw-uni.dll Utils.dll ../../../publish/bin/"
