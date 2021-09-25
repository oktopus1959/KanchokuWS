#include "Logger.h"

#include "StrokeTable.h"
#include "StayState.h"
#include "Mazegaki/Mazegaki.h"

#define _LOG_DEBUGH_FLAG (SETTINGS->debughState)

#define NAME_PTR    Name.c_str()

// Esc の処理
void StayState::handleEsc() {
    HANDLE_ESC_FOR_MAZEGAKI();
}

// ZenkakuConversionの処理
void handleZenkakuConversion() {

}

DEFINE_CLASS_LOGGER(StayState);

