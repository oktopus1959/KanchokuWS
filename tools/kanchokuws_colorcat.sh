#! /bin/bash

BINDIR=$(dirname $0)

$BINDIR/colorcat.sh 'Determiner.*(ENTER|LEAVE)|COMBO.*(PASSED|FOUND|NOT|FAILED)|(result|hotList|comboList|unprocList)=[0-9:]+|TIMER. (STARTED|ELAPSED)|TRY NEXT:.*|(NO )?RULE[ (].*|bTemporaryComboDisabled=True\
|No combo shift key|Single Hittable or SequentialShift|sendVkeyFromDeckey|SandS|vkeyQueue.Count=.*|KeyboardEventHandler.invokeHandler|cancelPreRewrite|LatticeState\
|ChangeCurrentPoolByDecoderMode.*|Imediate.*check|KeyboardHook.HookProcedure.*Key(Up|Down)|Handle as SingleHit: .*ms|TIMER-A (STARTED|ELAPSED)\
|left=0, top=0, width=0, height=0|MOVE: X=0, Y=0, W=0, H=0|VirtualKeys.ReadExtra.* line\([0-9]+\):|COMBO SEARCH: searchKey=.*|IsTemporaryComboDisabled=\w*\
| CurrentLine:[0-9]+:.*|Analyze for .* mode|extraInfo=1959|challengeList=[0-9:]+|isCombinationTiming.* RESULT.=\w+|TIMER-. ELAPSED:|SendInputHandler.\w+' | \
$BINDIR/colorcat.sh -r 'WARN.*' | \
$BINDIR/colorcat.sh -y ' WARN ' | \
$BINDIR/colorcat.sh -r ' ERROR ' | \
$BINDIR/colorcat.sh -c ' INFOH? |ENTER(: deckey=[^,]*, mod)|ENTER|LEAVE|CALLED|==== TEST\([0-9]+\):.* ====' | \
$BINDIR/colorcat.sh -b 'GetKeyCombinationWhenKeyUp|_findCombo|_isCombinationTiming' | \
$BINDIR/colorcat.sh -g '[、。ぁ-龠]+'
