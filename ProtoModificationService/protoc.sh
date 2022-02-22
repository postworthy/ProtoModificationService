#!/bin/bash

#if we only have 1 param then we do the raw decode
if [ $# -eq 1 ]; then
    protoc --decode_raw < $1
fi

#if we have 3 param then we do the decode -> strip bad
if [ $# -eq 3 ]; then
    # $1 = raw file
    # $2 = proto directory
    # $3 = proto file
    # $4 = not used
    cat $1 | protoc --decode="MyProto_0" --proto_path=$2 $3 | sed '/: 0x/d'
fi

#if we have 4 param then we do the decode -> strip bad -> encode
if [ $# -eq 4 ]; then
    # $1 = transformed file
    # $2 = proto directory
    # $3 = proto file
    # $4 = not used
    cat $1 | protoc --encode="MyProto_0" --proto_path=$2 $3
fi
