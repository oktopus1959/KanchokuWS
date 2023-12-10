#include "string_utils.h"
#include "misc_utils.h"
#include "path_utils.h"
#include "file_utils.h"
#include "Logger.h"

#include "settings.h"
#include "Lattice.h"

#define LATTICE_TEST

#ifdef LATTICE_TEST
namespace {
    DEFINE_LOCAL_LOGGER(LatticeTest);

    void latticeTest() {
        _LOG_DEBUGH(_T("ENTER"));
        auto path = utils::joinPath(SETTINGS->rootDir, _T("stroke-lattice-test.txt"));
        _LOG_DEBUGH(_T("LOAD: %s"), path.c_str());
        utils::IfstreamReader reader(path);
        if (reader.success()) {
            for (const auto& line : reader.getAllLines()) {
                std::vector<WordPiece> pieces;
                for (auto piece : utils::split(line, '|')) {
                    auto items = utils::split(piece, ',');
                    if (items.size() == 2) {
                        pieces.push_back(WordPiece(to_mstr(utils::strip(items[0])), std::stoi(utils::strip(items[1])), 0));
                    }
                }
                if (!pieces.empty()) WORD_LATTICE->addPieces(pieces);
            }
        } else {
            LOG_ERROR(_T("No such filie: %s"), path.c_str());
        }
        _LOG_DEBUGH(_T("LEAVE"));
    }

}
#endif

void Lattice::runTest() {
    __LOG_INFO(_T("\n======== ENTER: RUN TEST ========"));

#ifdef LATTICE_TEST
    int logLevel = Logger::LogLevel;
    if (logLevel < Logger::LogLevelInfoH) Logger::LogLevel = Logger::LogLevelInfoH;

    //loadCostFile();

    if (WORD_LATTICE) latticeTest();

    Logger::LogLevel = logLevel;
#endif

    __LOG_INFO(_T("LEAVE"));
}
