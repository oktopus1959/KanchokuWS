#include "StrokeTable.h"
#include "ResidentState.h"
#include "Mazegaki/Mazegaki.h"
#include "BushuComp/BushuComp.h"
#include "BushuComp/BushuDic.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughState)

#if 0 || defined(_DEBUG)
#define LOG_INFO LOG_INFOH
#define LOG_DEBUGH LOG_INFOH
#define _LOG_DEBUGH LOG_INFOH
#endif

// Esc の処理
void ResidentState::handleEsc() {
    _LOG_DEBUGH(_T("ENTER"));

    HANDLE_ESC_FOR_MAZEGAKI();
    HANDLE_ESC_FOR_AUTO_COMP();

    _LOG_DEBUGH(_T("CALL State::handleEsc()"));

    // 上記処理で return しなかったら下記処理に移る
    State::handleEsc();
    _LOG_DEBUGH(_T("LEAVE"));
}

//// ZenkakuConversionの処理
//void handleZenkakuConversion() {
//
//}

DEFINE_CLASS_LOGGER(ResidentState);

