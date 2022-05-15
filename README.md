# [WIP] NSO-FriendStatus

Nintendo Switch OnlineのフレンドのステータスをPC上で確認することが可能なWinUI 3製アプリ

## 機能

- タスクトレイ常駐
- フレンドがオンラインになった際の通知  
  ![image](https://user-images.githubusercontent.com/3415240/168440877-c76a7256-a5f1-4be5-860d-31781335fecb.png)
- フレンドステータスの一覧表示  
  ![image](https://user-images.githubusercontent.com/3415240/168440862-a31e573e-53aa-48fd-bd60-8f81709b3c5a.png)

## セットアップ

ステータス情報の取得に[nxapi](https://github.com/samuelthomas2774/nxapi)コマンドを使用しています。  
事前にnxapiコマンドを[インストール](https://github.com/samuelthomas2774/nxapi#install)して[認証](https://github.com/samuelthomas2774/nxapi#login-to-the-nintendo-switch-online-app)を行ってください。

```
npm install --global nxapi
nxapi nso auth
```

配布可能な形式のパッケージは現在準備中です。

## 使い方

アプリケーションを起動するとタスクトレイに常駐します。  
起動後はフレンドのステータスを1分間隔で確認し、フレンドが新たにオンラインになった際に通知します。  
終了するにはタスクトレイのアイコンを右クリックして終了させてください。

## TODO

- [ ] 配布可能なパッケージの作成
  - もしくはUnpackedでも実行できるようにする

## License

MIT
