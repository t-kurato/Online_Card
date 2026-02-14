# 本ページのポートフォリオについて
本ページは、制作したスクリプトと一部のトランプを保管してあります。
<br>動作は「YouTube」でご確認ください。

# 1.制作した主な機能

**以下の内容は「Notion」と同一です。**

## 1.1オンライン接続

<img width="1920" height="1080" alt="2" src="https://github.com/user-attachments/assets/324164e3-3da9-437d-95c5-53d250669916" />

オフライン対応も考えて自動で接続しないよう制御しています。
また、意図しない入室者をパスワードで防止しています。

## 1.2ルーム作成と入室

![2](https://github.com/user-attachments/assets/dbb82445-fc3c-4902-abb1-e26f58b666f8)


複数の部屋が作られないように、ルーム作成もパスワードで管理しています。
ルームの作成を検知して、入室が可能になります。

## 1.3ゲーム選択

<img width="1920" height="1080" alt="3 room" src="https://github.com/user-attachments/assets/7e63c505-a1cc-4cfd-8ef2-e94ae92d0e09" />

複数のゲーム導入を目指しているためゲーム選択の場面を用意しました。
プレイヤー間でゲームにずれが生じないように、ゲーム選択はルーム作成者のみが可能としその操作は参加者にも反映されます。

## 1.4ルール設定

<img width="1920" height="1080" alt="4 choice" src="https://github.com/user-attachments/assets/43c45d18-4914-48b0-82bc-7cd1aa6397db" />

ゲームの選択後、同様にプレイヤー間で設定にずれが生じないように、ルーム作成者がルールを指定します。
スタートボタンを押すと、ゲームが開始します。

## 1.5カード配布

![3](https://github.com/user-attachments/assets/1b386449-ce0d-4953-a80f-343fe274a643)

ゲーム開始と同時に、各プレイヤーにカードが配布されます。
カードIDとプレイヤーIDを結びつけることで、ユーザー間でずれないようにしています。

## 1.6カードの選択と提出

<img width="1920" height="1080" alt="2_cardchoice" src="https://github.com/user-attachments/assets/23818f87-9e38-41f6-b1ad-8222e643bab8" />

カードをクリックすると選択され、カードの位置が上がった状態を維持します。
選択したカードは、Enterキーで場に出します。

## 1.7ダウト宣言

![3](https://github.com/user-attachments/assets/35af3c0c-a8dc-4570-9f06-ae0fd2129022)

カードを出したプレイヤー以外は、ダウトを宣言できます。
残り時間内に宣言があると、カードが表向きになり判定が行われます。
現在地と一致している場合は宣言者が、
一致していない場合は、出したプレイヤーがカードを回収します。

## 1.8勝利判定

<img width="1920" height="1080" alt="4_judge" src="https://github.com/user-attachments/assets/d6c2abae-f174-4a3c-8a3a-34c570982326" />

手札のカードをすべて出したプレイヤーは、ターン進行から除外されます。
除外の判定は、即座に手札が0枚になっているかで判断するのではなく、ダウト宣言のタイマー終了時に0枚のプレイヤーが対象です。

## 1.9結果表示

<img width="1920" height="1080" alt="7_last" src="https://github.com/user-attachments/assets/cbbf59c6-32e6-463b-9469-e1955bdc4b20" />

ゲームに残っているプレイヤーが一人になった場合、結果発表を行います。
順位とプレイヤー名を表示し、各プレイヤーが、ゲームを続けるか終了するか決定します。

## 1.10捨てカードの上限

<img width="1920" height="1080" alt="6_uuper" src="https://github.com/user-attachments/assets/1e30e1f9-1e4f-4b8d-bef6-8b71bb2eb14c" />

一度に場に出すことができるカードの枚数を設定しています。
上限を超える場合は、カードを選択することはできません。

# 2.謝辞
本プロジェクトは、2025年4月14日にt-kuratoが始めたプロジェクトである。
制作にあたりご提供いただいたすべての素材・サービスに感謝申し上げます。
- ゲームエンジン：Unity
- サーバー：Photon Engine
- アニメーション：DOTween
- ファイルロード：Unity Standalone File Browser
- フォント：Noto Sans JP
- UIデザインツール（トランプ）：Figma
- トランプイラスト：DALL·E 3
- スクリプト生成・バグ修正：Codex
