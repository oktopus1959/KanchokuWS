#! /bin/bash

cat $* | \
    colorcat.sh \
'createRewriteNode|PostRewriteOneShotNode merged|makeSubTree|createNode.*(ENTER|LEAVE)|OUTPUT_STACK->setRewritable| targetStr=|tail_size_while_only_and_upto|ADD reversePath\
|RootStrokeNode[12]|StrokeMergerState|[Dd]estructor|LatticeImpl|DecoderImpl.HandleDeckey|MecabBridge.mecabCalcCost|ABANDON|handleStrokeKeys|StrokeMergerState.HandleDeckey\
|deckey=.* statesNum=.*|handleDeckey_single|BushuAssocExState|PostRewriteOneShotState|DoProcOnCreated|StrokeTableState.HandleDeckey|State.dispatchDeckey|CurrentStrokeTable=[12]\
|[Cc]reateStrokeTree[123]?|Eisu(State)?|: (ja|LastJapanese)Key=.*|NAMESPACE.RomanToKatakana' | \
    colorcat.sh -r 'WARN.*' | \
    colorcat.sh -y ' WARN ' | \
    colorcat.sh -r ' ERROR ' | \
    colorcat.sh -c ' INFOH? |ENTER(: deckey=[^,]*, mod)|ENTER|LEAVE|CALLED|==== TEST\([0-9]+\):.* ====' | \
    colorcat.sh -b 'GetKeyCombinationWhenKeyUp|_findCombo|_isCombinationTiming' | \
    colorcat.sh -g '[、。ぁ-龠]+'
