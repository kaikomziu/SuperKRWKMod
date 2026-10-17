# SuperKRWKMod

[English](README.md) | **日本語**

![Bongo Cat Logo](SuperKRWKMod/Assets/BongoCat/SuperKRWKMod/logo.png)

> [Bongo Cat](#bongo-cat-ゲーム-) 用の「見た目だけ」の MOD です。ズーミー、カーソルを追う目、おやすみする猫、打鍵速度メーター、コンボ、宝箱のお知らせ、タイピングの記録。無料・安全・チートなし。

Shilo さんの [Bongo Cat Catnip Mod ↗](https://github.com/Shilo/bongo-cat-catnip-mod/) をもとにしています。

| 打鍵速度メーターと宝箱カウントダウン | 設定 → MOD |
|---|---|
| ![打鍵速度メーターと宝箱カウントダウン](docs/images/meter-and-countdown.png) | ![MOD 設定画面](docs/images/mods-page.png) |

## 目次

- [機能](#機能)
- [対応言語](#対応言語)
- [安全性](#安全性)
- [インストール](#インストール)
  - [動作の確認](#動作の確認)
  - [アンインストール](#アンインストール)
- [Bongo Cat ゲーム](#bongo-cat-ゲーム-)
- [開発者向け ↗](docs/DEVELOPER.md)（英語）
- [ライセンス ↗](LICENSE.md)

## 機能

どの機能も**見た目だけ**です。タップを数えるだけで、ゲームのカウンター・Steam の統計・アイテム・ドロップは一切変更しません。すべての機能は、ゲーム内の **設定 → MOD** でオン・オフできます。

- **ズーミー:** 1秒あたり `BounceKps` 回より速く打つと猫がピョコピョコ跳ねて揺れ、`RainbowKps` を超えると虹色に光ります。
- **打鍵速度メーター:** 猫の上に、今の打鍵速度（KPS = 1秒あたりのキー数）・この回の最高・タップ数を表示します。`F7` で表示切り替え。
- **目でカーソルを追う:** 黒目がマウスの方向を見ます。目は自動で見つけるので、スキンを着せていても動きます。
- **おやすみモード:** `IdleSeconds` 秒キー入力がないと、猫がゆっくり呼吸し、目を閉じて「Zzz」を浮かべます。キーを押すと起きます。深夜（初期設定は 23時〜5時）は `NightIdleSeconds` 秒で早めに寝ます。
- **コンボ表示:** 手を止めずに打ち続けると、猫の横に「×128 COMBO」が弾みます。50 / 100 / 200 で色が変わります。
- **宝箱のお知らせ:** 宝箱が出ていて、タップ数が足りたら、猫が吹き出しで「宝箱が買えるよ！」と知らせ、ショップのボタンが揺れます（`ReminderMinutes` 分ごとに再通知）。コンボの横の小さなカウントダウンに、次の宝箱までの時間や、あと何タップかを表示します。宝箱を開けるのは自分です。MOD はゲームの状態を読むだけで、買ったり開けたりは一切しません。
- **記録:** 今日のタップ数・打っていた時間・最高 KPS・最長コンボ、これまでの最高記録、最近7日の記録を、**設定 → 記録** のページで見られます。`SuperKRWKMod\records.csv` に保存され、数えているのは MOD だけです。
- **アプリ別ルール:** 指定したアプリが起動している間だけ機能をオフにし、閉じたら自動で戻します（例: 音ゲー中は虹色をオフ）。ゲーム内の **設定 → MOD → アプリ別ルール →「＋ 起動中のアプリから追加」** でアプリを選び、オフにする機能にチェックを入れます。アプリ名を PC の中で確認するだけです。
- **見やすく、動かせる文字:** メーターとカウントダウンはゲームのカウンターと同じ枠に入り、それ以外の文字には縁取りが付きます。文字の大きさ・縁取りの色と太さは MOD 画面で変更でき、**「文字の位置を変える（ドラッグ）」** で好きな場所に動かせます。
- **設定ファイル:** 初回起動時に、ゲームフォルダに `SuperKRWKMod\superkrwk.ini` が作られます。書き換えたら、Bongo Cat をクリックしてから `F9` で読み込み直せます。

ホットキーは、Bongo Cat のウィンドウがアクティブなときだけ効きます（先に猫をクリックしてください）。

## 対応言語

MOD の文字は、ゲームの言語設定に合わせて切り替わります。対応言語: 日本語、英語、中国語（簡体字・繁体字）、韓国語、スペイン語、フランス語、ドイツ語、ポルトガル語、ロシア語、イタリア語、ポーランド語、トルコ語、ウクライナ語、オランダ語、チェコ語、デンマーク語、フィンランド語、ギリシャ語、ハンガリー語、ノルウェー語、ルーマニア語、スウェーデン語。ヘブライ語は今のところ英語で表示されます。

言語を固定したいときは、`superkrwk.ini` の `[General]` に `Language=ja` のように書きます（`auto` でゲームに合わせる）。翻訳の修正は [`Translations.cs`](SuperKRWKMod/ModList/Translations.cs) へのプルリクエストで歓迎します。

## 安全性

- **ネット通信なし。** MOD はインターネットに一切接続しません。
- **見た目だけ。** タップ数・Steam の統計・アイテム・ドロップは変更せず、宝箱を買ったり開けたりもしません。マルチプレイでは、他のプレイヤーからはふつうの猫に見えます。MOD は何も送信しません。
- **ファイルはゲームフォルダの中だけ。** 書き込むのは `SuperKRWKMod\superkrwk.ini` と `records.csv` だけです。
- **アプリ別ルール**は、機能を切り替えるために PC で起動中のアプリ名を確認します。アプリ名は、自分で追加したルール以外には保存されず、PC の外にも出ません。
- **オープンソース。** コードはすべてこのリポジトリにあります。同梱の `winhttp.dll` は公式の [UnityDoorstop v4.4.1](https://github.com/NeighTools/UnityDoorstop/releases/tag/v4.4.1)（Windows x64 verbose 版、SHA-256 `1018c7eb5f03ee359d1479d3648b16224794119c43092673fef2ff85dd3eb46d`）そのままです。
- ウイルス対策ソフトによっては `winhttp.dll` に警告が出ることがあります。ゲームに MOD を読み込ませる仕組みが、他の DLL 注入と似て見えるためです。ファイルは上記の公式のものから変更していません。

## インストール

1. [Releases ↗](https://github.com/kaikomziu/SuperKRWKMod/releases) ページを開く
2. 最新の `SuperKRWKMod.zip` をダウンロードする（`build.bat` で自分でビルドすることもできます。[開発者向け ↗](docs/DEVELOPER.md) を参照）
3. zip を展開する

**自動インストール（おすすめ）**

- 展開したフォルダの `install.bat` をダブルクリック
- Bongo Cat のインストール先は自動で見つけます
- Bongo Cat が起動中なら、自動で閉じてからインストールします
- ゲームフォルダにコピーされるのは MOD のファイル（`winhttp.dll`、`doorstop_config.ini`、`SuperKRWKMod\` フォルダ）だけです
- 完了すると、ゲームフォルダがエクスプローラーで開きます

**手動インストール**

- `BongoCat` フォルダの中身を、Bongo Cat のインストール先にすべてコピーします
  - **通常の場所:** `C:\Program Files (x86)\Steam\steamapps\common\BongoCat\`
  - コピーするもの:
    - `winhttp.dll`（直下）
    - `doorstop_config.ini`（直下）
    - `SuperKRWKMod\` フォルダ（`SuperKRWKMod.dll` などが入っています）

ゲームフォルダは次のようになります:
```
BongoCat/
├── BongoCat.exe
├── winhttp.dll          ← MOD のファイル
├── doorstop_config.ini  ← MOD のファイル
├── SuperKRWKMod/        ← MOD のフォルダ
│   ├── SuperKRWKMod.dll
│   └── ...（MOD のファイル）
└── ...（ゲームのファイル）
```

4. いつもどおり Steam から Bongo Cat を起動します

### 動作の確認

起動後、`%USERPROFILE%\AppData\LocalLow\Irox Games\BongoCat\Player.log` に `[SuperKRWKMod] SuperKRWKMod | Features ready` と書かれていれば動いています。ゲーム内の **設定** に **MOD** と **記録** のカテゴリが増えていることでも確認できます。

### アンインストール

**自動アンインストール**

- 展開した MOD のフォルダの `uninstall.bat` をダブルクリック
- Bongo Cat のインストール先は自動で見つけます
- Bongo Cat が起動中なら、自動で閉じてから削除します
- 削除されるのは MOD のファイル（`winhttp.dll`、`doorstop_config.ini`、`SuperKRWKMod\` フォルダ）だけです
- 完了すると、ゲームフォルダがエクスプローラーで開きます

**手動アンインストール**

- Bongo Cat のインストール先から、MOD のファイルを削除します
  - **通常の場所:** `C:\Program Files (x86)\Steam\steamapps\common\BongoCat\`
  - 削除するもの:
    - `winhttp.dll`（直下）
    - `doorstop_config.ini`（直下）
    - `SuperKRWKMod\` フォルダ

## [Bongo Cat ゲーム →](https://store.steampowered.com/app/3419430/Bongo_Cat/)

[![Steam](https://shared.fastly.steamstatic.com/store_item_assets/steam/apps/3419430/2e6c93c020fe13d09d8120bb9164ed7cb58f00c5/header.jpg?t=1759317272)](https://store.steampowered.com/app/3419430/Bongo_Cat/)

**パブリッシャー:** [Irox Games ↗](https://store.steampowered.com/publisher/irox?snr=1_5_9__2000)  
**開発:** [Marcel Zurawka ↗](https://store.steampowered.com/developer/irox?snr=1_5_9__2000), [Julius Krüger ↗](https://store.steampowered.com/developer/irox?snr=1_5_9__2000)

*Bongo Cat © Irox Games, Marcel Zurawka, Julius Krüger. Bongo Cat は各権利者の商標です。この MOD は Bongo Cat の制作者とは関係がなく、公認・後援を受けたものではありません。*
