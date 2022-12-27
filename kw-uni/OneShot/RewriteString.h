#pragma once

#define ANALYZE_REWRITE_STR(_str, _rew, _len) \
    _rew = utils::replace(_str, _T("/"), _T(""));\
    size_t _pos = _str.find('/', 0);\
    _len = _pos <= _rew.size() ? _rew.size() - _pos : _rew.empty() ? 0 : 1;
    //_len = _rew.size() - (_pos <= _rew.size() ? _pos : 0);

