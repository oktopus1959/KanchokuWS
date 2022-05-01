#pragma once

#include "string_type.h"
#include "langedge/pathname.hpp"
#include "langedge/pathutil.hpp"

namespace utils {
    // path の親ディレクトリを返す
    inline tstring getParentDirPath(const tstring& path) {
        return langedge::PathName::parentDirectory(path);
    }

    // path のファイル名の部分を返す
    inline tstring getFileName(const tstring& path) {
        return langedge::PathName::extractFileName(path);
    }

    /** ディレクトリを作成する.
     * 複数階層に渡ってディレクトリを作成する.
     * @param path ディレクトリパス (PathStringType 文字列)
     * @retval true 成功
     * @retval false 失敗
     */
    inline bool makeDirectory(const tstring& path) {
        return langedge::makeDirectory(path);
    }

    // 2つの path を結合する。後者が絶対パスなら、後者を返す
    inline tstring joinPath(const tstring& path1, const tstring& path2) {
        if (langedge::PathName::isAbsolutePath(path2)) {
            return path2;
        } else {
            return langedge::canonicalizePath(langedge::PathName::joinPathName(path1, path2));
        }
    }

    // path のデリミタの正規化
    inline tstring canonicalizePathDelimiter(const tstring& path) {
        return utils::replace_all(path, '/', '\\');
    }

    // カレントディレクトリの取得
    inline tstring getCurrentDirName() {
        TCHAR dirName[MAX_PATH + 1];
        GetCurrentDirectory(_countof(dirName), dirName);
        return dirName;
    }

    // ファイルが存在するか
    inline bool isFileExistent(const tstring& path) {
        return langedge::isFileExistent(path);
    }

    // ファイルの削除
    inline void removeFile(const tstring& path) {
        langedge::removeFile(path);
    }

    // ファイルの move
    inline void moveFile(const tstring& src, const tstring& dest) {
        if (isFileExistent(src) && !dest.empty()) {
            if (isFileExistent(dest)) removeFile(dest);
            _trename(src.c_str(), dest.c_str());
        }
    }

    // ファイルの copy
    inline void copyFile(const tstring& src, const tstring& dest) {
        CopyFile(src.c_str(), dest.c_str(), FALSE);
    }

    // ファイルを back ディレクトリに移動(backファイルのローテートもやる)
    inline bool moveFileToBackDirWithRotation(const tstring& path, int genNum = 3, bool bCopy = false) {
        // path と同じ階層に back ディレクトリを作成
        auto backDirPath = utils::joinPath(utils::getParentDirPath(path), _T("back"));
        if (!utils::makeDirectory(backDirPath)) return false;

        // backファイルのローテーション
        auto backFilePathFmt = utils::joinPath(backDirPath, utils::getFileName(path)) + _T(".%d");
        while (genNum > 1) {
            utils::moveFile(utils::format(backFilePathFmt.c_str(), genNum - 1), utils::format(backFilePathFmt.c_str(), genNum));
            --genNum;
        }
        // path を back/filename.back に move or copy
        if (bCopy)
            utils::copyFile(path, utils::format(backFilePathFmt.c_str(), 1));
        else
            utils::moveFile(path, utils::format(backFilePathFmt.c_str(), 1));
        return true;
    }

    // ファイルを back ディレクトリにコピー(backファイルのローテートもやる)
    inline bool copyFileToBackDirWithRotation(const tstring& path, int genNum = 3) {
        return moveFileToBackDirWithRotation(path, genNum, true);
    }
}
