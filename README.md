# KanchokuWS -- 漢直Win Spoiler
KanchokuWS は、[漢直Win（漢直窓）](https://github.com/kanchoku/kw)の使い勝手を受け継ぎ、
かつ、利用者を徹底的に甘やかすことを目標として新たに開発された、
Windows 用の漢字直接入力ツールです。

本稿では「KanchokuWS」と呼称していますが、「漢直窓S」「漢直WS」などの略称を用いる場合もあります。

## 目次

- [元祖漢直窓から受け継いだ機能](#元祖漢直窓から受け継いだ機能)
- [KanchokuWS で搭載された新機能](#KanchokuWS-で搭載された新機能)
- [動作環境](#動作環境)
- [インストールとアンインストール](#インストールとアンインストール)
    - [インストール](#インストール)
    - [アンインストール](#アンインストール)
- [起動](#起動)
    - [起動画面と設定ダイアログ](#起動画面と設定ダイアログ)
        - [起動画面の非表示](#起動画面の非表示)
- [漢直モードに入る](#漢直モードに入る)
    - [仮想鍵盤とモード標識](#仮想鍵盤とモード標識)
- [漢直モードから出る](#漢直モードから出る)
- [終了と再起動](#終了と再起動)
- [設定ファイル、その他各種ファイル](#設定ファイル、その他各種ファイル)
    - [kanchoku.ini の引き継ぎ](#kanchokuini-の引き継ぎ)
    - [各種ファイルの引き継ぎとUTF-8化](#各種ファイルの引き継ぎとUTF-8化)
        - [サロゲートペア](#サロゲートペア)
    - [辞書バックアップファイルの世代管理](#辞書バックアップファイルの世代管理)
- [補助入力機能](#補助入力機能)
    - [ミニバッファとブロッカー](#ミニバッファとブロッカー)
    - [入力履歴検索](#入力履歴検索)
        - [履歴検索](#履歴検索)
        - [履歴候補の操作](#履歴候補の操作)
    - [交ぜ書き変換](#交ぜ書き変換)
        - [動的交ぜ書き](#動的交ぜ書き)
        - [活用型](#活用型)
        - [交ぜ書き辞書](#交ぜ書き辞書)
        - [辞書登録](#辞書登録)
    - [連想入力](#連想入力)
        - [連想直接入力](#連想直接入力)
        - [辞書登録](#辞書登録)
    - [部首合成](#部首合成)
        - [計算順序](#計算順序)
        - [YAMANOBEアルゴリズム](#YAMANOBEアルゴリズム)
        - [部品としての使用頻度の低い文字を優先](#部品としての使用頻度の低い文字を優先)
        - [各ステップごとに逆順演算を適用](#各ステップごとに逆順演算を適用)
        - [再帰足し算(部品から合成可能な文字を用いた足し算)](#再帰足し算部品から合成可能な文字を用いた足し算)
        - [合成の次候補出力](#合成の次候補出力)
        - [辞書登録](#辞書登録)
    - [英数記号全角入力](#英数記号全角入力)
    - [カタカナ入力](#カタカナ入力)
- [設定ダイアログ](#設定ダイアログ)
- [その他の機能](#その他の機能)
    - [カレットに追随する仮想鍵盤](#カレットに追随する仮想鍵盤)
    - [打鍵ヘルプ](#打鍵ヘルプ)
        - [ミニバッファへのコピペによるヘルプ表示](#ミニバッファへのコピペによるヘルプ表示)
    - [Ctrl-H や Ctrl-B,F,P,Nなどの特殊キーへの変換](#Ctrl-H-や-Ctrl-BFPNなどの特殊キーへの変換)
- [今後の機能追加予定(順不同)](#今後の機能追加予定順不同)
- [引用元](#引用元)
- [利用条件と免責](#利用条件と免責)


## 元祖漢直窓から受け継いだ機能
以下の機能は KanchokuWS でも利用可能です。

- テーブルファイルに基づく漢字直接入力
- 後置部首合成
- 後置交ぜ書き変換
- 英数記号全角入力
- カタカナ入力
- Shiftかな入力をカタカナ変換するモード

逆に言うと以下のような機能は受け継いでいません。

- 前置部首合成
- 前置交ぜ書き変換
- 半角カタカナ入力
- 熟語ガイド
- IME連携
- その他、いろんな機能

## KanchokuWS で搭載された新機能

- 入力履歴検索
- 動的交ぜ書き変換
- 連想入力
- 部首合成の新アルゴリズム
- UTF-8の全面採用(サロゲートペアもサポート)
- 設定ダイアログによるカスタマイズ

## 動作環境
動作対象環境は以下のとおりです。

Windows 10 および .NET Framework 4.8

## インストールとアンインストール
### インストール
[zip ファイル](https://github.com/oktopus1959/KanchokuWS/releases/download/v1.0.0/KanchokuWS-v1.0.0.zip)をダウンロードして、適当なフォルダに展開してください。
以下のようなファイルが格納されています。以下、このフォルダを「ルートフォルダ」と呼称します。

![Tree](image/tree.png)

### アンインストール
上記のルートフォルダを削除します。なお、レジストリは使用していません。

## 起動
bin\KanchokuWS.exe を実行します。CMDプロンプトから実行してもよいですし、
ファイルエクスプローラからダブルクリックで実行してもかまいません。
日常的に使用するのであれば、エクスプローラの右クリックメニューから
「スタートメニューにピン留めする」を実行して、
スタートメニューから起動できるようにしておくとよいでしょう。

### 起動画面と設定ダイアログ
起動すると起動画面(スプラッシュウィンドウ)が表示されます。

![Splash](image/splash.png)

辞書ファイルなどの読み込みが完了すると「OK」「設定」というボタンが表示されるので、
初めての起動の場合は「設定」をクリックしてみてください。
設定ダイアログが開くので、各タブを一通り開いて見てください。
どんな機能があるか、おおよそ分かるかと思います。

![Settings1](image/settings1.png)

なお、ほとんどの設定項目にはツールチップによる説明を付与してあります。
テキストボックス、ラジオボタン、チェックボックスなど、入力用コントロールに
マウスポインタを合わせてみてください。約30秒間、その項目に対するチールチップが表示されます。

#### 起動画面の非表示
起動画面は、「OK」ボタンまたは「設定」ボタンをクリックするか、漢直モードに移行するか、
あるいは、一定時間が経過すると非表示になります。
最初から非表示したい場合は、「設定」-「基本設定」-「開始・終了」-「起動画面の表示時間(秒)」を
0 に設定してください。

## 漢直モードに入る
以下の方法で漢直モードに入ることができます。
- タスクトレイに格納されたアイコン ![Kanmini0](image/kanmini0.png) をクリックする
- Ctrl-￥を押す (デフォルト状態の場合。設定ダイアログから変更可能)

漢直モードに入ると、アイコンは ![Kanmini1](image/kanmini1.png) に変化し、
さらに下のような仮想鍵盤が表示されます。

![Vkb1](image/vkb1.png)

### 仮想鍵盤とモード標識

仮想鍵盤は、アクティブウィンドウのカレット位置に追随して移動します。
カレットからの相対的な表示位置を \[設定]-[基本設定]-[仮想鍵盤表示]-[カレットからの相対表示位置]
で設定することができます。

すでに漢直に習熟している方にとっては、仮想鍵盤の表示は煩わしいかと思います。
そのような場合は、\[設定]-[基本設定]-[仮想鍵盤表示]-[モード標識] を選択して、
モード標識表示に変更してみてください。

![Modemarker1](image/modemarker1.png) のようなモード標識を表示するだけの状態になります。

モード標識表示時でも、 Ctrl-T を押すことで一時的に仮想鍵盤を表示することができます。

また、モード標識の表示すら不要、という場合は、 \[設定]-[詳細設定]-[モード標識表示時間]で
- 漢直モード標識再表示間隔 = -1
- 英字モード標識表示時間 = 0

に設定してください。


## 漢直モードから出る
以下の方法で漢直モードから出ることができます。
- タスクトレイに格納されたアイコン ![Kanmini1](image/kanmini1.png) をクリックする
- 仮想鍵盤またはモード標識をクリックする
- Ctrl-￥を押す (デフォルト状態の場合。設定ダイアログから変更可能)

## 終了と再起動
- タスクトレイに格納されている本体アイコン
- 仮想鍵盤
- モード標識

のいずれかで右クリックすると、以下のようなメニューが表示されます。

![Menu1](image/menu1.png)

終了する場合は「終了」をクリックしてください。
この場合、デコーダが保持している辞書の内容が自動的にファイルに保存されます。

再起動する場合は、さらに下図のようなサブメニューが表示されます。

![Menu2](image/menu2.png)

辞書内容を保存するか、破棄するかに応じて適切なほうをクリックしてください。
たとえば、辞書ファイルを手作業で更新したので、自動保存をしたくない場合などは
「辞書内容を破棄して再起動」を選択してください。


## 設定ファイル、その他各種ファイル
ルートフォルダにある kanchoku.ini および kanchoku.user.ini が設定ファイルです。
ただ、このファイルを直接修正することはあまり必要ありません。
上述の設定ダイアログによって必要なカスタマイズは行えるようになっています。

カスタマイズの結果は kanchoku.user.ini に書き込まれます。
kanchoku.ini と kanchoku.user.ini の両方に同じエントリがある場合は、
kanchoku.user.ini のほうが優先されます。

### kanchoku.ini の引き継ぎ
インストール直後には、最低限の設定が記述された kanchoku.user.ini だけが存在しています。
元祖漢直窓を使用されている場合は、その kanchoku.ini をそのまま使うこともできます。
引き継がれる設定は、以下のとおりです。

|項目|内容|
|-|-|
|hotKey|Ctrl修飾ありの漢直モード トグルキー|
|unmodifiedHotKey|Ctrl修飾なしの漢直モード トグルキー|
|offHotKey|Ctrl修飾ありの漢直モード OFFキー|
|unmodifiedOffHotKey|Ctrl修飾なしの漢直モード OFFキー|
|tableFile|漢直方式のテーブルファイル|
|keyboard|キーボードファイル|
|bushu|部首合成辞書ファイル|
|shiftKana|Shiftでカタカナを入力|

元祖漢直窓の kanchoku.ini を使う場合は、それを KanchokuWS のルートフォルダにコピーしてから
起動（または再起動）してください。
なお、キーボードファイルとして 106.key 以外を設定している場合は、
「設定」-「基本設定」「ファイル」-「キー・文字マップファイル」
も対応したものに変更してから起動(または再起動)してください。

### 各種ファイルの引き継ぎとUTF-8化

KanchokuWS が使用するファイルは、設定ファイルを除き、すべて UTF-8 でエンコードされています。
もし、 ShiftJIS で書かれた既存ファイルを引き継ぎたい場合は、エディタなどで
UTF-8 に変換してからルートフォルダにコピーしてください。

#### サロゲートペア
KanchokuWS はサロゲートペアもサポートしています。
- テーブルファイル
- 交ぜ書き辞書ファイル
- 部首合成辞書ファイル

などのファイルで「𠮷」や「𩸽」などの文字を使って確認してみてください。
（部首合成辞書については、部品のほうにはサロゲートペアを使えません）

### 辞書バックアップファイルの世代管理
終了時（または辞書内容を保存て再起動時）には、既存の辞書ファイルが back ディレクトリに保存されます。
バックアップファイルの名前には、末尾に世代を表す数字が付加されます。
たとえば、履歴ファイルであれば、 back\\kwhist.user.txt.1 のような名前で保存されます。

バックアップファイルは、さらに何世代かにわたって保存されます。古い世代ほど、末尾の数字が大きくなります。
保存したい世代数はデフォルトで3世代となっています。
世代数を変更したい場合は、「設定」-「詳細設定」-「辞書ファイル」-「ファイル保存世代数」で設定してください。


## 補助入力機能
KanchokuWS は漢字直接入力以外に、以下のような補助入力機能を備えています。

- 入力履歴検索
- 交ぜ書き変換
- 部首合成
- 連想入力
- 英数記号全角入力
- カタカナ入力

### ミニバッファとブロッカー
ミニバッファは、仮想鍵盤の上部に位置するテキストボックスで、
アクティブウィンドウに送出された 文字列を表示する、読み取り専用のバッファです。

履歴検索、交ぜ書き変換、部首合成など、後述するいくつかの機能は、
このミニバッファに表示されている文字列の末尾部分を採取して、
それに対して各機能を呼び出しています。

このミニバッファへの出力は、Ctrl-G を押すとクリアされ、
それまでに出力した文字列が採取できなくなります。

見た目ではクリアされていますが、実際には「出力文字列の末尾に採取ブロッカーを設定」しています。
内部的には、そのブロッカーを遡って出力文字列を採取しないようになっているわけです。
 「ブロッカー」は Ctrl-G の他に Enter キーでも設定されます。

この「ブロッカー」は Ctrl-Shift-G で解除することができます。
また「Ctrl-Space」は、このブロッカーを解除してから履歴検索を行います。

### 入力履歴検索

#### 履歴検索
漢直モードで入力した漢字文字列およびカタカナ文字列を自動的に履歴として登録します。
その後、何か文字が入力されると自動的に履歴検索をして、入力された文字が先頭部とマッチする
候補を表示します。
以下は、「中」と入力したときの履歴候補表示画面です（百人一首を入力した後の履歴です）。

![History Naka](image/history_naka.png)

背景色が緑色になっているものは優先候補です。
下の設定ダイアログで「Enterキーで先頭候補を選択する」にチェックが入っていると、
ここでEnterキーを押すことで先頭候補を出力することができます。

他の候補を選択したい場合は、上下矢印キーで選択候補を移動させてください。
または「@`」(T-Code/106.key;デフォルト)を押して、後述の縦列鍵盤に移行して直接候補を選択することもできます。

履歴の自動検索を行いたくない場合は、
「設定」-「履歴・交ぜ書き」-「自動履歴検索を行う」のチェックを外してください。

![History Tab](image/history_tab.png)

「Ctrl-Space で履歴検索・候補選択する」または 「Shift-Space で履歴検索・候補選択する」
にチェックを入れておけば、それらのキーを押すことで、任意のタイミングで履歴検索ができます。

この設定タブでは、「何文字入力されたら自動検索を発動するか」を文字種ごとに設定できます。
他にもいろいろ設定項目があるので、マウスポインタを各項目のコントロールの上に置いて、
表示されるツールチップを参照してください。

#### 履歴候補の操作

自動履歴検索(あるいは Ctrl-Space などによる検索)では、候補が10個までしか表示されません。
それ以上の個数の候補を表示させたり、ゴミ登録されてしまった候補を削除したりするためには、
明示的な履歴検索を実行します。
この機能はデフォルト設定(T-Code/106.key)では「@`」に割り当てられています。
このキーを押すと以下のような縦列鍵盤が表示されます。

![History Da](image/history_da.png)

右矢印キーを押すと、さらに候補が表示されます。候補を選択するには、
列位置に対応するキーを押してください。<br/>
下図は元祖漢直窓の[ドキュメント](https://github.com/kanchoku/kw/tree/master/doc) から引用した
106キーボードの仮想鍵盤ですが、上図の「打鍵列」が「1」の列に、「打鍵用」が「0」の列に対応しています。

![keys_106_vkb](image/keys_106_vkb.png)

##### 履歴候補の削除
縦列鍵盤が表示されているときに「-=」キーを押してみてください(T-Code/106.key;デフォルト)。
下図のような表示に変わります。

![History Del](image/history_del.png)

不要な履歴があれば、ここで対応するキーを押すことで削除できます。
たとえば「打鍵目用」を削除したい場合は、「5」の列のキーを押します。

ゴミ登録された語以外にも、機微な語や四文字言葉など、人目に触れてはまずいものは削除しておきましょう。

#### 履歴の手動登録
ひらがなを含む語や、短縮形は、手動で登録します。
登録方法には下記の2通りがあります。

- 仮想鍵盤の上部ミニバッファに文字列をコピペする
- 「設定」-「辞書登録」-「履歴登録」で登録する

たとえば「お世話になっております」を履歴として登録するには、
この文字列をコピーして仮想鍵盤のミニバッファにペーストするか、
あるいは「履歴登録」のところで直接入力して「登録」ボタンを押します。

##### 短縮形
手動による履歴の応用として、「短縮形」登録があります。これは、
```
短縮形文字列 | 本体文字列
```
という形式で履歴登録しておくと、選択された際に「本体文字列」だけが出力されて入力した短縮形を
置換する、というものです。

たとえば、
```
DKRED|<span style="color: darkred">
```
という登録をしておいて、漢直モードで「DKR」と打つと上記候補が表示されるので、それを選択すると
`DKR` が `<span style="color: darkred">` で置換される、というわけです。

### 交ぜ書き変換
KanchokuWS では後置式の交ぜ書き変換をサポートしています。
直前の出力文字列から、
「設定」-「履歴・交ぜ書き」-「読み入力の最大長」で設定した文字数までを読みとして取得し、
それにマッチする交ぜ書きエントリを検索して候補を表示します。

#### 動的交ぜ書き
ひらがなの読みとその変換形だけ用意すれば、
交ぜ書き入力された文字列にマッチするエントリをシステムのほうで自動で検索します。
たとえば、
```
かそうけんばん /仮想鍵盤/
```
という登録があったとしましょう。ここで、「か想けん盤」と打ってから「fj」(T-Code/106.key;デフォルト)と打つと、
交ぜ書き変換が呼び出され、「仮想鍵盤」に変換されます。
これは、ひらがな部分を「か.\*けん.\*」、漢字部分を「.\*想.\*盤」という正規表現に変換して、
読みと変換形の両方ともマッチするエントリを検索するという仕組みで実現しています。

後置式の特徴として、下図では、それ以外の候補も表示されています。

![Mazegaki Vkb](image/mazegaki_vkb.png)

長い読みにマッチするものが優先候補として表示されます。
背景色が緑色のものは Enter キーを押すことでも選択できます。

動的交ぜ書き変換の仕組みによる副産物として、ワイルドカード(? と *)を使った交ぜ書きも可能です。
'?' は正規表現の '.' に、'\*' は同じく '.\*' に置換した上で上記の仕組みで検索が行われます。
たとえば「ア\*国」で交ぜ書き変換すると「アメリカ合衆国」が出力されます
(kwmaze.ipa.dic を使用している場合)。

下記ツイートに動的交ぜ書き変換の様子を動画で投稿していますので、興味のある方は参照ください。<br/>
https://twitter.com/kanchokker/status/1403533755701026826 <br/>
https://twitter.com/kanchokker/status/1411910765976506373


#### 活用型

読みの末尾に「/{基本形の語尾}」を付加することで、ある程度活用型を考慮した変換ができるようになります。
サポートしている活用型は以下のとおりです。変換形のほうは語尾を除いた形で登録します。

|語尾|活用型|登録例
|-|-|-|
|く|カ行五段|か/く /書/|
|ぐ|ガ行五段|こ/ぐ /漕/|
|す|サ行五段|はな/す /話/|
|つ|タ行五段|た/つ /立/|
|ぬ|ナ行五段|し/ぬ /死/|
|ぶ|バ行五段|と/ぶ /飛/|
|む|マ行五段|う/む /生/|
|る|ラ行五段|ふ/る /振/|
|る:5|ラ行五段|かえ/る:5 /帰/|
|う|ワ行五段|あ/う /会/|
|る|一段|み/る /見/|
|る:1|一段|かえ/る:1 /変え/|
|する|サ変|かいはつ/する /開発/|
|ずる|ザ変|しん/ずる /信/|
|い|形容詞|うつくし/い /美し/|
|な|形容動詞ナ型|しずか/な /静か/|
|の|形容動詞ノ型|ほんとう/の /本当/|

イ段、エ段に後接する「る」については、ある程度システムのほうで一段活用か五段活用かを
推定しますが、推定が間違う場合ものあるので、そのような場合は、語尾の後ろに「:1」や
「:5」を付加して「一段」「五段」であることを明示します。

活用する語については、交ぜ書き変換の際に語幹の後に何文字まで語尾を許容するかを設定する
ことができます。これをたとえば「2文字」に設定していると、「あった」は「会った」に
変換できますが、「あわない」は「会わない」に変換できません。ただしこの語尾長をあまり長くすると
誤変換も増えるので、適当な長さに調節してください。

#### 交ぜ書き辞書
交ぜ書き辞書として、以下のものを用意してあります。

|辞書名|内容|
|-|-|
|kwmaze.slim.dic|従来の交ぜ書き辞書から非活用語を抽出したもの|
|kwmaze.inflex.dic|従来の交ぜ書き辞書から活用語を抽出したもの|
|kwmaze.ipa.dic|mecab-ipadic から交ぜ書き辞書を構築したもの|
|kwmaze.wiki.dic|Wikipedia のタイトルから交ぜ書き辞書を構築したもの|

ただし、 kwmaze.wiki.dic は、インストール直後は kwmaze.wiki.txt というファイル名になっており、
そのままでは読み込まれません。この辞書はかなり大きいので、メモリを400MB程度使います。
また、起動時の読み込み時間が長くなり、交ぜ書き変換にも時間がかかる場合があります。
そのあたりを考慮した上で、利用する場合は拡張子 `.txt` を `.dic` に変更してください。

#### 辞書登録
既存辞書に存在しない読みと変換形を登録することができます。
登録した内容は、 kwmaze.user.dic (デフォルトの場合)に保存されます。
（なお、ユーザ辞書には、優先順位変更のためにシステムが自動登録するエントリも含まれます）

登録方法には下記の2通りがあります。

- 仮想鍵盤の上部ミニバッファに文字列をコピペする
- 「設定」-「辞書登録」-「交ぜ書き登録」で登録する

形式は、
```
読み <空白> /変換形/ ...
```
です。
たとえば「とうきょうとっきょきょかきょく /東京特許許可局/」を登録するには、
この文字列をコピーして仮想鍵盤のミニバッファにペーストするか、
あるいは「交ぜ書き登録」のところで直接入力して「登録」ボタンを押します。

同一読みに対して複数の変換形がある場合は、それを「/」で区切って一行で記述することができます。
あるいは、1行1変化形で、同じ読みのエントリを複数回に分けて登録してもかまいません。

辞書登録については、設定ダイアログの「辞書登録」-「交ぜ書き登録」
のテキストボックスに付与したツールチップも参照ください。

### 連想入力
連想入力とは、ある一文字を別の一文字に変換するマッピング機能です。
マッピングは以下の4通りで提供されます。

1. 部首合成辞書から取得した部品<br/>
    例：恋 ⇒ 亦心
2. 自身および部品のうちの頻度が低いほうから合成可能な文字群<br/>
    例：恋 ⇒ 変奕弯・・・
3. 部首合成の実行<br/>
    例：「恋+糸 ⇒ 戀」を実行すると「恋 ⇒ 戀」が追加
4. 利用者が定義<br/>
    例： 「恋=愛」という辞書定義を記述する

上記のうち、利用者が明示的にかかわるのは 3 と 4 です。
3、4を実行の上、「恋」入力後に「\\\_」(T-Code/106.key;デフォルト)を押して、連想入力機能を呼び出すと、
下図のような縦列鍵盤が表示されます。

![Assoc](image/assoc.png)

列位置に対応するキーを押すと文字が選択されて元の文字を置換します。<br/>
下図は元祖漢直窓の[ドキュメント](https://github.com/kanchoku/kw/tree/master/doc) から引用した
106キーボードの仮想鍵盤ですが、上図の「愛」が「1」の列に、「蛮」が「0」の列に対応しています。

![keys_106_vkb](image/keys_106_vkb.png)

#### 連想直接入力
上記の縦列鍵盤を表示せずに直接第1候補で置換する機能です。
デフォルトでは「:*」(T-Code/106.key)に割り当てられています。

続けて同機能を呼び出すと第2候補に置換されます。
さらに同機能を呼び出すと、上記縦列鍵盤が表示されて、候補選択モードに移行します。

#### 辞書登録
部首合成辞書から導出できない連想文字を登録することができます。
登録した内容は、 kwassoc.txt (デフォルトの場合)に保存されます。
（なお、当辞書には、システムが自動登録したエントリも含まれます）

登録方法には下記の2通りがあります。

- 仮想鍵盤の上部ミニバッファに文字列をコピペする
- 「設定」-「辞書登録」-「部首連想登録」で登録する

形式は、
```
文字=連想文字列
```
です。
たとえば「恋=愛」を登録するには、
この文字列をコピーして仮想鍵盤のミニバッファにペーストするか、
あるいは「交ぜ書き登録」のところで直接入力して「登録」ボタンを押します。

辞書登録については、設定ダイアログの「辞書登録」-「部首連想登録」
のテキストボックスに付与したツールチップも参照ください。


### 部首合成
部首合成とは、2つの文字から別の1つの文字へのマッピングの集まりです。
このマッピングを利用して、
- 2つの文字の足し算(合成)
- 文字を合成する部品の一方を取り出す(引き算)

といった演算を繰り返して、解が得られたら、それを出力する、という機能です。

KanchokuWS の部首合成は、以下のような特徴・機能をもっています。

- 山辺アルゴリズムの標準採用
- 部品としての使用頻度の低い文字を優先
- 同一文字に対する複数の合成定義が可能
  - 先頭のものは合成と分解の両方のステップに用いられる
  - 残りは合成ステップだけで用いられる
- 各ステップごとに逆順演算を適用
- 部品から合成可能な文字を用いた足し算(再帰足し算)

#### 計算順序
1. 本体文字同士の足し算(合成)
1. 同逆順
1. 等価文字を使って足し算(合成)
1. 同逆順
1. 等価文字同士で足し算(合成)
1. 同逆順

ここまでで合成文字が見つからなければ、本体文字を構成する部品を使います。

7. 引き算 (たとえば 村－木 ⇒ (木＋寸)－木 ⇒ 寸)
1. 同逆順
1. YAMANOBE_ADD(たとえば 村＋木 ⇒ 木＋寸＋木 ⇒ (木＋木)＋寸 ⇒ 林＋寸 ⇒ 欝)
1. 同逆順
1. YAMANOBE_SUBTRACT(たとえば 準－シ ⇒ (淮＋十)－シ ⇒ (淮－シ)＋十 ⇒ 隹＋十 ⇒ 隼)
1. 同逆順
1. 一方が部品による足し算
1. 同逆順
1. YAMANOBE_ADD(Bが部品)
1. 同逆順(Aが部品)
1. 両方が部品による足し算
1. 同逆順
1. 部品による引き算
1. 同逆順
1. YAMANOBE_SUBTRACT (Bが部品)
1. 同逆順(Aが部品)
1. 再帰足し算
1. 同逆順

#### YAMANOBEアルゴリズム
YAMANOBEアルゴリズムを端的に説明すれば「部品入れ替えによる足し算と引き算」ということになろうかと思います。

#### 部品としての使用頻度の低い文字を優先
たとえば「巧」を「工＋５」で、「朽」を「木＋５ｊで定義しているとき、従来のアルゴリズムでは
「木＋巧」⇒「木＋（工＋５）」⇒「木＋工」⇒「杠」となっていました。

KanchokuWS では、辞書読み込み時に部品としての使用頻度を計算しておき、
使用頻度の低いほうの部品を優先的に使うようにしています。

上の例では、「工」よりも「５」のほうが部品しての使用頻度が低いので、
「木＋（工＋５）」⇒「木＋５」⇒「朽」となります。


#### 各ステップごとに逆順演算を適用
元祖漢直窓では、まず2つの文字の順序を固定して一通りの計算を実行し、それでも解が得られなかった場合、
2つの文字の順序を入れ替えて再計算を行っていました。
 その場合、たとえば「甚＋土」は「（其＋ル）＋土」⇒「其＋土」⇒「基」が得られます。

一方、KanchokuWS のように各計算ステップごとに文字の順序入れ替え(逆順)も計算すると、
「甚＋土」は「甚＋土」⇒なし⇒「土＋甚」⇒「堪」が得られます。

どちらが良いかは一概には決められないかと思います。
元祖漢直窓の部首合成定義は、全計算ステップ終了後での逆順計算を念頭に作成されているという面もあります。
また、初期の頃のT-Code実装では、2つの文字の順序をあらかじめソートしておき、
文字順にかかわらず同じ解が出るようになっていたと記憶しています。
これについては tcode-ml で議論があり、作者も当時は「文字順序を保持した合成手順にすべき」
(つまり元祖漢直窓と同じ手順)という主張をしていました。

今回は、各ステップごとの逆順演算でしばらく使用してみて、利用者の方からの意見を聞いてみたいと思っています。

#### 再帰足し算(部品から合成可能な文字を用いた足し算)
元祖漢直窓では、「踏」の部首合成定義は「踏＝足＋水」になっていました。
標準 T-Code では「沓」が2ストロークで打てないので、苦肉の策としてそのような定義にしていたわけです。

でもこれだと「足 + 日」では「踏」が出せません。
そこで漢直窓Sでは、「部品から足し算で合成可能な文字も部品として使う」という方策を取り入れました。
つまり「踏＝足＋沓」という定義にしておいて、「足＋日」の組み合わせからは合成文字が得られなかった場合、
1. 「足」と「『日』から合成可能な文字」の組み合わせ
2. 「『足』から合成可能な文字」と「日」の組み合わせ

を試す、ということです。これだと「沓」は「日」から合成可能なので、
「足＋沓」から「踏」が得られるというわけです。

他にも「瞳」は従来から「瞳＝目＋童」になっていたわけですが、
そして標準 T-Code では「童」を2ストロークで打てないわけですが、
これからは「目＋立」または「目＋里」で「瞳」を合成できるわけです。

#### 合成の次候補出力
部首合成の実行直後に上述の「連想直接入力」を呼び出すと、次の合成候補出力になります。
たとえば、「糸」と「工」を合成すると、第1候補として「紅」が出力されますが、
ここで「:*」(T-Code/106.key;デフォルト)を押すと、次の合成候補である「縒」に置換されます。
候補が一巡すると最初の候補に戻ります。

#### 辞書登録
新しい部首合を登録することができます。
登録した内容は、 kwbushu.rev (デフォルトの場合)に保存されます。

設定ダイアログの「辞書登録」-「部首合成登録」で登録してください。
記述方法については、そこのテキストボックスに付与したツールチップを参照ください。

### 英数記号全角入力
「^~」(T-Code/106.key;デフォルト)を押すと全角変換モードに移行します。
当モードでの通常打鍵およびShift打鍵は、
「キー・文字マップファイル」に定義された文字を全角変換して出力します。

当モードから抜けるには、Esc または Ctrl-G を押してください。

他に、直後の1文字を全角変換する機能もあります。
「]}」(T-Code/106.key;デフォルト)を押してください。

### カタカナ入力
ひらがなからカタカナに変換して入力する機能として、以下の3通りをサポートしています。

- 入力したひらがなをその場でカタカナに変換して出力するモード
- 直前に入力したひらがな列を一括でカタカナに変換する後置変換
- Shiftキーを押しながら入力したひらがなをカタカナに変換して出力するモード

それぞれ、設定ダイアログの
- 「機能キー割当」-「前置呼び出し機能」-「カタカナ変換(モード)」
- 「機能キー割当」-「後置呼び出し機能」-「カタカナ変換(一括)」
- 「履歴・交ぜ書き・他」-「その他変換」-「Shift入力された平仮名をカタカナに変換する」

のツールチップを参照ください。

## 設定ダイアログ
タスクトレイに格納されたアイコン、仮想鍵盤、またはモード標識で右クリックすると
以下のようなメニューが表示されます。

![Menu1](image/menu1.png)

ここで「設定」をクリックすると設定ダイアログが開きます。

設定ダイアログの内容については、実際に使ってみれば分かとかと思うので、説明は省略します。
要所々々にツールチップを埋め込んであるので、それを参照ください。


## その他の機能
### カレットに追随する仮想鍵盤
おおむねアクティブウィンドウのカレット位置に追随します。

- CMDプロンプト
- Word や Excel などで、矢印キーでカレットを移動した直後
- アクティブウィンドウのどの入力コントールもフォーカスを持っていない

という場合は、カレット位置が取れなくなることがあります。
カレット位置が取れない場合は、アクティブウィンドウの右下位置に仮想鍵盤を移動させます。

### 打鍵ヘルプ
仮想鍵盤に表示される打鍵ヘルプには以下のものがあります。
- 第1打鍵時のヘルプ
- 第2打鍵以降を待っているときの打鍵位置の文字表示

後者は、元祖漢直窓から引き継いだ機能です。

前者には、さらに
- ストローク位置形式 ： 第1打鍵位置への文字表示
- ストロークキー形式 ： 文字に対する第1・第2打鍵を、英数モード時の文字並びで表示

の2通りが用意されています。

これには以下のようなものがあります。
これらは、仮想鍵盤表示時に Ctrl-T または Ctrl-Shift-T によってローテートすることができます。

|項目|方式|説明|
|-|-|-|
|追加文字|位置|オリジナルT-Code に対して追加された、最上段キーを打鍵に含み、左・左または右・右という運指となる文字の、代表文字を表示|
|文字集合|位置|利用者が指定した文字列に含まれる文字を第1打鍵位置に表示|
|ひらがな第1面|キー|ひらがな清音(あい～かき～わをん)のストロークキーを表示|
|ひらがな第2面|キー|ひらがな(半)濁音・促音・拗音(ぁぃ～がぎ～ゃゅょゎゐゑ)のストロークキーを表示|
|カタカナ第1面|キー|カタカナ清音(アイ～カキ～ワヲン)のストロークキーを表示|
|カタカナ第2面|キー|カタカナ(半)濁音・促音・拗音(ァィ～ガギ～ャュョヮヰヱヵヶ)のストロークキーを表示|

どのような第1打鍵ヘルプをどの順序で表示するかは、stroke-help.txt に記述します。
記述方法の具体例については、同ファイルを参照ください。

#### ミニバッファへのコピペによるヘルプ表示
1文字をミニバッファにコピペすると、
- その文字の打鍵ヘルプ
- あるいは、部首合成ヘルプ

を表示します。

### Ctrl-H や Ctrl-B,F,P,Nなどの特殊キーへの変換
これらの機能は、h や b,f,p,n などのキーを常時ホットキーとして横取りしているので、
お使いのアプリによってはキー入力ができなくなったりする場合があります。
その時は、いったん設定ダイアログの「Ctrl-キー変換」-「Ctrl修飾キー変換
(漢直/英字両モード)」をOFFにしてください。


## 今後の機能追加予定(順不同)

- ホットキー方式以外にグローバルフック方法もサポート
- 文字送出方法として、WM\_CHAR 以外に SendInput もサポートする
- 「ひらがな」キーなど、特殊キーへの機能割り当て
  - TUT-Code ユーザだと使い道あるかな？
- 辞書ファイルの上書き警告
  - 終了時または再起動時に、読み込み時のタイムスタンプと比較して辞書ファイルのそれが新しかったら警告する
- 縦列鍵盤で、優先順位の並べ替え
  - 現在は左手小指が最優先
  - デフォルト(例：左人、右人、左中、右中の順)も用意するが、設定で変えられるようにする
- 半角カタカナ変換
  - 直前に出力されたカタカナ文字列を半角カタカナで置換する
- 英数字入力モード
  - 英数字文字列も履歴検索の対象となる
- 打鍵ヘルプ
  - ローマ字入力に対して、その読みを持つ漢字・ひらがなを第1打鍵位置に表示する
  - 難打鍵文字(容易打鍵文字以外)を第1打鍵位置に表示する
  - モード標識表示時でも指定打鍵目では仮想鍵盤でヘルプ表示(TT-Codeの利用時を想定)
- ホットキー順に文字(列)の並んだ任意の文字(列)テーブルを呼び出せる
  - a→α、b→βとか、TUT互換の記号テーブルとか
  - 定義はファイルに用意しておく
  - 呼び出しキーの割り当てを変えることで、複数ファイルの同時利用可
- ミニバッファ編集
  - 出力先のエディタでの ← → によるカーソル移動や BS/DEL による文字削除に同期する
  - 同期がずれたら、End キーで末尾にアライン
  - カーソル位置での後置式変換が可能になる
- 漢索窓連携
  - Ctrl-^ とかで、現在選択されている文字列を漢索窓にコピペする


## 引用元
- 元祖漢直窓のソースコードを全般的に参考にさせてもらっています。
  - とくにキーボードファイルのパーザはかなりの部分をそのまま引用しています。
  - 各種アイコンについても元祖漢直窓のものを流用しています。
- C# によるプログラミングでは、[DOBON.NET]()のコードスニペットを多数利用させていただいています。
- その他、キーポイントとなる箇所にはソース中に参照したページのURLを記しています。
- 本稿中のいくつかの図は、元祖漢直窓の[ドキュメント](https://github.com/kanchoku/kw/tree/master/doc) から引用させていただいています。
- kwbushu.rev は元祖漢直窓の kwbushu.dic を改編したものです。
- kwmaze.{slim,inflex}.dic は元祖漢直窓の kwmaze.dic を改編したものです。
- kwmaze.ipa.dic は NAIST 松本研で作成された mecab-ipadic を改編したものです。
  - mecab-ipadic のライセンスについては COPYING.mecab-ipadic を参照ください。
- kwmaze.wiki.txt は、 Wikipedia 日本語版のタイトル部分に対して読みを付与したものです。
  - 全タイトルの半分くらいが採取されています。

## 利用条件と免責
- 本プログラムおよびソースコードの利用は無償かつ自由ですが、無保証です。
  - 利用に際しては、それに起因するいかなる損害についても作者に責を負わせないことに同意ください。
  - 部首合成モジュールは新たに作成しました。したがってGPLの適用はありません。
  - 新しく追加した部首合成アルゴリズムや動的交ぜ書き方法が他の漢直ツールにも普及してくれると嬉しいです。
- 辞書その他のデータの利用については、引用元のライセンスに従ってください。

