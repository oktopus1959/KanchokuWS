# KanchokuWS -- 漢直Win Spoiler -- インストールガイド
KanchokuWS は、[漢直Win（漢直窓）](https://github.com/kanchoku/kw)の使い勝手を受け継ぎ、
かつ、利用者を徹底的に甘やかすことを目標として新たに開発された、
Windows 用の漢字直接入力ツールです。
また、複数キーの同時打鍵もサポートしているので、
各種かな配列の実装プラットフォームとして利用することもできます。

本書は KanchokuWS のインストール、起動および終了について説明したものです。
やりたいことから調べるには[FAQ](blob/main/FAQ.md)を、
より詳細な利用方法については[利用者ガイド](blob/main/MANUAL.md)を参照ください。

## 目次

- [動作環境](#動作環境)
- [インストールとアンインストール](#インストールとアンインストール)
    - [インストール](#インストール)
    - [アンインストール](#アンインストール)
- [起動](#起動)
    - [起動時に警告が表示される場合](#起動時に警告が表示される場合)
    - [kw-uni.dll が起動できない場合](#kw-unidll-が起動できない場合)
    - [起動画面と設定ダイアログ](#起動画面と設定ダイアログ)
- [漢直モードに入る](#漢直モードに入る)
    - [漢直モードキーの変更](#漢直モードキーの変更)
- [漢直モードから出る](#漢直モードから出る)
- [終了](#終了)
- [利用条件と免責](#利用条件と免責)

## 動作環境
動作対象環境は以下のとおりです。

Windows 10 および .NET Framework 4.8

## インストールとアンインストール
### インストール
[Releaseページ](https://github.com/oktopus1959/KanchokuWS/releases/)
から最新の KanchokuWS-v*.zip をダウンロードして、適当なフォルダに展開してください。
以下のようなファイルが格納されています。以下、このフォルダを「**ルートフォルダ**」と呼称します。

![Root Folder](image/root-folder.png)

### アンインストール
上記のルートフォルダを削除します。なお、レジストリは使用していません。

## 起動
bin\KanchokuWS.exe を実行します。CMDプロンプトから実行してもよいですし、
ファイルエクスプローラからダブルクリックで実行してもかまいません。

![Bin Folder](image/bin-folder.png)

日常的に使用するのであれば、エクスプローラの右クリックメニューから
「スタートメニューにピン留めする」を実行して、
スタートメニューから起動できるようにしておくとよいでしょう。

### 起動時に警告が表示される場合
zipファイルをダウンロードして展開した直後には、以下のような警告が表示される場合があります。

![Warning1](image/warning1.png)

この場合は、「詳細情報」をクリックして、次の画面で「実行」をクリックしてください。

![Warning2](image/warning2.png)

### kw-uni.dll が起動できない場合
次のような画面が表示された場合は、kw-uni.dll (漢直デコーダ部)の実行に必要な環境が不足している可能性があります。

![Kw Uni Error](image/kw-uni-error.png)

この場合は、「vc_redist.x86 download」で検索をしてヒットする Microsoft のサイト
（2021年9月19日現在だと「[最新のサポートされる Visual C++ のダウンロード](https://support.microsoft.com/ja-jp/topic/%E6%9C%80%E6%96%B0%E3%81%AE%E3%82%B5%E3%83%9D%E3%83%BC%E3%83%88%E3%81%95%E3%82%8C%E3%82%8B-visual-c-%E3%81%AE%E3%83%80%E3%82%A6%E3%83%B3%E3%83%AD%E3%83%BC%E3%83%89-2647da03-1eea-4433-9aff-95f26a218cc0)」）
から、x86用の Visual C++ 再頒布可能パッケージ (vc_redist.x86.exe)をダウンロードして、
インストールしてください。
これは VC++ で作成されたプログラムを実行する際に必要となるもので、Microsoft社が提供しているものです。

![Vc Redist.X86](image/vc_redist.x86.png)

### 起動画面と設定ダイアログ
起動すると起動画面(スプラッシュウィンドウ)が表示されます。

![Splash](image/splash.png)

辞書ファイルなどの読み込みが完了すると「OK」「設定」というボタンが表示されるので、
初めての起動の場合は「設定」をクリックしてみてください。
設定ダイアログが開くので、各タブを一通り開いて見てください。
どんな機能があるか、おおよそ分かるかと思います。
（設定ダイアログは、タスクトレイのアイコンの右クリックメニューからも開くことができます）

![Settings1](image/settings1.png)

辞書ファイルを除き、ほとんどの設定項目および設定ファイルの変更は、
「設定」または「再読込」ボタンをクリックすることで反映されます。

なお、ほとんどの設定項目にはツールチップによる説明を付与してあります。
テキストボックス、ラジオボタン、チェックボックスなど、入力用コントロールに
マウスポインタを合わせてみてください。約30秒間、その項目に対するツールチップが表示されます。

右端の「情報」タブにはバージョン番号、ビルド日時などの情報が記載されていますので、
ツールの不具合などを作者に連絡する際は、それをお伝えください。

## 漢直モードに入る
以下の方法で漢直モードに入ることができます。
- タスクトレイに格納されたアイコン ![Kanmini0](image/kanmini0.png) をクリックする
- Ctrl-￥を押す (デフォルト状態の場合。設定ダイアログから変更可能)

漢直モードに入ると、アイコンは ![Kanmini1](image/kanmini1.png) に変化し、
さらに下のような仮想鍵盤が表示されます。

![Vkb1](image/vkb1.png)

### 漢直モードキーの変更
漢直モードに入るためのキーは、設定ダイアログの「基本設定」>「漢直モードトグル/OFFキー」で設定できます。
下図は、「変換」キー（Ctrlキーの修飾なし）を漢直モードのON/OFFキーとして設定した画面です。

![Kanchoku Mode Key](image/kanchoku-mode-key.png)

主なキーについてはあらかじめコンボボックスに用意してありますのでそこから選択してください。

## 漢直モードから出る
以下の方法で漢直モードから出ることができます。
- タスクトレイに格納されたアイコン ![Kanmini1](image/kanmini1.png) をクリックする
- 仮想鍵盤をクリックする
- Ctrl-￥を押す (デフォルト状態の場合。設定ダイアログから変更可能)

## ツールの終了
- タスクトレイに格納されている本体アイコン
- 仮想鍵盤

のいずれかで右クリックすると、以下のようなメニューが表示されます。

![Menu1](image/menu1.png)

終了する場合は「終了」をクリックしてください。

## 利用条件と免責
- 本プログラムおよびソースコードの利用は無償かつ自由ですが、無保証です。
  - 利用に際しては、それに起因するいかなる損害についても作者に責を負わせないことに同意ください。
  - 部首合成モジュールは新たに作成しました。したがってGPLの適用はありません。
  - 新しく追加した部首合成アルゴリズムや動的交ぜ書き方法が他の漢直ツールにも普及してくれると嬉しいです。
- 辞書その他のデータの利用については、引用元のライセンスに従ってください。

