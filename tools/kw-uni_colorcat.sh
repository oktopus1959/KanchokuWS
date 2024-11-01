#! /bin/bash

BINDIR=$(dirname $0)

cat $* | \
    $BINDIR/colorcat.sh \
'createRewriteNode|PostRewriteOneShotNode merged|makeSubTree|(createNode|addOnePiece).*(ENTER|LEAVE)|OUTPUT_STACK->setRewritable| targetStr=|tail_size_while_only_and_upto|ADD reversePath\
|RootStrokeNode[12]|StrokeMergerState|[Dd]estructor|DecoderImpl.HandleDeckey|MecabBridge.mecabCalcCost|ABANDON|handleStrokeKeys|StrokeMergerState.HandleDeckey\
|deckey=.* statesNum=.*|handleDeckey_single|BushuAssocExState|PostRewriteOneShotState|DoProcOnCreated|StrokeTableState.HandleDeckey|State.dispatchDeckey|CurrentStrokeTable=[12]\
|[Cc]reateStrokeTree[123]?|Eisu(State)?|: (ja|LastJapanese)Key=.*|NAMESPACE.RomanToKatakana|RootStrokeTableState|GetResultString(Chain)?|StrokeStreamList.HandleDeckeyProc\
|handleNextOrPrevCandTrigger|outputHistResult|StrokeMergerHistoryResidentStateImpl.HandleDeckeyChain|StartStateImpl.StartHandleDeckey.*==|^==*=$|\w+ candidate preferred|skip=true\
|LatticeImpl.clear\w*|kanjiPref=true|rollOver=\w+|Non rollover.*penalty=[0-9]+' | \
    $BINDIR/colorcat.sh -r 'WARN[H:].*' | \
    $BINDIR/colorcat.sh -y ' WARN ' | \
    $BINDIR/colorcat.sh -r ' ERROR ' | \
    $BINDIR/colorcat.sh -c ' INFOH? |ENTER(: deckey=[^,]*, mod)|ENTER|LEAVE|CALLED|==== TEST\([0-9]+\):.* ====' | \
    $BINDIR/colorcat.sh -b 'GetKeyCombinationWhenKeyUp|_findCombo|_isCombinationTiming' | \
    $BINDIR/colorcat.sh -g '[、。ぁ-龠]+'
