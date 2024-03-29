#include "StrokeTable.h"
#include "ResidentState.h"
#include "Mazegaki/Mazegaki.h"
#include "BushuComp/BushuComp.h"
#include "BushuComp/BushuDic.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughState)

// Esc の処理
void ResidentState::handleEsc() {
    HANDLE_ESC_FOR_MAZEGAKI();
    HANDLE_ESC_FOR_AUTO_COMP();

    // 上記処理で return しなかったら下記処理に移る
    State::handleEsc();
}

//// ZenkakuConversionの処理
//void handleZenkakuConversion() {
//
//}

DEFINE_CLASS_LOGGER(ResidentState);

