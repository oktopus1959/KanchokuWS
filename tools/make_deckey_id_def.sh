#!/bin/bash

SRCDIR=KanchokuWS/Domain
TGTDIR=kw-uni

SRCFILE=$SRCDIR/DecoderKeys.cs
TGTFILE=$TGTDIR/KeysAndChars/deckey_id_defs.h

cat <<EOS > $TGTFILE
// DO NOT EDIT THIS FILE!!!!
// このファイルは $0 により $SRCFILE から自動的に作成されました ($(date '+%Y/%m/%d %H:%M:%S'))
#pragma once

EOS

sed -n '/public static class DecoderKeys/,$ p' $SRCFILE | \
    tail -n +2 | \
    grep -v '^ *[{}]' | \
    grep -v 'private' | \
    sed 's/^ *//' | \
    sed 's/public const int/#define/' | \
    sed 's/= /(/' | \
    sed 's/;.*/)/' >> $TGTFILE

cat <<EOS >> $TGTFILE

namespace deckey_id_defs { const wchar_t* GetDeckeyNameFromId(int id); }
#define DECKEY_NAME_FROM_ID(id) deckey_id_defs::GetDeckeyNameFromId(id)
EOS

###
TGTFILE1=$TGTDIR/KeysAndChars/deckey_id_defs.cpp

cat <<EOS > $TGTFILE1
// DO NOT EDIT THIS FILE!!!!
// このファイルは $0 により $SRCFILE から自動的に作成されました ($(date '+%Y/%m/%d %H:%M:%S'))

#include "string_type.h"
#include "deckey_id_defs.h"

namespace deckey_id_defs {

    std::map<int, const wchar_t*> deckeyId_name_map = {
EOS

sed -n -r 's/^ *public const int *(\w+)_DECKEY *= *([^;]+).*$/        {(\2), _T("\1")},/p' $SRCFILE >> $TGTFILE1

cat <<EOS >> $TGTFILE1
    };

    const wchar_t* GetDeckeyNameFromId(int id) {
        auto iter = deckeyId_name_map.find(id);
        return iter != deckeyId_name_map.end() ? iter->second : _T("?");
    }

} // namespace deckey_id_defs
EOS
