#! /bin/bash

colorcat.sh 'Determiner.*(ENTER|LEAVE)|COMBO.*(PASSED|FOUND|NOT|FAILED)|(result|hotList|comboList|unprocList)=[0-9:]+|TIMER. (STARTED|ELAPSED)|TRY NEXT:.*|(NO )?RULE[ (].*|bTemporaryComboDisabled=True\
|No combo shift key|Single Hittable or SequentialShift|sendVkeyFromDeckey|SandS|vkeyQueue.Count=.*|KeyboardEventHandler.invokeHandler|cancelPreRewrite|LatticeState\
|ChangeCurrentPoolByDecoderMode.*|Imediate.*check|KeyboardHook.HookProcedure.*Key(Up|Down)|Handle as SingleHit: .*ms|TIMER-A (STARTED|ELAPSED)\
|left=0, top=0, width=0, height=0|MOVE: X=0, Y=0, W=0, H=0|VirtualKeys.ReadExtra.* line\([0-9]+\):|COMBO SEARCH: searchKey=.*|IsTemporaryComboDisabled=\w*\
| CurrentLine:[0-9]+:.*|Analyze for .* mode|extraInfo=1959' | \
colorcat.sh -r 'WARN.*' | \
colorcat.sh -y ' WARN ' | \
colorcat.sh -r ' ERROR ' | \
colorcat.sh -c ' INFOH? |ENTER(: deckey=[^,]*, mod)|ENTER|LEAVE|CALLED|==== TEST\([0-9]+\):.* ====' | \
colorcat.sh -b 'GetKeyCombinationWhenKeyUp|_findCombo|_isCombinationTiming' | \
colorcat.sh -g '[、。ぁ-龠]+'
